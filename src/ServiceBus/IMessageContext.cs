using System.Threading.Tasks;

namespace ServiceBus
{
    public interface IMessageContext
    {
        Task Send(object message);
    }
}
