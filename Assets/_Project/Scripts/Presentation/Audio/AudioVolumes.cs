namespace Moqui.Unity.Presentation.Audio
{
    /// <summary>사용자 설정의 효과음·음악 볼륨 (0~1). 설정(UI)이 쓰고 오디오 출구가 매 프레임 읽는다. 마스터 볼륨은 AudioListener가 맡는다.</summary>
    public static class AudioVolumes
    {
        public static float Sfx { get; set; } = 1f;

        public static float Music { get; set; } = 1f;

        public static float For(AudioBus bus) => bus == AudioBus.Music ? Music : Sfx;
    }
}
