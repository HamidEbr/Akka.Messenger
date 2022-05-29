using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Event;
using Akka.Hosting;
using Akka.Messenger.Shared.Models;
using Akka.Messenger.Shared.Sharding;
using Akka.Remote.Hosting;
using Akka.TestKit;
using Akka.TestKit.TestActors;
using Akka.TestKit.Xunit2.Internals;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace Akka.Messenger.Tests
{
    public class UnitTest1 : TestKit.Xunit2.TestKit
    {
        public UnitTest1(ITestOutputHelper output) : base(output: output)
        {
        }

        [Fact]
        public void Test1()
        {
            var blackHoleActor = Sys.ActorOf<BlackHoleActor>("blackHoleActor");
            blackHoleActor.Tell("hello");
            ExpectNoMsg();
        }

        [Fact]
        public void Test2()
        {
            var probe = CreateTestProbe();
            probe.Tell("hello");
            probe.ExpectMsg("hello");
            probe.Forward(TestActor);
            ExpectMsg("hello");
            Assert.Equal(TestActor, LastSender);
        }

        [Fact]
        public async void Test3()
        {
            var probe = CreateTestProbe();
            var user = ActorOf(UserEntity.Props("09021879309"));
            probe.SetAutoPilot(new DelegateAutoPilot((sender, message) =>
            {
                sender.Tell(message, ActorRefs.NoSender);
                return AutoPilot.KeepRunning;
            }));
            //user.Tell(UserEntity.SendSmsMessage.Create("09021879309", new Sms("hello")), sender);
            //Assert.NotEqual(result, Guid.Empty);
            //var probe = CreateTestProbe();
            //probe.Tell("hello");
            //probe.ExpectMsg("hello");
            //probe.Forward(TestActor);
            //ExpectMsg("hello");
            //Assert.Equal(TestActor, LastSender);
            //first one is replied to directly
            //var result = await probe.Ask(UserEntity.SendSmsMessage.Create("09021879309", new Sms("hello")));
            var result = await probe.Ask(UserEntity.ReadAllSmsesMessage.Instance());
            ExpectMsg("Hello");
            //... but then the auto-pilot switched itself off
            probe.Tell("world");
            ExpectNoMsg();
        }

        private async Task<IHost> CreateHost(Action<AkkaConfigurationBuilder> specBuilder, ClusterOptions options)
        {
            var tcs = new TaskCompletionSource();
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var host = new HostBuilder()
                .ConfigureServices(collection =>
                {
                    collection.AddAkka("TestSys", (configurationBuilder, provider) =>
                    {
                        _ = configurationBuilder
                            .WithRemoting("localhost", 0)
                            .WithClustering(options)
                            .WithActors((system, registry) =>
                            {
                                var extSystem = (ExtendedActorSystem)system;
                                var logger = extSystem.SystemActorOf(Props.Create(() => new TestOutputLogger(Output)), "log-test");
                                logger.Tell(new InitializeLogger(system.EventStream));
                            })
                            .WithActors(async (system, registry) =>
                            {
                                var cluster = Akka.Cluster.Cluster.Get(system);
                                cluster.RegisterOnMemberUp(() =>
                                {
                                    tcs.SetResult();
                                });
                                if (options.SeedNodes == null || options.SeedNodes.Length == 0)
                                {
                                    var myAddress = cluster.SelfAddress;
                                    await cluster.JoinAsync(myAddress); // force system to wait until we're up
                                }
                            });
                        specBuilder(configurationBuilder);
                    });
                }).Build();

            await host.StartAsync(cancellationTokenSource.Token);
            await (tcs.Task.WaitAsync(cancellationTokenSource.Token));

            return host;
        }
    }
}