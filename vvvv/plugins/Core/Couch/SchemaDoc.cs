using System;

namespace Iris.Core.Couch
{
    public class SchemaDoc
    {
        public string _id     { get; set; }
        public string _rev    { get; set; }
        public int    Version { get; set; }

        public SchemaDoc()
        {
            _id = "schema";
        }

        public override bool Equals(object obj)
        {
            return obj.Equals(Version);
        }

        public override Int32 GetHashCode()
        {
            return Version.GetHashCode();
        }

        public static bool operator >(SchemaDoc s1, SchemaDoc s2)
        {
            return s1.Version > s2.Version;
        }

        public static bool operator >=(SchemaDoc s1, SchemaDoc s2)
        {
            return s1.Version >= s2.Version;
        }

        public static bool operator <(SchemaDoc s1, SchemaDoc s2)
        {
            return s1.Version < s2.Version;
        }

        public static bool operator <=(SchemaDoc s1, SchemaDoc s2)
        {
            return s1.Version <= s2.Version;
        }

        public static bool operator >(SchemaDoc s1, int s2)
        {
            return s1.Version > s2;
        }

        public static bool operator >=(SchemaDoc s1, int s2)
        {
            return s1.Version >= s2;
        }

        public static bool operator <(SchemaDoc s1, int s2)
        {
            return s1.Version < s2;
        }

        public static bool operator <=(SchemaDoc s1, int s2)
        {
            return s1.Version <= s2;
        }

        public static bool operator ==(SchemaDoc s1, SchemaDoc s2)
        {
            return s1.Version == s2.Version;
        }

        public static bool operator !=(SchemaDoc s1, SchemaDoc s2)
        {
            return s1 != s2;
        }

        public static bool operator ==(SchemaDoc s1, int s2)
        {
            return s1.Version == s2;
        }

        public static bool operator !=(SchemaDoc s1, int s2)
        {
            return s1 != s2;
        }

        public bool ShouldSerialize_id()
        {
            return _id != null;
        }

        public bool ShouldSerialize_rev()
        {
            return _rev != null;
        }
    }
}