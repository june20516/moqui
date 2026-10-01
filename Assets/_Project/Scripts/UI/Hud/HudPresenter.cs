namespace Moqui.Unity.UI.Hud
{
    /// <summary>HUD 효과음 출구. 테스트에서 가짜로 바꿔 재생 여부를 확인한다.</summary>
    public interface IHudAudio
    {
        void PlayTelegraph();
    }

    /// <summary>HudState를 뷰에 반영하고, 공격 예고가 시작되는 프레임에 경고음 `sfx_telegraph`를 낸다 (spec/02 §7, spec/08).</summary>
    public sealed class HudPresenter
    {
        private readonly HudView _view;
        private readonly IHudAudio _audio;
        private bool _wasTelegraphing;

        public HudPresenter(HudView view, IHudAudio audio)
        {
            _view = view;
            _audio = audio;
        }

        public void Present(HudState state, float time)
        {
            _view.Apply(state, time);
            if (state.AttackTelegraphing && !_wasTelegraphing)
            {
                _audio?.PlayTelegraph();
            }

            _wasTelegraphing = state.AttackTelegraphing;
        }
    }
}
