//#define LOG_DEBUG

using System.Collections.Generic;
using ioSoftSmiths.ioSupport;
using System.Collections;
using System.Linq;
using ioSoftSmiths.ioVector;

namespace ioSoftSmiths.TileMap
{
    public class Room
    {

        private HashSet<IVector2> m_Doorways;
        public HashSet<IVector2> Doorways { get { return m_Doorways; } }
        private ShapeType m_ShapeType;
        public ShapeType Shape { get { return m_ShapeType; } }
        private Bounds m_Bounds;

        public Bounds Bounds { get { return m_Bounds; } }

        public IVector2 Anchor
        {
            get
            {
                switch (m_ShapeType)
                {
                    case ShapeType.Rectangle:
                    case ShapeType.Ellipse:
                    default:
                        return new IVector2(Bounds.xMin + (int)(Bounds.Width / 2f), (int)(Bounds.yMin + Bounds.Height / 2f));

                }
            }
        }

        public IEnumerable<IVector2> Corners
        {
            get
            {
                switch (m_ShapeType)
                {
                    case ShapeType.Rectangle:
                        return Bounds.Corners;
                    case ShapeType.Ellipse:
                    default:
                        return new List<IVector2>();
                }
            }
        }


        public enum ShapeType : byte
        {
            Rectangle,
            Ellipse,
            Custom
        }

        public Room(Bounds _bounds, ShapeType _shapeType)
        {
            m_ShapeType = _shapeType;
            m_Doorways = new HashSet<IVector2>();
            m_Bounds = _bounds;
        }

        public bool ContainsCoord(IVector2 _coord, bool _includeWalls)
        {
            var roomCoords = new HashSet<IVector2>();
            if (_includeWalls) roomCoords.UnionWith(GetWallCoords(false));
            roomCoords.UnionWith(GetFloorCoords(false));

            return roomCoords.Contains(_coord);
        }

        internal void AddDoorway(IVector2 _doorway)
        {
            if (!m_Doorways.Contains(_doorway))
                m_Doorways.Add(_doorway);
        }

        public List<IVector2> GetWallCoords(bool _excludeDoorways)
        {
            var walls = new System.Collections.Generic.List<IVector2>();
            switch (m_ShapeType)
            {
                case ShapeType.Rectangle:
                    walls.AddRange(Support2D.CreateRect(m_Bounds));
                    break;
                case ShapeType.Ellipse:
                    walls.AddRange(Support2D.CreateEllipseRect(m_Bounds));
                    break;
            }
            if (_excludeDoorways)
                return walls;

            foreach (var doorway in m_Doorways)
                walls.Remove(doorway);

            return walls;
        }

        public System.Collections.Generic.List<IVector2> GetFloorCoords(bool _includeDoorways)
        {
            var floor = new System.Collections.Generic.List<IVector2>();
            switch (m_ShapeType)
            {
                case ShapeType.Rectangle:
                    for (int y = Bounds.yMin + 1; y < Bounds.yMax; ++y)
                        for (int x = Bounds.xMin + 1; x < Bounds.xMax; ++x)
                            floor.Add(new IVector2(x, y));
                    break;

                case ShapeType.Ellipse:
                    Support2D.FillBoundary(GetWallCoords(true), Anchor, false, ref floor);
                    break;
            }

            if (!_includeDoorways)
                return floor;

            floor.AddRange(m_Doorways);

            return floor;
        }

        public HashSet<IVector2> GetAllCoords()
        {
            var allCoords = new HashSet<IVector2>();
            allCoords.UnionWith(GetFloorCoords(true));
            allCoords.UnionWith(GetWallCoords(true));
            return allCoords;
        }
    }

    public class RoomGroup : IEnumerable<Room>
    {
        private System.Collections.Generic.List<Room> m_Rooms = new System.Collections.Generic.List<Room>();

        //public List<Room> Rooms { get { return m_Rooms; } }

        public IEnumerator<Room> GetEnumerator() { return m_Rooms.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return (IEnumerator)GetEnumerator(); }

        public Room this[int _index] { get { return m_Rooms[_index]; } }
        public int Count { get { return m_Rooms.Count; } }

        public void Add(Room _room)
        {
            m_Rooms.Add(_room);
        }

        public Room Get(IVector2 _anchor)
        {
            for (int i = 0; i < m_Rooms.Count; ++i)
                if (m_Rooms[i].Anchor.Equals(_anchor)) return m_Rooms[i];
            return null;
        }

        public List<IVector2> Anchors
        {
            get
            {
                return this.Select(_room => _room.Anchor).ToList();
            }
        }


    }
}
