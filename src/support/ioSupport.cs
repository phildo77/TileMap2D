using System;
using System.Collections.Generic;
using System.Linq;
using ClipperLib;
using ioSoftSmiths.ioVector;

namespace ioSoftSmiths.ioSupport
{
    public class ISegment
    {
        private IVector2 m_Start;
        public IVector2 Start { get { return m_Start; } }
        private IVector2 m_End;
        public IVector2 End { get { return m_End; } }

        public ISegment(IVector2 _start, IVector2 _end)
        {
            m_Start = _start;
            m_End = _end;
        }

        public bool Contains(IVector2 _point)
        {
            int maxX = (int)Math.Max(m_Start.x, m_End.x);
            int maxY = (int)Math.Max(m_Start.y, m_End.y);
            int minX = (int)Math.Min(m_Start.x, m_End.x);
            int minY = (int)Math.Min(m_Start.y, m_End.y);
            if (_point.x > maxX || _point.x < minX || _point.y > maxY || _point.y < minY) return false;


            double slope = (double)(m_Start.y - m_End.y) / (double)(m_Start.x - m_End.x);
            int yInt;
            if (slope == double.PositiveInfinity || slope == double.NegativeInfinity)
            {
                return _point.x == m_Start.x;

            }
            else
            {
                yInt = (int)(-slope * m_Start.x + m_Start.y);
                return ((int)_point.y == (int)(slope * _point.x + yInt));
            }
        }

        public bool Intersects(ISegment _segment)
        {
            int denom = ((_segment.End.y - _segment.Start.y) * (End.x - Start.x))
                - ((_segment.End.x - _segment.Start.x) * (End.y - Start.y));

            if (denom == 0) {
                return false;
            }

            int ua = ((_segment.End.x - _segment.Start.x) * (Start.y - _segment.Start.y))
                - ((_segment.End.y - _segment.Start.y) * (Start.x - _segment.Start.x)) / denom;
            int ub = ((End.x - Start.x) * (Start.y - _segment.Start.y))
                - ((End.y - Start.y) * (Start.x - _segment.Start.x)) / denom;

            return ((ua >= 0) && (ua <= 1) && (ub >= 0) && (ub <= 1));
        }

        public IVector2? GetIntersection(ISegment _segment)
        {
            return Support2D.FindIntersection(m_Start, m_End, _segment.m_Start, _segment.m_End);
        }


    }



    public static class ArrayOf<T>
    {
        public static T[] Create(int size, T initialValue)
        {
            T[] array = (T[])Array.CreateInstance(typeof(T), size);
            for (int i = 0; i < array.Length; i++)
                array[i] = initialValue;
            return array;
        }
    }

    public struct ValuePair<T1, T2> : IEquatable<ValuePair<T1, T2>>
    {
        public T1 v1;
        public T2 v2;

        public ValuePair(T1 _v1, T2 _v2)
        {
            v1 = _v1;
            v2 = _v2;
        }

        public ValuePair<T2, T1> Swapped { get { return new ValuePair<T2, T1>(v2, v1); } }
        public bool Equals(ValuePair<T1, T2> _other)
        {
            return v1.Equals(_other.v1) && v2.Equals(_other.v2);
        }

        public override string ToString()
        {
            return v1 + ", " + v2;
        }
    }

    public static class Support2D
    {
        public static IEnumerable<T> DedupCollection<T>(IEnumerable<T> _input)
        {
            HashSet<T> passedValues = new HashSet<T>();

            //relatively simple dupe check alg used as example
            foreach (T item in _input)
                if (passedValues.Contains(item))
                    continue;
                else
                {
                    passedValues.Add(item);
                    yield return item;
                }
        }

        public static List<IntPoint> ToIntPoint(List<IVector2> _list)
        {
            var outPoints = new List<IntPoint>();
            foreach (IVector2 pt in _list)
                outPoints.Add(new IntPoint(pt.x, pt.y));
            return outPoints;
        }

        public static List<IVector2> ToIVector2(List<IntPoint> _list)
        {
            var outPoints = new List<IVector2>();
            checked
            {
                foreach (IntPoint pt in _list)
                    outPoints.Add(new IVector2((int)pt.X, (int)pt.Y));
            }
            return outPoints;
        }

