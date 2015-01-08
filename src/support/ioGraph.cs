using System;
using System.Collections.Generic;
using System.Text;
using CDataArray;
using ioSoftSmiths.ioVector;
using ioSoftSmiths.ioSupport;

namespace ioSoftSmiths.Collections
{

    //TODO Remove is totally broken here. if a node is removed I think the class is busted.
    public class ioGraph<T>
    {
        private List<List<ioGraphEdge>> m_Edges;

        public List<ValuePair<int, int>> NodeEdgeIndices
        {
            get
            {
                var edges = new List<ValuePair<int, int>>();
                for (int nodeIdx = 0; nodeIdx < m_Edges.Count; ++nodeIdx)
                    for (int edgeIdx = 0; edgeIdx < m_Edges[nodeIdx].Count; ++edgeIdx)
                        edges.Add(new ValuePair<int, int>(nodeIdx, edgeIdx));
                return edges;

            }
        }

        public int EdgeCount
        {
            get
            {
                int count = 0;
                foreach (var edges in m_Edges) count += edges.Count;
                return count;
            }
        }

        private List<T> m_Nodes;
        public int NodeCount { get { return m_Nodes.Count; } }

        public ioGraph()
        {
            m_Nodes = new List<T>();
            m_Edges = new List<List<ioGraphEdge>>();
        }

        public ioGraph(List<T> _nodes, List<List<ioGraphEdge>> _edges)
        {
            m_Nodes = _nodes;
            m_Edges = _edges;
        }

        public void AddEdge(int _nodeIdx, ioGraphEdge _edge)
        {
            AddEdge(m_Nodes[_nodeIdx], m_Nodes[_edge.ToNode], _edge.Weight);
        }

        public void AddEdge(T _from, T _to, double _weight)
        {
            int fromIdx = GetNodeIdx(_from);
            if (fromIdx == -1)
            {
                m_Nodes.Add(_from);
                m_Edges.Add(new List<ioGraphEdge>());
                fromIdx = m_Nodes.Count - 1;
            }

            int toIdx = GetNodeIdx(_to);
            if (toIdx == -1)
            {
                m_Nodes.Add(_to);
                m_Edges.Add(new List<ioGraphEdge>());
                toIdx = m_Nodes.Count - 1;
            }

            for (int edgeIdx = 0; edgeIdx < m_Edges[fromIdx].Count; ++edgeIdx)
            {
                if (m_Edges[fromIdx][edgeIdx].ToNode == toIdx)
                {
                    m_Edges[fromIdx][edgeIdx].Weight = _weight;
                    return;
                }

            }
            m_Edges[fromIdx].Add(new ioGraphEdge(_weight, toIdx));
        }

        /// <summary>
        /// Sets the weight of any edge going to _node to _newWeight
        /// </summary>
        /// <param name="_toNode"></param>
        /// <param name="_newWeight"></param>
        public void UpdateEdgesTo(T _toNode, double _newWeight)
        {
            int nodeIdx = GetNodeIdx(_toNode);
            foreach (List<ioGraphEdge> edges in m_Edges)
                foreach (ioGraphEdge edge in edges)
                {
                    if (edge.ToNode == nodeIdx)
                        edge.Weight = _newWeight;
                }
        }


        public int GetNodeIdx(T _node)
        {
            for (int i = 0; i < m_Nodes.Count; ++i)
                if (_node.Equals(m_Nodes[i])) return i;
            return -1;
        }

        public T GetNode(int _nodeIdx)
        {
            return m_Nodes[_nodeIdx];
        }

        public List<T> GetNodes(List<int> nodePath)
        {
            List<T> nodes = new List<T>();
            foreach (int nodeIdx in nodePath) nodes.Add(GetNode(nodeIdx));
            return nodes;

        }

        public List<ioGraphEdge> GetConnectionsSlow(T _node)
        {
            var edges = new List<ioGraphEdge>();
            int node = GetNodeIdx(_node);
            if (node == -1) return null;
            return m_Edges[node];
        }

        public List<ioGraphEdge> GetConnectionsFast(int _node)
        {
            return m_Edges[_node];
        }

