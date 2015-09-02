
namespace log4net.RabbitMQ
{
    /// <summary>
    /// Defines a binding between "our" exchange another "destination" exchange.
    /// </summary>
    public class ExchangeBinding
    {
        /// <summary>
        /// The name of the destination exchange.
        /// </summary>
        public string Destination { get; set; }

        /// <summary>
        /// The topic (routing key) than controls which messages get sent to the destination exchange.
        /// </summary>
        public string Topic { get; set; }

        public ExchangeBinding()
        {
            this.Topic = "#";
        }
    }
}
