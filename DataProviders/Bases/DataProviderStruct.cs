using System;
using System.Collections.Generic;
using System.Text;

namespace Wokhan.Data.Providers.Bases
{
    public class DataProviderStruct
    {
        public string Copyright { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string IconPath { get; set; }
        public Type Type { get; set; }

        public bool IsExternal { get; set; }
    }
}
