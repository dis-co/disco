using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.Graph;
using VVVV.Core.Logging;

using Iris.Core.Types;
using Iris.Core.Events;
using Iris.Core.Logging;

using Iris.Nodes.Types;

namespace Iris.Nodes.Graph
{
    using Address = OSCAddress;

    using NodePatch     = Tuple<string, string>;
    using GraphPatchJob = Tuple<int, int, string, string>;

    public enum WriteType
    {
        Write, Reset
    }

    public struct WriteJob
    {
        public int       TargetFrame;
        public WriteType Type;
        public IPin2     Pin;
        public bool      NeedsReset;
        public string    Payload;
        public string    ResetPayload;
    }

    public class VVVVGraph : IDisposable
    {
        private const string TAG = "VVVVGraph";

        public event EventHandler<PinEventArgs> PinAdded;
        public event EventHandler<PinEventArgs> PinRemoved;
        public event EventHandler<PinEventArgs> PinChanged;

        public event EventHandler<PatchEventArgs> PatchAdded;
        public event EventHandler<PatchEventArgs> PatchRemoved;
        public event EventHandler<PatchEventArgs> PatchChanged;

        private IIrisPatches Patches { get; set; }

        public int  FrameCount = 0;
        public bool Debug { get; set; }

        public IPluginHost V1Host { get; set; }
        public IHDEHost    V2Host { get; set; }
        public ILogger     Logger { get; set; }

        private Dictionary<Address, CancellationTokenSource> PinEventSubscriptions =
            new Dictionary<Address, CancellationTokenSource>();

        //   ____ ____      _    ____  _   _
        //  / ___|  _ \    / \  |  _ \| | | |
        // | |  _| |_) |  / _ \ | |_) | |_| |
        // | |_| |  _ <  / ___ \|  __/|  _  |
        //  \____|_| \_\/_/   \_\_|   |_| |_|updates
        //
        public ConcurrentQueue<WriteJob> WriteQueue =
            new ConcurrentQueue<WriteJob> ();

        //  ____   _  _____ ____ _   _
        // |  _ \ / \|_   _/ ___| | | |
        // | |_) / _ \ | || |   | |_| |
        // |  __/ ___ \| || |___|  _  |
        // |_| /_/   \_\_| \____|_| |_|ing the graph
        //
        private Dictionary<int, NodePatch> NodePatches =
            new Dictionary<int, NodePatch>();

        public ConcurrentQueue<GraphPatchJob> GraphPatchJobQueue =
            new ConcurrentQueue<GraphPatchJob> ();

        #region Constructor
        public VVVVGraph ()
        {
            Patches = new Patches();
        }
        #endregion

        #region Destructor
        public void Dispose ()
        {
            UnRegister();

            PinEventSubscriptions.ToList().ForEach(kv => {
                    kv.Value.Cancel();
                });

            Patches.PatchAdded -= OnPatchAdded;
            Patches.PatchRemoved -= OnPatchRemoved;
            Patches.Dispose();
        }
        #endregion

        /// <summary>
        /// Add VVVV host, a dispatcher created on the VVVV thread and logger to this
        /// instance of broker. Register callbacks to observe addition and removal
        /// of exposed nodes, and process exposed already present in the current VVVV
        /// context.
        /// </summary>
        /// <param name="host">VVVV IHDEHost reference</param>
        /// <param name="dispatcher">Dispatcher for doing updates on VVVV thread</param>
        /// <param name="logger">Logger handle</param>
        public void Register()
        {
            string nodepath = String.Empty;
            V1Host.GetNodePath(false, out nodepath);

            var parent = V2Host.GetNodeFromPath(nodepath).Parent;
            Patches.Create(
                new NodePath(parent.GetNodePath(false)),
                new Name(parent.Name));

            V2Host.ExposedNodeService.Nodes.ToList().ForEach(CreatePin);

            // CALLBACKS/EVENTS
            Patches.PatchAdded   += OnPatchAdded;
            Patches.PatchRemoved += OnPatchRemoved;

            V2Host.ExposedNodeService.NodeAdded += OnNodeAdded;
            V2Host.ExposedNodeService.NodeRemoved += OnNodeRemoved;
        }

        public void UnRegister ()
        {
            // remove global node callbacks
            V2Host.ExposedNodeService.NodeAdded -= OnNodeAdded;
            V2Host.ExposedNodeService.NodeRemoved -= OnNodeRemoved;
        }

        /// <summary>
        /// When a node is added (exposed) in VVVV, this callback adds it to the context,
        /// and registers all important event handlers on the node.
        /// </summary>
        /// <param name="node">INode2 to add to process</param>
        public void OnNodeAdded (INode2 node)
        {
            CreatePin (node);
        }

