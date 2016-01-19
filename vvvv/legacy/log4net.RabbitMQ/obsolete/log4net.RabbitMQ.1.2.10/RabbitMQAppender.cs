using System;
using System.IO;
using log4net.Core;

namespace log4net.RabbitMQ {
    public class RabbitMQAppender : RabbitMQAppenderBase {
        protected override string Format(LoggingEvent loggingEvent) {
            var sw = new StringWriter();
            this.MessageProperties.ContentType.Format(sw, loggingEvent);
            return sw.ToString();
        }

        protected override void Debug(string format, params object[] args) {
            log4net.Util.LogLog.Debug(string.Format("type: {0} ", typeof(RabbitMQAppender)) + String.Format(format, args));
        }
    }
}
