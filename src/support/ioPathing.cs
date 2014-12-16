using System;
using System.Collections.Generic;
using ioSoftSmiths.Collections;
using ioSoftSmiths.ioLog;
using ioSoftSmiths.ioVector;
//using UnityEngine;

namespace ioSoftSmiths.ioPathing
{
    public static class AStarGen
    {
        private static string TAG_DEBUG = "ioSoftSmiths.Pathing.AStarGen";

        public static List<int> AStar<T>(ioGraph<T> _graph, int _start, int _end, Func<int, double> _fHeuristic)
        {
            //var startRecord = new NodeRecord(_start, -1, 0, _fHeuristic(_start));
            var startRecord = new NodeRecord<T>(_start, null, 0, _fHeuristic(_start));

            var openList = new Dictionary<int, NodeRecord<T>>();
            openList.Add(_start, startRecord);
            var closedList = new Dictionary<int, NodeRecord<T>>();
            int curNode = -1;

            while (openList.Count > 0)
            {
                //Find smallest element in open list (est total cost)
                curNode = FindSmallest(openList);

                //Exit if goal found
                if (curNode == _end)
                {
                    closedList.Add(curNode, openList[curNode]);
                    break;
                }

                var curEdges = _graph.GetConnectionsFast(curNode);

                foreach (ioGraph<T>.ioGraphEdge edge in curEdges)
                {

                    //Set end node and add cost
                    var endNode = edge.ToNode;
                    var endNodeCost = openList[curNode].CostSoFar + edge.Weight;
                    NodeRecord<T> endNodeRecord;
                    double endNodeHeuristic;

                    if (openList.ContainsKey(endNode))
                    {
                        if (openList[endNode].CostSoFar <= endNodeCost) continue;
                        openList.Remove(endNode);
                    }
                    if (closedList.ContainsKey(endNode))
                    {
                        if (closedList[endNode].CostSoFar <= endNodeCost) continue;
                        closedList.Remove(endNode);
                    }

                    endNodeRecord = new NodeRecord<T>();
                    endNodeRecord.FromNode = curNode;
                    endNodeHeuristic = _fHeuristic(endNode);

                    endNodeRecord.CostSoFar = endNodeCost;
                    endNodeRecord.Connection = edge;
                    endNodeRecord.EstTotalCost = endNodeCost + endNodeHeuristic;
                    openList.Add(endNode, endNodeRecord);


                    /*if(closedList.ContainsKey(endNode))
                    {
                        
                        if (closedList[endNode].CostSoFar <= endNodeCost)
                            continue;

                        endNodeRecord = closedList[endNode];
                        endNodeHeuristic = endNodeRecord.EstTotalCost - endNodeRecord.CostSoFar;
                        closedList.Remove(endNode);
                    } 
                    if(openList.ContainsKey(endNode))
                    {
                        if (openList[endNode].CostSoFar <= endNodeCost)
                            continue;

                        endNodeRecord = openList[endNode];
                        endNodeHeuristic = endNodeRecord.EstTotalCost - openList[endNode].CostSoFar;
                        openList.Remove(endNode);

                    } 
                    else
                    {
                        endNodeRecord = new NodeRecord();
                        endNodeRecord.FromNode = curNode;
                        endNodeHeuristic = _fHeuristic(endNode);
                    }

                    endNodeRecord.CostSoFar = endNodeCost;
                    endNodeRecord.Connection = edge;
                    endNodeRecord.EstTotalCost = endNodeCost + endNodeHeuristic;

                    if (!openList.ContainsKey(endNode)) openList.Add(endNode, endNodeRecord);*/
                }

                closedList.Add(curNode, openList[curNode]);
                openList.Remove(curNode);
            }
            if (curNode != _end)
            {
                String dbgMsg = "End Node Null -- No path found?";
                Msg.LogDebug(TAG_DEBUG, dbgMsg, MsgPriLvl.HIGH);
                return null;
            }
            else
            {
                var path = new List<int>();
                while (curNode != _start)
                {
                    path.Add(curNode);
                    Msg.LogDebug(TAG_DEBUG, "Added to path: " + _graph.GetNode(curNode), MsgPriLvl.LOW);
                    curNode = closedList[curNode].FromNode;  //Seed 705 crashes here with invalid key
                }

                return path;
            }



        }

