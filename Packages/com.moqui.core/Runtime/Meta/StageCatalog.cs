using System;
using System.Collections.Generic;
using System.Linq;
using Moqui.Core.Data;

namespace Moqui.Core.Meta
{
    /// <summary>스테이지 하나: 레벨 ID와 선택 화면 제목, 환경음(없으면 장의 환경음).</summary>
    public sealed class StageEntry
    {
        public StageEntry(string levelId, string title, string ambience = null)
        {
            LevelId = levelId ?? throw new ArgumentNullException(nameof(levelId));
            Title = title ?? string.Empty;
            Ambience = ambience;
        }

        public string LevelId { get; }

        public string Title { get; }

        public string Ambience { get; }
    }

    /// <summary>장 하나: 같은 방·주제의 스테이지 묶음.</summary>
    public sealed class ChapterDefinition
    {
        public ChapterDefinition(string id, string title, string ambience, IReadOnlyList<StageEntry> stages)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Title = title ?? string.Empty;
            Ambience = ambience;
            Stages = stages ?? throw new ArgumentNullException(nameof(stages));
        }

        public string Id { get; }

        public string Title { get; }

        /// <summary>장의 기본 환경음 ID.</summary>
        public string Ambience { get; }

        public IReadOnlyList<StageEntry> Stages { get; }
    }

    /// <summary>
    /// 스테이지 목록 (data/stages.json, spec/07·08, D-061): 장과 스테이지의 순서가 해금·다음 스테이지·엔딩·선택 화면을 정한다.
    /// 레벨을 추가할 때는 레벨 데이터와 봇 시나리오를 만들고 이 목록에 한 줄 넣으면 된다(코드 변경 없음).
    /// 화면의 스테이지 번호는 목록 순서(1부터)이고, 저장 기록 키는 레벨 ID다.
    /// </summary>
    public sealed class StageCatalog
    {
        public const string FilePath = "stages.json";
        public const int CurrentFormatVersion = 1;

        private readonly List<StageEntry> _stages = new List<StageEntry>();
        private readonly Dictionary<string, ChapterDefinition> _chapterOf = new Dictionary<string, ChapterDefinition>(StringComparer.Ordinal);

        public StageCatalog(IReadOnlyList<ChapterDefinition> chapters)
        {
            Chapters = chapters ?? throw new ArgumentNullException(nameof(chapters));
            foreach (var chapter in chapters)
            {
                if (chapter.Stages.Count == 0)
                {
                    throw new ArgumentException($"Chapter '{chapter.Id}' has no stages.", nameof(chapters));
                }

                foreach (var stage in chapter.Stages)
                {
                    if (_chapterOf.ContainsKey(stage.LevelId))
                    {
                        throw new ArgumentException($"Stage '{stage.LevelId}' is listed twice.", nameof(chapters));
                    }

                    _chapterOf.Add(stage.LevelId, chapter);
                    _stages.Add(stage);
                }
            }

            if (_stages.Count == 0)
            {
                throw new ArgumentException("The stage catalog is empty.", nameof(chapters));
            }
        }

        public IReadOnlyList<ChapterDefinition> Chapters { get; }

        /// <summary>모든 스테이지 (플레이 순서).</summary>
        public IReadOnlyList<StageEntry> Stages => _stages;

        public IEnumerable<string> LevelIds => _stages.Select(stage => stage.LevelId);

        public bool Contains(string levelId) => levelId != null && _chapterOf.ContainsKey(levelId);

        /// <summary>화면에 보이는 스테이지 번호 (1부터). 목록에 없으면 0.</summary>
        public int Number(string levelId) => _stages.FindIndex(stage => stage.LevelId == levelId) + 1;

        public StageEntry Entry(string levelId) => _stages.FirstOrDefault(stage => stage.LevelId == levelId);

        public ChapterDefinition ChapterOf(string levelId) => _chapterOf.TryGetValue(levelId, out var chapter) ? chapter : null;

        /// <summary>바로 앞 스테이지 (첫 스테이지면 null).</summary>
        public string Previous(string levelId)
        {
            int index = Number(levelId) - 1;
            return index > 0 ? _stages[index - 1].LevelId : null;
        }

        /// <summary>바로 다음 스테이지 (마지막이면 null).</summary>
        public string Next(string levelId)
        {
            int index = Number(levelId) - 1;
            return index >= 0 && index + 1 < _stages.Count ? _stages[index + 1].LevelId : null;
        }

        /// <summary>마지막 스테이지 (클리어하면 엔딩).</summary>
        public bool IsLast(string levelId) => Number(levelId) == _stages.Count;

        /// <summary>환경음: 스테이지에 따로 있으면 그것, 없으면 장의 것.</summary>
        public string AmbienceOf(string levelId)
        {
            var entry = Entry(levelId);
            return entry == null ? null : entry.Ambience ?? ChapterOf(levelId).Ambience;
        }

        public static StageCatalog Load(IDataSource source)
        {
            return Parse(source.ReadText(FilePath), FilePath);
        }

        public static StageCatalog Parse(string text, string file)
        {
            var root = JsonAccess.Parse(text, file);
            int version = root.Get("formatVersion").Int();
            if (version != CurrentFormatVersion)
            {
                throw root.Get("formatVersion").Error($"must be {CurrentFormatVersion}");
            }

            var chapters = new List<ChapterDefinition>();
            foreach (var chapter in root.Get("chapters").Items())
            {
                var stages = new List<StageEntry>();
                foreach (var stage in chapter.Get("stages").Items())
                {
                    stages.Add(new StageEntry(stage.Get("level").String(), stage.Get("title").String(), stage.Has("ambience") ? stage.Get("ambience").String() : null));
                }

                chapters.Add(new ChapterDefinition(chapter.Get("id").String(), chapter.Get("title").String(), chapter.Has("ambience") ? chapter.Get("ambience").String() : null, stages));
            }

            try
            {
                return new StageCatalog(chapters);
            }
            catch (ArgumentException exception)
            {
                throw new DataFormatException($"{file}: {exception.Message}");
            }
        }
    }
}
