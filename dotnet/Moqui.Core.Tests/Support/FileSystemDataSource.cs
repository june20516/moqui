using System.IO;
using Moqui.Core.Data;

namespace Moqui.Core.Tests.Support
{
    /// <summary>IDataSource 테스트 구현 (tech/architecture.md §4.7).</summary>
    public sealed class FileSystemDataSource : IDataSource
    {
        private readonly string _root;

        public FileSystemDataSource(string root)
        {
            _root = root;
        }

        public static FileSystemDataSource ForRepoData()
        {
            return new FileSystemDataSource(RepoPaths.Data);
        }

        public string ReadText(string relativePath)
        {
            return File.ReadAllText(Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
