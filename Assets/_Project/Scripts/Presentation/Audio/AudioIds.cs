using System.Collections.Generic;
using System.Linq;

namespace Moqui.Unity.Presentation.Audio
{
    /// <summary>소리 묶음. 음악은 음악 볼륨, 나머지는 효과음 볼륨을 따른다.</summary>
    public enum AudioBus
    {
        Sfx,
        Ui,
        Ambience,
        Music,
    }

    /// <summary>사운드 ID 한 개의 재생 정의.</summary>
    public sealed class AudioDefinition
    {
        public AudioDefinition(string id, AudioBus bus, bool loop, float volume)
        {
            Id = id;
            Bus = bus;
            Loop = loop;
            Volume = volume;
        }

        public string Id { get; }

        public AudioBus Bus { get; }

        public bool Loop { get; }

        /// <summary>카탈로그 기본 음량 (0~1, 버스 볼륨과 곱한다).</summary>
        public float Volume { get; }
    }

    /// <summary>spec/10 사운드 ID 목록. 파일은 `tools/gen_audio.py`가 `Audio/Generated/<id>.wav`로 합성한다.</summary>
    public static class AudioIds
    {
        public const string WingLoop = "sfx_wing_loop";
        public const string Dash = "sfx_dash";
        public const string Attach = "sfx_attach";
        public const string Detach = "sfx_detach";
        public const string SuckLoop = "sfx_suck_loop";
        public const string Slap = "sfx_slap";
        public const string Clap = "sfx_clap";
        public const string Frenzy = "sfx_frenzy";
        public const string FrenzyLoop = "sfx_frenzy_loop";
        public const string Telegraph = "sfx_telegraph";
        public const string Spray = "sfx_spray";
        public const string Toxin = "sfx_toxin";
        public const string Breath = "sfx_breath";
        public const string Dislodge = "sfx_dislodge";
        public const string Decoy = "sfx_decoy";
        public const string DropTrap = "sfx_drop_trap";
        public const string Escape = "sfx_escape";
        public const string UiSelect = "sfx_ui_select";
        public const string UiConfirm = "sfx_ui_confirm";
        public const string UiCancel = "sfx_ui_cancel";
        public const string Snore = "sfx_snore";
        public const string Wake = "sfx_wake";
        public const string Drip = "sfx_drip";
        public const string Steam = "sfx_steam";
        public const string WindLoop = "sfx_wind_loop";
        public const string WindGust = "sfx_wind_gust";
        public const string Footstep = "sfx_footstep";
        public const string BgmTitle = "bgm_title";
        public const string BgmStage = "bgm_stage";
        public const int StageCount = 5;

        public static readonly IReadOnlyList<AudioDefinition> Definitions = new[]
        {
            new AudioDefinition(WingLoop, AudioBus.Sfx, true, 0.35f),
            new AudioDefinition(Dash, AudioBus.Sfx, false, 0.8f),
            new AudioDefinition(Attach, AudioBus.Sfx, false, 0.7f),
            new AudioDefinition(Detach, AudioBus.Sfx, false, 0.6f),
            new AudioDefinition(SuckLoop, AudioBus.Sfx, true, 0.6f),
            new AudioDefinition(Slap, AudioBus.Sfx, false, 1f),
            new AudioDefinition(Clap, AudioBus.Sfx, false, 1f),
            new AudioDefinition(Frenzy, AudioBus.Sfx, false, 0.9f),
            new AudioDefinition(FrenzyLoop, AudioBus.Sfx, true, 0.5f),
            new AudioDefinition(Telegraph, AudioBus.Sfx, false, 0.8f),
            new AudioDefinition(Spray, AudioBus.Sfx, false, 0.8f),
            new AudioDefinition(Toxin, AudioBus.Sfx, false, 0.7f),
            new AudioDefinition(Breath, AudioBus.Sfx, false, 0.35f),
            new AudioDefinition(Dislodge, AudioBus.Sfx, false, 0.8f),
            new AudioDefinition(Decoy, AudioBus.Sfx, false, 0.7f),
            new AudioDefinition(DropTrap, AudioBus.Sfx, false, 0.8f),
            new AudioDefinition(Escape, AudioBus.Sfx, false, 0.8f),
            new AudioDefinition(UiSelect, AudioBus.Ui, false, 0.5f),
            new AudioDefinition(UiConfirm, AudioBus.Ui, false, 0.6f),
            new AudioDefinition(UiCancel, AudioBus.Ui, false, 0.6f),
            new AudioDefinition(Snore, AudioBus.Sfx, true, 0.4f),
            new AudioDefinition(Wake, AudioBus.Sfx, false, 0.7f),
            new AudioDefinition(Drip, AudioBus.Sfx, false, 0.5f),
            new AudioDefinition(Steam, AudioBus.Sfx, true, 0.4f),
            new AudioDefinition(WindLoop, AudioBus.Sfx, true, 0.5f),
            new AudioDefinition(WindGust, AudioBus.Sfx, false, 0.6f),
            new AudioDefinition(Footstep, AudioBus.Sfx, false, 0.5f),
            new AudioDefinition(Ambience(1), AudioBus.Ambience, true, 0.4f),
            new AudioDefinition(Ambience(2), AudioBus.Ambience, true, 0.4f),
            new AudioDefinition(Ambience(3), AudioBus.Ambience, true, 0.45f),
            new AudioDefinition(Ambience(4), AudioBus.Ambience, true, 0.45f),
            new AudioDefinition(Ambience(5), AudioBus.Ambience, true, 0.4f),
            new AudioDefinition(BgmTitle, AudioBus.Music, true, 0.5f),
            new AudioDefinition(BgmStage, AudioBus.Music, true, 0.4f),
        };

        public static IEnumerable<string> All => Definitions.Select(definition => definition.Id);

        /// <summary>스테이지 환경음 (1: TV, 2: TV, 3: 선풍기, 4: 환풍기·물소리, 5: 풀벌레).</summary>
        public static string Ambience(int stageNumber) => $"amb_stage{stageNumber}";

        /// <summary>"stage03" 같은 레벨 ID의 환경음. 번호를 읽지 못하면 null.</summary>
        public static string AmbienceForLevel(string levelId)
        {
            const string prefix = "stage";
            if (levelId == null || !levelId.StartsWith(prefix) || !int.TryParse(levelId.Substring(prefix.Length), out int number))
            {
                return null;
            }

            return number >= 1 && number <= StageCount ? Ambience(number) : null;
        }
    }
}