        public static List<IVector2> FindIntersections(List<IVector2> _polyA, List<IVector2> _polyB, bool _closedPoly)
        {

            var intsctList = new HashSet<IVector2>();

            if (_closedPoly)
            {
                for (int ai = 0, aj = _polyA.Count - 1; ai < _polyA.Count; aj = ai++)
                    for (int bi = 0, bj = _polyB.Count - 1; bi < _polyB.Count; bj = bi++)
                    {
                        IVector2? intsct = FindIntersection(_polyA[aj], _polyA[ai], _polyB[bj], _polyB[bi]);
                        if (intsct != null) intsctList.Add(intsct.Value);
                    }
            }
            else
            {
                for (int ai = 0; ai < _polyA.Count - 1; ++ai)
                    for (int bi = 0; bi < _polyB.Count - 1; ++bi)
                    {
                        IVector2? intsct = FindIntersection(_polyA[ai], _polyA[ai + 1], _polyB[bi], _polyB[bi + 1]);
                        if (intsct != null) intsctList.Add(intsct.Value);
                    }
            }
            return intsctList.ToList();
        }

        public static IVector2? FindIntersection(IVector2 _start1, IVector2 _end1, IVector2 _start2, IVector2 _end2)
        {
            double denom = ((_end1.x - _start1.x) * (_end2.y - _start2.y)) - ((_end1.y - _start1.y) * (_end2.x - _start2.x));

            //  AB & CD are parallel 
            if (denom == 0)
                return null;

            double numer = ((_start1.y - _start2.y) * (_end2.x - _start2.x)) - ((_start1.x - _start2.x) * (_end2.y - _start2.y));

            double r = numer / denom;

            double numer2 = ((_start1.y - _start2.y) * (_end1.x - _start1.x)) - ((_start1.x - _start2.x) * (_end1.y - _start1.y));

            double s = numer2 / denom;

            if ((r < 0 || r > 1) || (s < 0 || s > 1))
                return null;

            // Find intersection IVector2
            IVector2 result = new IVector2(0, 0);
            result.x = _start1.x + (int)(r * ((double)_end1.x - (double)_start1.x));
            result.y = _start1.y + (int)(r * ((double)_end1.y - (double)_start1.y));

            return result;
        }

        public static List<IVector2> CreatePath(List<IVector2> _wayPoints, bool _closed)
        {
            var path = new List<IVector2>();
            for (int i = 0; i < _wayPoints.Count - 1; ++i)
            {
                path.AddRange(CreatePath(_wayPoints[i], _wayPoints[i + 1]));
            }
            if (_closed)
                path.AddRange(CreatePath(_wayPoints[_wayPoints.Count - 1], _wayPoints[0]));

            return DedupCollection(path).ToList();
        }

        public static HashSet<IVector2> CreatePath(IVector2 _from, IVector2 _to)
        {
            double slope;
            bool useX;
            bool vert = false;


            if (_from.x == _to.x)
            {
                slope = double.PositiveInfinity;
                vert = true;
                useX = false;
            }
            else
            {
                slope = (float)(_to.y - _from.y) / (float)(_to.x - _from.x);
                useX = Math.Abs(slope) <= 1f;
            }

            var path = new HashSet<IVector2>();


            if (useX)
            {
                int dir = _from.x < _to.x ? 1 : -1;


                for (int x = (int)_from.x; x != (int)_to.x; x = x + dir)
                {
                    int y = (int)(slope * ((float)x - _from.x) + _from.y);
                    path.Add(new IVector2(x, y));
                }

            }
            else
            {
                int dir = _from.y < _to.y ? 1 : -1;
                for (int y = (int)_from.y; y != (int)_to.y; y = y + dir)
                {
                    int x;
                    if (!vert)
                        x = (int)(((float)y - _from.y) / slope + _from.x);
                    else
                        x = (int)_from.x;

                    path.Add(new IVector2(x, y));
                }
            }
            return path;
        }

