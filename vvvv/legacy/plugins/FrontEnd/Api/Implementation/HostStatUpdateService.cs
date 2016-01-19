using System;
using System.Reactive.Linq;

using Iris.FrontEnd.Types;
using Iris.Core.Logging;

namespace Iris.FrontEnd.Api.Impl
{
    public class HostStatUpdateService
    {
        private const string TAG = "HostStatUpdateService";
        private IDisposable task;

        public HostStatUpdateService()
        {
        }

        public void Start()
        {
            task = Observable.Timer(TimeSpan.FromMilliseconds(0),
                TimeSpan.FromMilliseconds(5000))
                .Subscribe(x =>
                {
                    try
                    {
                        // HostStat stat = new HostStat();
                        // Publish(null, "iris.host.stats/create", stat);
                    }
                    catch(Exception e)
                    {
                        LogEntry.Debug(TAG, "FIXME: " + e.Message);
                    }
                });
        }

        public void Stop()
        {
            if(task != null) task.Dispose();
        }
    }
}
