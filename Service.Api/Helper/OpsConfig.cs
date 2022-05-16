//using Akka.Configuration;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Service.Api.Helper
//{
//    public class OpsConfig
//    {
//        public const string PHOBOS_ENABLED = "PHOBOS_ENABLED";

//        public const string STATSD_URL = "STATSD_URL";

//        public const string STATSD_PORT = "STATSD_PORT";

//        public static Akka.Configuration.Config GetOpsConfig()
//        {
//            return ConfigurationFactory.FromResource<OpsConfig>("Messenger.Shared.DevOps.Config.messenger.DevOps.conf");
//        }

//        public static Akka.Configuration.Config GetPhobosConfig()
//        {
//            var rawPhobosConfig =
//                ConfigurationFactory.FromResource<OpsConfig>("Messenger.Shared.DevOps.Config.messenger.Phobos.conf");
//            var statsdUrl = Environment.GetEnvironmentVariable(STATSD_URL);
//            var statsDPort = Environment.GetEnvironmentVariable(STATSD_PORT);
//            if (!string.IsNullOrEmpty(statsdUrl) && int.TryParse(statsDPort, out var portNum))
//                return ConfigurationFactory.ParseString($"phobos.monitoring.statsd.endpoint=\"{statsdUrl}\"" +
//                                                        Environment.NewLine +
//                                                        $"phobos.monitoring.statsd.port={portNum}")
//                    .WithFallback(rawPhobosConfig);

//            return rawPhobosConfig;
//        }
//    }
//}