        public static List<IVector2> CreateEllipseRect(Bounds _bounds)
        {
            return CreateEllipseRect(new IVector2(_bounds.xMin, _bounds.yMin), new IVector2(_bounds.xMax, _bounds.yMax));
        }
        public static List<IVector2> CreateEllipseRect(IVector2 _ll, IVector2 _ur)
        {
            return CreateEllipseRect(_ll.x, _ll.y, _ur.x, _ur.y);
        }
        public static List<IVector2> CreateEllipseRect(int _x0, int _y0, int _x1, int _y1)
        {
            var x0 = _x0; var y0 = _y0; var x1 = _x1; var y1 = _y1;

            var ellipse = new List<IVector2>();

            int a = Math.Abs(x1 - x0), b = Math.Abs(y1 - y0), b1 = b & 1; //* values of diameter 
            long dx = 4 * (1 - a) * b * b, dy = 4 * (b1 + 1) * a * a; //* error increment 
            long err = dx + dy + b1 * a * a, e2; //* error of 1.step 

            if (x0 > x1) { x0 = x1; x1 += a; } //* if called with swapped points 
            if (y0 > y1) y0 = y1; // .. exchange them 
            y0 += (b + 1) / 2; y1 = y0 - b1;   //* starting pixel 
            a *= 8 * a; b1 = 8 * b * b;

            do
            {
                ellipse.Add(new IVector2(x1, y0)); //   I. Quadrant 
                ellipse.Add(new IVector2(x0, y0)); //  II. Quadrant 
                ellipse.Add(new IVector2(x0, y1)); // III. Quadrant 
                ellipse.Add(new IVector2(x1, y1)); //  IV. Quadrant 
                e2 = 2 * err;
                if (e2 <= dy) { y0++; y1--; err += dy += a; }  //* y step 
                if (e2 >= dx || 2 * err > dy) { x0++; x1--; err += dx += b1; } //* x step 
            } while (x0 <= x1);

            while (y0 - y1 < b)
            {  //* too early stop of flat ellipses a=1 
                ellipse.Add(new IVector2(x0 - 1, y0)); //* -> finish tip of ellipse 
                ellipse.Add(new IVector2(x1 + 1, y0++));
                ellipse.Add(new IVector2(x0 - 1, y1));
                ellipse.Add(new IVector2(x1 + 1, y1--));
            }

            return ellipse;
            //var orderedEllipse = ellipse.OrderBy(_coord => Math.Atan2(_coord.x - (_x1 - (_x1 - _x0)/2f), _coord.y - _y1 - ((_y1 - y0)/2f))).ToList();
            //return orderedEllipse;

        }

        /*public static List<IVector2> CreateEllipse(IVector2 _center, IVector2 _size, int _segmentsPerQuadrant)
        {
            const double piHalf = System.Math.PI * 0.5d;

            var step = piHalf / _segmentsPerQuadrant;

            //var pts = new IVector2[4 * _segmentsPerQuadrant + 1];
            var pts = new List<IVector2>();
            var angle = 0d;
            for (var i = 0; i < 4 * _segmentsPerQuadrant; i++)
            {
                var point = new IVector2((_center.x + (int)(Math.Cos(angle)*_size.x/2f)),
                     (_center.y + (int)(Math.Sin(angle)*_size.y/2f)));
                if (!pts.Contains(point)) pts.Add(point);
                angle += step;
            }
            //pts[pts.Length - 1] = pts[0];
            return pts;
        }*/


        public static void FillBoundary(List<IVector2> _boundary, IVector2 _startCoord, bool _eightWay, ref List<IVector2> _fillcoords)
        {
            if (_boundary.Contains(_startCoord) || _fillcoords.Contains(_startCoord)) return;
            _fillcoords.Add(_startCoord);

            FillBoundary(_boundary, _startCoord.Xnext, _eightWay, ref _fillcoords);
            FillBoundary(_boundary, _startCoord.Xprev, _eightWay, ref _fillcoords);
            FillBoundary(_boundary, _startCoord.Ynext, _eightWay, ref _fillcoords);
            FillBoundary(_boundary, _startCoord.Yprev, _eightWay, ref _fillcoords);
            if (!_eightWay) return;
            FillBoundary(_boundary, _startCoord + IVector2.XpYp, _eightWay, ref _fillcoords);
            FillBoundary(_boundary, _startCoord + IVector2.XnYp, _eightWay, ref _fillcoords);
            FillBoundary(_boundary, _startCoord + IVector2.XpYn, _eightWay, ref _fillcoords);
            FillBoundary(_boundary, _startCoord + IVector2.XnYn, _eightWay, ref _fillcoords);

        }


        public static IVector2?[] GetNearestCoords(HashSet<IVector2> _fromPoly, HashSet<IVector2> _toPoly, ref double _distance)
        {
            IVector2? closestFrom = null;
            IVector2? closestTo = null;
            foreach (var fromCoord in _fromPoly)
                foreach (var toCoord in _toPoly)
                {
                    var checkDistance = Math.Abs(fromCoord.x - toCoord.x) + Math.Abs(fromCoord.y - toCoord.y);
                    if (checkDistance < _distance)
                    {
                        closestFrom = fromCoord;
                        closestTo = toCoord;
                        _distance = checkDistance;
                    }
                }
            return new IVector2?[] { closestFrom, closestTo };
        }

