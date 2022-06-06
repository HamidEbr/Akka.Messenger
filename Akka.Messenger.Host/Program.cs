using Akka.Actor;
using Akka.Cluster;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Hosting;
using Akka.Messenger.Host;
using Akka.Messenger.Shared.Sharding;
using Akka.Remote.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Cluster.Sharding;
using Petabridge.Cmd.Host;

var builder = new HostBuilder()
    .ConfigureAppConfiguration(c => c.AddEnvironmentVariables()
    .AddJsonFile("appsettings.json"))
    .ConfigureServices((context, services) =>
    {
        services.AddAkka("messenger-system", (configurationBuilder, provider) =>
        {
            AkkaSettings settings = context.Configuration.GetValue<AkkaSettings>("AkkaSettings");
            var a = context.Configuration.GetSection("AkkaSettings");
            configurationBuilder
                .WithRemoting("localhost", 7919)
                .WithClustering(new ClusterOptions()
                {
                    Roles = new[] { "hostRole" },
                    SeedNodes = new[]
                    {
                        Address.Parse("akka.tcp://messenger-system@localhost:7919") }
                })
                .WithShardRegion<UserEntity>("userActions", s => UserEntity.Props(s),
                    new MessageExtractor(),
                    new ShardOptions()
                    {
                        RememberEntities = true,
                        Role = "smsRole",
                        StateStoreMode = StateStoreMode.DData
                    })
                .StartActors((system, registry) =>
                {
                    var cmd = PetabridgeCmd.Get(system);
                    cmd.RegisterCommandPalette(ClusterCommands.Instance);
                    cmd.RegisterCommandPalette(ClusterShardingCommands.Instance);
                    cmd.Start();

                    var cluster = Cluster.Get(system);
                    cluster.RegisterOnMemberUp(() =>
                    {
                        //ProduceMessages(system, shardRegionProxy);
                    });

                    var userActionsShard = registry.Get<UserEntity>();
                    var indexer = system.ActorOf(Props.Create(() => new UserProxy(userActionsShard)), "index");
                    registry.TryRegister<Index>(indexer); // register for DI                    
                });

        });
    })
    .Build();


await builder.RunAsync();