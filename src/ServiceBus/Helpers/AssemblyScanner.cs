using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ServiceBus.Helpers
{
    class AssemblyScanner
    {
        private List<Assembly> _scannabAssemblies;

        static string[] FileSearchPatternsToUse =
        {
            "*.dll",
            "*.exe"
        };

        public AssemblyScanner(string basePath)
        {
            _scannabAssemblies = new List<Assembly>();

            foreach (var assemblyFile in ScanDirectoryForAssemblyFiles(basePath))
            {
                if (TryLoadScannableAssembly(assemblyFile.FullName, out var assembly))
                {
                    _scannabAssemblies.Add(assembly);
                }
            }
        }

        public List<Type> GetMessageTypes()
        {
            var types = new List<Type>();
            foreach (var assembly in _scannabAssemblies)
            {
                types.AddRange(GetMessageTypes(assembly));
            }

            return types;
        }

        private IEnumerable<Type> GetMessageTypes(Assembly assembly)
        {
            var allCommands =
                from type in assembly.GetTypes()
                where !type.IsAbstract
                && !string.IsNullOrEmpty(type.Namespace)
                && type.Namespace.Contains("Messages.Commands")
                select type;

            return allCommands?.ToList();
        }

        public List<Type> GetDispatchableHandlers()
        {
            var types = new List<Type>();
            foreach (var assembly in _scannabAssemblies)
            {
                types.AddRange(GetHandlers(assembly));
            }

            return types;
        }

        private static IEnumerable<Type> GetHandlers(Assembly assembly)
        {
            var allMessageHandlers =
                from type in assembly.GetTypes()
                where !type.IsAbstract
                from interfaceType in type.GetInterfaces()
                where interfaceType.IsGenericType
                where interfaceType.GetGenericTypeDefinition() == typeof(IHandleMessages<>)
                select type;

            return allMessageHandlers.ToList();
        }

        static List<FileInfo> ScanDirectoryForAssemblyFiles(string directoryToScan)
        {
            var fileInfo = new List<FileInfo>();
            var baseDir = new DirectoryInfo(directoryToScan);

            foreach (var searchPattern in FileSearchPatternsToUse)
            {
                foreach (var info in baseDir.GetFiles(searchPattern, SearchOption.AllDirectories))
                {
                    fileInfo.Add(info);
                }
            }

            return fileInfo;
        }

        bool TryLoadScannableAssembly(string assemblyPath, out Assembly assembly)
        {
            assembly = null;

            // Very simplistic scanner, will run into zillions of issues.
            try
            {
                assembly = Assembly.LoadFrom(assemblyPath);

                return true;
            }
            catch (Exception ex) when (ex is BadImageFormatException || ex is FileLoadException)
            {
                return false;
            }
        }
    }
}
