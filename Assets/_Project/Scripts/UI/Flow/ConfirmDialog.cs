using System;
using UnityEngine;
using UnityEngine.UI;

namespace Moqui.Unity.UI.Flow
{
    /// <summary>예/아니오 확인 대화상자. 닫으면 연 버튼으로 포커스를 돌려준다. 기본 포커스는 "아니오".</summary>
    public sealed class ConfirmDialog : MonoBehaviour
    {
        private Action _onConfirm;
        private Selectable _returnFocus;

        public Button YesButton { get; private set; }

        public Button NoButton { get; private set; }

        public bool IsOpen => gameObject.activeSelf;

        public static ConfirmDialog Create(Transform canvas, string message, Action onConfirm)
        {
            var panel = UiFactory.CreatePanel("ConfirmDialog", canvas, new Vector2(900f, 360f));
            var dialog = panel.gameObject.AddComponent<ConfirmDialog>();
            dialog._onConfirm = onConfirm;
            var text = UiFactory.CreateText("Message", panel, message, 30, TextAnchor.MiddleCenter, 820f, 150f);
            text.rectTransform.anchoredPosition = new Vector2(0f, 60f);
            var row = UiFactory.CreateRow("Buttons", panel, 40f);
            row.anchoredPosition = new Vector2(0f, -100f);
            dialog.YesButton = UiFactory.CreateButton("Yes", row, "예", dialog.Confirm, 240f);
            dialog.NoButton = UiFactory.CreateButton("No", row, "아니오", dialog.Close, 240f);
            panel.gameObject.SetActive(false);
            return dialog;
        }

        public void Open(Selectable returnFocus)
        {
            _returnFocus = returnFocus;
            gameObject.SetActive(true);
            UiFactory.Focus(NoButton);
        }

        public void Confirm()
        {
            _onConfirm?.Invoke();
            Close();
        }

        public void Close()
        {
            gameObject.SetActive(false);
            UiFactory.Focus(_returnFocus);
        }
    }
}
