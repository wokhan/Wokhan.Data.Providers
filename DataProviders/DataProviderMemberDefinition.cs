using System;
using System.Collections.Generic;
using System.ComponentModel;
using Wokhan.Data.Providers.Attributes;
using Wokhan.Data.Providers.Contracts;

namespace Wokhan.Data.Providers.Bases
{
    public class DataProviderMemberDefinition : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Type MemberType { get; set; }
        public bool IsFile { get; set; }
        public string FileFilter { get; set; }
        public ProviderParameterAttribute.MethodDel ValuesGetter { get; set; }
        public bool HasValuesGetter { get { return ValuesGetter != null; } }
        public Dictionary<string, string> Values { get { return ValuesGetter(); } }
        public string ExclusionGroup { get; set; }

        public object ValueWrapper
        {
            get => Container.Type.GetProperty(Name).GetValue(Container);
            set { Container.Type.GetProperty(Name).SetValue(Container, value); NotifyPropertyChanged(nameof(ValueWrapper)); }
        }

        public int Position { get; set; }

        public IDataProvider Container { get; internal set; }
        public bool IsActive { get; internal set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
