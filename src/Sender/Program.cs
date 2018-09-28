using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using Sales.Messages.Commands;
using ServiceBus;

namespace Sender
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var endpointConfiguration = new EndpointConfiguration("Sender");
            endpointConfiguration.HostName = "192.168.1.25";

            var routing = endpointConfiguration.Routing();
            routing.RouteToEndpoint(typeof(SubmitOrder), "Sales");

            var endpointInstance = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);

            Console.WriteLine("Press [s] to send a message");
            Console.WriteLine("Press any other key to exit");

            while (Console.ReadKey(true).Key == ConsoleKey.S)
            {
                var order = new SubmitOrder
                {
                    CustomerId = Guid.NewGuid(),
                    Id = Guid.NewGuid()
                };

                await endpointInstance.Send(order).ConfigureAwait(false);

                Console.WriteLine($"Send message with OrderId {order.Id}");
            }

            await endpointInstance.Shutdown().ConfigureAwait(false);
        }
    }
}