        /// <Summary>
        /// When a node is removed (un-exposed) in VVVV, this callback removes
        /// all registered event handlers, and finally the corresponding PinData object
        /// from the context.
        /// </summary>
        /// <param name="node">INode2 that was removed</param>
        public void OnNodeRemoved (INode2 node)
        {
            DeletePin (node);
        }

        /// <summary>
        /// Get list of patches;
        /// </summary>
        /// <returns></returns>
        public IIrisPatches GetPatches ()
        {
            return Patches;
        }

        /// <summary>
        /// Get a patch by ID
        /// </summary>
        /// <param name="id">ID of Patch to get</param>
        /// <returns>Patch</returns>
        public IIrisPatch GetPatch (IrisId id)
        {
            var patch = Patches.Find(id);
            if (patch == null)
                throw new IrisException (IrisException.Type.ObjectNotFound);
            return patch;
        }

        /// <summary>
        /// Update a Patch
        /// </summary>
        /// <param name="_sid">Client session ID</param>
        /// <param name="id">ID of Patch to update</param>
        /// <param name="newPatch">Patch attributes</param>
        /// <returns>UPdated Patch</returns>
        public IIrisPatch UpdatePatch (IrisId id, IIrisPatch newPatch)
        {
            var patch = Patches.Find (id);
            if (patch == null)
                throw new IrisException (IrisException.Type.ObjectNotFound);
            patch.Update (newPatch);
            return patch;
        }

        /// <summary>
        /// Get all Pins for a given Patch ID (i.e. its nodepath)
        /// </summary>
        /// <param name="nodepath">Node path of Patch in Graph</param>
        /// <returns>Pins in Patch</returns>
        public IIrisPins GetPins (NodePath nodepath)
        {
            var patch = Patches.Find (nodepath);
            if (patch == null)
                throw new IrisException (IrisException.Type.ObjectNotFound);
            return patch.GetPins();
        }

        /// <summary>
        /// Get a PinData by its Node path
        /// </summary>
        /// <param name="nodepath">Node path of PinData</param>
        /// <returns>PinData</returns>
        public IIrisPin GetPin (NodePath nodepath)
        {
            var pin = Patches.FindPin (nodepath);
            if (pin == null)
                throw new IrisException (IrisException.Type.ObjectNotFound);
            return pin;
        }

        public IIrisPin UpdatePin (IIrisPin newpin)
        {
            if(Debug)
            {
                Logger.Log(LogType.Debug, "--------------------- UpdatePin -----------------------------" );
            }
            IIrisPin pin = null;
            Patches.FindPins(newpin.GetId())
                .ForEach(p => {
                        p.Update(newpin);
                        MkWriteJob((Pin)p);
                    });

            return pin;
        }

        public void CallCue(Cue cue)
        {
            if(Debug)
            {
                Logger.Log(LogType.Debug, "--------------------- CallCue -----------------------------" );
            }
            cue.Values.ForEach(value => {
                    Patches
                    .FindPins(value.Target)
                    .ForEach(pin => {
                            pin.Update(value.Values);
                            MkWriteJob((Pin)pin);
                        });
                });
        }

        private void MkWriteJob(Pin pin)
        {
            WriteQueue.Enqueue(new WriteJob {
                    Type         = WriteType.Write,
                    TargetFrame  = -1,
                    NeedsReset   = pin.IsBang(),
                    Pin          = pin.GetIPin(),
                    Payload      = pin.GetValues().ToSpread(),
                    ResetPayload = pin.IsBang() ? pin.GetResetSpread() : String.Empty
                });
        }

        /// <summary>
        /// Parse an IOBox of type INode2 and add it to its corresponding Patch object.
        /// In case the Patch does not exist, construct it.
        /// </summary>
        /// <param name="node">INode2 IOBox to parse and add</param>
        public void CreatePin (INode2 node)
        {
            try
            {
                var pin = Pin.FromNode(node, this);
                var cts = new CancellationTokenSource();

                LogEntry.Info(TAG, "Created pin for " + pin.GetBehavior() + " " + pin.GetName());

                pin.GetObservable().Subscribe(args =>
                {
                    if (PinChanged != null) PinChanged(null, args);
                }, cts.Token);

                PinEventSubscriptions[pin.GetAddress()] = cts;

                var Patch = Patches.Find(new NodePath(node.Parent.GetNodePath(false)));
                if (Patch == null)
                {
                    Patch = Patches.Create(
                        new NodePath(node.Parent.GetNodePath(false)),
                        new Name(node.Parent.Name));
                    Patch.Subscribe(patch =>
                    {
                        if (PatchChanged != null)
                            PatchChanged(null, new PatchEventArgs(null, patch));
                    });
                }

                Patch.AddPin(pin);

                if (PinAdded != null)
                    PinAdded(null, new PinEventArgs(UpdateDirection.Graph, pin));
            }
            catch(Exception ex)
            {
                LogEntry.Fatal(TAG, "Exception in CreatePin: " + ex.Message);
                LogEntry.Fatal(TAG, ex.StackTrace);
            }
        }