        public List<ValuePair<int, int>> GetNodeConnectionsList()
        {
            var conns = new List<ValuePair<int, int>>();

            for (int i = 0; i < m_Nodes.Count; ++i)
                foreach (var edge in m_Edges[i])
                    conns.Add(new ValuePair<int, int>(i, edge.ToNode));
            return conns;
        }

        public bool ContainsConnection(int _nodeFrom, int _nodeTo, bool _checkBothDirs)
        {
            if (_nodeFrom >= m_Edges.Count) return false;

            foreach (var edge in m_Edges[_nodeFrom])
                if (edge.ToNode.Equals(_nodeTo)) return true;
            if (!_checkBothDirs) return false;
            foreach (var edge in m_Edges[_nodeTo])
                if (edge.ToNode.Equals(_nodeFrom)) return true;
            return false;
        }


        public bool ContainsEdge(int _node, ioGraphEdge _edge)
        {
            foreach (ioGraphEdge edge in m_Edges[_node])
            {
                if (edge.Equals(_edge)) return true;
            }
            return false;
        }

        public string GetEdgeDesc()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Graph Description:");

            for (int i = 0; i < m_Nodes.Count; ++i)
            {
                sb.AppendLine("Node Idx: " + i + " Node Value: " + m_Nodes[i].ToString());
                foreach (var edge in m_Edges[i])
                {
                    sb.AppendLine("   To: " + edge.ToNode + " Value: " + m_Nodes[edge.ToNode].ToString());// + " Weight:  " + edge.Weight);
                }
            }

            return sb.ToString();
        }


        /// <summary>
        /// Creates and returns MST graph from this graph's nodes and edges.
        /// </summary>
        /// <param name="_graph"></param>
        /// <returns></returns>
        public ioGraph<T> PrimMST()
        {
            var exSet = new List<int>();
            for (int i = 1; i < m_Nodes.Count; ++i)
                exSet.Add(i);

            //Start with first node (could be random)
            var inSet = new List<int>() { 0 };
            var edges = new List<List<ioGraphEdge>>();
            foreach (List<ioGraphEdge> nodeEdges in m_Edges)
                edges.Add(new List<ioGraphEdge>());

            while (exSet.Count > 0)
            {
                double leastCost = double.PositiveInfinity;
                int leastNode = -1;
                int curNode = -1;
                int curEdge = -1;

                for (int nodeIdx = 0; nodeIdx < inSet.Count; ++nodeIdx)
                {
                    int tmpCurNode = inSet[nodeIdx];
                    for (int edge = 0; edge < m_Edges[tmpCurNode].Count; ++edge)
                        if (m_Edges[tmpCurNode][edge].Weight < leastCost &&
                            !inSet.Contains(m_Edges[tmpCurNode][edge].ToNode))
                        {
                            curNode = tmpCurNode;
                            curEdge = edge;
                            leastCost = m_Edges[tmpCurNode][edge].Weight;
                            leastNode = m_Edges[tmpCurNode][edge].ToNode;
                        }
                }

                if (leastCost.Equals(double.PositiveInfinity))
                    throw new Exception("PrimMST: least node not found!");

                edges[curNode].Add(m_Edges[curNode][curEdge]);
                inSet.Add(leastNode);
                exSet.Remove(leastNode);
            }

            return new ioGraph<T>(m_Nodes, edges);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            if (m_Nodes.Count == 0)
                return "<EMPTY>";

            for (int nodeIdx = 0; nodeIdx < m_Nodes.Count; ++nodeIdx)
            {
                sb.AppendLine("Node " + nodeIdx + "  [ " + m_Nodes[nodeIdx].ToString() + " ] :");
                if (m_Edges[nodeIdx].Count != 0)
                {
                    for (int edgeIndx = 0; edgeIndx < m_Edges[nodeIdx].Count; ++edgeIndx)
                    {
                        sb.AppendLine("  =>  " + m_Edges[nodeIdx][edgeIndx].ToNode + "    [ " +
                                      m_Nodes[m_Edges[nodeIdx][edgeIndx].ToNode].ToString() + " ]");
                    }
                }
                else
                {
                    sb.AppendLine("   No Edges");
                }

            }

            return sb.ToString();
        }

