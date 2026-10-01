using System.IO;
using System.Linq;
using Moqui.Core.Data;
using Moqui.Unity.Data;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Moqui.Unity.Editor
{
    /// <summary>Tools/build.ps1의 진입점 (tech/verification.md §4).</summary>
    public static class BuildScript
    {
        private const string ProductFileName = "Moqui.exe";
        private const string WindowsFolder = "Windows";
        private const string BuildsFolder = "Builds";

        /// <summary>
        /// 프로젝트에서 제거할 수 없는 패키지 내부 경고 (plan/decisions.md D-024). 이 밖의 빌드 경고는 실패로 처리한다.
        /// </summary>
        private static readonly string[] KnownExternalWarnings =
        {
            "Shader 'Hidden/Core/DebugOccluder': All SubShaders were stripped",
            "Shader 'Hidden/Core/DebugOcclusionTest': All SubShaders were stripped",
        };

        public static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        public static string WindowsOutputDirectory => Path.Combine(ProjectRoot, BuildsFolder, WindowsFolder);

        [MenuItem("Moqui/Build Standalone (Windows)")]
        public static void BuildStandalone()
        {
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0)
            {
                throw new BuildFailedException("No enabled scenes in build settings.");
            }

            string outputPath = Path.Combine(WindowsOutputDirectory, ProductFileName);
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.StrictMode,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            int unexpectedWarnings = LogReportMessages(report);
            Debug.Log($"[BuildScript] result={summary.result}, errors={summary.totalErrors}, warnings={summary.totalWarnings} (unexpected={unexpectedWarnings}), size={summary.totalSize}, output={outputPath}");
            if (summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Build failed: {summary.result}, errors={summary.totalErrors}");
            }

            if (unexpectedWarnings > 0)
            {
                throw new BuildFailedException($"Build produced {unexpectedWarnings} unexpected warning(s). Fix them or record them in D-024.");
            }

            VerifyBuiltData(outputPath);
        }

        /// <summary>경고와 에러를 로그에 남기고, 허용 목록에 없는 경고 수를 돌려준다.</summary>
        private static int LogReportMessages(BuildReport report)
        {
            int unexpected = 0;
            foreach (BuildStep step in report.steps)
            {
                foreach (BuildStepMessage message in step.messages)
                {
                    if (message.type == LogType.Log)
                    {
                        continue;
                    }

                    bool known = message.type == LogType.Warning
                        && KnownExternalWarnings.Any(known => message.content.Contains(known));
                    if (message.type == LogType.Warning && !known)
                    {
                        unexpected++;
                    }

                    string label = known ? "Known warning" : message.type.ToString();
                    Debug.Log($"[BuildScript] {label} in '{step.name}': {message.content}");
                }
            }

            return unexpected;
        }

        private static void VerifyBuiltData(string exePath)
        {
            string dataFolder = Path.Combine(Path.GetDirectoryName(exePath), Path.GetFileNameWithoutExtension(exePath) + "_Data");
            string builtTuning = Path.Combine(dataFolder, "StreamingAssets", UnityDataSource.DataFolderName, TuningLoader.FilePath);
            if (!File.Exists(builtTuning))
            {
                throw new BuildFailedException($"Built player is missing data: {builtTuning}");
            }
        }
    }
}
