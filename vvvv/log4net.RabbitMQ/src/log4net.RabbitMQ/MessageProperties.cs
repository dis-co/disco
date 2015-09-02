using System;
using log4net.Layout;

namespace log4net.RabbitMQ
{
    /// <summary>
    /// Customizable RabbitMQ message properties.
    /// </summary>
    public class MessageProperties
    {
        /// <summary>
        /// Gets or sets the application id to specify when sending. Defaults to null,
        /// and then IBasicProperties.AppId will be the name of the logger instead.
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// Get or set the message topic (routing key).  Default value is "%level". 
        /// </summary>
        public PatternLayout Topic { get; set; }

        /// <summary>
        /// Get or set the message content type.  Default value is "text/plain". 
        /// </summary>
        public PatternLayout ContentType { get; set; }

        /// <summary>
        /// Get or set the message delivery mode.  Default is "not persistent". 
        /// </summary>
        public bool Persistent { get; set; }

        /// <summary>
        /// Get or set the message priority.  
        /// Must resolve to a string in the range is "0" - "9".
        /// </summary>
        public PatternLayout Priority { get; set; }

        /// <summary>
        /// Gets or sets whether the logger should log extended data.
        /// Defaults to false.
        /// </summary>
        public bool ExtendedData { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MessageProperties()
        {
            this.Topic = new PatternLayout(String.Empty);   // when empty the original "Topic" will be used.
            this.ContentType = new PatternLayout("text/plain");
            this.Persistent = false;
            this.Priority = new PatternLayout(String.Empty);  // use default
        }
    }
}
