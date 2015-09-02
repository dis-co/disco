using System;
using System.Collections.Generic;

using RabbitMQ.Client;

using Iris.FrontEnd.Api.Impl;

namespace Iris.FrontEnd.Types
{
    public class IrisServices : ServiceFactory
    {
        public IrisServices()
        {
            services = new List<IrisService>()
            {
                new ProjectService(),
                new PatchService(),
                new PinService(),
                new CueService(),
                new CueListService(),
                new HostStatService(),
                new FileSystemService(),
                new LogService(),
                new ClusterService()
            };
        }
    }
}