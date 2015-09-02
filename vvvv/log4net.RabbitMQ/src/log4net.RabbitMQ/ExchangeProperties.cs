using System.Collections.Generic;
using RabbitMQExchangeType = RabbitMQ.Client.ExchangeType;

namespace log4net.RabbitMQ
{
    /// <summary>
    /// Properties for creating and configuring a RabbitMQ Exchange.  These are used
    /// when the exchange is declared (typically near startup).
    /// </summary>
    public class ExchangeProperties
    {
        internal ICollection<ExchangeBinding> Bindings { get; private set; }

        /// <summary>
        /// 	Gets or sets the exchange name.
        /// </summary>
        /// <remarks>
        /// 	Default is 'app-logging'
        /// </remarks>
        public string Name { get; set; }

        /// <summary>
        /// 	Gets or sets the exchange type.
        /// </summary>
        /// <remarks>
        /// 	Default is 'topic'
        /// </remarks>
        public string ExchangeType { get; set; }

        /// <summary>
        /// 	Gets or sets the exchange durability.
        /// </summary>
        /// <remarks>
        /// 	Default is false
        /// </remarks>
        public bool Durable { get; set; }

        /// <summary>
        /// 	Gets or sets the exchange auto-delete feature.
        /// </summary>
        /// <remarks>
        /// 	Default is true
        /// </remarks>
        public bool AutoDelete { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ExchangeProperties()
        {
            this.Name = "app-logging";
            this.ExchangeType = RabbitMQExchangeType.Topic;
            this.Durable = false;
            this.AutoDelete = true;
            this.Bindings = new List<ExchangeBinding>();
        }

        /// <summary>
        /// Add an ExchangeBinding.
        /// </summary>
        /// <param name="exchangeBinding">The exchangeBinding to add</param>
        public void AddBinding(ExchangeBinding exchangeBinding)
        {
            this.Bindings.Add(exchangeBinding);
        }
    }
}