        public static List<IVector2> CreateRect(Bounds _bounds)
        {
            return CreatePath(_bounds.Corners, true);
        }

        public static List<IVector2> GetTurnsInPath(List<IVector2> _path)
        {
            var turns = new List<IVector2>();
            for (int idx = 1; idx < _path.Count - 1; ++idx)
            {
                var prevCoord = _path[idx - 1];
                var nextCoord = _path[idx + 1];
                if (!(prevCoord.x == nextCoord.x || prevCoord.y == nextCoord.y))
                    turns.Add(_path[idx]);
            }
            return turns;
        }
    }

    public struct Bounds
    {
        int m_xMin;
        public int xMin { get { return m_xMin; } }
        int m_yMin;
        public int yMin { get { return m_yMin; } }
        int m_xMax;
        public int xMax { get { return m_xMax; } }
        int m_yMax;
        public int yMax { get { return m_yMax; } }

        public IVector2 Dims { get { return new IVector2(Width, Height); } }

        public List<IVector2> Corners
        {
            get
            {
                return new List<IVector2>
                {
                    new IVector2(m_xMin, m_yMin),
                    new IVector2(m_xMin, m_yMax),
                    new IVector2(m_xMax, m_yMax),
                    new IVector2(m_xMax, m_yMin)
                };
            }
        }

        public Bounds(int _xMin, int _yMin, int _xMax, int _yMax)
        {
            m_xMin = _xMin;
            m_xMax = _xMax;
            m_yMin = _yMin;
            m_yMax = _yMax;
        }

        public Bounds(List<Bounds> _bndyList)
        {
            if (_bndyList.Count == 0)
                throw new ArgumentException("Empty List found.  Cannot create Bounds.");

            m_xMin = _bndyList[0].xMin;
            m_xMax = _bndyList[0].xMax;
            m_yMin = _bndyList[0].yMin;
            m_yMax = _bndyList[0].yMax;

            for (int idx = 1; idx < _bndyList.Count; ++idx)
            {
                if (m_xMin > _bndyList[idx].xMin) m_xMin = _bndyList[idx].xMin;
                if (m_xMax < _bndyList[idx].xMax) m_xMax = _bndyList[idx].xMax;
                if (m_yMin > _bndyList[idx].yMin) m_yMin = _bndyList[idx].yMin;
                if (m_yMax < _bndyList[idx].yMax) m_yMax = _bndyList[idx].yMax;
            }
        }

        public int Width { get { return m_xMax - m_xMin + 1; } }
        public int Height { get { return m_yMax - m_yMin + 1; } }



        public Bounds CopyAndInclude(Bounds _toAdd)
        {
            Bounds roomBnds = _toAdd;
            int nxMax = m_xMax, nxMin = m_xMin, nyMax = m_yMax, nyMin = m_yMin;

            if (roomBnds.xMax > nxMax) nxMax = roomBnds.xMax;
            if (roomBnds.xMin < nxMin) nxMin = roomBnds.xMin;
            if (roomBnds.yMax > nyMax) nyMax = roomBnds.yMax;
            if (roomBnds.yMin < nyMin) nyMin = roomBnds.yMin;

            return new Bounds(nxMin, nyMin, nxMax, nyMax);
        }

        public bool IsOnEdge(IVector2 _point)
        {
            return ((_point.x == xMin) || (_point.x == xMax) || (_point.y == yMin) || (_point.y == yMax)) ? true : false;
        }

        public void Translate(IVector2 trVec)
        {
            m_xMin += trVec.x;
            m_xMax += trVec.x;
            m_yMin += trVec.y;
            m_yMax += trVec.y;
        }

        public bool Encapsulates(IVector2 _point)
        {
            return _point.x >= m_xMin && _point.x <= m_xMax && _point.y >= m_yMin && _point.y <= m_yMax;
        }

        public bool Overlaps(Bounds _other)
        {
            return (xMin < _other.xMax && xMax > _other.xMin &&
                    yMin < _other.yMax && yMax > _other.yMin);
        }

        public Bounds ShrinkBy(int _shrink)
        {
            var nxmin = m_xMin + _shrink;
            var nxmax = m_xMax - _shrink;
            var nymin = m_yMin + _shrink;
            var nymax = m_yMax - _shrink;

            if ((nxmin > nxmax) || (nymin > nymax))
                throw new ArgumentOutOfRangeException("Shrink caused inverse bounds.");

            return new Bounds(nxmin, nymin, nxmax, nymax);

        }

        public Bounds GrowBy(int _grow)
        {
            return ShrinkBy(-_grow);
        }
    }
}
