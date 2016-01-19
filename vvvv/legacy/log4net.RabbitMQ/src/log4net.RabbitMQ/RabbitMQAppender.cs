using System;
using log4net.Core;

namespace log4net.RabbitMQ {
    public class RabbitMQAppender : RabbitMQAppenderBase {
        protected override string Format(LoggingEvent loggingEvent) {
            return this.MessageProperties.ContentType.Format(loggingEvent);
        }

        protected override void Debug(string format, params object[] args) {
            log4net.Util.LogLog.Debug(typeof(RabbitMQAppender), String.Format(format, args));
        }
    }
}
