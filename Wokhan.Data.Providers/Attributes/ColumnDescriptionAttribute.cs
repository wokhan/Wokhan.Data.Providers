using System;

namespace Wokhan.Data.Providers.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnDescriptionAttribute : Attribute
    {
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public bool IsKey { get; set; }
    }
}