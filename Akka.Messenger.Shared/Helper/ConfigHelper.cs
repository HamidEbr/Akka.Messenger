namespace Akka.Messenger.Shared.Helper
{
    public static class ConfigHelper
    {
        public static Configuration.Config GetAkkaConfig()
        {
            var config = Configuration.ConfigurationFactory.ParseString("akka : {cluster: {\n"+
                                  "roles : [smsRole]\n"+
                                  "seed-nodes : [\"akka.tcp://messenger-system@localhost:7919\"]\n"+
                                "}\n" +
                                "remote : {\n" +
                                      "dot-netty : \n" +
                                        "tcp : {\n" +
                                         " hostname : 0.0.0.0\n" +
                                         " public-hostname : localhost\n" +
                                         " port : 0\n" +
                                         " public-port : 0\n" +
                                        "}}\n" +
                                    "}\n" +
                                    "}\n" +
                                  "}");
            return config;
        }
    }
}
