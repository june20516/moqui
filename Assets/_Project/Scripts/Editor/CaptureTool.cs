using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Moqui.Unity.Editor
{
    /// <summary>
    /// Tools/capture.ps1의 진입점 (tech/verification.md §4). 그래픽이 필요하므로 -nographics 없이 실행한다.
    /// M0 단계: 빌드 설정의 각 씬을 메인 카메라로 1장씩 캡처한다. 스테이지별 포즈 캡처는 M7에서 확장한다.
    /// </summary>
    public static class CaptureTool
    {
        private const int Width = 1920;
        private const int Height = 1080;
        private const int DepthBits = 24;
        private const string CapturesFolder = "Captures";

        [MenuItem("Moqui/Capture All")]
        public static void CaptureAll()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                throw new InvalidOperationException("CaptureAll needs a graphics device. Run without -nographics.");
            }

            string outputDirectory = Path.Combine(
                BuildScript.ProjectRoot,
                CapturesFolder,
                DateTime.Now.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(outputDirectory);

            foreach (string scenePath in EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path))
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                Camera camera = Camera.main;
                if (camera == null)
                {
                    throw new InvalidOperationException($"No main camera in scene {scenePath}.");
                }

                string fileName = Path.GetFileNameWithoutExtension(scenePath) + ".png";
                CaptureCamera(camera, Path.Combine(outputDirectory, fileName));
            }

            Debug.Log($"[CaptureTool] Captures written to {outputDirectory}");
        }

        public static void CaptureCamera(Camera camera, string path)
        {
            var renderTexture = new RenderTexture(Width, Height, DepthBits);
            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
