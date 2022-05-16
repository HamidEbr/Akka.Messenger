//#define Sharding
using Akka.Actor;
using Akka.Cluster;
using Akka.Cluster.Sharding;
using Akka.Configuration;
using Akka.DependencyInjection;
using Service.Api.Actors;
using Service.Api.Helper;

namespace Service.Api.Services
{
    using ClusterSharding = Akka.Cluster.Sharding.ClusterSharding;
    public static class AkkaExtensions
    {
        public static void AddAkka(this IServiceCollection services)
        {
            // creates an instance of the ISignalRProcessor that can be handled by SignalR
            services.AddSingleton<IMessageSessionHandler, AkkaService>();

            // starts the IHostedService, which creates the ActorSystem and actors
            services.AddHostedService<AkkaService>(sp => (AkkaService)sp.GetRequiredService<IMessageSessionHandler>());
        }
    }

    public sealed class AkkaService : IHostedService, IMessageSessionHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _applicationLifetime;

        private ActorSystem _system;
        private IActorRef _shardRegion;
        private IActorRef _messageManager;

        public AkkaService(IServiceProvider serviceProvider, IHostApplicationLifetime applicationLifetime)
        {
            _serviceProvider = serviceProvider;
            _applicationLifetime = applicationLifetime;
        }

#if Sharding
        public async Task StartAsync(CancellationToken cancellationToken)
#else
        public Task StartAsync(CancellationToken cancellationToken)
#endif
        {

#if Sharding
            var config = ConfigurationFactory.ParseString(await File.ReadAllTextAsync("app.conf", cancellationToken));
            _system = ActorSystem.Create("MessengerSystem", config);

            //var sharding = ClusterSharding.Get(_system);
            //_shardRegion = await sharding.StartAsync(
            //    typeName: "user",
            //    entityPropsFactory: e => Props.Create(() => new User(e)),
            //    settings: ClusterShardingSettings.Create(_system),
            //    messageExtractor: new MessageExtractor(10));

            _shardRegion = await ClusterSharding.Get(_system).StartAsync(
                typeName: "processesCoordinator",
                entityPropsFactory: s => Props.Create(() => new ProcessesCoordinator()),
                settings: ClusterShardingSettings.Create(_system),
                messageExtractor: new MessageExtractor(10));
#else
            var spSetup = DependencyResolverSetup.Create(_serviceProvider);

            var bootstrapSetup = BootstrapSetup.Create();

            var actorSystemSetup = spSetup.And(bootstrapSetup);
            _system = ActorSystem.Create("MessengerSystem", actorSystemSetup);
            _messageManager = _system.ActorOf(Props.Create(() => new ProcessesCoordinator()), "messenger");
            _system.WhenTerminated.ContinueWith(tr =>
            {
                _applicationLifetime.StopApplication();
            }, cancellationToken);

            return Task.CompletedTask;
#endif
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _system.Terminate();
        }

        public void Handle(object msg)
        {
#if Sharding
            _shardRegion.Tell(msg);
#else
            _messageManager.Tell(msg);
#endif
        }

        public async Task<T> Ask<T>(object msg)
        {
#if Sharding
            return await _shardRegion.Ask<T>(msg);
#else
            return await _messageManager.Ask<T>(msg);
#endif
        }
    }
}
