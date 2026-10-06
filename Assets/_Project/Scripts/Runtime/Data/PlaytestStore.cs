using System;
using System.Collections.Generic;
using System.IO;
using Moqui.Core.Data;
using UnityEngine;

namespace Moqui.Unity.Data
{
    /// <summary>
    /// 플레이 검증 파일 (plan/gulf-improvements.md §11, D-065): 덮어쓰기 playtest.json, 기록 playtest-log.jsonl, 프리셋 playtest-A/B.json.
    /// 위치는 저장 데이터와 같은 폴더(Application.persistentDataPath)다. 정본 data/는 건드리지 않는다.
    /// </summary>
    public sealed class PlaytestStore
    {
        public static readonly string[] PresetSlots = { "A", "B" };

        public PlaytestStore()
            : this(DirectoryOverride ?? Application.persistentDataPath)
        {
        }

        /// <summary>테스트가 쓰는 임시 폴더. null이면 저장 데이터 폴더.</summary>
        public static string DirectoryOverride { get; set; }

        public PlaytestStore(string directory)
        {
            Directory = directory;
        }

        public string Directory { get; }

        public string OverridesPath => Path.Combine(Directory, PlaytestOverrides.FileName);

        public string LogPath => Path.Combine(Directory, PlaytestLog.FileName);

        /// <summary>
        /// 덮어쓰기를 쓰는가: 배치 모드(자동 테스트·캡처·성능 측정)에서는 쓰지 않아 결과가 개발자 PC의 파일에 흔들리지 않는다.
        /// </summary>
        public static bool IsActive => ActiveOverride ?? (IsAvailable && !Application.isBatchMode);

        /// <summary>
        /// 플레이 검증 도구를 쓸 수 있는 빌드인가: 에디터, 개발 빌드, 또는 실행 인자 -playtest.
        /// 출시 빌드에서 저장 폴더의 파일이 게임 수치를 바꾸지 않게 한다.
        /// </summary>
        public static bool IsAvailable => Application.isEditor || Debug.isDebugBuild || HasPlaytestArgument();

        public const string PlaytestArgument = "-playtest";

        private static bool HasPlaytestArgument()
        {
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (string.Equals(argument, PlaytestArgument, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>테스트가 켜고 끄는 재정의. null이면 배치 모드 여부를 따른다.</summary>
        public static bool? ActiveOverride { get; set; }

        /// <summary>마지막 읽기에서 난 오류 (파일이 깨졌을 때). 없으면 null.</summary>
        public string LastError { get; private set; }

        public PlaytestOverrides Load()
        {
            return Read(OverridesPath);
        }

        /// <summary>저장한다. 실패하면 이유(파일 잠김·권한)를 돌려주고 LastError에도 남긴다. 성공하면 null.</summary>
        public string Save(PlaytestOverrides overrides)
        {
            return Write(OverridesPath, overrides);
        }

        /// <summary>기록 한 줄을 덧붙인다. 실패하면 이유, 성공(또는 줄 없음)이면 null.</summary>
        public string AppendLog(string line)
        {
            if (line == null)
            {
                return null;
            }

            return Guard(() =>
            {
                System.IO.Directory.CreateDirectory(Directory);
                File.AppendAllText(LogPath, line + "\n");
            });
        }

        public string PresetPath(string slot)
        {
            return Path.Combine(Directory, $"playtest-{slot}.json");
        }

        public bool HasPreset(string slot)
        {
            return File.Exists(PresetPath(slot));
        }

        public PlaytestOverrides LoadPreset(string slot)
        {
            return Read(PresetPath(slot));
        }

        public string SavePreset(string slot, PlaytestOverrides overrides)
        {
            return Write(PresetPath(slot), overrides);
        }

        private PlaytestOverrides Read(string path)
        {
            LastError = null;
            if (!File.Exists(path))
            {
                return new PlaytestOverrides();
            }

            try
            {
                return PlaytestOverrides.Parse(File.ReadAllText(path));
            }
            catch (Exception exception) when (exception is DataFormatException || exception is IOException || exception is UnauthorizedAccessException)
            {
                // 깨지거나 잠긴 파일 때문에 게임이 멈추지 않게 덮어쓰기 없이 진행하고, 이유는 패널·로그에 보인다.
                LastError = exception.Message;
                Debug.LogWarning($"[Playtest] {path}: {exception.Message}");
                return new PlaytestOverrides();
            }
        }

        private string Write(string path, PlaytestOverrides overrides)
        {
            return Guard(() =>
            {
                System.IO.Directory.CreateDirectory(Directory);
                File.WriteAllText(path, overrides.ToJson());
            });
        }

        private string Guard(Action action)
        {
            try
            {
                action();
                return null;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                LastError = exception.Message;
                Debug.LogWarning($"[Playtest] {exception.Message}");
                return exception.Message;
            }
        }
    }

    /// <summary>tuning.json 위에 playtest.json을 덮어쓴 결과와, 어떤 키가 적용·거부됐는지 (HUD·패널·로그 표시용).</summary>
    public static class PlaytestTuning
    {
        public static IReadOnlyList<string> Applied { get; private set; } = Array.Empty<string>();

        public static IReadOnlyList<string> Rejected { get; private set; } = Array.Empty<string>();

        /// <summary>덮어쓰기가 켜져 있으면 적용하고, 꺼져 있거나 덮어쓴 것이 없으면 그대로 돌려준다.</summary>
        public static Tuning Apply(Tuning baseTuning, IDataSource source, PlaytestStore store)
        {
            Applied = Array.Empty<string>();
            Rejected = Array.Empty<string>();
            if (!PlaytestStore.IsActive)
            {
                return baseTuning;
            }

            var overrides = store.Load();
            if (overrides.Count == 0)
            {
                return baseTuning;
            }

            var catalog = PlaytestKeyCatalog.Load(source, baseTuning);
            var tuning = overrides.Apply(baseTuning, catalog, out var applied, out var rejected);
            Applied = applied;
            Rejected = rejected;
            Debug.Log($"[Playtest] {store.OverridesPath}: {applied.Count} overridden ({string.Join(", ", applied)})" + (rejected.Count > 0 ? $", rejected: {string.Join("; ", rejected)}" : string.Empty));
            return tuning;
        }
    }
}
