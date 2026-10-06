using System;
using Moqui.Core.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Moqui.Unity.Input
{
    /// <summary>
    /// Input System의 Gameplay 맵을 읽어 틱마다 PlayerCommand를 만든다 (tech/architecture.md §4.2).
    /// 프레임과 틱의 주기가 달라 눌림(edge)이 사라지지 않도록, 다음 틱에서 꺼낼 때까지 래치해 둔다.
    /// 시점 전환·일시정지처럼 Core가 모르는 입력은 별도로 꺼낸다.
    /// </summary>
    public sealed class CommandCollector : IDisposable
    {
        public const string MapName = "Gameplay";

        private readonly InputActionMap _map;
        private readonly InputAction _move;
        private readonly InputAction _vertical;
        private readonly InputAction _look;
        private readonly InputAction _dash;
        private readonly InputAction _precision;
        private readonly InputAction _attach;
        private readonly InputAction _suck;
        private readonly InputAction _toggleView;
        private readonly InputAction _skill;
        private readonly InputAction _pause;
        private readonly LookSettings _lookSettings;

        private bool _dashLatched;
        private bool _attachLatched;
        private bool _skillLatched;
        private bool _toggleViewLatched;
        private bool _pauseLatched;

        public CommandCollector(InputActionAsset asset, LookSettings lookSettings)
        {
            _map = asset.FindActionMap(MapName, throwIfNotFound: true);
            _move = _map.FindAction("Move", true);
            _vertical = _map.FindAction("Vertical", true);
            _look = _map.FindAction("Look", true);
            _dash = _map.FindAction("Dash", true);
            _precision = _map.FindAction("Precision", true);
            _attach = _map.FindAction("Attach", true);
            _suck = _map.FindAction("Suck", true);
            _toggleView = _map.FindAction("ToggleView", true);
            _skill = _map.FindAction("Skill", true);
            _pause = _map.FindAction("Pause", true);
            _lookSettings = lookSettings;
            Look = new LookState(lookSettings.PitchLimit);
            _map.Enable();
        }

        public LookState Look { get; }

        /// <summary>감도 배율 (설정 0.25~4배, spec/08).</summary>
        public float SensitivityScale { get; set; } = 1f;

        /// <summary>세로 반전 (설정, spec/08).</summary>
        public bool InvertY { get; set; }

        /// <summary>비행 조작 방식 (호버 / 자유 비행, gulf §5). 규칙은 같고 입력을 나누는 방식만 다르다.</summary>
        public FlightControlMode FlightMode { get; set; }

        /// <summary>매 프레임 호출한다. 시점을 갱신하고 눌림을 래치한다.</summary>
        public void Sample(float deltaTime)
        {
            ReadLook(out Vector2 mouseDelta, out Vector2 stick);
            Vector2 degrees = (mouseDelta * _lookSettings.MouseSensitivity) + (stick * (_lookSettings.GamepadLookSpeed * deltaTime));
            degrees *= SensitivityScale;
            Look.Rotate(degrees.x, InvertY ? -degrees.y : degrees.y);

            _dashLatched |= _dash.WasPressedThisFrame();
            _attachLatched |= _attach.WasPressedThisFrame();
            _skillLatched |= _skill.WasPressedThisFrame();
            _toggleViewLatched |= _toggleView.WasPressedThisFrame();
            _pauseLatched |= _pause.WasPressedThisFrame();
        }

        /// <summary>시뮬레이션 1틱에 넣을 커맨드. 래치된 눌림은 여기서 소비된다.</summary>
        public PlayerCommand NextCommand()
        {
            Vector2 move = _move.ReadValue<Vector2>();
            FlightControl.Map(FlightMode, new System.Numerics.Vector2(move.x, move.y), _vertical.ReadValue<float>(), Look.Pitch, out var mappedMove, out float mappedVertical);
            var command = new PlayerCommand
            {
                Move = mappedMove,
                Vertical = mappedVertical,
                LookYaw = Look.Yaw,
                LookPitch = Look.Pitch,
                DashPressed = _dashLatched,
                PrecisionHeld = _precision.IsPressed(),
                AttachPressed = _attachLatched,
                SuckHeld = _suck.IsPressed(),
                SkillPressed = _skillLatched,
            };

            _dashLatched = false;
            _attachLatched = false;
            _skillLatched = false;
            return command;
        }

        /// <summary>래치된 눌림을 모두 버린다 (일시정지 해제 시).</summary>
        public void ClearLatches()
        {
            _dashLatched = false;
            _attachLatched = false;
            _skillLatched = false;
            _toggleViewLatched = false;
            _pauseLatched = false;
        }

        public bool ConsumeToggleView()
        {
            bool pressed = _toggleViewLatched;
            _toggleViewLatched = false;
            return pressed;
        }

        public bool ConsumePause()
        {
            bool pressed = _pauseLatched;
            _pauseLatched = false;
            return pressed;
        }

        public void Dispose()
        {
            _map.Disable();
        }

        private void ReadLook(out Vector2 mouseDelta, out Vector2 stick)
        {
            mouseDelta = Vector2.zero;
            stick = Vector2.zero;
            foreach (InputControl control in _look.controls)
            {
                if (!(control is InputControl<Vector2> vectorControl))
                {
                    continue;
                }

                Vector2 value = vectorControl.ReadValue();
                if (control.device is Pointer)
                {
                    mouseDelta += value;
                }
                else
                {
                    stick += value;
                }
            }
        }
    }
}
