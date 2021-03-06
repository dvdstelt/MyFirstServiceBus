﻿using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ServiceBus.Helpers;

namespace ServiceBus
{
    public class EndpointConfiguration
    {
        private string Name { get; set; }
        public string HostName { get; set; }
        private RoutingSettings RoutingSettings { get; set; }

        public EndpointConfiguration(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Endpoint must have a name");

            this.RoutingSettings = new RoutingSettings();

            Name = name;
        }

        internal async Task<EndpointInstance> Build(EndpointConfiguration configuration)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            var assemblyScanner = new AssemblyScanner(path);
            var messages = assemblyScanner.GetMessageTypes();
            var handlers = assemblyScanner.GetDispatchableHandlers();

            var endpoint = new EndpointInstance(configuration.Name, messages, handlers);
            if (RoutingSettings.TypeMappings.Count != 0)
                endpoint.RoutingSettings = RoutingSettings;
            await endpoint.Build(configuration.HostName);

            return endpoint;
        }

        public RoutingSettings Routing()
        {
            return this.RoutingSettings;
        }
    }
}
