using System.IO;
using Moqui.Core.Data;
using UnityEngine;

namespace Moqui.Unity.Data
{
    /// <summary>
    /// IDataSource의 Unity 구현 (tech/architecture.md §4.7).
    /// 에디터에서는 저장소의 data/를 직접 읽고, 플레이어 빌드에서는 빌드 전처리가 복사한 StreamingAssets/data/를 읽는다.
    /// </summary>
    public sealed class UnityDataSource : IDataSource
    {
        public const string DataFolderName = "data";

        public UnityDataSource()
            : this(DefaultRoot)
        {
        }

        public UnityDataSource(string root)
        {
            Root = root;
        }

        public static string DefaultRoot => Application.isEditor ? RepoDataRoot : StreamingDataRoot;

        /// <summary>저장소 루트의 data/ (Assets의 상위 폴더).</summary>
        public static string RepoDataRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", DataFolderName));

        public static string StreamingDataRoot => Path.Combine(Application.streamingAssetsPath, DataFolderName);

        public string Root { get; }

        public string ReadText(string relativePath)
        {
            return File.ReadAllText(Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
