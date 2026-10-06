using Moqui.Core.Meta;

namespace Moqui.Unity.Data
{
    /// <summary>
    /// 게임의 스테이지 목록 (data/stages.json, D-061). 한 번 읽어 둔다. 레벨 추가는 데이터만 바꾸면 되고,
    /// 흐름·선택 화면·환경음·성능 측정·캡처가 모두 이 목록을 따른다.
    /// </summary>
    public static class StageCatalogs
    {
        private static StageCatalog _repo;

        public static StageCatalog Repo => _repo ??= StageCatalog.Load(new UnityDataSource());

        /// <summary>목록의 레벨 ID 배열 (테스트 케이스 소스용).</summary>
        public static string[] LevelIdList => System.Linq.Enumerable.ToArray(Repo.LevelIds);
    }
}
