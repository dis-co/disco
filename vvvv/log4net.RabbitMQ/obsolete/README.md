log4net.RabbitMQ.1.2.10 project was moved to this folder because the solution dependencies are now being managed by paket. paket does not allow two different versions of log4net in the same solution. And log4net 1.2.10 is really old, so people can still use the older versions of this appender or ugrade their log4net version.

Refer to the conversation on Github for additional details: https://github.com/haf/log4net.RabbitMQ/pull/17
