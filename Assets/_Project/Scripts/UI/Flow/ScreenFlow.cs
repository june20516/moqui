using Moqui.Core.Meta;
using Moqui.Core.Simulation;
using Moqui.Unity.Presentation.Stage;
using UnityEngine.SceneManagement;

namespace Moqui.Unity.UI.Flow
{
    /// <summary>씬 전환 포트. 실행에서는 씬을 불러오고, 테스트에서는 요청만 기록한다.</summary>
    public interface ISceneNavigator
    {
        void Load(ScreenId screen);

        void Quit();
    }

    public sealed class SceneNavigator : ISceneNavigator
    {
        public void Load(ScreenId screen)
        {
            SceneManager.LoadScene(screen.ToString());
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }
    }

    /// <summary>
    /// 화면 흐름 (spec/08): Title → StageSelect → Stage → Result → (Skills | Retry | StageSelect), Stage 5 첫 클리어 → Ending → Title.
    /// 버튼 처리는 모두 여기를 거치므로 테스트에서 버튼 이벤트 대신 직접 호출해 전이를 검증한다.
    /// </summary>
    public sealed class ScreenFlow
    {
        private readonly GameSession _session;
        private readonly ISceneNavigator _navigator;

        public ScreenFlow(GameSession session, ISceneNavigator navigator)
        {
            _session = session;
            _navigator = navigator;
        }

        public GameSession Session => _session;

        public void OpenTitle() => _navigator.Load(ScreenId.Title);

        public void OpenStageSelect(bool openSkills = false)
        {
            _session.OpenSkillsOnStageSelect = openSkills;
            _navigator.Load(ScreenId.StageSelect);
        }

        public bool CanStart(int stageNumber)
        {
            return _session.Save.IsUnlocked(stageNumber) && _session.LevelAvailable(stageNumber);
        }

        /// <summary>잠긴 스테이지나 데이터가 없는 스테이지는 시작하지 않는다.</summary>
        public bool StartStage(int stageNumber)
        {
            if (!CanStart(stageNumber))
            {
                return false;
            }

            _session.SelectedLevelId = RewardCalculator.LevelId(stageNumber);
            LoadStage();
            return true;
        }

        public void Retry()
        {
            LoadStage();
        }

        /// <summary>
        /// 스테이지 종료를 기록한다 (Result 진입 시 즉시 저장, spec/09 §3). 클리어면 보상을 지급하고 기록을 갱신한다.
        /// </summary>
        public StageOutcomeInfo CompleteStage(StageOutcome outcome, StageResult result, DeathCause? cause, float seconds)
        {
            string levelId = _session.SelectedLevelId;
            int stageNumber = RewardCalculator.StageNumber(levelId);
            var info = new StageOutcomeInfo { LevelId = levelId, Outcome = outcome, DeathCause = cause, Seconds = seconds };
            if (outcome == StageOutcome.Cleared)
            {
                bool firstClear = !(_session.Save.Record(levelId)?.Cleared ?? false);
                info.Reward = _session.Rewards.Compute(stageNumber, result);
                info.UnlocksEnding = firstClear && stageNumber == SaveData.StageCount;
                _session.Save.RecordClear(levelId, result, info.Reward);
            }

            _session.LastOutcome = info;
            _session.Persist();
            return info;
        }

        public void OpenEnding() => _navigator.Load(ScreenId.Ending);

        public PurchaseResult Purchase(string skillId)
        {
            var result = _session.Shop.TryPurchase(_session.Save, skillId);
            if (result == PurchaseResult.Purchased)
            {
                _session.Persist();
            }

            return result;
        }

        public bool Equip(string activeSkillId)
        {
            bool equipped = _session.Shop.Equip(_session.Save, activeSkillId);
            if (equipped)
            {
                _session.Persist();
            }

            return equipped;
        }

        public void ResetData()
        {
            _session.ResetData();
        }

        public void Quit() => _navigator.Quit();

        private void LoadStage()
        {
            StageBootstrap.RequestedLevelId = _session.SelectedLevelId;
            StageBootstrap.RequestedSkills = _session.Save.Loadout;
            _navigator.Load(ScreenId.Stage);
        }
    }
}
