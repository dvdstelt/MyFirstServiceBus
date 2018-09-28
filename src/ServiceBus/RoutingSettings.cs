using System;
using System.Collections.Generic;

namespace ServiceBus
{
    public class RoutingSettings
    {
        internal Dictionary<Type, List<string>> TypeMappings { get; set; }

        public RoutingSettings()
        {
            TypeMappings = new Dictionary<Type, List<string>>();
        }

        public void RouteToEndpoint(Type type, string endpointName)
        {
            if (!TypeMappings.ContainsKey(type))
                TypeMappings.Add(type, new List<string>());

            TypeMappings[type].Add(endpointName);
        }

    }
}
