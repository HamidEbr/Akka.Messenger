using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Hosting;
using Akka.Messenger.Shared.Sharding;
using Akka.Remote.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = new HostBuilder()
    .ConfigureAppConfiguration(c => c.AddEnvironmentVariables())
    .ConfigureServices((context, services) =>
    {
        services.AddAkka("messenger-system", (configurationBuilder, provider) =>
        {
            configurationBuilder
                .WithRemoting("localhost", 7919)
                .WithClustering(new ClusterOptions()
                {
                    Roles = new[] { "hostRole" },
                    SeedNodes = new[]
                { 
                    Address.Parse("akka.tcp://messenger-system@localhost:7919") ,
                    Address.Parse("akka.tcp://messenger-system@127.0.0.1:4053") }
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
                    var userActionsShard = registry.Get<UserEntity>();
                    var indexer = system.ActorOf(Props.Create(() => new UserProxy(userActionsShard)), "index");
                    registry.TryRegister<Index>(indexer); // register for DI                    
                });

        });
    })
    .Build();

await builder.RunAsync();