namespace Akka.Messenger.Host
{
    public class AkkaSettings
    {
        public string ActorSystemName { get; set; }
        public string HostName { get; set; }
        public int Port { get; set; }
        public string[] SeedNodes { get; set; }
        public string[] Roles { get; set; }
        public string ShardRole { get; set; }
    }
}
