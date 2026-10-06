using System;
using System.Collections.Generic;
using Moqui.Core.Data;
using Moqui.Core.Meta;
using Moqui.Unity.Data;
using Moqui.Unity.Settings;
using Moqui.Unity.UI.Flow;

namespace Moqui.Unity.Tests
{
    /// <summary>실제 save.json·PlayerPrefs를 건드리지 않는 메모리 저장소.</summary>
    public sealed class MemorySaveStorage : ISaveStorage
    {
        private readonly Dictionary<string, string> _files = new Dictionary<string, string>();

        public bool Exists(string fileName) => _files.ContainsKey(fileName);

        public string Read(string fileName) => _files[fileName];

        public void Write(string fileName, string text) => _files[fileName] = text;

        public void Move(string fromFileName, string toFileName)
        {
            _files[toFileName] = _files[fromFileName];
            _files.Remove(fromFileName);
        }

        public void Delete(string fileName) => _files.Remove(fileName);
    }

    /// <summary>PlayMode 테스트용 게임 세션.</summary>
    public static class TestSessions
    {
        /// <summary>메모리 저장소 세션으로 바꾼다. 스테이지 목록은 게임 데이터(data/stages.json) 그대로.</summary>
        public static void UseMemorySession()
        {
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            GameSession.Replace(new GameSession(tuning, new SaveStore(new MemorySaveStorage()), new MemoryPreferenceStore(), StageCatalogs.Repo));
        }
    }
}
