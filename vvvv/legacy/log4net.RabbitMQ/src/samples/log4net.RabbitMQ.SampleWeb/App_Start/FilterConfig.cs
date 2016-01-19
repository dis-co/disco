using System.Web;
using System.Web.Mvc;

namespace log4net.RabbitMQ.SampleWeb
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
