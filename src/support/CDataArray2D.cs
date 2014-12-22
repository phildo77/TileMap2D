using System;
using System.Collections.Generic;
using ioSoftSmiths.ioVector;

namespace CDataArray
{
    public class CDataArray2D<T> where T : struct, IEquatable<T>
    {
        private bool m_Compressed = true;
        private T[,] m_UncompressedData;
        //Internal Properties ---------------------------------------------------------------
        private readonly List<CompData<T>> DataBlock;
        public readonly IVector2 Dims;
        private readonly T m_InitData;
        //Constructor ------------------------------------------------------------------------

        public CDataArray2D(int _x, int _y, T _initData, bool _compressed)
        {
            Dims = new IVector2(_x, _y);
            DataBlock = new List<CompData<T>>();
            m_InitData = _initData;
            if (_compressed)
            {
                DataBlock.Add(new CompData<T>(m_InitData, Dims.x * Dims.y));
            }
            else
            {
                m_UncompressedData = new T[Dims.x, Dims.y];
                for (var y = 0; y < Dims.y; ++y)
                    for (var x = 0; x < Dims.x; ++x)
                        m_UncompressedData[x, y] = m_InitData;
            }
            m_Compressed = _compressed;
        }

        public bool Compressed
        {
            get { return m_Compressed; }
            set
            {
                if (m_Compressed != value)
                {
                    if (m_Compressed)
                    {
                        m_UncompressedData = new T[Dims.x, Dims.y];
                        for (var y = 0; y < Dims.y; ++y)
                            for (var x = 0; x < Dims.x; ++x)
                                m_UncompressedData[x, y] = this[x, y];
                        DataBlock.Clear();
                        m_Compressed = false;
                    }
                    else
                    {
                        DataBlock.Add(new CompData<T>(m_InitData, Dims.x * Dims.y));
                        for (var y = 0; y < Dims.y; ++y)
                            for (var x = 0; x < Dims.x; ++x)
                                this[x, y] = m_UncompressedData[x, y];
                        m_UncompressedData = null;
                        m_Compressed = true;
                    }
                }
            }
        }

        /// <summary>
        ///     Returns binary data at specified coordinate.
        /// </summary>
        /// <param name="_x">X coordinate</param>
        /// <param name="_y">Y coordinate</param>
        /// <param name="_z">Z coordinate</param>
        /// <returns></returns>
        public T this[int _x, int _y]
        {
            get
            {
                if (Compressed)
                {
                    if (!CoordInRange(new IVector2(_x, _y)))
                        throw new IndexOutOfRangeException("CDataArray index [" + _x + ", " + _y + "] out of range.");

                    return GetData(_x, _y).Data;
                }
                return m_UncompressedData[_x, _y];
            }


            set
            {
                if (Compressed)
                {
                    Update(value, new IVector2(_x, _y));
                }
                else
                {
                    m_UncompressedData[_x, _y] = value;
                }
            }
        }

        /// <summary>
        ///     Returns binary data at specified coordinate
        /// </summary>
        /// <param name="_coord">Coordinate of binary data to be returned</param>
        /// <returns></returns>
        public T this[IVector2 _coord]
        {
            get { return this[_coord.x, _coord.y]; }

            set { this[_coord.x, _coord.y] = value; }
        }

        public bool InBounds(IVector2 _coord)
        {
            return CoordInRange(_coord);
        }

        public void DrawPath(List<IVector2> _path, T _value, bool _additive = false)
        {
            for (var i = 0; i < _path.Count - 1; ++i)
                DrawLine(_path[i], _path[i + 1], _value, _additive);
        }

        public void DrawClosedPoly(List<IVector2> _poly, T _value)
        {
            for (int i = 0, j = _poly.Count - 1; i < _poly.Count; j = i++)
                DrawLine(_poly[j], _poly[i], _value);
        }

        public void DrawLine(List<IVector2[]> _segs, T _value, bool _additive = false)
        {
            foreach (var segment in _segs)
            {
                if (segment.Length != 2)
                    throw new ArgumentException("CDataArray2D:DrawLine - Segment does not contain 2 points");
                DrawLine(segment[0], segment[1], _value, _additive);
            }
        }

