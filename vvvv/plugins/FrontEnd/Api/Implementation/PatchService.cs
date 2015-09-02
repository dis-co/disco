using System;
using System.Text;

using Iris.Core.Types;
using Iris.Core.Logging;
using Iris.FrontEnd.Types;
using Iris.FrontEnd.Api.Interfaces;

using RabbitMQ.Client.Events;

namespace Iris.FrontEnd.Api.Impl
{
    public class PatchService : IrisService, IPatchService
    {
        private const string TAG = "PatchService";

        public PatchService() : base(DataUri.PatchBase)
        {
        }

        protected override void BeforeStart()
        {
        }

        protected override void OnMessage(BasicDeliverEventArgs ea)
        {
            var role = Role.Parse(ea.BasicProperties.Type);
            if (role != Roles.Renderer) return;

            string action;

            switch(ea.RoutingKey)
            {
                case DataUri.CreateAction:
                    action = DataUri.PatchCreate;
                    break;
                case DataUri.UpdateAction:
                    action = DataUri.PatchUpdate;
                    break;
                case DataUri.DeleteAction:
                    action = DataUri.PatchDelete;
                    break;
                default:
                    action = DataUri.PatchCreate;
                    break;
            }

            Publish(action, Encoding.UTF8.GetString(ea.Body));
        }

        public IIrisData ListPatches(string sid)
        {
            LogEntry.Debug(TAG, "Calling " + DataUri.PatchList);
            try
            {
                Call(DataUri.ListAction);
            }
            catch(Exception ex)
            {
                LogEntry.Debug(TAG, "Exception: " + ex.Message);
                LogEntry.Debug(TAG, ex.StackTrace);
            }
            return new Patches();
        }
    }
}
