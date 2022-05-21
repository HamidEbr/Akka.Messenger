using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Configuration;
using Service.Api.Actors;
using Service.Api.Helper;

namespace Service.Api.Services
{
    public static class AkkaExtensions
    {
        public static void AddAkka(this IServiceCollection services)
        {
            // creates an instance of the ISignalRProcessor that can be handled by SignalR
            services.AddSingleton<IMessageSessionHandler, AkkaService>();

            // starts the IHostedService, which creates the ActorSystem and actors
            services.AddHostedService(sp => (AkkaService)sp.GetRequiredService<IMessageSessionHandler>());
        }
    }

    public sealed class AkkaService : IHostedService, IMessageSessionHandler
    {
        private ActorSystem _system;
        public IActorRef ShardRegion { get; private set; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var config = ConfigurationFactory.ParseString(await File.ReadAllTextAsync("app.conf", cancellationToken));
            _system = ActorSystem.Create("messenger-system", config);

            ShardRegion = await ClusterSharding.Get(_system).StartAsync(
                typeName: "user",
                entityPropsFactory: entityId => Props.Create<User>(this, entityId),
                settings: ClusterShardingSettings.Create(_system).WithRole("MessengerUser"),
                messageExtractor: new MessageExtractor(10));
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _system.Terminate();
        }

        public void Handle(object msg)
        {
            ShardRegion.Tell(msg);
        }

        public async Task<T> Ask<T>(object msg)
        {
            return await ShardRegion.Ask<T>(msg);
        }
    }
}