        public class ioGraphEdge
        {
            private int m_To;
            //private int m_From;
            private double m_Weight;

            public double Weight { get { return m_Weight; } set { m_Weight = value; } }
            //public int FromNode { get { return m_From; } }
            public int ToNode { get { return m_To; } }

            public ioGraphEdge(double _cost, int _to)
            {
                m_Weight = _cost;
                m_To = _to;
                //m_From = _from;
            }

        }
    }



    public enum G2Dir : int
    {
        UL = 0,
        U = 1,
        UR = 2,
        R = 3,
        DR = 4,
        D = 5,
        DL = 6,
        L = 7,
    }

    public static class DirMap
    {
        private static readonly List<IVector2> Vecs = new List<IVector2>()
            {
                IVector2.XnYp,
                IVector2.Yp,
                IVector2.XpYp,
                IVector2.Xp,
                IVector2.XpYn,
                IVector2.Yn,
                IVector2.XnYn,
                IVector2.Xn
            };

        private static readonly Dictionary<IVector2, G2Dir> Dirs = new Dictionary<IVector2, G2Dir>()
            {
                {IVector2.XnYp, G2Dir.UL},
                {IVector2.Yp, G2Dir.U},
                {IVector2.XpYp, G2Dir.UR},
                {IVector2.Xp, G2Dir.R},
                {IVector2.XpYn, G2Dir.DR},
                {IVector2.Yn, G2Dir.D},
                {IVector2.XnYn, G2Dir.DL},
                {IVector2.Xn, G2Dir.L}
            };

        public static IVector2 Get(G2Dir _dir)
        {
            return Vecs[(int)_dir];
        }

        public static G2Dir Get(IVector2 _vec)
        {
            return Dirs[_vec];
        }

        public static IVector2 Get(int _dir)
        {
            return Get((G2Dir)_dir);
        }

        public static G2Dir VecToDir(IVector2 _from, IVector2 _to)
        {
            IVector2 result = _from - _to;
            if (result.Equals(IVector2.XnYp)) return G2Dir.UL;
            if (result.Equals(IVector2.Yp)) return G2Dir.U;
            if (result.Equals(IVector2.XpYp)) return G2Dir.UR;
            if (result.Equals(IVector2.Xp)) return G2Dir.R;
            if (result.Equals(IVector2.XpYn)) return G2Dir.DR;
            if (result.Equals(IVector2.Yn)) return G2Dir.D;
            if (result.Equals(IVector2.XnYn)) return G2Dir.DL;
            if (result.Equals(IVector2.Xn)) return G2Dir.L;
            throw new ArgumentOutOfRangeException("Vector " + _from + " is not a neighbor of Vector " + _to);

        }

    }

    public class Graph2d
    {
        private const string TAG_DEBUG = "ioSoftSmiths.Collections.Graph2d";
        private CDataArray2D<Graph2dNode> m_Nodes;

        public const double IMPASSABLE = double.MaxValue;
        public CDataArray2D<double> Weights
        {
            get
            {
                var weights = new CDataArray2D<double>(Dims.x, Dims.y, 1, false);
                for (int x = 0; x < Dims.x; ++x)
                    for (int y = 0; y < Dims.y; ++y)
                        weights[x, y] = m_Nodes[x, y].Weight;
                return weights;

            }
        } //TODO remove this (Debug)
        private readonly bool Bidirectional;


        public IVector2 Dims { get { return m_Nodes.Dims; } }



        public Graph2d(IVector2 _size, double _defaultWeight, bool _bidirectional, bool _compressed = false)
        {
            m_Nodes = new CDataArray2D<Graph2dNode>(_size.x, _size.y, new Graph2dNode(_defaultWeight, _bidirectional), false);
            Bidirectional = _bidirectional;
        }


        /*public Graph2dNode this[IVector2 _coord]
        {
            get { return m_Nodes[_coord]; }
        }*/

        public double GetEdgeWeight(IVector2 _from, G2Dir _direction)
        {
            if (Bidirectional)
            {
                return m_Nodes[_from].GetEdge(_direction);
            }
            else
            {
                return m_Nodes[_from + DirMap.Get(_direction)].Weight;
            }
        }

        public double GetNodeWeight(IVector2 _coord)
        {
            if (Bidirectional)
                throw new InvalidOperationException("Graph is bidirectional.  Cannot get node weight. (Edge weights only)");
            return !InBounds(_coord) ? IMPASSABLE : m_Nodes[_coord].Weight;
        }




        /*public void SetEdgeWeight(IVector2 _node, G2Dir _direction, double _weight)
        {
            m_Nodes[_node] = m_Nodes[_node].CreateUpdatedEdge(_direction, _weight);
        }*/

        /// <summary>
        /// Updates all neighbors' edges weight to _weight.
        /// </summary>
        /// <param name="_node"></param>
        /// <param name="_weight"></param>
        public void SetNodeWeight(IVector2 _node, double _weight, bool _allDirs = false)
        {
            if (InBounds(_node))
                if (Bidirectional)
                {
                    IVector2 selNode = new IVector2(_node + IVector2.Yp);
                    if (m_Nodes.InBounds(selNode))
                        m_Nodes[selNode] = m_Nodes[selNode].CreateUpdatedEdge(G2Dir.D, _weight);

                    selNode = new IVector2(_node + IVector2.Xp);
                    if (m_Nodes.InBounds(selNode))
                        m_Nodes[selNode] = m_Nodes[selNode].CreateUpdatedEdge(G2Dir.L, _weight);

                    selNode = new IVector2(_node + IVector2.Yn);
                    if (m_Nodes.InBounds(selNode))
                        m_Nodes[selNode] = m_Nodes[selNode].CreateUpdatedEdge(G2Dir.U, _weight);

                    selNode = new IVector2(_node + IVector2.Xn);
                    if (m_Nodes.InBounds(selNode))
                        m_Nodes[selNode] = m_Nodes[selNode].CreateUpdatedEdge(G2Dir.R, _weight);

                    if (_allDirs)
                    {
                        selNode = new IVector2(_node + IVector2.XnYp);
                        if (m_Nodes.InBounds(selNode))
                            m_Nodes[selNode] = m_Nodes[selNode].CreateUpdatedEdge(G2Dir.DR, _weight);

                        selNode = new IVector2(_node + IVector2.XpYp);
                        if (m_Nodes.InBounds(selNode))
                            m_Nodes[selNode] = m_Nodes[selNode].CreateUpdatedEdge(G2Dir.DL, _weight);

                        selNode = new IVector2(_node + IVector2.XpYn);
                        if (m_Nodes.InBounds(selNode))
                            m_Nodes[selNode] = m_Nodes[selNode].CreateUpdatedEdge(G2Dir.UL, _weight);

                        selNode = new IVector2(_node + IVector2.XnYn);
                        if (m_Nodes.InBounds(selNode))
                            m_Nodes[selNode] = m_Nodes[selNode].CreateUpdatedEdge(G2Dir.UR, _weight);
                    }
                }
                else
                {
                    m_Nodes[_node] = new Graph2dNode(_weight, false);
                }
        }

        public List<IVector2> GetNeighbors(IVector2 _node, bool _allDirs = false)
        {
            var neighbors = new List<IVector2>();
            var selNode = new IVector2(_node + IVector2.Yp);
            if (m_Nodes.InBounds(selNode))
                neighbors.Add(selNode);

            selNode = new IVector2(_node + IVector2.Xp);
            if (m_Nodes.InBounds(selNode))
                neighbors.Add(selNode);

            selNode = new IVector2(_node + IVector2.Yn);
            if (m_Nodes.InBounds(selNode))
                neighbors.Add(selNode);

            selNode = new IVector2(_node + IVector2.Xn);
            if (m_Nodes.InBounds(selNode))
                neighbors.Add(selNode);

            if (_allDirs)
            {
                selNode = new IVector2(_node + IVector2.XnYp);
                if (m_Nodes.InBounds(selNode))
                    neighbors.Add(selNode);

                selNode = new IVector2(_node + IVector2.XpYp);
                if (m_Nodes.InBounds(selNode))
                    neighbors.Add(selNode);

                selNode = new IVector2(_node + IVector2.XpYn);
                if (m_Nodes.InBounds(selNode))
                    neighbors.Add(selNode);

                selNode = new IVector2(_node + IVector2.XnYn);
                if (m_Nodes.InBounds(selNode))
                    neighbors.Add(selNode);
            }
            return neighbors;
        }

        public bool InBounds(IVector2 _coord)
        {
            return m_Nodes.InBounds(_coord);
        }



        public void ScaleNodeWeight(IVector2 _node, double _scaleF, bool _allDirs = false)
        {
            if (Bidirectional)
            {
                var selNode = new IVector2(_node + IVector2.Yp);
                if (m_Nodes.InBounds(selNode))
                    m_Nodes[selNode] = m_Nodes[selNode].CreateUpdatedEdge(G2Dir.D,
                        m_Nodes[selNode].GetEdge(G2Dir.D) * _scaleF);

                selNode = new IVector2(_node + IVector2.Xp);
                if (m_Nodes.InBounds(selNode))
                    m_Nodes[selNode] = m_Nodes[selNode].CreateUpdatedEdge(G2Dir.L,
                        m_Nodes[selNode].GetEdge(G2Dir.L) * _scaleF);

                selNode = new IVector2(_node + IVector2.Yn);
                if (m_Nodes.InBounds(selNode))
                    m_Nodes[selNode] = m_Nodes[selNode].CreateUpdatedEdge(G2Dir.U,
                        m_Nodes[selNode].GetEdge(G2Dir.U) * _scaleF);

                selNode = new IVector2(_node + IVector2.Xn);
                if (m_Nodes.InBounds(selNode))
                    m_Nodes[selNode] = m_Nodes[selNode].CreateUpdatedEdge(G2Dir.R,
                        m_Nodes[selNode].GetEdge(G2Dir.R) * _scaleF);

                if (_allDirs)
                {
                    selNode = new IVector2(_node + IVector2.XnYp);
                    if (m_Nodes.InBounds(selNode))
                        m_Nodes[selNode] = m_Nodes[selNode].CreateUpdatedEdge(G2Dir.DR,
                            m_Nodes[selNode].GetEdge(G2Dir.DR) * _scaleF);

                    selNode = new IVector2(_node + IVector2.XpYp);
                    if (m_Nodes.InBounds(selNode))
                        m_Nodes[selNode] = m_Nodes[selNode].CreateUpdatedEdge(G2Dir.DL,
                            m_Nodes[selNode].GetEdge(G2Dir.DL) * _scaleF);

                    selNode = new IVector2(_node + IVector2.XpYn);
                    if (m_Nodes.InBounds(selNode))
                        m_Nodes[selNode] = m_Nodes[selNode].CreateUpdatedEdge(G2Dir.UL,
                            m_Nodes[selNode].GetEdge(G2Dir.UL) * _scaleF);

                    selNode = new IVector2(_node + IVector2.XnYn);
                    if (m_Nodes.InBounds(selNode))
                        m_Nodes[selNode] = m_Nodes[selNode].CreateUpdatedEdge(G2Dir.UR,
                            m_Nodes[selNode].GetEdge(G2Dir.UR) * _scaleF);
                }
            }
            else
            {
                if (!m_Nodes.InBounds(_node)) return;
                var newWeight = m_Nodes[_node].Weight * _scaleF;
                m_Nodes[_node] = new Graph2dNode(newWeight, Bidirectional);
            }
        }

        public bool IsImpassable(IVector2 _node)
        {
            return GetNodeWeight(_node) >= IMPASSABLE;
        }

        /*public void DebugToCSV(string _path)
        {
            m_Nodes.DebugToCSV(_path);
        }*/

        private struct Graph2dNode : IEquatable<Graph2dNode>
        {
            private const string TAG_DEBUG = "ioSoftSmiths.Collections.Graph2dNode";
            private const double DEFAULT_WEIGHT = 1;
            private const double TOLERANCE = 0.00001;

            //UL, U, UR, R, DR, D, DL, L
            private readonly double[] m_EdgeWeight;  //For Bidirectional
            private readonly double? m_Weight; //For non-directional
            private readonly bool bidirectional;


            public Graph2dNode(double _defWeight, bool _bidirectional)
            {
                bidirectional = _bidirectional;
                if (bidirectional)
                {
                    m_EdgeWeight = new[] { _defWeight, _defWeight, _defWeight, _defWeight, _defWeight, _defWeight, _defWeight, _defWeight };
                    m_Weight = -1;
                }
                else
                {
                    m_EdgeWeight = null;
                    m_Weight = _defWeight;
                }
            }

            private Graph2dNode(double[] _weights)
            {
                bidirectional = true;
                if (_weights.Length != 8)
                    throw new ArgumentException("Graph2dNode Constructor: Received invalid array length.  (Should be 8)");

                m_EdgeWeight = new[] { _weights[0], _weights[1], _weights[2], _weights[3], _weights[4], _weights[5], _weights[6], _weights[7] };
                m_Weight = -1;
            }

            public double Weight
            {
                get
                {
                    return m_Weight.Value;
                }
            }


            public override bool Equals(Object obj)
            {
                return obj is Graph2dNode && this == (Graph2dNode)obj;

            }

            public static bool operator ==(Graph2dNode _a, Graph2dNode _b)
            {
                if (_a.bidirectional != _b.bidirectional) return false;

                if (_a.bidirectional)
                {
                    for (int i = 0; i < 8; ++i)
                    {
                        if (Math.Abs(_a.GetEdge((G2Dir)i) - _b.GetEdge((G2Dir)i)) > TOLERANCE) return false;
                    }
                    return true;
                }
                else
                {
                    return !(Math.Abs(_a.Weight) - Math.Abs(_b.Weight) > TOLERANCE);
                }
            }

            public static bool operator !=(Graph2dNode _a, Graph2dNode _b)
            {
                return !(_a == _b);
            }

            /// <summary>
            /// Bidirectional use only.  Returns edge weight in specified direction.
            /// </summary>
            /// <param name="_direction"></param>
            /// <returns></returns>
            internal double GetEdge(G2Dir _direction)
            {
                if (!bidirectional)
                    throw new NotImplementedException();
                if ((int)_direction < 0 || (int)_direction > 7) 
                    throw new ArgumentException();
                return m_EdgeWeight[(int)_direction];
            }

            /// <summary>
            /// Bidirectional use only.  Creates and returns an exact copy with the new weight at specified edge in specified direction.
            /// </summary>
            /// <param name="_direction"></param>
            /// <param name="_weight"></param>
            /// <returns></returns>
            internal Graph2dNode CreateUpdatedEdge(G2Dir _direction, double _weight)
            {
                if (bidirectional)
                {
                    if (m_EdgeWeight[(int)_direction] == _weight) return this;
                    var newWeights = new double[8];
                    for (int i = 0; i < 8; ++i)
                    {
                        if (i == (int)_direction)
                            newWeights[i] = _weight;
                        else
                            newWeights[i] = m_EdgeWeight[i];
                    }
                    return new Graph2dNode(newWeights);
                }
                throw new NotImplementedException();
            }

            public bool Equals(Graph2dNode other)
            {
                return base.Equals(other);
            }


            /// <summary>
            /// Debug
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                var sb = new StringBuilder();
                var debugArr = new int[8];
                for (int i = 0; i < 8; ++i)
                {

                    int val = (int)m_EdgeWeight[i];
                    if (m_EdgeWeight[i] > 9) val = 9;
                    debugArr[i] = val;
                }
                sb.Append("\"" + debugArr[0] + "|" + debugArr[1] + "|" + debugArr[2] + "\n");
                sb.Append(debugArr[7] + "|X|" + debugArr[3] + "\n");
                sb.Append(debugArr[6] + "|" + debugArr[5] + "|" + debugArr[4] + "\"");
                return sb.ToString();
            }
        }

    }




}
