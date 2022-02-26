using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Wokhan.Data.Providers.Attributes;

namespace Wokhan.Data.Providers.Bases
{
    /// <summary>
    /// Describes a column (property) for a provider (that is, metadata for a property or indexer).
    /// </summary>
    public class ColumnDescription
    {
        /// <summary>
        /// Name of the column (i.e. the property/indexer for an object: can be anything - i.e. [0] for an array would work).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// User frienly display name to use as a column title.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Short description of what the column contains.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Specifies the column data type.
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// A generic string to categorize columns.
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Defines if the column is part of a key (composite or not).
        /// </summary>
        public bool IsKey { get; internal set; }

        /// <summary>
        /// Builds a list of <see cref="ColumnDescription"/> from the specified type, filtering on public properties decorated with the <see cref="ColumnDescriptionAttribute"/> attribute.
        /// </summary>
        /// <param name="sourceType">Type to take properties from.</param>
        /// <returns>List of column descriptors.</returns>
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
                    Category = details?.Category ?? "Default", 
                    IsKey = details?.IsKey ?? false
                };
            })
            .ToList();
        }
    }
}
