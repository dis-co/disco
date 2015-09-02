using Iris.Core.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Iris.Core.Logging
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Level
    {
        Debug,
        Info,
        Warn,
        Error,
        Fatal
    }

    public class LogEntry
    {
        public Level  LogLevel { get; set; }
        public IrisId ID       { get; set; }
        public IrisIP IP       { get; set; }
        public Roles  Role     { get; set; }
        public string Tag      { get; set; }
        public string Message  { get; set; }

        [JsonConstructor]
        private LogEntry()
        {

        }

        private LogEntry(Level level, string tag, string msg)
        {
            LogLevel = level;
            ID = Host.HostId;
            IP = Host.IP;
            Role = Host.Role;
            Tag = tag;
            Message = msg;

            var payload = JsonConvert.SerializeObject(this);

            switch(level)
            {
                case Level.Debug:
                    Log.Debug(payload);
                    break;
                case Level.Info:
                    Log.Info(payload);
                    break;
                case Level.Warn:
                    Log.Warn(payload);
                    break;
                case Level.Error:
                    Log.Error(payload);
                    break;
                case Level.Fatal:
                    Log.Fatal(payload);
                    break;
            }
        }

        public static LogEntry Debug(string tag, string msg)
        {
            return new LogEntry(Level.Debug, tag, msg);
        } 

        public static LogEntry Info(string tag, string msg)
        {
            return new LogEntry(Level.Info, tag, msg);
        } 

        public static LogEntry Warn(string tag, string msg)
        {
            return new LogEntry(Level.Warn, tag, msg);
        } 

        public static LogEntry Error(string tag, string msg)
        {
            return new LogEntry(Level.Error, tag, msg);
        } 

        public static LogEntry Fatal(string tag, string msg)
        {
            return new LogEntry(Level.Fatal, tag, msg);
        } 
    }
}
