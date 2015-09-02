using System;

namespace Iris.Core.Types
{
    public sealed class Host
    {
        public static IrisId HostId;
        public static IrisIP IP;
        public static Name  HostName;
        public static Roles Role = Roles.Undefined;

        private static Host instance;

        static Host()
        {
            var name = Environment.GetEnvironmentVariable("IRIS_HOST_NAME");

            IP = new IrisIP();
            HostId = new IrisId();
            HostName = new Name(name ?? System.Net.Dns.GetHostName());
        }

        private Host(Roles role)
        {
            Role = role;
        }

        public static Host Renderer
        {
            get
            {
                if (instance == null)
                {
                    instance = new Host(Roles.Renderer);
                }
                return instance;
            }
        }

        public static Host FrontEnd
        {
            get
            {
                if (instance == null)
                {
                    instance = new Host(Roles.FrontEnd);
                }
                return instance;
            }
        }

        public static Host Processor
        {
            get
            {
                if (instance == null)
                {
                    instance = new Host(Roles.Processor);
                }
                return instance;
            }
        }
    }
}
