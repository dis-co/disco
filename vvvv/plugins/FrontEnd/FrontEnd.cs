using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using System.Security.Principal;
using System.Security.AccessControl;

using Iris.Web;
using Iris.FrontEnd.Api;
using Iris.FrontEnd.Api.Listeners;
using Iris.FrontEnd.Types;
using Iris.Core.Types;
using Iris.Core.Couch;
using Iris.Core.Logging;

namespace Iris.FrontEnd
{
    class FrontEnd
    {
        static void Main(string[] args)
        {
            Host h = Host.FrontEnd;

#if DEBUG
            var dir = @"E:\projects\iris\iris\www\dist";
#else
            var dir = @"C:\Iris\www";
#endif

            Schema.Migrate();

            AppServer AppServer = new AppServer("0.0.0.0", 8080, dir);
            ApiServer ApiServer = new ApiServer();

            AppServer.Start();
            ApiServer.Start();

            Console.ReadLine();
        }
    }
}
