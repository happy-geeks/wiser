using System;
using System.IO;
using System.Reflection;

namespace Api.Core.Helpers
{
    /// <summary>
    /// Class that contains Type Helpers
    /// </summary>
    public class TypeHelpers
    {
        /// <summary>
        /// Checks if a given type is numeric or not
        /// </summary>
        /// <param name="type">Type what needs to be checked</param>
        public static bool IsNumericType(Type type)
        {
            return CheckIsNumericType(Nullable.GetUnderlyingType(type)) || CheckIsNumericType(type);
        }

        /// <summary>
        /// Load plugins for Wiser. This will load all DLL files in the given directory.
        /// This method should be called during the startup of the application, before registering the services.
        /// </summary>
        /// <remarks>
        /// This function does not do any checks on the DLL files. It will try to load all DLL files in the given directory.
        /// This is meant for plugins that are developed by Wiser developers, so we know they are safe to load.
        /// If we want to be able to load external plugins in the future,
        /// we will need to extend this function with some security checks and load the plugins in isolated contexts.
        /// </remarks>
        /// <param name="pluginsDirectory">The directory that contains the plugins.</param>
        public static void LoadPlugins(string pluginsDirectory)
        {
            if (!Directory.Exists(pluginsDirectory))
            {
                // Handle the case when the Plugins directory does not exist
                return;
            }

            var dllFiles = Directory.GetFiles(pluginsDirectory, "*.dll");

            foreach (var dllFile in dllFiles)
            {
                try
                {
                    Assembly.LoadFrom(dllFile);
                }
                catch (Exception ex)
                {
                    // Handle assembly loading exceptions
                    Console.WriteLine($"Error loading plugin assembly: {ex}");
                }
            }
        }

        private static bool CheckIsNumericType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }
}