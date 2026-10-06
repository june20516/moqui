using System;
using System.Collections.Generic;
using Moqui.Unity.Presentation;
using Moqui.Unity.UI.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace Moqui.Unity.UI.Hud
{
    /// <summary>
    /// 모키 표현 큐의 임시 표시기 (spec/12): 모델 동작 대신 모키 머리 옆에 아주 작은 글자를 띄운다.
    /// 한 번 큐는 0.6초 동안 떠오르며 사라지고, 지속 큐는 조건이 이어지는 동안 머문다.
    /// 3D 모델·VFX 표시기가 준비되면 이 컴포넌트를 빼고 같은 <see cref="IMokiCuePresenter"/>를 구현한 표시기를 붙인다.
    /// </summary>
    public sealed class TextCuePresenter : MonoBehaviour, IMokiCuePresenter
    {
        public const float Lifetime = 0.6f;
        public const int FontSize = 18;

        /// <summary>떠오르는 높이와 모키 머리에서의 거리 (캔버스 단위).</summary>
        private const float RisePixels = 26f;
        private const float SideOffset = 34f;
        private const float LoopSpacing = 20f;

        private static readonly Color OneShotColor = new Color(1f, 0.92f, 0.97f);
        private static readonly Color LoopColor = new Color(1f, 0.8f, 0.9f, 0.85f);

        private readonly List<Item> _items = new List<Item>();
        private RectTransform _canvas;
        private Camera _camera;
        private Func<Vector3> _anchor;

        private sealed class Item
        {
            public string Id;
            public Text Text;
            public float Age;
            public bool Loop;
        }

        /// <summary>지금 보이는 글자 (테스트·디버그용).</summary>
        public IEnumerable<string> VisibleTexts
        {
            get
            {
                foreach (var item in _items)
                {
                    if (item.Text.enabled)
                    {
                        yield return item.Text.text;
                    }
                }
            }
        }

        /// <param name="anchor">모키 머리 근처 월드 위치 (매 프레임 다시 읽는다).</param>
        public void Bind(Camera camera, Func<Vector3> anchor)
        {
            _camera = camera;
            _anchor = anchor;
            if (_canvas == null)
            {
                _canvas = UiFactory.CreateCanvas("MokiCues", 5, transform);
            }
        }

        public void Play(MokiCueDefinition cue)
        {
            _items.Add(new Item { Id = cue.Id, Text = CreateText(cue.Text, OneShotColor), Loop = false });
        }

        public void SetLoop(MokiCueDefinition cue, bool active)
        {
            var existing = _items.Find(item => item.Loop && item.Id == cue.Id);
            if (active && existing == null)
            {
                _items.Add(new Item { Id = cue.Id, Text = CreateText(cue.Text, LoopColor), Loop = true });
            }
            else if (!active && existing != null)
            {
                Remove(existing);
            }
        }

        /// <summary>한 프레임 갱신 (테스트에서 직접 부를 수 있다).</summary>
        public void Tick(float deltaTime)
        {
            if (_canvas == null || _camera == null || _anchor == null)
            {
                return;
            }

            Vector3 screen = _camera.WorldToScreenPoint(_anchor());
            bool visible = screen.z > 0f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, screen, null, out Vector2 local);
            int loopIndex = 0;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var item = _items[i];
                item.Age += deltaTime;
                if (!item.Loop && item.Age >= Lifetime)
                {
                    Remove(item);
                    continue;
                }

                item.Text.enabled = visible;
                float t = item.Loop ? 0f : item.Age / Lifetime;
                Vector2 offset = item.Loop
                    ? new Vector2(-SideOffset, -LoopSpacing * loopIndex++)
                    : new Vector2(SideOffset, RisePixels * EaseOut(t));
                item.Text.rectTransform.anchoredPosition = local + offset;
                Color color = item.Text.color;
                color.a = item.Loop ? LoopColor.a : 1f - (t * t);
                item.Text.color = color;
            }
        }

        private void LateUpdate()
        {
            Tick(Time.deltaTime);
        }

        private Text CreateText(string content, Color color)
        {
            var text = UiFactory.CreateText("Cue", _canvas, content, FontSize, TextAnchor.MiddleCenter, 120f, 24f);
            text.color = color;
            text.raycastTarget = false;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.25f, 0.05f, 0.2f, 0.8f);
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            return text;
        }

        private void Remove(Item item)
        {
            _items.Remove(item);
            if (item.Text == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(item.Text.gameObject);
            }
            else
            {
                DestroyImmediate(item.Text.gameObject);
            }
        }

        private static float EaseOut(float t) => 1f - ((1f - t) * (1f - t));
    }
}
