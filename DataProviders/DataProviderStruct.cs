using System;

namespace Wokhan.Data.Providers.Bases
{
    public class DataProviderStruct
    {
        public string Copyright { get; internal set; }
        public string Name { get; internal set; }
        public string Category { get; internal set; }
        public string Description { get; internal set; }
        public string IconPath { get; internal set; }
        public Type Type { get; internal set; }
        public bool IsDirectlyBindable { get; internal set; }
        public bool IsExternal { get; internal set; }
    }
}
