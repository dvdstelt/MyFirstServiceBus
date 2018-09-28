using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Sales.Messages.Commands;
using ServiceBus;

namespace Sales.Handlers
{
    public class SubmitOrderHandler : IHandleMessages<SubmitOrder>
    {
        public Task Handle(SubmitOrder message)
        {
            Console.WriteLine($"Received {nameof(SubmitOrder)} with id {message.Id}");

            return Task.CompletedTask;
        }
    }
}
