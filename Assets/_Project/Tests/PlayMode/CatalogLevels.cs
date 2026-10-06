using System.Collections.Generic;
using System.Linq;
using Moqui.Unity.Data;

namespace Moqui.Unity.Tests
{
    /// <summary>테스트 대상 레벨: 스테이지 목록(data/stages.json) 전체 (레벨을 추가하면 자동으로 포함, D-061).</summary>
    public static class CatalogLevels
    {
        public static IEnumerable<string> LevelIds => StageCatalogs.Repo.LevelIds;

        public static IEnumerable<string> ClearScenarios => StageCatalogs.Repo.LevelIds.Select(levelId => $"{levelId}_clear");
    }
}
