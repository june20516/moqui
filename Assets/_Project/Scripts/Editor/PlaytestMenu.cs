using System.IO;
using Moqui.Core.Data;
using Moqui.Unity.Data;
using UnityEditor;
using UnityEngine;

namespace Moqui.Unity.Editor
{
    /// <summary>
    /// 플레이 검증 확정 도구 (D-065, plan/gulf-improvements.md §11). playtest.json의 값을 정본(data/tuning.json·spec/tuning.md)에 옮긴다.
    /// 옮긴 뒤에는 Tools/run-tests.ps1(문서·json 일치, 봇 회귀)로 확인하고 커밋한다.
    /// </summary>
    public static class PlaytestMenu
    {
        private const string MenuRoot = "Moqui/Playtest/";

        private static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        [MenuItem(MenuRoot + "정본으로 올리기")]
        public static void PromoteMenu()
        {
            var store = new PlaytestStore();
            var overrides = store.Load();
            if (overrides.Count == 0)
            {
                EditorUtility.DisplayDialog("플레이 검증", $"덮어쓴 값이 없습니다.\n{store.OverridesPath}", "확인");
                return;
            }

            string summary = string.Join("\n", System.Linq.Enumerable.Select(overrides.Values, pair => $"{pair.Key} = {PlaytestOverrides.Format(pair.Value)}"));
            if (!EditorUtility.DisplayDialog("정본으로 올리기", $"tuning.json과 spec/tuning.md를 고칩니다.\n\n{summary}", "올리기", "취소"))
            {
                return;
            }

            Promote(overrides, RepoRoot);
            store.Save(new PlaytestOverrides());
            Debug.Log($"[Playtest] promoted {overrides.Count} values; run Tools/run-tests.ps1 and commit.\n{summary}");
        }

        /// <summary>정본 두 파일을 고친다. 목록에 없는 키가 있으면 아무것도 고치지 않고 예외.</summary>
        public static void Promote(PlaytestOverrides overrides, string repoRoot)
        {
            string tuningPath = Path.Combine(repoRoot, "data", TuningLoader.FilePath);
            string specPath = Path.Combine(repoRoot, "spec", "tuning.md");
            string json = File.ReadAllText(tuningPath);
            string spec = File.ReadAllText(specPath);
            var catalog = PlaytestKeyCatalog.Parse(File.ReadAllText(Path.Combine(repoRoot, "data", PlaytestKeyCatalog.FilePath)), TuningLoader.Parse(json));
            foreach (var pair in overrides.Values)
            {
                if (!catalog.TryGet(pair.Key, out _))
                {
                    throw new DataFormatException($"{pair.Key}: not a playtest key");
                }

                json = TuningPromotion.ReplaceInTuningJson(json, pair.Key, pair.Value);
                spec = TuningPromotion.ReplaceInSpec(spec, pair.Key, pair.Value);
            }

            File.WriteAllText(tuningPath, json);
            File.WriteAllText(specPath, spec);
        }

        [MenuItem(MenuRoot + "덮어쓰기 지우기")]
        public static void ClearMenu()
        {
            new PlaytestStore().Save(new PlaytestOverrides());
            Debug.Log("[Playtest] overrides cleared.");
        }

        [MenuItem(MenuRoot + "파일 폴더 열기")]
        public static void RevealMenu()
        {
            EditorUtility.RevealInFinder(new PlaytestStore().OverridesPath);
        }
    }
}