        private static int FindSmallest<T>(Dictionary<int, NodeRecord<T>> _list)
        {
            if (_list.Count == 0)
                throw new Exception("FindSmallest: Received Empty List");

            double minCost = double.MaxValue;
            int key = -1;
            foreach (KeyValuePair<int, NodeRecord<T>> node in _list)
            {
                if (node.Value.EstTotalCost < minCost)
                {
                    minCost = node.Value.EstTotalCost;
                    key = node.Key;
                }
            }

            //Debug
            if (key == -1) //problem
                key = int.MaxValue;

            return key;

        }

        private struct NodeRecord<T>
        {
            //int m_Node;
            //public int Node { get { return m_Node; } }
            public int FromNode;
            public ioGraph<T>.ioGraphEdge Connection;
            public double CostSoFar;
            public double EstTotalCost;

            public NodeRecord(int _fromNode, ioGraph<T>.ioGraphEdge _connection, float _CostSoFar, double _EstTotalCost)
            {
                FromNode = _fromNode;
                Connection = _connection;
                CostSoFar = _CostSoFar;
                EstTotalCost = _EstTotalCost;
            }
        }



    }

    public class AStar2D
    {
        public const string TAG_DEBUG = "ioSoftSmiths.Pathing.AStar2D";

        public static List<IVector2> AStarPath;


        private class AStarOpenNode : IComparable<AStarOpenNode>
        {
            public readonly double CostSoFar;
            private readonly double EstTotalCost;
            public IVector2? Parent;
            public IVector2 Coord;

            public AStarOpenNode(IVector2 _coord, IVector2? _parent, double _costSoFar, double _estTotalCost)
            {
                Coord = _coord;
                Parent = _parent;
                CostSoFar = _costSoFar;
                EstTotalCost = _estTotalCost;
            }

            public int CompareTo(AStarOpenNode other)
            {
                return EstTotalCost.CompareTo(other.EstTotalCost);
            }

            public G2Dir StepDirection
            {
                get
                {
                    var dir = DirMap.Get(Coord - Parent.Value);
                    return dir;
                }
            }
        }

        private struct AStarClosedNode
        {
            public readonly IVector2? Parent;
            public readonly double CostSoFar;

            public AStarClosedNode(IVector2? _parent, double _costSoFar)
            {
                Parent = _parent;
                CostSoFar = _costSoFar;
            }

            public AStarClosedNode(AStarOpenNode _node)
            {
                Parent = _node.Parent;
                CostSoFar = _node.CostSoFar;
            }

            public object Clone()
            {
                IVector2? newCoord;
                if (Parent == null)
                    newCoord = null;
                else
                    newCoord = Parent.Value;

                return new AStarClosedNode(newCoord, CostSoFar);

            }
        }

        public static double ManhattanDistance(IVector2 _from, IVector2 _to, double _defaultVal)
        {
            double result = Math.Abs(_from.x - _to.x) + Math.Abs(_from.y - _to.y);
            return result;
        }

        public static double DiagonalDistance(IVector2 _from, IVector2 _to, double _defaultVal)
        {
            var a = Math.Abs(_from.x - _to.x);
            var b = Math.Abs(_from.y - _to.y);
            return (double)Math.Ceiling(Math.Sqrt(a * a + b * b));
        }


        private static int debugFrameCount = 0;

