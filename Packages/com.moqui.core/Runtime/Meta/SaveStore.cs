using System;

namespace Moqui.Core.Meta
{
    /// <summary>저장 파일 읽기/쓰기 포트 (spec/09 §3). Unity 구현은 Application.persistentDataPath를 쓴다.</summary>
    public interface ISaveStorage
    {
        bool Exists(string fileName);

        string Read(string fileName);

        void Write(string fileName, string text);

        /// <summary>파일 이름을 바꾼다. 대상이 이미 있으면 덮어쓴다.</summary>
        void Move(string fromFileName, string toFileName);

        void Delete(string fileName);
    }

    /// <summary>
    /// 저장 파일 관리 (spec/09 §3). 파일이 없으면 기본값, 손상되었으면 save.corrupt.json으로 보존하고 기본값으로 시작한다.
    /// </summary>
    public sealed class SaveStore
    {
        public const string FileName = "save.json";
        public const string CorruptFileName = "save.corrupt.json";

        private readonly ISaveStorage _storage;

        public SaveStore(ISaveStorage storage)
        {
            _storage = storage;
        }

        /// <summary>마지막 Load에서 손상 파일을 발견했는가.</summary>
        public bool LastLoadWasCorrupt { get; private set; }

        public SaveData Load()
        {
            LastLoadWasCorrupt = false;
            if (!_storage.Exists(FileName))
            {
                return new SaveData();
            }

            try
            {
                return SaveSerializer.Deserialize(_storage.Read(FileName));
            }
            catch (Exception)
            {
                LastLoadWasCorrupt = true;
                _storage.Move(FileName, CorruptFileName);
                return new SaveData();
            }
        }

        public void Save(SaveData data)
        {
            _storage.Write(FileName, SaveSerializer.Serialize(data));
        }

        /// <summary>Title의 "데이터 초기화" (spec/09 §3).</summary>
        public SaveData Reset()
        {
            if (_storage.Exists(FileName))
            {
                _storage.Delete(FileName);
            }

            return new SaveData();
        }
    }
}
