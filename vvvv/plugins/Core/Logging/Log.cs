using System;

using log4net.Config;
using log4net.Layout;
using log4net.Appender;
using log4net.RabbitMQ;

namespace Iris.Core.Logging
{
    public static class Log
    {
        private static AppenderSkeleton Appender;

        static Log()
        {
            try
            {
                var MQAppender = new RabbitMQAppender();
                var layout = new PatternLayout();
                layout.ConversionPattern = "%message";

#if DEBUG
                MQAppender.Threshold = log4net.Core.Level.All;
#else
                MQAppender.Threshold = log4net.Core.Level.Error;
#endif

                MQAppender.ExchangeProperties.AutoDelete = true;
                MQAppender.ExchangeProperties.Durable = false;
                MQAppender.ExtendedData = true;
                MQAppender.Layout = layout;
                MQAppender.Exchange = "iris.logging";
                MQAppender.Topic = "iris.log.{0}";
                MQAppender.ActivateOptions();

                Appender = MQAppender;
                BasicConfigurator.Configure(Appender);
            }
            catch (Exception ex) // could not configure MQ-based appender for one reason or another
            {
                var ODSAppender = new OutputDebugStringAppender();
                ODSAppender.Layout = new SimpleLayout();

                Appender = ODSAppender;
                BasicConfigurator.Configure(Appender);

                log.Fatal(ex.Message);
                log.Fatal(ex.StackTrace);
            }
        }

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
        (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void SetLogLevel(log4net.Core.Level level)
        {
            Appender.Threshold = level;
            Appender.ActivateOptions();
        }

        public static void Debug(string thing)
        {
            log.Debug(thing);
        }

        public static void Info(string thing)
        {
            log.Info(thing);
        }

        public static void Warn(string thing)
        {
            log.Warn(thing);
        }

        public static void Error(string thing)
        {
            log.Error(thing);
        }

        public static void Fatal(string thing)
        {
            log.Fatal(thing);
        }
    }
}
