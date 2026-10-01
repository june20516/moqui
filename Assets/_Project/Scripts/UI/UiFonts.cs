using UnityEngine;

namespace Moqui.Unity.UI
{
    /// <summary>
    /// UI 글꼴. 허용 라이선스(tech/asset-pipeline.md)의 한글 글꼴이 없어 폰트 파일을 넣지 않고,
    /// 실행 중 OS 글꼴로 동적 글꼴을 만든다 (D-041). 앞쪽 이름부터 찾는다.
    /// </summary>
    public static class UiFonts
    {
        private const int DynamicFontSize = 32;
        private static readonly string[] PreferredOsFonts = { "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Segoe UI", "Arial" };
        private static Font _default;

        public static Font Default
        {
            get
            {
                if (_default == null)
                {
                    _default = Font.CreateDynamicFontFromOSFont(PreferredOsFonts, DynamicFontSize);
                    _default.hideFlags = HideFlags.DontSave;
                }

                return _default;
            }
        }
    }
}
