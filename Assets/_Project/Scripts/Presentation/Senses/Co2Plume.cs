using System.Collections.Generic;
using UnityEngine;

namespace Moqui.Unity.Presentation.Senses
{
    /// <summary>CO₂ 연기 덩이 하나.</summary>
    public sealed class Co2Puff
    {
        public Co2Puff(Vector3 position, Vector3 forward, float strength)
        {
            Position = position;
            Forward = forward;
            Strength = strength;
        }

        public Vector3 Position { get; set; }

        public Vector3 Forward { get; }

        public float Strength { get; }

        public float Age { get; set; }
    }

    /// <summary>
    /// 날숨 CO₂ 흐름 (spec/11 §2). 날숨 구간마다 코/입에서 연기 덩이를 내보내고, 덩이는 머리 정면으로 밀려 나가다가
    /// 위로 퍼지며 사라진다. 세기(취한 타겟 1.6배)만큼 크고 진하다. 표현 전용이며 판정에 쓰지 않는다.
    /// 바람 영역에서 흩어지는 표현은 선풍기 기믹(M9)과 함께 추가한다.
    /// </summary>
    public sealed class Co2Plume
    {
        private readonly SensesSettings _settings;
        private readonly List<Co2Puff> _puffs = new List<Co2Puff>();
        private float _spawnTimer;

        public Co2Plume(SensesSettings settings)
        {
            _settings = settings;
            _spawnTimer = settings.Co2PuffInterval;
        }

        public IReadOnlyList<Co2Puff> Puffs => _puffs;

        public void Update(float deltaTime, bool isExhaling, Vector3 exhalePosition, Vector3 headForward, float strength)
        {
            for (int i = _puffs.Count - 1; i >= 0; i--)
            {
                var puff = _puffs[i];
                puff.Age += deltaTime;
                if (puff.Age >= _settings.Co2PuffLifetime)
                {
                    _puffs.RemoveAt(i);
                    continue;
                }

                float forwardFade = 1f - Progress(puff);
                Vector3 velocity = (Vector3.up * _settings.Co2RiseSpeed) + (puff.Forward * (_settings.Co2ForwardSpeed * forwardFade));
                puff.Position += velocity * deltaTime;
            }

            if (!isExhaling)
            {
                // 다음 날숨이 시작되는 프레임에 바로 첫 덩이가 나오도록 한다.
                _spawnTimer = _settings.Co2PuffInterval;
                return;
            }

            _spawnTimer += deltaTime;
            while (_spawnTimer >= _settings.Co2PuffInterval)
            {
                _spawnTimer -= _settings.Co2PuffInterval;
                _puffs.Add(new Co2Puff(exhalePosition, headForward, strength));
            }
        }

        /// <summary>0(막 나옴) ~ 1(사라짐).</summary>
        public float Progress(Co2Puff puff)
        {
            return Mathf.Clamp01(puff.Age / _settings.Co2PuffLifetime);
        }

        public float Radius(Co2Puff puff)
        {
            return Mathf.Lerp(_settings.Co2PuffStartRadius, _settings.Co2PuffEndRadius, Progress(puff)) * puff.Strength;
        }

        /// <summary>진하기 0~1. 세기가 클수록 진하고, 퍼질수록 옅어진다.</summary>
        public float Opacity(Co2Puff puff)
        {
            return Mathf.Clamp01(puff.Strength * (1f - Progress(puff)));
        }

        /// <summary>CO₂는 안개와 무관하게 co2VisibleRange 안에서 보인다.</summary>
        public bool IsVisibleFrom(Co2Puff puff, Vector3 viewer)
        {
            return Vector3.Distance(puff.Position, viewer) <= _settings.Co2VisibleRange;
        }
    }
}
