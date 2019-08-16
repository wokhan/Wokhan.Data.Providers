using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Wokhan.Data.Providers.Attributes;
using Wokhan.Data.Providers.Bases;
using Wokhan.Data.Providers.Contracts;

namespace Wokhan.Data.Providers
{
    public class DataProviders
    {
        static DataProviders()
        {
            AllProviders = FromTypes(false, Assembly.GetExecutingAssembly().GetTypes()).ToList();
        }

        public static List<IGrouping<string, DataProviderMemberStruct>> GetParameters(IDataProvider prv)
        {
            var tr = prv.Type.GetProperties();
            return tr.Where(t => t.GetCustomAttributes<ProviderParameterAttribute>(true).Any())
                    .Select(p =>
                    {
                        var attr = p.GetCustomAttribute<ProviderParameterAttribute>(true);
                        return new
                        {
                            Pos = attr.Position,
                            Prov = new DataProviderMemberStruct()
                            {
                                IsActive = prv.SelectedGroups.Contains(attr.ExclusionGroup),
                                Container = prv,
                                Name = p.Name,
                                Description = attr.Description,
                                MemberType = p.PropertyType,
                                IsFile = attr.IsFile,
                                FileFilter = attr.FileFilter,
                                ExclusionGroup = attr.ExclusionGroup,
                                ValuesGetter = attr.Method
                            }
                        };
                    })
                    .Where(t => t.Prov.Description != null)
                    .OrderBy(t => t.Pos)
                    .ThenBy(t => t.Prov.Description)
                    .Select(t => t.Prov)
                    .GroupBy(c => c.ExclusionGroup ?? null)
                    .ToList();
        }

        public static IDataProvider CreateInstance(string provider, Dictionary<string, object> parameters)
        {
            var tp = AllProviders.FirstOrDefault(a => a.Name == provider);
            if (tp == null)
            {
                throw new Exception($"Unable to find provider '{provider}'. Please check your configuration.");
            }

            var ret = (IDataProvider)Activator.CreateInstance(tp.Type);
            var provprm = GetParameters(ret);
            foreach (var parameter in parameters)
            {
                var prov = provprm.SelectMany(p => p).SingleOrDefault(p => p.Name == parameter.Key);
                if (prov != null)
                {
                    try
                    {
                        prov.ValueWrapper = Convert.ChangeType(parameter.Value, prov.MemberType);
                    }
                    catch { }
                }
            }

            return ret;
        }

        public static void AddPath(string basePath)
        {
            if (!Directory.Exists(basePath))
            {
                throw new DirectoryNotFoundException(basePath);
            }
            else
            {
                var loadContext = new DataProviderLoadContext(basePath);
                var assemblies = Directory.GetFiles(basePath, "*.dll")
                                        .Select(f =>
                                        {
                                            try
                                            {
                                                return loadContext.LoadFromAssemblyPath(f);
                                            }
                                            catch (Exception e)
                                            {
                                                return null;
                                            }
                                        })
                                        .Where(_ => _ != null)
                                        .ToArray();

                AllProviders.AddRange(FromTypes(true, assemblies.SelectMany(a => a.GetTypes()).ToArray()));
            }
        }

        public static void AddAssemblies(params Assembly[] assemblies)
        {
            AllProviders.AddRange(FromTypes(false, assemblies.SelectMany(a => a.GetTypes()).ToArray()));
        }

        public static void AddTypes(params Type[] types)
        {
            var failed = types.FirstOrDefault(t => !t.IsClass || !typeof(IExposedDataProvider).IsAssignableFrom(t));
            if (failed != null)
            {
                throw new ArgumentException($"{failed.Name} type doesn't inherit from {nameof(AbstractDataProvider)} or doesn't implement {nameof(IExposedDataProvider)}. Cannot continue.");
            }
            AllProviders.AddRange(FromTypes(false, types));
        }

        private static DataProviderStruct[] FromTypes(bool external, params Type[] types)
        {
            return types.Where(t => t.IsClass && typeof(IExposedDataProvider).IsAssignableFrom(t))
                   .Select(t => new { Type = t, Attributes = t.GetCustomAttributes<DataProviderAttribute>(true).SingleOrDefault() })
                   .Select(t => new DataProviderStruct()
                   {
                       IsExternal = external,
                       Description = t.Attributes.Description,
                       Name = t.Attributes.Name,
                       Category = t.Attributes.Category,
                       Copyright = t.Attributes.Copyright,
                       IconPath = t.Attributes.Icon,
                       Type = t.Type,
                       IsDirectlyBindable = t.Attributes.IsDirectlyBindable
                   })
                   .ToArray();
        }

        public static List<DataProviderStruct> AllProviders { get; private set; }
    }
}
