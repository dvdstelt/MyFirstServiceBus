using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;

namespace ServiceBus
{
    public class EndpointInstance : IMessageContext
    {
        private readonly string _name;
        private readonly IEnumerable<Type> _messages;
        private readonly IEnumerable<Type> _handlers;
        IConnection _connection;
        IModel _channel;

        public EndpointInstance(string name, IEnumerable<Type> messages, IEnumerable<Type> handlers)
        {
            _name = name;
            _messages = messages;
            _handlers = handlers;
        }

        internal RoutingSettings RoutingSettings { get; set; }

        internal async Task Build(string hostName)
        {
            // RabbitMQ
            var factory = new ConnectionFactory() { HostName = hostName };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Create incoming queue for 'commands'
            _channel.QueueDeclare(queue: _name.ToLower(),
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += ConsumerOnReceived;

            _channel.BasicConsume(queue: _name.ToLower(),
                autoAck: false,
                consumer: consumer);
        }

        public async Task Shutdown()
        {
            _channel.Close();
            _connection.Close();
            
            _channel.Dispose();
            _connection.Dispose();
        }

        private void ConsumerOnReceived(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                var body = e.Body;
                if (body == null || body.Length == 0)
                {
                    Console.WriteLine("No body found, nothing to deserialize");
                    return;
                }

                var messageBody = Encoding.UTF8.GetString(body);

                var properties = e.BasicProperties;
                var messageId = Guid.Parse(Encoding.UTF8.GetString((byte[]) properties.Headers["messageId"]));
                var fullMessageType = Encoding.UTF8.GetString((byte[]) properties.Headers["messageType"]);

                var messageType = Type.GetType(fullMessageType, false);

                if (messageType == null)
                {
                    Console.WriteLine(
                        $"Unable to determine the message type for message {messageId}\n\t [{fullMessageType}]");
                    return;
                }

                var physicalMessage = JsonConvert.DeserializeObject(messageBody, messageType);

                DispatchToHandlers(physicalMessage);
                
                _channel.BasicAck(e.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to process message because\n{ex.StackTrace}");
                _channel.BasicReject(e.DeliveryTag, true);
            }
        }

        private void DispatchToHandlers(object physicalMessage)
        {
            // Filter all classes that implement this specific message type
            Type messageInterface = typeof(IHandleMessages<>).MakeGenericType(physicalMessage.GetType());
            var myMessageHandlers = _handlers
                .Where(type => type.GetInterfaces()
                    .Any(it => it == messageInterface))
                .Distinct();

            if (myMessageHandlers.Count() == 0)
                Console.WriteLine($"No message handlers found for message {physicalMessage.GetType()}");

            // Loop through all handlers found
            foreach (var handler in myMessageHandlers)
            {
                object handlerInstance = Activator.CreateInstance(handler);

                // Find methods that are called "Handle" and expect as parameter this message type in particular
                var methods = from m in handler.GetMethods()
                              where m.Name == "Handle"
                              from p in m.GetParameters()
                              where p.ParameterType == physicalMessage.GetType()
                              select m;

                // Invoke the `Handle` method with the parameter
                var methodInfo = methods.Single();
                methodInfo.Invoke(handlerInstance, new[] { physicalMessage });
            }
        }

        public Task Send(object message)
        {
            var messageType = message.GetType();
            var messageFullyQualifiedName = messageType.AssemblyQualifiedName;

            var serializedBody = JsonConvert.SerializeObject(message);
            var encodedBody = Encoding.UTF8.GetBytes(serializedBody);

            var properties = new BasicProperties();
            properties.Headers = new Dictionary<string, object>();
            properties.Headers.Add("messageId", Guid.NewGuid().ToString());
            properties.Headers.Add("messageType", messageFullyQualifiedName);
            properties.Headers.Add("ReplyAddress", _name);

            var typeMappings = RoutingSettings.TypeMappings[messageType];

            if (typeMappings.Count == 0)
            {
                Console.WriteLine($"No type mapping found for message type {nameof(messageType)}");
            }

            Parallel.ForEach(typeMappings, destination =>
            {
                _channel.BasicPublish(exchange: "",
                    routingKey: destination.ToLower(),
                    basicProperties: properties,
                    body: encodedBody);
            });

            return Task.CompletedTask;
        }
    }
}
