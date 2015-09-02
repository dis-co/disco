using System;
using System.Text;

using Iris.Core.Types;
using Iris.Core.Logging;
using Iris.FrontEnd.Types;
using Iris.FrontEnd.Api.Interfaces;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using WampSharp.V2;
using WampSharp.V2.Realm;
using WampSharp.V2.Core.Contracts;

namespace Iris.FrontEnd.Api.Impl
{
    public class LogService : IrisService, ILogService
    {
        public LogService() : base(DataUri.LogBase)
        {
        }

        protected override void BeforeStart()
        {
        }

        protected override void OnMessage(BasicDeliverEventArgs ea)
        {
            var body = Encoding.UTF8.GetString(ea.Body);

            try {
                var e = JsonConvert.DeserializeObject<LogEntry>(body);
                Publish(DataUri.LogCreate, e);
            }
            catch(Exception ex)
            {
                ex.Message.ToString();
            }
        }
    }
}
