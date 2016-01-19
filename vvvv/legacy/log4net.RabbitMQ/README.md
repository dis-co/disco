# log4net RabbitMQ Appender

**note: I would recommend looking at [Logary](https://github.com/logary/logary) for a future-proof way of logging - while it doesn't have RabbitMQ support right now, adding it could be done in an hours worth of coding for a newbie, by following the structure of this repo or that of NLog.RabbitMQ.**

**Nuget key: `log4net.RabbitMQAppender`**

An appender for logging over AMQP, specifically RabbitMQ. Why? Because sometimes you want to log with topics, without deciding on where the data/logs end up. Publish-subscribe, that is. The appender uses topics; a tutorial on topic routing, [can be found at RabbitMQ's web site](http://www.rabbitmq.com/tutorials/tutorial-five-python.html).

Appender properties:

 * **VHost** - `string` - the virtual host to use. This needs to be configured in RabbitMQ before put to use. Defaults to `/`.
 * **UserName** - `string` - the username to authenticate with. Defaults to `guest`.
 * **Password** - `string` - the password to authenticate with. Defaults to `guest`.
 * **Port** - `uint` - what port the RabbitMQ broker is listening to. Defaults to `5672`.
 * **Topic** - `string` - what topic to publish with. It must contain a string: `{0}`, or the logger won't work. The string inserted here will be used together with `string.Format`.
 * **Protocol** - `IProtocol` - what protocol to use for RabbitMQ-communication. See also `SetProtocol`.
 * **HostName** - `string` - the host name of the computer/node to connect to. Defaults to `localhost`.
 * **Exchange** - `string` - what exchange to publish log messages to. Defaults to `app-logging` and is declared when the appender is started.
 * **ExchangeProperties** - `ExchangeProperties` - defines exchange properties.
 * **MessageProperties** - `MessageProperties` - defines message properties.
 * **AppId** - `string` - the name of the publishing application
 * **ExtendedData** - `bool` - whether to include the class, file and the line of the log message as headers in `IBasicProperties`.

ExchangeProperties allow you to customize the RabbitMQ exchange messages are published to.  These properties are typically used when the exchange is 
declared and initialized:

 * **Name** - `string` - what exchange to publish log messages to. Defaults to `app-logging` and is declared when the appender is started.  This is an alternative to the appender Exchange property.
 * **ExchangeType** - `string` - the exchange type. Defaults to `topic`. Used when the exchange is declared when the appender is started.
 * **Durable** - `bool` - the exchange durability. Defaults to false. Used when the exchange is declared when the appender is started.
 * **Binding** - `ExchangeBinding` - defines a binding between our Exchange and another exchange in RabbitMQ. Multiple bindings are allowed.

ExchangeBinding allow you to define a binding between our exchange and another RabbitMQ exchange:

 * **Destination** - `string` - The name of the exchange to bind to.
 * **Topic** - `string` - The topic (routing key) assoicated with the binding.
 
MessageProperties allow you to customize the message headers when publishing messages to RabbitMQ:

 * **AppId** - `string` - The name of the publishing application. Alternative to the appender AppId property.
 * **Topic** - `LayoutPattern` - topic format string to publish with. Superceeds the appender Topic property if set.
 * **ContentType** - 'LayoutPattern' - Default is 'text/plain'.
 * **Persistent** - 'bool' - Sets the message delivery mode. Default value is false (non-persistent).
 * **Priority** - 'LayoutPattern' - Must resolve to a byte in the range 0 - 9. Default value is 0.
 * **ExtendedData** - `bool` - whether to include the class, file and the line of the log message as headers in `IBasicProperties`. Alternative to the appender ExtendedData property.

For SSL -- have a look at: http://www.rabbitmq.com/ssl.html

A guide to setting up a secure corporate messaging infrastructure w/ .Net might be in the works... ;) Keep tuned to @henrikfeldt on twitter!

## Example log4net.config

This configuration demonstrates usage of the properties from above:

```xml
<log4net>
	<appender name="AmqpAppender" type="log4net.RabbitMQ.RabbitMQAppender, log4net.RabbitMQ">
		<exchangeProperties>
			<name value = "app-logging" />
			<exchangeType value = "topic" />
			<durable value = false />
			<binding>
				<destination value="SomeOtherRabbitMQExchange" />
				<topic value="#" />
			</binding>
		</exchangeProperties>

		<messageProperties>
			<appId value="My Web Application" />
			<topic type="log4net.Layout.PatternLayout">
				<conversionPattern value="samples.web.%level" />
			</topic>
			<contentType type="log4net.Layout.PatternLayout">
				<conversionPattern value="text/plain" />
			</contentType>
			<persistent value = "false" />
			<priority type="log4net.Layout.PatternLayout">
				<conversionPattern value="0" />
			</priority>
			<ExtendedData value="true" />
		</messageProperties>

		<layout type="log4net.Layout.PatternLayout">
				<conversionPattern value="%date [%thread] %-5level - %message%newline" />
		</layout>
	</appender>
	<root>
		<level value="DEBUG"/>
		<appender-ref ref="AmqpAppender" />
	</root>
</log4net>
```

You would register log4net in a web application as such, in `Application_Start`:

```csharp
using log4net.Config;
// ...
XmlConfigurator.ConfigureAndWatch(new FileInfo(Server.MapPath("~/log4net.config")));
```

In `Application_End`:

```csharp
LogManager.Shutdown();
```

If you put the log4net configuration in web.config, reloading and restarting the AMQP channel won't work after an AppDomain recycle or change to the web.config file.

## From the receiving side

For full documentation, see the RabbitMQ web site. An example receiver has this main method:

```csharp
private static void Main(string[] args)
{
	var factory = new ConnectionFactory
	{
		HostName = "localhost",
		UserName = "guest",
		Password = "guest",
		Protocol = Protocols.DefaultProtocol
	};

	using (var c = factory.CreateConnection())
	using (var m = c.CreateModel())
	{
		var consumer = new QueueingBasicConsumer(m);
		var q = m.QueueDeclare("", false, true, true, null);

		m.QueueBind(q, "app-logging", "#");
		m.BasicConsume(q, true, consumer);
				
		while (true)
			Console.Write(((BasicDeliverEventArgs) consumer.Queue.Dequeue()).Body.AsUtf8String());
	}
}
// ...
static class Extensions {
	public static string AsUtf8String(this byte[] args) {
		return Encoding.UTF8.GetString(args);
	}
}
```

It should be noted that the message's IBasicProperties' following properties are also set:

 * **ContentEncoding** - to "utf8"
 * **ContentType** - default is "text/plain".  Can be overridden in the MessageProperties.
 * **AppId** - to `loggingEvent.Domain`. Can be overridden in the MessageProperties.
 * **Timestamp** - to `new AmqpTimestamp(Convert.ToInt64((loggingEvent.TimeStamp - _Epoch).TotalSeconds))` where _Epoch is 1/1/1970 at 00:00. Hence, it's the unix timestamp of when the log event happened in the application, according to that application's clock.

Furthermore, if ExtendedData (default false) is set to true (`<extendedData value="true" />`), these headers are set:

 * `Headers["ClassName"]` - to the name of the class performing the logging
 * `Headers["FileName"]` - to the name of the file where the logger resides
 * `Headers["MethodName"]` - to the name of the method performing the logging
 * `Headers["LineNumber"]` - to the line number of the code performing the logging
 
## Final Remarks

Report issues at this repository's **Issues** page.
 
E-mail feedback to henrik at haf dot se or send me a pm over github.

Cheers,
Henrik Feldt