        /// <summary>
        /// Remove the corresponding PinData object from its Patch.
        /// </summary>
        /// <param name="node">INode2 in graph to be removed</param>
        public void DeletePin (INode2 node)
        {
            var patch = Patches.Find (new NodePath(node.Parent.GetNodePath (false)));
            if (patch == null) return;

            var pin = patch.FindPin(new NodePath(node.GetNodePath (false)));

            if(PinRemoved != null)
                PinRemoved(this, new PinEventArgs(UpdateDirection.Graph, pin));

            // Cancel & remove event subscription
            if(PinEventSubscriptions.ContainsKey(pin.GetAddress()))
            {
                PinEventSubscriptions[pin.GetAddress()].Cancel();
                PinEventSubscriptions.Remove(pin.GetAddress());
            }

            patch.RemovePin (pin);

            if (patch.GetPins().Size() == 0) Patches.Remove (patch);
        }

        /// <summary>
        /// Get the PinData object corresponding to the passed INode2.
        /// </summary>
        /// <param name="node">INode2 to find PinData for</param>
        /// <returns>PinData</returns>
        public IIrisPin GetPin (INode2 node)
        {
            var patch = Patches.Find(new NodePath(node.Parent.GetNodePath (false)));
            if(patch == null) return null;
            return patch.GetPins().Find(new NodePath(node.GetNodePath(false)));
        }

        private void ProcessGraphPatches()
        {
            if(GraphPatchJobQueue.Count == 0) return;

            GraphPatchJob job;
            while(GraphPatchJobQueue.TryPeek(out job))
            {
                if(job.Item1 < FrameCount)
                {
                    GraphPatchJobQueue.TryDequeue(out job);

                    if(NodePatches.ContainsKey(job.Item2))
                    {
                        var tmp = NodePatches[job.Item2] + job.Item4;
                        NodePatches[job.Item2] = Tuple.Create(job.Item3, tmp);
                    }
                    else
                    {
                        NodePatches[job.Item2] =
                            Tuple.Create(job.Item3, job.Item4);
                    }
                }
                else break;
            }

            if(NodePatches.Count > 0)
            {
                NodePatches.ToList().ForEach(item  => {
                        var patch = NodeParser.FormatPatchTagSnippet(
                            item.Key,
                            item.Value.Item2);

                        V2Host.SendXMLSnippet(item.Value.Item1, patch, false);
                    });
                NodePatches.Clear();
            }
        }

        private void ProcessGraphWrites()
        {
            if(WriteQueue.Count == 0) return;

            int count = WriteQueue.Count;
            int processed = 0;

            WriteJob job;

            while(WriteQueue.TryPeek(out job) && processed < count)
            {
                processed += 1;

                /// <summary>
                ///   this one is timed, and not ready to be written to graph
                /// </summary>
                if(job.Type == WriteType.Reset && job.TargetFrame > FrameCount)
                {
                    continue;
                }

                if(WriteQueue.TryDequeue (out job))
                {
                    switch(job.Type)
                    {
                        case WriteType.Write:
                            if(Debug)
                            {
                                Logger.Log(LogType.Debug, "WRITE - target: " + job.TargetFrame + " frame: " + FrameCount + " spread " + job.Payload);
                            }

                            // WRITE TO GRAPH
                            job.Pin.Spread = job.Payload;

                            if(job.NeedsReset)
                            {
                                job.Type = WriteType.Reset;
                                job.TargetFrame = FrameCount + 1;
                                job.NeedsReset = false;
                                WriteQueue.Enqueue(job);
                            }
                            break; 
                        case WriteType.Reset:
                            if(Debug)
                            {
                                Logger.Log(LogType.Debug, "RESET - target: " + job.TargetFrame + " frame: " + FrameCount + " spread " + job.ResetPayload);
                            }

                            // WRITE TO GRAPH
                            job.Pin.Spread = job.ResetPayload;
                            break; 
                    }
                }
            }
        }

        public void NextTick()
        {
            FrameCount++;
            ProcessGraphPatches();
            ProcessGraphWrites();
        }

        #region Data Event Delegates
        public void OnPatchAdded(object sender, PatchEventArgs args)
        {
            if(PatchAdded != null)
                PatchAdded(sender, args);
        }

        public void OnPatchRemoved(object sender, PatchEventArgs args)
        {
            if(PatchRemoved != null)
                PatchRemoved(sender, args);
        }
        #endregion
    }
}
