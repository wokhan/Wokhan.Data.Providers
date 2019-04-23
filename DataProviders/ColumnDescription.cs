using System;

namespace Wokhan.Data.Providers.Bases
{
    public class ColumnDescription
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public Type Type { get; set; }
        public string Category { get; set; }
    }
}
