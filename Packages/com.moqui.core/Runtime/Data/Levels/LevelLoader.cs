using System.Collections.Generic;

namespace Moqui.Core.Data.Levels
{
    /// <summary>IDataSource에서 레벨과 그 방을 읽는다. 같은 방은 한 번만 읽는다.</summary>
    public sealed class LevelLoader
    {
        private readonly IDataSource _source;
        private readonly Dictionary<string, RoomDefinition> _rooms = new Dictionary<string, RoomDefinition>();

        public LevelLoader(IDataSource source)
        {
            _source = source;
        }

        public LevelDefinition Load(string levelId)
        {
            string file = LevelDefinition.FilePath(levelId);
            return LevelDefinition.Parse(_source.ReadText(file), file, LoadRoom);
        }

        public RoomDefinition LoadRoom(string roomId)
        {
            if (!_rooms.TryGetValue(roomId, out var room))
            {
                string file = RoomDefinition.FilePath(roomId);
                room = RoomDefinition.Parse(_source.ReadText(file), file);
                _rooms.Add(roomId, room);
            }

            return room;
        }
    }
}
