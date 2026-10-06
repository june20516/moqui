using System;
using Moqui.Core.Data;
using Moqui.Core.Data.Levels;
using Moqui.Core.Meta;
using Moqui.Core.Simulation;
using Moqui.Unity.Data;
using Moqui.Unity.Settings;

namespace Moqui.Unity.UI.Flow
{
    /// <summary>스테이지가 끝났을 때 Result 화면이 보여 줄 내용 (spec/08 Result).</summary>
    public sealed class StageOutcomeInfo
    {
        public string LevelId { get; set; }

        public StageOutcome Outcome { get; set; }

        public DeathCause? DeathCause { get; set; }

        public float Seconds { get; set; }

        /// <summary>클리어했을 때만. 실패면 null.</summary>
        public RewardBreakdown Reward { get; set; }

        /// <summary>마지막 스테이지 첫 클리어 → Ending으로 간다.</summary>
        public bool UnlocksEnding { get; set; }
    }

    /// <summary>
    /// 씬을 넘나드는 게임 상태: 기본 tuning, 저장 데이터, 사용자 설정, 선택한 스테이지, 마지막 결과.
    /// 실행 중에는 하나만 있고(Ensure), 테스트는 메모리 저장소로 만든 세션으로 바꿔 끼운다(Replace).
    /// </summary>
    public sealed class GameSession
    {
        public GameSession(Tuning tuning, SaveStore store, IPreferenceStore preferences, StageCatalog catalog)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            BaseTuning = tuning;
            Store = store;
            Save = store.Load();
            Preferences = preferences;
            Settings = new UserSettings(preferences);
            Shop = new SkillShop(tuning);
            Rewards = new RewardCalculator(tuning);
        }

        /// <summary>스테이지 목록 (data/stages.json, D-061).</summary>
        public StageCatalog Catalog { get; }

        public static GameSession Current { get; private set; }

        public Tuning BaseTuning { get; }

        public SaveStore Store { get; }

        public SaveData Save { get; private set; }

        public IPreferenceStore Preferences { get; }

        public UserSettings Settings { get; }

        public SkillShop Shop { get; }

        public RewardCalculator Rewards { get; }

        public string SelectedLevelId { get; set; }

        public StageOutcomeInfo LastOutcome { get; set; }

        /// <summary>StageSelect에 들어가며 Skills 패널을 바로 열지 (Result → Skills).</summary>
        public bool OpenSkillsOnStageSelect { get; set; }

        public static GameSession Ensure()
        {
            if (Current == null)
            {
                var source = new UnityDataSource();
                Current = new GameSession(TuningLoader.Load(source), new SaveStore(new FileSaveStorage()), new PlayerPrefsStore(), StageCatalogs.Repo);
                Current.Settings.ApplyToEngine();
            }

            return Current;
        }

        public static void Replace(GameSession session)
        {
            Current = session;
        }

        public void Persist()
        {
            Store.Save(Save);
        }

        public void ResetData()
        {
            Save = Store.Reset();
        }
    }
}
