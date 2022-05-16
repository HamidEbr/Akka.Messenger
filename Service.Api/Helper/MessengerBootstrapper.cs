//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Akka.Actor;

//namespace Service.Api.Helper
//{
//    public static class MessengerBootstrapper
//    {
//        public static Akka.Configuration.Config ApplyOpsConfig(this Akka.Configuration.Config previousConfig)
//        {
//            var nextConfig = previousConfig.BootstrapFromDocker();
//            return OpsConfig.GetOpsConfig().ApplyPhobosConfig().WithFallback(nextConfig);
//        }

//        public static Akka.Configuration.Config ApplyPhobosConfig(this Akka.Configuration.Config previousConfig)
//        {
//            var enabledPhobosStr =
//                Environment.GetEnvironmentVariable(OpsConfig.PHOBOS_ENABLED)?.Trim().ToLowerInvariant() ?? "false";
//            if (bool.TryParse(enabledPhobosStr, out var enabledPhobos) && enabledPhobos)
//                return OpsConfig.GetPhobosConfig().WithFallback(previousConfig);

//            return previousConfig;
//        }

//        /// <summary>
//        ///     Start Petabridge.Cmd
//        /// </summary>
//        /// <param name="system">The <see cref="ActorSystem" /> that will run Petabridge.Cmd</param>
//        /// <returns>The same <see cref="ActorSystem" /></returns>
//        public static ActorSystem StartPbm(this ActorSystem system)
//        {
//            var pbm = PetabridgeCmd.Get(system);
//            pbm.RegisterCommandPalette(ClusterCommands.Instance); // enable cluster management commands
//            pbm.Start();
//            return system;
//        }
//    }
//}
