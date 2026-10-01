using System.IO;
using Moqui.Unity.Data;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Moqui.Unity.Editor
{
    /// <summary>
    /// 빌드 직전에 저장소 data/를 StreamingAssets/data/로 복사한다 (tech/architecture.md §3).
    /// 복사본은 git에서 제외되며 매 빌드마다 새로 만든다.
    /// </summary>
    public sealed class DataSync : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            CopyToStreamingAssets();
        }

        public static void CopyToStreamingAssets()
        {
            string source = UnityDataSource.RepoDataRoot;
            string destination = UnityDataSource.StreamingDataRoot;
            if (!Directory.Exists(source))
            {
                throw new BuildFailedException($"Data folder not found: {source}");
            }

            if (Directory.Exists(destination))
            {
                Directory.Delete(destination, true);
            }

            CopyDirectory(source, destination);
            Debug.Log($"[DataSync] Copied {source} -> {destination}");
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
            }

            foreach (string directory in Directory.GetDirectories(source))
            {
                CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
            }
        }
    }
}
