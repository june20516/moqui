using System.IO;
using Moqui.Core.Meta;
using UnityEngine;

namespace Moqui.Unity.Data
{
    /// <summary>저장소 포트의 파일 구현 (spec/09 §3). 기본 위치는 Application.persistentDataPath.</summary>
    public sealed class FileSaveStorage : ISaveStorage
    {
        public FileSaveStorage()
            : this(Application.persistentDataPath)
        {
        }

        public FileSaveStorage(string directory)
        {
            Directory = directory;
        }

        public string Directory { get; }

        public bool Exists(string fileName) => File.Exists(PathOf(fileName));

        public string Read(string fileName) => File.ReadAllText(PathOf(fileName));

        public void Write(string fileName, string text)
        {
            System.IO.Directory.CreateDirectory(Directory);

            // 쓰는 도중 꺼져도 이전 파일이 남도록 임시 파일에 쓴 뒤 바꾼다.
            string temporary = PathOf(fileName + ".tmp");
            File.WriteAllText(temporary, text);
            if (File.Exists(PathOf(fileName)))
            {
                File.Delete(PathOf(fileName));
            }

            File.Move(temporary, PathOf(fileName));
        }

        public void Move(string fromFileName, string toFileName)
        {
            if (File.Exists(PathOf(toFileName)))
            {
                File.Delete(PathOf(toFileName));
            }

            File.Move(PathOf(fromFileName), PathOf(toFileName));
        }

        public void Delete(string fileName) => File.Delete(PathOf(fileName));

        private string PathOf(string fileName) => Path.Combine(Directory, fileName);
    }
}
