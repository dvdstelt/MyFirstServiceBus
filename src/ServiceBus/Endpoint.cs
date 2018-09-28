using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace ServiceBus
{
    public class Endpoint
    {
        public static async Task<EndpointInstance> Start(EndpointConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentException("Must provide some configuration", nameof(configuration));

            return await configuration.Build(configuration);
        }
    }
}
