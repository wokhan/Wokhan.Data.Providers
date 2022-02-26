using System;
using System.Reflection;
using System.Runtime.Loader;

namespace Wokhan.Data.Providers
{
    /// <summary>
    /// Allows to resolve and load assemblies from a custom path.
    /// </summary>
    internal class DataProviderLoadContext : AssemblyLoadContext
    {
#if !__NETSTANDARD20__
        private AssemblyDependencyResolver _resolver;
#endif

        internal DataProviderLoadContext(string pluginPath)
        {
#if !__NETSTANDARD20__
            _resolver = new AssemblyDependencyResolver(pluginPath);
#endif
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
#if __NETSTANDARD20__
            return LoadFromAssemblyName(assemblyName);
#else
            string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
#endif
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
#if __NETSTANDARD20__
            return LoadUnmanagedDll(unmanagedDllName);
#else
            string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
#endif
        }
    }
}
