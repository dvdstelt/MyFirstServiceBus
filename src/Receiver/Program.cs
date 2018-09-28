using System;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ServiceBus;

namespace Receiver
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var endpointConfiguration = new EndpointConfiguration("Sales");
            endpointConfiguration.HostName = "192.168.1.25";
            var endpointInstance = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();

            await endpointInstance.Shutdown().ConfigureAwait(false);
        }
    }
}

