using System.Web.Mvc;

namespace log4net.RabbitMQ.Web.Controllers
{
	public class HomeController : Controller
	{
		private static readonly ILog _Logger = LogManager.GetLogger(typeof (HomeController));

		public ActionResult Index()
		{
			_Logger.Info("index page requested");
			return View();
		}

		public ActionResult DoThings()
		{
			_Logger.Info("do things requested");
			return View();
		}
	}
}
