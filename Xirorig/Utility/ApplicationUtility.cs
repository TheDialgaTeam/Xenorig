using System.Reflection;
using System.Runtime.Versioning;

namespace Xirorig.Utility
{
    internal static class ApplicationUtility
    {
        public static string Name => "Xirorig";

        public static string Version { get; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";

        public static string FrameworkVersion { get; } = Assembly.GetExecutingAssembly().GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ?? "";
    }
}