        public void DrawLine(IVector2 _from, IVector2 _to, T _value, bool _additive = false)
        {
            double slope;
            bool useX;
            var vert = false;


            if (_from.x == _to.x)
            {
                slope = double.PositiveInfinity;
                vert = true;
                useX = false;
            }
            else
            {
                slope = (_to.y - _from.y) / (float)(_to.x - _from.x);
                useX = Math.Abs(slope) <= 1f;
            }


            if (useX)
            {
                var dir = _from.x < _to.x ? 1 : -1;

                for (var x = _from.x; x != (_to.x + dir); x = x + dir)
                {
                    var y = (int)(slope * ((float)x - _from.x) + _from.y);
                    if ((x < 0) || (x >= Dims.x) || (y < 0) || (y >= Dims.y)) continue;
                    this[x, y] = _value;
                }
            }
            else
            {
                var dir = _from.y < _to.y ? 1 : -1;
                for (var y = _from.y; y != (_to.y + dir); y = y + dir)
                {
                    int x;
                    if (!vert)
                        x = (int)(((float)y - _from.y) / slope + _from.x);
                    else
                        x = _from.x;

                    if ((x < 0) || (x >= Dims.x) || (y < 0) || (y >= Dims.y)) continue;
                    this[x, y] = _value;
                }
            }
        }

        //Internal Methods ---------------------------------------------------------------------

