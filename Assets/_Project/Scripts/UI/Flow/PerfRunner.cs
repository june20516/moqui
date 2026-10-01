using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using Moqui.Unity.Diagnostics;
using Moqui.Unity.Presentation.Stage;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Moqui.Unity.UI.Flow
{
    /// <summary>
    /// 성능 측정 실행 (M11): 실행 인자 `-moquiPerf <보고서 경로>`가 있으면 Boot에서 Title 대신
    /// 다섯 스테이지를 차례로 열어 워밍업 뒤 프레임 타임을 기록하고, 보고서(Markdown 표)를 쓴 다음 종료한다.
    /// VSync와 프레임 제한을 끄고 재므로 실제 여유를 볼 수 있다. 기준은 tuning `perf.targetFps`.
    /// </summary>
    public sealed class PerfRunner : MonoBehaviour
    {
        public const string Argument = "-moquiPerf";
        public const float WarmupSeconds = 3f;
        public const float MeasureSeconds = 10f;
        public static readonly string[] LevelIds = { "stage01", "stage02", "stage03", "stage04", "stage05" };

        private string _reportPath;

        /// <summary>실행 인자에서 보고서 경로를 찾는다. 없으면 null.</summary>
        public static string ReportPathFrom(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == Argument)
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        /// <summary>성능 측정 인자가 있으면 실행기를 만들고 true.</summary>
        public static bool TryStart(string[] args)
        {
            string reportPath = ReportPathFrom(args);
            if (reportPath == null)
            {
                return false;
            }

            var go = new GameObject(nameof(PerfRunner));
            DontDestroyOnLoad(go);
            go.AddComponent<PerfRunner>()._reportPath = reportPath;
            return true;
        }

        private IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            float targetFps = GameSession.Ensure().BaseTuning.GetFloat("perf.targetFps");
            float budgetMs = FrameStats.BudgetMs(targetFps);

            var report = new StringBuilder();
            report.AppendLine($"# 성능 측정 ({System.DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)})");
            report.AppendLine();
            report.AppendLine($"- 해상도 {Screen.width}×{Screen.height}, GPU {SystemInfo.graphicsDeviceName}, CPU {SystemInfo.processorType}");
            report.AppendLine($"- 목표 {targetFps:0} fps (예산 {budgetMs:0.00} ms), VSync 끔, 스테이지마다 워밍업 {WarmupSeconds:0}초 후 {MeasureSeconds:0}초 측정 (플레이어 정지 호버링)");
            report.AppendLine();
            report.AppendLine("| 스테이지 | 프레임 | 평균 ms | p95 ms | p99 ms | 최대 ms | 예산 초과 |");
            report.AppendLine("|---|---|---|---|---|---|---|");

            foreach (string levelId in LevelIds)
            {
                StageBootstrap.RequestedLevelId = levelId;
                yield return SceneManager.LoadSceneAsync(ScreenId.Stage.ToString(), LoadSceneMode.Single);
                yield return new WaitForSecondsRealtime(WarmupSeconds);

                var stats = new FrameStats();
                float elapsed = 0f;
                while (elapsed < MeasureSeconds)
                {
                    yield return null;
                    float delta = Time.unscaledDeltaTime;
                    elapsed += delta;
                    stats.Add(delta * 1000f);
                }

                report.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "| {0} | {1} | {2:0.00} | {3:0.00} | {4:0.00} | {5:0.00} | {6:0.0}% |",
                    levelId, stats.Count, stats.Average, stats.Percentile(95f), stats.Percentile(99f), stats.Max, stats.OverBudgetRatio(budgetMs) * 100f));
                Debug.Log($"[PerfRunner] {levelId}: avg={stats.Average:0.00}ms p95={stats.Percentile(95f):0.00}ms");
            }

            string directory = Path.GetDirectoryName(Path.GetFullPath(_reportPath));
            Directory.CreateDirectory(directory);
            File.WriteAllText(_reportPath, report.ToString(), new UTF8Encoding(false));
            Debug.Log($"[PerfRunner] report written to {_reportPath}");
            StageBootstrap.RequestedLevelId = StageBootstrap.DefaultLevelId;
            Application.Quit();
        }
    }
}
