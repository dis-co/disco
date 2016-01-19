using System;
using System.Threading;
using System.Linq;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Security.Principal;
using System.Security.AccessControl;

using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

using Iris.Core.Types;
using Newtonsoft.Json;

namespace VVVV.Nodes
{
    #region PluginInfo
    [PluginInfo(Name = "SetCueHosts", Category = "Iris", Help = "Set the Hosts field when Cues are fired from Iris web interface", Tags = "", AutoEvaluate = true)]
    #endregion PluginInfo

    public class SetCueHosts : IPluginEvaluate, IDisposable
    {
        [Import()]
        public IHDEHost V2Host;

        [Import()]
        public ILogger Logger;

        [Input("Hosts")]
        ISpread<string> InHosts;

        [Input("Enabled", IsSingle = true, DefaultValue = 0)]
        ISpread<bool> InEnabled;

        private int BufSize = 5000;
        private bool Ready = false;
        private MemoryMappedFile File;
        private MemoryMappedViewAccessor Accessor;

        public SetCueHosts ()
        {
        }

        public void Dispose()
        {
            Disable();
        }

        private void Enable()
        {
            if(Ready) return; // already ready 
            
            int size_t = sizeof(char) * BufSize;

            Logger.Log(LogType.Debug, "init with buffer size " + BufSize);

            try {
                File = MemoryMappedFile.CreateOrOpen(
                    Cue.HOSTS_FILE_NAME, size_t, MemoryMappedFileAccess.ReadWrite);

                Accessor = File.CreateViewAccessor(
                    0, size_t, MemoryMappedFileAccess.ReadWrite);

                Process();

                Ready = true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Debug, ex.Message);
                Logger.Log(LogType.Debug, ex.StackTrace);
            }
        }

        public void Disable()
        {
            if(!Ready) return; // already disposed and all
            Logger.Log(LogType.Debug, "closing shared mem file");
            Accessor.Dispose();
            File.Dispose();
            Ready = false;
        }
            
        public void Evaluate(int _)
        {
            if(InEnabled[0] && !Ready)
            {
                Enable();
            }
            else if(!InEnabled[0] && Ready)
            {
                Disable();
            }

            if(InHosts.IsChanged && Ready) Process();
        }

        private void Process() 
        {
            var strs = new List<string>();

            for(int i = 0; i < InHosts.SliceCount; i++)
            {
                strs.Add(InHosts[i]);
            }

            var chars = JsonConvert.SerializeObject(strs).ToCharArray();
            var count = chars.Count();

            if(count > BufSize)
            {
                Logger.Log(LogType.Debug, "increasing buffer size from " + BufSize + " to " + count);
                BufSize = count;
                Disable();
                Enable();
            }

            for(int i = 0; i < chars.Count(); i++)
            {
                Accessor.Write(i, chars[i]);
            }
        }
    }
}
