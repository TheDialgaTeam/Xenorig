using System.Reflection;
using System.Runtime.Versioning;

namespace Xirorig.Utility
{
    internal static class ApplicationUtility
    {
        public static string Name { get; } = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>()!.Product;

        public static string Version { get; } = Assembly.GetExecutingAssembly().GetName().Version!.ToString();

        public static string FrameworkVersion { get; } = Assembly.GetExecutingAssembly().GetCustomAttribute<TargetFrameworkAttribute>()!.FrameworkName;
    }
}