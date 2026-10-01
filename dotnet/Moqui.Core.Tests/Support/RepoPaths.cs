using System;
using System.IO;

namespace Moqui.Core.Tests.Support
{
    public static class RepoPaths
    {
        private const string RootMarker = "GOAL.md";

        private static readonly Lazy<string> RootPath = new Lazy<string>(FindRoot);

        public static string Root => RootPath.Value;

        public static string Data => Path.Combine(Root, "data");

        public static string Spec => Path.Combine(Root, "spec");

        private static string FindRoot()
        {
            var directory = new DirectoryInfo(TestContextDirectory());
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, RootMarker)))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException($"Repository root ({RootMarker}) not found above {TestContextDirectory()}.");
        }

        private static string TestContextDirectory()
        {
            return AppContext.BaseDirectory;
        }
    }
}
