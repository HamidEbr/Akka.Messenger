using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.NUnit3;
using Akka.TestKit.TestActors;
using NUnit.Framework.Interfaces;

namespace Akka.Messenger.Test
{
    public class Tests : TestKit.NUnit3.TestKit
    {

        [SetUp]
        public void Setup()
        {
            var probe = this.CreateTestProbe();
            ExpectNoMsg(1);
        }

        [TestCase(100, 300)]
        public void Test1(int delay, int cutoff)
        {
            Within(TimeSpan.FromMilliseconds(cutoff), () =>
            {
                var actor = Sys.ActorOf(Props.Create(() => new BlackHoleActor()));
                actor.Tell(delay);
                ExpectNoMsg();
            });
            Assert.Pass();
        }
    }
}