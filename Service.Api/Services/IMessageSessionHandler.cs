namespace Service.Api.Services
{
    public interface IMessageSessionHandler
    {
        void Handle(object msg);
        Task<T> Ask<T>(object msg);
    }
}
