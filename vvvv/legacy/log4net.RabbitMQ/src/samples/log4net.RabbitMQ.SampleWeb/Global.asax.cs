using System.IO;
using System.Web.Mvc;
using System.Web.Routing;
using log4net.Config;
using System.Web.Optimization;

namespace log4net.RabbitMQ.SampleWeb
{
	public class MvcApplication : System.Web.HttpApplication
	{
		// NOTE: THIS IS CUSTOM FOR log4net SAMPLE!
		private static readonly ILog _Logger = LogManager.GetLogger(typeof(MvcApplication));

        protected void Application_Start()
        {
            // NOTE: THIS IS CUSTOM FOR log4net SAMPLE!
            XmlConfigurator.ConfigureAndWatch(new FileInfo(Server.MapPath("~/log4net.config")));

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
		
		// NOTE: THIS IS CUSTOM FOR log4net SAMPLE!
        protected void Application_Error()
        {
            var lastError = Server.GetLastError();
            Server.ClearError();

            _Logger.Error("app error", lastError);
        }

		// NOTE: THIS IS CUSTOM FOR log4net SAMPLE!
		protected void Application_End()
		{
			_Logger.Info("shutting down application");

			LogManager.Shutdown();
		}
	}
}