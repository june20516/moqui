using System.IO;
using System.Linq;
using Moqui.Unity.Presentation.Audio;
using UnityEditor;
using UnityEngine;

namespace Moqui.Unity.Editor
{
    /// <summary>
    /// `Audio/Generated/<id>.wav`(tools/gen_audio.py 합성물)로 오디오 카탈로그를 만든다 (spec/10).
    /// 빌드에서도 쓰도록 Resources에 둔다. 클립이 없는 ID가 있으면 실패한다.
    /// </summary>
    public static class AudioCatalogBuilder
    {
        public const string GeneratedFolder = "Assets/_Project/Audio/Generated";
        public const string CatalogPath = "Assets/_Project/Resources/" + AudioCatalog.ResourcePath + ".asset";

        public static AudioCatalog BuildCatalog()
        {
            var entries = AudioIds.Definitions.Select(definition => new AudioCatalog.Entry
            {
                Id = definition.Id,
                Clip = LoadClip(definition.Id),
                Volume = definition.Volume,
                Loop = definition.Loop,
                Bus = definition.Bus,
            }).ToList();

            var catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<AudioCatalog>();
                Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath));
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.SetEntries(entries);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        public static string ClipPath(string id) => $"{GeneratedFolder}/{id}.wav";

        private static AudioClip LoadClip(string id)
        {
            string path = ClipPath(id);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path)
                ?? throw new System.InvalidOperationException($"Missing audio clip '{path}'. Run python tools/gen_audio.py.");
        }
    }
}
