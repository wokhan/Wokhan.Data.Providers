using System;
using System.Collections.Generic;
using System.ComponentModel;

using Wokhan.Data.Providers.Attributes;
using Wokhan.Data.Providers.Contracts;

namespace Wokhan.Data.Providers.Bases
{
    /// <summary>
    /// Defines a data provider property definition (that is, a class representing metadata for a provider property).
    /// Can be used as a ViewModel in a MVVM approach to dynamically retrieve / update properties for a Data Provider instance.
    /// </summary>
    public class DataProviderMemberDefinition : INotifyPropertyChanged
    {
        /// <summary>
        /// The property name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The property description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The property type.
        /// </summary>
        public Type MemberType { get; set; }

        /// <summary>
        /// Specifies if the property represents a file (in case a file picker is needed).
        /// </summary>
        public bool IsFile { get; set; }

        /// <summary>
        /// Specifies a file name filter (should obviously be used only when <see cref="IsFile"/> is true).
        /// </summary>
        public string FileFilter { get; set; }

        /// <summary>
        /// References a <see cref="ProviderParameterAttribute.ValueGetterDelegate"/> delegate to retrieve possible values for the current property.
        /// </summary>
        public ProviderParameterAttribute.ValueGetterDelegate? ValuesGetter { get; set; }

        /// <summary>
        /// Check if the property actually offers a method to get possible values from.
        /// </summary>
        public bool HasValuesGetter => ValuesGetter != null;

        /// <summary>
        /// Calls the method to retrieves values and returns them as a dictionary.
        /// </summary>
        public Dictionary<string, string> Values => ValuesGetter();

        /// <summary>
        /// An exclusion group is used to group properties that defines similar configuration items.
        /// For instance, one could have a "ConnectionString" exclusion group, with only one property directly containing the said connection string, 
        /// and then a "Details" goup, containing separated values for the connection string, allowing to specify them independently.
        /// </summary>
        public string? ExclusionGroup { get; set; }

        /// <summary>
        /// The ValueWrapper is an accessor allowing to set the underlying value and notify any observer about the update.
        /// </summary>
        public object ValueWrapper
        {
            get => Container.Type.GetProperty(Name).GetValue(Container);
            set
            {
                var prop = Container.Type.GetProperty(Name);
                prop.SetValue(Container, Convert.ChangeType(value, prop.PropertyType));
                NotifyPropertyChanged(nameof(ValueWrapper));
            }
        }

        /// <summary>
        /// Position of the parameter (to order them in a UI).
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// The parent <see cref="IDataProvider"/>.
        /// </summary>
        public IDataProvider Container { get; internal set; }

        /// <summary>
        /// Specifies if the parameter is actually used (i.e. if it's part of the selected <see cref="ExclusionGroup"/>).
        /// </summary>
        public bool IsActive { get; internal set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
