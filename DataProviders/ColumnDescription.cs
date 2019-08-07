using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Wokhan.Data.Providers.Attributes;

namespace Wokhan.Data.Providers.Bases
{
    public class ColumnDescription
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public Type Type { get; set; }
        public string Category { get; set; }

        public static List<ColumnDescription> FromType(Type sourceType)
        {
            return sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p =>
            {
                var details = p.GetCustomAttributes<ColumnDescriptionAttribute>(true).FirstOrDefault();
                return new ColumnDescription
                {
                    Name = p.Name,
                    DisplayName = details?.DisplayName ?? p.Name,
                    Description = details?.Description ?? p.Name,
                    Type = p.PropertyType,
                    Category = details?.Category ?? "Default"
                };
            })
            .ToList();
        }
    }
}