        /// <summary>
        ///     Returns true if _pos coordinate is within dimensions and false if not.
        /// </summary>
        /// <param name="_pos"></param>
        /// <returns></returns>
        private bool CoordInRange(IVector2 _pos)
        {
            if (_pos.x >= Dims.x || _pos.y >= Dims.y ||
                _pos.x < 0 || _pos.y < 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Inspects data strips around the specified index and combines them into one strip if the data is the same.
        /// </summary>
        /// <param name="_index"></param>
        private void Meld(int _index, MeldDir _dir)
        {
            var index = _index;
            if (index != 0 && (_dir == MeldDir.Front || _dir == MeldDir.Both))
            {
                //Meld to prior index if possible
                if (DataBlock[index].Data.Equals(DataBlock[index - 1].Data))
                {
                    var newCount = DataBlock[index].count + DataBlock[index - 1].count;
                    DataBlock[index - 1] = new CompData<T>(DataBlock[index].Data, newCount);
                    DataBlock.RemoveAt(index);
                    index--;
                }
            }

            if (index != DataBlock.Count - 1 && (_dir == MeldDir.Back || _dir == MeldDir.Both))
            {
                //Meld to following index if possible
                if (DataBlock[index].Data.Equals(DataBlock[index + 1].Data))
                {
                    var newCount = DataBlock[index].count + DataBlock[index + 1].count;
                    DataBlock[index] = new CompData<T>(DataBlock[index].Data, newCount);
                    DataBlock.RemoveAt(index + 1);
                }
            }
        }

        /// <summary>
        ///     Get Data from specified position.
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        /// <param name="_z"></param>
        /// <returns></returns>
        private CompData<T> GetData(int _x, int _y)
        {
            int junk1, junk2;
            return GetData(new IVector2(_x, _y), out junk1, out junk2);
        }

        private CompData<T> GetData(IVector2 _coord)
        {
            int junk1, junk2;
            return GetData(_coord, out junk1, out junk2);
        }

        private CompData<T> GetData(IVector2 _coord, out int _index, out int _stripPos)
        {
            //Check that coords are valid
            if (CoordInRange(_coord))
            {
                var curBlkLinear = 0;
                var trgBlkLinear = _coord.x + _coord.y * Dims.x;

                if (trgBlkLinear == 0)
                {
                    _index = 0;
                    _stripPos = 0;
                    return DataBlock[_index];
                }

                //Find target block
                _index = 0;
                while (true)
                {
                    curBlkLinear += DataBlock[_index].count;
                    if (curBlkLinear > trgBlkLinear)
                    {
                        curBlkLinear -= DataBlock[_index].count;
                        _stripPos = trgBlkLinear - curBlkLinear;
                        break;
                    }
                    _index++;
                }

                return DataBlock[_index];
            }
            throw new ArgumentOutOfRangeException(
                "Coords (" + _coord.x + ", " + _coord.y + ") out of range.  Dims (" + Dims.x + ", " + Dims.y + ")",
                "x, y");
        }

        public List<IVector2> GetNeighborCoordsOf(int _x, int _y, bool _useDiag = false)
        {
            var neighbors = new List<IVector2>();
            for (var x = _x - 1; x <= _x + 1; ++x)
                for (var y = _y - 1; y <= _y + 1; ++y)
                {
                    var p = new IVector2(x, y);
                    if (CoordInRange(new IVector2(p.x, p.y)))
                        if (!p.Equals(new IVector2(_x, _y)))
                            if (!_useDiag)
                            {
                                if (Math.Abs(_x - x) + Math.Abs(_y - y) == 1)
                                    neighbors.Add(p);
                            }
                            else
                            {
                                neighbors.Add(p);
                            }
                }
            return neighbors;
        }


        public CDataArray2D<T> GetChunk(IVector2 _root, IVector2 _size)
        {
            var chunk = new CDataArray2D<T>(_size.x, _size.y, default(T), true);

            for (var x = 0; x < _size.x; ++x)
                for (var y = 0; y < _size.y; ++y)
                {
                    chunk[x, y] = this[_root.x + x, _root.y + y];
                }

            return chunk;
        }

        /// <summary>
        ///     Debug tool.  Returns the entire DataBlock array in the form of a string -->  (Data Strip Count) x (Data), ....
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var dataString = "";
            for (var i = 0; i < DataBlock.Count; ++i)
            {
                dataString += DataBlock[i].count + "x" + DataBlock[i].Data + ", ";
            }
            return dataString;
        }

        /*public void DebugToCSV(string _path)
        {
            StringBuilder sb = new StringBuilder();
            for (int y = Dims.y - 1; y >= 0; --y)
            {
                sb.AppendLine();
                for (int x = 0; x < Dims.x; ++x)
                    sb.Append(this[x, y] + ",");
            }
            WriteAllText(_path, sb.ToString());
        }*/

        /// <summary>
        ///     Updates the data at _coord to _data
        /// </summary>
        /// <param name="_data"></param>
        /// <param name="_coord"></param>
        /// <returns></returns>
        public bool Update(T _data, IVector2 _coord)
        {
            if (!CoordInRange(_coord))
            {
                throw new ArgumentOutOfRangeException(GetType().Name + "-- Index out of range: " + _coord);
            }
            //Get data at position
            int dataIndex, dataStripPos;
            //CompData[] workBlocks = GetDataGroup(_coord, out dataIndex, out dataStripPos);
            var workBlock = GetData(_coord, out dataIndex, out dataStripPos);

            //Check for bad index
            if (workBlock.count == 0)
            {
                throw new Exception(GetType().Name + "-- Update: Found zero count DataStrip at index " + dataIndex + ".");
            }

            //Check no change condition
            if (workBlock.Data.Equals(_data))
                return true;


            //Check single item strip condition
            if (workBlock.count == 1)
            {
                var updatedBlock = new CompData<T>(_data, 1);
                DataBlock[dataIndex] = updatedBlock;
                Meld(dataIndex, MeldDir.Both);
                return true;
            }

            //Check middle of strip condition
            if ((workBlock.count > 2) && (dataStripPos != 0) && (dataStripPos != workBlock.count - 1))
            {
                //Current data strip shrink to data in front
                var frontStrip = new CompData<T>(workBlock.Data, dataStripPos);

                //Insert this block
                var newStrip = new CompData<T>(_data, 1);

                //Create data in back
                var backStrip = new CompData<T>(workBlock.Data, workBlock.count - dataStripPos - 1);

                //Update Data Array
                DataBlock.Insert(dataIndex + 1, backStrip);
                DataBlock.Insert(dataIndex + 1, newStrip);
                DataBlock[dataIndex] = frontStrip;
                //Meld(dataIndex);

                return true;
            }

            //Check beginning of strip condition
            if (dataStripPos == 0)
            {
                var newStrip = new CompData<T>(_data, 1);

                var backStrip = new CompData<T>(workBlock.Data, workBlock.count - 1);

                DataBlock.Insert(dataIndex + 1, backStrip);
                DataBlock[dataIndex] = newStrip;
                Meld(dataIndex, MeldDir.Front);

                return true;
            }

            //Check end of strip condition
            if (dataStripPos == DataBlock[dataIndex].count - 1)
            {
                var newStrip = new CompData<T>(_data, 1);

                var frontStrip = new CompData<T>(workBlock.Data, workBlock.count - 1);

                DataBlock[dataIndex] = newStrip;
                DataBlock.Insert(dataIndex, frontStrip);
                Meld(dataIndex + 1, MeldDir.Back);

                return true;
            }

            return false;
        }

        //DEBUG ----------------------------------------------------
        public int getBlockCount()
        {
            return DataBlock.Count;
        }

        public List<T> getBlockDataList()
        {
            var storedData = new List<T>();
            foreach (var block in DataBlock)
            {
                storedData.Add(block.Data);
            }
            return storedData;
        }

        public List<int> getBlockDataCountList()
        {
            var storedDataCounts = new List<int>();
            foreach (var block in DataBlock)
            {
                storedDataCounts.Add(block.count);
            }
            return storedDataCounts;
        }

        //Internal structs ------------------------------------------------------------------

        /// <summary>
        ///     CompData -- Compressed Data.
        ///     Struct used to represent data in the CDataArray.
        ///     Contains binary data and repeat count.
        /// </summary>
        private struct CompData<T>
        {
            public readonly int count;
            public readonly T Data;

            public CompData(T _data, int _count)
            {
                count = _count;
                Data = _data;
            }
        }

        private enum MeldDir : byte
        {
            Both,
            Front,
            Back
        }

    }
}