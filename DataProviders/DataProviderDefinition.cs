using System;
using Wokhan.Data.Providers.Attributes;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace Wokhan.Data.Providers.Bases
{
    public class DataProviderDefinition
    {
        public string Copyright { get; private set; }
        public string Name { get; private set; }
        public string Category { get; private set; }
        public string Description { get; private set; }
        public string IconPath { get; private set; }
        public Type Type { get; private set; }
        public bool IsDirectlyBindable { get; private set; }
        public bool IsExternal { get; private set; }

        internal static DataProviderDefinition From(Type t, bool external)
        {
            var attr = t.GetCustomAttributes<DataProviderAttribute>(true).SingleOrDefault();
            if (attr == null)
            {
                return null;
            }
            return new DataProviderDefinition
            {
                IsExternal = external,
                Description = attr.Description,
                Name = attr.Name,
                Category = attr.Category,
                Copyright = attr.Copyright,
                IconPath = attr.Icon,
                Type = t,
                IsDirectlyBindable = attr.IsDirectlyBindable
            };
        }
    }
}
