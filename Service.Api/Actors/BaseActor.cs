using Akka.Actor;
using Akka.DependencyInjection;

namespace Service.Api.Actors
{
    public abstract class BaseActor : ReceiveActor
    {
        protected DependencyResolver _sp;

        protected override void PreStart()
        {
            _sp = DependencyResolver.For(Context.System);
        }
    }
}
