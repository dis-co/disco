using System.Collections.Generic;
using System.Diagnostics;

namespace Iris.Core.Types
{
    /// type alias for a category name and the corresponding metrics to collect
    using Category = System.Tuple<string, System.Collections.Generic.List<string>>;

    /// type alias for the outgoing object data-structure, which maps
    /// instance id (i.e. network interface name) to an object containing
    /// key/value pairs of metric name and value itself
    using Metrics = Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;

    /// <summary>
    /// Container type to manage retrieval and massaging of relevant PerformanceCounter
    /// objects.
    /// </summary>
    public class HostStat
    {
        // private Category processorMetrics =
        //     new Category("Processor",
        //         new List<string>
        //         {
        //             "% User Time",
        //             "% Privileged Time",
        //             "% Interrupt Time"
        //         });

        // private Category processMetrics =
        //     new Category("Process",
        //         new List<string>
        //         {
        //             "% User Time",
        //             "% Privileged Time",
        //             "% Processor Time",
        //             "Virtual Bytes",
        //             "Working Set",
        //             "Private Bytes"
        //         });

        // private Category memoryMetrics =
        //     new Category(".NET CLR Memory",
        //         new List<string>
        //             {
        //                 "# Bytes in all Heaps",
        //                 "# GC Handles",
        //                 "# Induced GC",
        //                 "# of Pinned Objects",
        //                 "# Total committed Bytes",
        //                 "# Total reserved Bytes",
        //                 "% Time in GC",
        //                 "Large Object Heap size",
        //                 "Process ID"
        //             });

        // private Category networkMetrics =
        //     new Category("Network Interface",
        //         new List<string>
        //         {
        //             "Bytes Received/sec",
        //             "Bytes Sent/sec",
        //             "Bytes Total/sec"
        //         });

        // public Metrics Processor { get; private set; }

        // public Metrics Processes { get; private set; }

        // public Metrics Network { get; private set; }

        // public Metrics Memory { get; private set; }

        public HostStat()
        {
            // Memory = CollectInstances(memoryMetrics);
            // Processor = CollectInstances(processorMetrics);
            // Processes = CollectInstances(processMetrics);
            // Network = CollectInstances(networkMetrics);
        }

        /// <summary>
        /// Collects performance stats for categories that have no instances (like system memory, for instance)
        /// </summary>
        /// <returns>The single.</returns>
        /// <param name="category">Category.</param>
        private Dictionary<string, object> CollectSingle(Category category)
        {
            Dictionary<string, object> output = new Dictionary<string,object>();
            category.Item2.ForEach(m =>
                    {
                        PerformanceCounter counter = new PerformanceCounter(category.Item1, m);
                        output.Add(m, counter.RawValue);
                    });

            return output;
        }

        /// <summary>
        /// Collects data for each instance of a PerformanceCounterCategory.
        /// </summary>
        /// <returns>The instances.</returns>
        /// <param name="category">Category.</param>
        private Metrics CollectInstances(Category category)
        {
            Metrics metrics = new Metrics();

            PerformanceCounterCategory cat =
                new PerformanceCounterCategory(category.Item1);

            string[] instances = cat.GetInstanceNames();

            // iterate through all instances for this category
            for (int i = 0; i < instances.Length; i++)
            {
                Dictionary<string, object> counters = new Dictionary<string, object>();

                // for each instance collect one counter object
                category.Item2.ForEach(m =>
                        {
                            // add a perfomance counter for this particular category, counter and instance name
                            PerformanceCounter counter = new PerformanceCounter(category.Item1, m, instances[i]);
                            // we're really only interested in the value at this point
                            counters.Add(counter.CounterName, counter.RawValue);
                        });

                // this instance to return list
                metrics.Add(instances[i], counters);
            }
            return metrics;
        }

    }
}