        //public static IEnumerator AStar(Graph2d _graph, IVector2 _start, IVector2 _end, double _turnCostAdder, double _defVaul, Func<IVector2, IVector2, double, double> _heuristic)
        public static void AStar(Graph2d _graph, IVector2 _start, IVector2 _end, double _turnCostAdder, double _defVaul, Func<IVector2, IVector2, double, double> _heuristic)
        {

            bool debugMe = true;
            int frameUpdateRate = 30;

            var openList = new ioMinPriQ<AStarOpenNode>();
            openList.Enqueue(new AStarOpenNode(_start, null, 0, _heuristic(_start, _end, _defVaul)));
            var closedList = new Dictionary<IVector2, AStarClosedNode>();

            /* //UNITY
            var texture = (Texture2D)TileMap2D.meshRenderer.material.mainTexture;
            if (debugMe)
            {

                texture.SetPixel(_start.x, _start.y, new Color(0.6f, 1f, 0.6f, 1));
                texture.SetPixel(_end.x, _end.y, new Color(0.6f, 1f, 0.6f, 1));
                texture.Apply();
            }
             * */

            AStarOpenNode curNode = null;
            var prevDir = -1;

            while (!openList.IsEmpty)
            {
                //Find smallest element in open list (est total cost)
                curNode = openList.Dequeue();

                //Get direction of step from current node
                if (curNode.Parent != null)
                    prevDir = (int)curNode.StepDirection;

                //Exit if goal found
                if (curNode.Coord.Equals(_end))
                {
                    closedList[_end] = new AStarClosedNode(curNode);
                    break;
                }

                for (int i = 1; i < 8; i = i + 2)
                //foreach (var neighbor in curNode.Coord.Neighbors())
                {
                    IVector2 neighbor = curNode.Coord + DirMap.Get(i);
                    if (!_graph.InBounds(neighbor) || (_graph.GetNodeWeight(neighbor) == double.MaxValue)) continue;
                    //Set end node and add cost
                    //var endNode = neighbor;
                    //var endNodeCost = curNode.CostSoFar + _graph.GetEdgeWeight(curNode.Coord, (G2Dir) i);//_graph[neighbor].GetEdge((G2Dir)i);
                    var endNodeCost = curNode.CostSoFar + _graph.GetNodeWeight(neighbor);
                    if (i != prevDir && prevDir != -1) endNodeCost += _turnCostAdder;

                    //Skip if not walkable
                    if (endNodeCost == double.MaxValue) continue;

                    //Skip if better path already exists
                    var skipNode = false;
                    for (int j = 0; j < openList.DataList.Count; ++j)
                    {
                        if (openList.DataList[j].Coord.Equals(neighbor))
                        {
                            if (openList.DataList[j].CostSoFar <= endNodeCost)
                            {
                                skipNode = true;
                                break;
                            }
                            openList.Remove(openList.DataList[j]);
                        }
                    }
                    if (skipNode) continue;

                    //Skip or update if current path is worse/better
                    if (closedList.ContainsKey(neighbor))
                    {
                        if (closedList[neighbor].CostSoFar <= endNodeCost) continue;
                        closedList[neighbor] = new AStarClosedNode(curNode.Coord, endNodeCost);
                        
                        /* //UNITY
                        if (debugMe)
                            texture.SetPixel(neighbor.x, neighbor.y, new Color(0.0f, 1f, 1f, 1f));
                         */
                    }


                    var endNode = new AStarOpenNode(neighbor, curNode.Coord, endNodeCost, endNodeCost + _heuristic(neighbor, _end, _defVaul));

                    openList.Enqueue(endNode);


                }

                closedList[curNode.Coord] = new AStarClosedNode(curNode);

                /* //UNITY
                if (debugMe)
                {

                    var pixel = texture.GetPixel(curNode.Coord.x, curNode.Coord.y);
                    pixel = new Color(pixel.r + 0.3f, pixel.g, pixel.b, pixel.a);
                    texture.SetPixel(curNode.Coord.x, curNode.Coord.y, pixel);
                    texture.Apply();
                    if (++debugFrameCount == frameUpdateRate)
                    {
                        debugFrameCount = 0;
                        yield return null;
                    }

                }
                 */
            }
            if (curNode.Coord != _end)
            {
                String dbgMsg = "End Node Null -- No path found?";
                Msg.LogDebug(TAG_DEBUG, dbgMsg, MsgPriLvl.HIGH);
                //return null; new List<IVector2>();
            }
            else
            {
                var path = new List<IVector2>() { curNode.Coord };
                var closedNode = new AStarClosedNode(curNode);
                ;
                while (closedNode.Parent != null)
                {
                    path.Insert(0, closedNode.Parent.Value);
                    Msg.LogDebug(TAG_DEBUG, "Added to path: " + closedNode.Parent, MsgPriLvl.LOW);
                    closedNode = closedList[closedNode.Parent.Value];  //TODO Seed 705 crashes here with invalid key
                }
                AStarPath = path;

                closedList[curNode.Coord] = new AStarClosedNode(curNode);
                /* //UNITY
                if (debugMe)
                {
                    foreach (var coord in path)
                        texture.SetPixel(coord.x, coord.y, new Color(0.8f, 0.8f, 1f, 1));
                    texture.Apply();
                    yield return null;
                }
                */

            }
            //yield return null; //UNITY

        }

    }



}
