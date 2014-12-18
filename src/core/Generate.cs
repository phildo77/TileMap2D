using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CDataArray;
using ClipperLib;
using ioSoftSmiths.Collections;
using ioSoftSmiths.ioLog;
using ioSoftSmiths.ioPathing;
using ioSoftSmiths.ioSupport;
using ioSoftSmiths.ioVector;
//using UnityEngine;

namespace ioSoftSmiths.TileMap
{
    public static class Generate
    {
        private const string TAG_DEBUG = "ioSoftSmiths.TileMap.Generate";
        public const string LOGKEY_MESSAGES = "TileMap.Generate.Messages";
        private const double MAX_ASPECT = 8.0;
        private static Random m_Random;
        private static ushort EmptyMaterial;
        private static ushort WallMaterial = 1;
        private static ushort FloorMaterial = 2;
        private static TileMap2D m_Tilemap;
        public static bool GenerateDone;
        public static double Progress;
        public static bool m_Generating;

        public static bool Generating
        {
            get { return m_Generating; }
        }

        public static bool FlipCoin()
        {
            return m_Random.Next(2) == 1;
        }

        public static TileMap2D GetGeneratedMap()
        {
            GenerateDone = false;
            var map = m_Tilemap;
            m_Tilemap = null;
            return map;
        }

        public static String DebugSettingsToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Dungeon Seed: " + Settings.RndSeed);
            sb.AppendLine("Map Aspect Ratio: " + Settings.AspectRatio);
            sb.AppendLine("Room Count (Min-Max): " + Settings.RoomCountMin + "-" + Settings.RoomCountMax);
            sb.AppendLine("Room Size (Min-Max): " + Settings.RoomSizeMin + "-" + Settings.RoomSizeMax);
            sb.AppendLine("Room Buffer (Min-Max): " + Settings.RoomBufferMin + "-" + Settings.RoomBufferMax);
            sb.AppendLine("Map Outer Buffer: " + Settings.MapOuterBuffer);
            sb.AppendLine("Room to Tunnel Buffer: " + Settings.TunnelBuffer);
            sb.AppendLine("Tunnel Pathing Magnetism: " + Settings.TunnelPathMag);
            sb.AppendLine("Extra Room Connection Pct: " + Settings.ExtraTunnelMinPct + "-" + Settings.ExtraTunnelMaxPct);
            sb.AppendLine("Tunnel Turns: " + Settings.TunnelTurningCost);
            sb.AppendLine("Allowed room shapes: ");
            foreach (var shape in Settings.AllowedRoomShapes)
                sb.AppendLine("   " + Enum.GetName(typeof (Room.ShapeType), shape));
            return sb.ToString();
        }

        public static void SetDefaultMaterials(ushort _wall, ushort _floor, ushort _empty = 0)
        {
            WallMaterial = _wall;
            FloorMaterial = _floor;
            EmptyMaterial = _empty;
        }

        //private static IEnumerator Dungeon()  //UNITY
        public static void Dungeon()
        {
            GenerateDone = false;
            m_Generating = true;
            //TileMap2D.meshRenderer.material.mainTexture = null;

            //Set Logging action
            Msg.CreateLog(LOGKEY_MESSAGES, LogVerbosity.HIGH, LogStyle.MESSAGE_ONLY, Settings.ActionForUserMessages);


            //Set random seed
            if (Settings.RndSeed == null)
            {
                byte[] rngCont = {0, 0, 0, 0};
                var rng = RandomNumberGenerator.Create();
                rng.GetBytes(rngCont);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(rngCont);

                Settings.RndSeed = BitConverter.ToInt32(rngCont, 0);
            }
            m_Random = new Random(Settings.RndSeed.Value);

            var timeStamp = DateTime.Now.Ticks/TimeSpan.TicksPerMillisecond;
            Msg.LogDebug(TAG_DEBUG, DebugSettingsToString(), MsgPriLvl.HIGH);


            var logMsg = "Creating Dungeon... (Seed [ " + Settings.RndSeed + " ])\n  Generating Rooms...";
            Msg.Log(LOGKEY_MESSAGES, LOGKEY_MESSAGES, logMsg, MsgPriLvl.HIGH);

            //1.  Generate and place rooms ===========================================================================

            //Set "aspect ratio" of entire dungeon space.
            var tarAspect = Settings.AspectRatio;
            if (tarAspect > MAX_ASPECT || tarAspect < 1f/MAX_ASPECT)
            {
                //invalid target aspect
                if (FlipCoin()) 
                {
                    tarAspect = m_Random.NextDouble() * (MAX_ASPECT - 1) + 1;
                }
                else 
                {
                    tarAspect = (m_Random.NextDouble()*1/MAX_ASPECT) + 1/MAX_ASPECT;
                }
            }

            Msg.LogDebug(TAG_DEBUG, "Dungeon Map Aspect Ratio: " + tarAspect, MsgPriLvl.HIGH);

            //Create rooms and place in map
            m_Tilemap = CreateMapAndPlaceRooms();
            
            logMsg = "Done placing rooms.  Creating Tunnels.";
            Msg.Log(LOGKEY_MESSAGES, LOGKEY_MESSAGES, logMsg, MsgPriLvl.HIGH);

            //2.  Add connectors/Tunnels and Draw to map ====================================================================
            m_Tilemap.m_EntryPoint = new IVector2(m_Random.Next(Settings.MapOuterBuffer, m_Tilemap.Dims.x),
                m_Random.Next(Settings.MapOuterBuffer, m_Tilemap.Dims.y));

            //Check special case entry points
            foreach (var room in m_Tilemap.m_Rooms)
            {
                if (room.Shape != Room.ShapeType.Ellipse) continue;
                while (true)
                    if (room.Bounds.Encapsulates(m_Tilemap.m_EntryPoint) &&
                        !room.ContainsCoord(m_Tilemap.m_EntryPoint, true))
                    {
                        m_Tilemap.m_EntryPoint = new IVector2(m_Random.Next(Settings.MapOuterBuffer, m_Tilemap.Dims.x),
                            m_Random.Next(Settings.MapOuterBuffer, m_Tilemap.Dims.y));
                    }
                    else
                    {
                        break;
                    }
            }

            Msg.LogDebug(TAG_DEBUG, "Entry point created at " + m_Tilemap.EntryPoint, MsgPriLvl.MED);

            //yield return TileMap2D.instance.StartCoroutine(CreateTunnels(m_Tilemap.EntryPoint));  //UNITY
            CreateTunnels(m_Tilemap.EntryPoint);

            if (m_Tilemap.m_Tunnels == null)
            {
                var newSeed = (int) Math.Pow(Settings.RndSeed.Value, 2);
                Msg.LogDebug(TAG_DEBUG,
                    "Bad Triangulation restarting Dungeon Thread with Seed " + Settings.RndSeed + " trying Seed " +
                    newSeed,
                    MsgPriLvl.HIGH);
                Settings.RndSeed = newSeed;
                //yield return Dungeon(); //UNITY
                //yield break; //UNITY
                Dungeon();
            }

            //Draw Tunnel walls
            foreach (var tunnelNet in m_Tilemap.m_Tunnels)
                foreach (var path in tunnelNet.Paths)
                {
                    var solution = new PolyTree();
                    var co = new ClipperOffset();
                    co.AddPath(Support2D.ToIntPoint(path.Points), JoinType.jtMiter, EndType.etOpenButt);
                    co.Execute(ref solution, 1);

                    m_Tilemap.m_MapData.DrawClosedPoly(Support2D.ToIVector2(solution.Childs[0].Contour), WallMaterial);
                }

            //Draw Entry Point Walls
            foreach (var neighbor in m_Tilemap.EntryPoint.Neighbors(true))
                if (m_Tilemap.m_MapData.InBounds(neighbor)) m_Tilemap.m_MapData[neighbor] = WallMaterial;

            //Draw room walls and floors
            DrawRooms(m_Tilemap.m_Rooms, ref m_Tilemap.m_MapData);


            //Draw Tunnel Floor
            foreach (var tunnel in m_Tilemap.m_Tunnels)
                foreach (var path in tunnel.Paths)
                    m_Tilemap.m_MapData.DrawPath(path.Points, FloorMaterial);

            //Add Doorways
            AddDoorways();

            var timeStampEnd = DateTime.Now.Ticks/TimeSpan.TicksPerMillisecond;
            logMsg = "Dungeon Created in " + (timeStampEnd - timeStamp) + "ms";
            //Log(logMsg);
            Msg.LogDebug(TAG_DEBUG, logMsg, MsgPriLvl.HIGH);

            GenerateDone = true;
            m_Generating = false;
            //m_MapHolder.m_MapData.Compressed = true;
            Msg.DeleteLog(LOGKEY_MESSAGES);
        }

        internal static void AddDoorways()
        {
            //Add Doorways
            //Get all paths that connect to rooms
            var roomConnectedPaths = new HashSet<TunnelNetwork.IPath>();
            foreach (var tNet in m_Tilemap.m_Tunnels)
                foreach (var path in tNet.Paths)
                    foreach (var room in m_Tilemap.m_Rooms)
                        if (path.ConnectsTo(room))
                            roomConnectedPaths.Add(path);

            foreach (var path in roomConnectedPaths)
                for (var roomSelect = 0; roomSelect <= 1; ++roomSelect)
                {
                    var curRoom = path.Rooms[roomSelect];

                    if (curRoom == null)
                        continue;

                    var tunScanIdx = 0;
                    var scanDir = 1;

                    if (roomSelect == 1)
                    {
                        tunScanIdx = path.Points.Count - 1;
                        scanDir = -1;
                    }

                    while (tunScanIdx > -1 && tunScanIdx < path.Points.Count)
                    {
                        //Check for two opposite walls and place door if true
                        var xCount = 0;
                        var yCount = 0;
                        var curPathCoord = path.Points[tunScanIdx];

                        if (!m_Tilemap.InBounds(curPathCoord + IVector2.Xp))
                            xCount++;
                        else if (m_Tilemap[curPathCoord + IVector2.Xp] == WallMaterial)
                            xCount++;

                        if (!m_Tilemap.InBounds(curPathCoord + IVector2.Xn))
                            xCount++;
                        else if (m_Tilemap[curPathCoord + IVector2.Xn] == WallMaterial)
                            xCount++;

                        if (!m_Tilemap.InBounds(curPathCoord + IVector2.Yp))
                            yCount++;
                        else if (m_Tilemap[curPathCoord + IVector2.Yp] == WallMaterial)
                            yCount++;

                        if (!m_Tilemap.InBounds(curPathCoord + IVector2.Yn))
                            yCount++;
                        else if (m_Tilemap[curPathCoord + IVector2.Yn] == WallMaterial)
                            yCount++;

                        if (xCount == 2 || yCount == 2 && xCount + yCount == 2)
                        {
                            curRoom.AddDoorway(curPathCoord);
                            break;
                        }
                        tunScanIdx += scanDir;
                    }
                }
        }

        private static void DrawRooms(IEnumerable<Room> _rooms, ref CDataArray2D<ushort> _map)
        {
            foreach (var room in _rooms)
            {
                foreach (var coord in room.GetWallCoords(false))
                    if (_map.InBounds(coord)) _map[coord] = WallMaterial;
                foreach (var coord in room.GetFloorCoords(false))
                    if (_map.InBounds(coord)) _map[coord] = FloorMaterial;
            }
        }


        /// <summary>
        /// Creates and places rooms.  Creates Tilemap containing created rooms.
        /// Creates set of boundaries and uses Settings to determine size and placement.
        /// </summary>
        private static TileMap2D CreateMapAndPlaceRooms()
        {

            var roomCount = m_Random.Next(Settings.RoomCountMin, Settings.RoomCountMax + 1);
            var roomBndys = new List<Bounds>();
            var mapBounds = new Bounds();
            var rooms = new RoomGroup();
            for (var bndyIdx = 0; bndyIdx < roomCount; ++bndyIdx)
            {
                var curPlacementBuffer = m_Random.Next(Settings.RoomBufferMin, Settings.RoomBufferMax + 1);
                var bndySizeX = m_Random.Next(Settings.RoomSizeMin, Settings.RoomSizeMax + 1) + curPlacementBuffer * 2;
                var bdnySizeY = m_Random.Next(Settings.RoomSizeMin, Settings.RoomSizeMax + 1) + curPlacementBuffer * 2;
                var origin = new IVector2(Settings.MapOuterBuffer, Settings.MapOuterBuffer);
                var curBnds = new Bounds(origin.x, origin.y, origin.x + bndySizeX, origin.y + bdnySizeY);

                if (bndyIdx == 0)
                {
                    mapBounds = new Bounds(new List<Bounds> { curBnds });
                    roomBndys.Add(curBnds);
                    continue;
                }

                var curAspect = mapBounds.Width / (double)mapBounds.Height;



                //Random room placement
                if (curAspect > Settings.AspectRatio) //Current map bounds too wide
                {
                    
                    var xTranslate = mapBounds.xMax - curBnds.Width;
                    curBnds.Translate(new IVector2(m_Random.Next(xTranslate < 0 ? 0 : xTranslate), 0));
                    if (Settings.RoomSpread == Settings.RSType.Loose)
                        curBnds.Translate(new IVector2(0, curBnds.yMax));
                   
                }
                else // Current map bounds too tall
                {
                    var yTranslate = mapBounds.yMax - curBnds.Height;
                    curBnds.Translate(new IVector2(0, m_Random.Next(yTranslate < 0 ? 0 : yTranslate)));
                    if (Settings.RoomSpread == Settings.RSType.Loose)
                        curBnds.Translate(new IVector2(curBnds.xMax, 0));
                }

                while (true)
                {
                    var overlaps = false;
                    for (var otherBndys = 0; otherBndys < bndyIdx; ++otherBndys)
                        if (curBnds.Overlaps(roomBndys[otherBndys])) overlaps = true;

                    if (!overlaps) break;
                    curBnds.Translate(curAspect > Settings.AspectRatio ? IVector2.Yp : IVector2.Xp);
                }

                roomBndys.Add(curBnds);
                mapBounds = new Bounds(roomBndys);
                var shape = Settings.AllowedRoomShapes[m_Random.Next(Settings.AllowedRoomShapes.Count)];
                rooms.Add(new Room(roomBndys[bndyIdx].ShrinkBy(curPlacementBuffer), shape));

                Progress = (bndyIdx/(double)(roomCount - 1))/2d;
                Msg.Log(LOGKEY_MESSAGES, LOGKEY_MESSAGES,
                    "Placing Rooms: " + (int)((double)bndyIdx / (roomCount - 1) * 100), MsgPriLvl.HIGH);
            }

            //Add outer buffer
            mapBounds = mapBounds.GrowBy(mapBounds.xMin);
            if (mapBounds.xMin != 0 || mapBounds.yMin != 0)
                throw new Exception("Map bounds origin not at 0,0 (" + mapBounds.xMin + ", " + mapBounds.yMin + ")");
            Msg.Log(LOGKEY_MESSAGES, LOGKEY_MESSAGES, roomCount + " rooms generated.", MsgPriLvl.HIGH);

            return new TileMap2D(new IVector2(mapBounds.Width, mapBounds.Height), EmptyMaterial) { m_Rooms = rooms };
        }

        //private static IEnumerator CreateTunnels(IVector2 _entryPt) //UNITY
        private static void CreateTunnels(IVector2 _entryPt)
        {
            var map = m_Tilemap.m_MapData;
            var points = new List<Point2D>();
            foreach (var room in m_Tilemap.m_Rooms)
                points.Add(new Point2D(room.Anchor.x, room.Anchor.y));

            var dt = new DelaunayTriangulation(points);
            var tris = dt.Triangulate(); //TODO Creates null on seed 22
            if (tris == null)
            {
                m_Tilemap.m_Tunnels = null;
                //yield break; //UNITY
            }

            var roomGraph = new ioGraph<IVector2>();

            foreach (var tri in tris)
            {
                var p1 = new IVector2((int) tri.a.x, (int) tri.a.y);
                var p2 = new IVector2((int) tri.b.x, (int) tri.b.y);
                var p3 = new IVector2((int) tri.c.x, (int) tri.c.y);

                roomGraph.AddEdge(p1, p2, m_Random.NextDouble()*100d);
                roomGraph.AddEdge(p2, p1, m_Random.NextDouble()*100d);
                roomGraph.AddEdge(p2, p3, m_Random.NextDouble()*100d);
                roomGraph.AddEdge(p3, p2, m_Random.NextDouble()*100d);
                roomGraph.AddEdge(p1, p3, m_Random.NextDouble()*100d);
                roomGraph.AddEdge(p3, p1, m_Random.NextDouble()*100d);
            }

            var roomMST = roomGraph.PrimMST();


            //Get extra room connections beyond mst and add them to the graph

            var extTunnelPct = (Settings.ExtraTunnelMinPct +
                                (Settings.ExtraTunnelMaxPct - Settings.ExtraTunnelMinPct)*m_Random.NextDouble());
            var remEdgeAddCnt = (int) ((roomGraph.EdgeCount - roomMST.EdgeCount)*extTunnelPct);

            Msg.LogDebug(TAG_DEBUG, "Room MST:", MsgPriLvl.HIGH);
            Msg.LogDebug(TAG_DEBUG, roomMST.ToString(), MsgPriLvl.HIGH);


            var remAvailEdges = roomGraph.GetNodeConnectionsList();

            foreach (var connection in roomMST.GetNodeConnectionsList())
            {
                if (remAvailEdges.Contains(connection)) remAvailEdges.Remove(connection);
                if (remAvailEdges.Contains(connection.Swapped)) remAvailEdges.Remove(connection.Swapped);
            }


            if (remEdgeAddCnt > remAvailEdges.Count)
            {
                Msg.LogDebug(TAG_DEBUG,
                    "Additional Edge count (beyond MST) greater than available edges remaining.  Trimming",
                    MsgPriLvl.HIGH);
                remEdgeAddCnt = remAvailEdges.Count;
            }

            for (var i = 0; i < remEdgeAddCnt; ++i)
            {
                var connection = remAvailEdges[m_Random.Next(remAvailEdges.Count)];

                var edge = new ioGraph<IVector2>.ioGraphEdge(1, connection.v2);
                roomMST.AddEdge(connection.v1, edge);
                remAvailEdges.Remove(connection);
            }

            Msg.LogDebug(TAG_DEBUG, "Room MST (with extra conns):", MsgPriLvl.HIGH);
            Msg.LogDebug(TAG_DEBUG, roomMST.ToString(), MsgPriLvl.HIGH);


            Msg.LogDebug(TAG_DEBUG, roomMST.GetEdgeDesc(), MsgPriLvl.LOW);

            //Create tunnel paths
            m_Tilemap.m_Tunnels = new List<TunnelNetwork>();

            var logMsg = "Creating Tunnels";
            Msg.Log(LOGKEY_MESSAGES, LOGKEY_MESSAGES, logMsg, MsgPriLvl.HIGH);
            var edgesDone = 0;

            double debugRoutingTime = 0;
            double debugAStarTime = 0;
            for (var nodeIdx = 0; nodeIdx < roomMST.NodeCount; ++nodeIdx)
            {
                foreach (var edge in roomMST.GetConnectionsFast(nodeIdx))
                {
                    var fromVector = roomMST.GetNode(nodeIdx);
                    var toVector = roomMST.GetNode(edge.ToNode);
                    Progress = 0.5d + (edgesDone/(double) roomMST.EdgeCount)/2d;
                    logMsg = "Creating Tunnels " + (int) ((edgesDone/(float) roomMST.EdgeCount)*100);
                    //Log(logMsg);
                    Msg.Log(LOGKEY_MESSAGES, LOGKEY_MESSAGES, logMsg, MsgPriLvl.HIGH);

                    var roomsToConnect = new[] {m_Tilemap.m_Rooms.Get(fromVector), m_Tilemap.m_Rooms.Get(toVector)};
                    Msg.LogDebug(TAG_DEBUG, fromVector + " -> " + toVector, MsgPriLvl.MED);

                    if (roomsToConnect[0] == null || roomsToConnect[1] == null)
                        throw new NullReferenceException(TAG_DEBUG + ":Room not found in roomGroup.");

                    //Find closest tunnel tie if exists =========================================
                    //If Rooms are already connected  then skip
                    var fromNets = new List<TunnelNetwork>();
                    var toNets = new List<TunnelNetwork>();

                    var skipConnection = false;
                    foreach (var tNet in m_Tilemap.m_Tunnels)
                    {
                        if (tNet.ConnectsTo(roomsToConnect[0])) fromNets.Add(tNet);
                        if (tNet.ConnectsTo(roomsToConnect[1])) toNets.Add(tNet);
                    }

                    if (fromNets.Any(toNets.Contains)) skipConnection = true;

                    if (skipConnection)
                    {
                        Msg.LogDebug(TAG_DEBUG, "Rooms already connected by same Net... Skipping", MsgPriLvl.MED);
                        edgesDone++;
                        continue;
                    }


                    if (Settings.ClosestConnect)
                    {
                        var fromCoordsf = new HashSet<IVector2>();
                        var toCoordsf = new HashSet<IVector2>();

                        //Get all coords of from and to nets (exluding room doorways)
                        foreach (var net in fromNets)
                            fromCoordsf.UnionWith(net.GetAllCoords(true));
                        foreach (var net in toNets)
                            toCoordsf.UnionWith(net.GetAllCoords(true));

                        //Remove room wall coordinates
                        foreach (var room in m_Tilemap.m_Rooms)
                        {
                            fromCoordsf.RemoveWhere(_coord => room.GetWallCoords(true).Contains(_coord));
                            toCoordsf.RemoveWhere(_coord => room.GetWallCoords(true).Contains(_coord));
                        }

                        //Add anchors of rooms to connect
                        fromCoordsf.Add(fromVector);
                        toCoordsf.Add(toVector);

                        //Use Manhattan Distance for distance calc
                        var distance = AStar2D.ManhattanDistance(fromVector, toVector, 1);
                        var closestTunnelCoords =
                            Support2D.GetNearestCoords(fromCoordsf, toCoordsf, ref distance);
                        if (closestTunnelCoords[0] != null)
                            if (!closestTunnelCoords[0].Value.Equals(fromVector))
                            {
                                roomsToConnect[0] = null;
                                fromVector = closestTunnelCoords[0].Value;
                            }

                        if (closestTunnelCoords[1] != null)
                            if (!closestTunnelCoords[1].Value.Equals(toVector))
                            {
                                roomsToConnect[1] = null;
                                toVector = closestTunnelCoords[1].Value;
                            }
                    }


                    Msg.LogDebug(TAG_DEBUG, "Creating Routing Cost Map.", MsgPriLvl.MED);
                    var routingTimeStamp = DateTime.Now;
                    var costGrid = CreateRoutingCostMap(roomsToConnect.ToList(), new IVector2(map.Dims.x, map.Dims.y),
                        Settings.TunnelBuffer, Settings.TunnelPathMag, Settings.CalcRoutingStrength);
                    debugRoutingTime += (DateTime.Now - routingTimeStamp).TotalSeconds;
                    Msg.LogDebug(TAG_DEBUG,
                        "Rounting map finished. Took " + (DateTime.Now - routingTimeStamp).TotalSeconds + " seconds.",
                        MsgPriLvl.MED);

                    //UNITY DEBUG
                    /*
                    // Unity Draw routing map to texture



                    //Clear texture to default weight


                    var texture = (Texture2D)TileMap2D.meshRenderer.material.mainTexture;
                    if (!texture)
                    {

                        texture = new Texture2D(costGrid.Dims.x, costGrid.Dims.y);
                        texture.filterMode = FilterMode.Point;
                        TileMap2D.meshRenderer.material.mainTexture = texture;
                        if (costGrid.Dims.x > costGrid.Dims.y)
                            TileMap2D.instance.transform.localScale = new Vector3(1, 0, costGrid.Dims.y / (float)costGrid.Dims.x);
                        else
                            TileMap2D.instance.transform.localScale = new Vector3(costGrid.Dims.x / (float)costGrid.Dims.y, 1, 1);
                    }
                    if (true)
                    {

                        var defCost = Settings.CalcRoutingStrength * 2d + 1;
                        var weightMax = (defCost + defCost * Settings.TunnelBuffer);

                        var weights = costGrid.Weights;
                        for (int y = 0; y < costGrid.Dims.y; ++y)
                            for (int x = 0; x < costGrid.Dims.x; ++x)
                            {
                                double weight = weights[x, y];
                                float whiteLevel = (float)(weight / defCost);
                                var color = new Color(whiteLevel / 2f, whiteLevel / 2f, whiteLevel / 2f, 1f);
                                if (whiteLevel > 1f)
                                {
                                    whiteLevel = (float)(weight - defCost) / (float)(weightMax - defCost);
                                    whiteLevel = (whiteLevel / 2f + 0.5f);
                                    color = new Color(whiteLevel, whiteLevel, whiteLevel, 1f);
                                }
                                texture.SetPixel(x, y, color);
                            }

                        var doors = new HashSet<IVector2>();
                        foreach (var tNet in m_Tilemap.m_Tunnels)
                            foreach (var room in tNet.ConnectedRooms)
                                if (room != null) doors.UnionWith(room.Doorways);

                        foreach (var doorway in doors)
                            texture.SetPixel(doorway.x, doorway.y, new Color(0f, 0f, 1f, 1f));

                        texture.Apply();

                        yield return null;
                    }
                    // --------------------------
                    */ //END UNITY BLOCK


                    Msg.LogDebug(TAG_DEBUG, "Running AStar algorithm...", MsgPriLvl.MED);
                    routingTimeStamp = DateTime.Now;
                    var routingStr = Settings.CalcRoutingStrength;
                    var turnCost = Settings.CalcRoutingStrength*Settings.CalcTurnCost;
                    if (routingStr == 0)
                        turnCost = Settings.CalcTurnCost;
                    /*yield return  //UNITY
                        TileMap2D.instance.StartCoroutine(AStar2D.AStar(costGrid, fromVector, toVector,
                            turnCost, Settings.TunnelPathMag,
                            AStar2D.ManhattanDistance));
                    */
                    AStar2D.AStar(costGrid, fromVector, toVector, turnCost, Settings.TunnelPathMag,
                        AStar2D.ManhattanDistance);
                    debugAStarTime += (DateTime.Now - routingTimeStamp).TotalSeconds;
                    Msg.LogDebug(TAG_DEBUG,
                        "AStar finished. Took " + (DateTime.Now - routingTimeStamp).TotalSeconds + " seconds.",
                        MsgPriLvl.MED);

                    var vecPath = AStar2D.AStarPath;

                    if (vecPath == null)
                    {
                        logMsg = "Unable to create path:  " + fromVector + " --> " + toVector;
                        //Log(logMsg);
                        Msg.LogDebug(TAG_DEBUG, logMsg, MsgPriLvl.HIGH);
                        continue;
                    }

                    var path = new TunnelNetwork.IPath(vecPath, roomsToConnect[0], roomsToConnect[1]);
                    TrimTunnelAtRooms(ref path);
                    AddPathToNetwork(path);


                    ++edgesDone;
                }
            }

            //Add Entry Point and connect to closest room

            var connectEntry = true;
            var entryDist = int.MaxValue;

            //Connect entry point to closest room or tunnel
            foreach (var room in m_Tilemap.m_Rooms)
                if (room.ContainsCoord(_entryPt, true))
                {
                    connectEntry = false;
                    break;
                }

            if (connectEntry)
            {
                var availCoords = new HashSet<IVector2>();
                foreach (var tNet in m_Tilemap.m_Tunnels)
                    availCoords.UnionWith(tNet.GetAllCoords(false));

                foreach (var room in m_Tilemap.m_Rooms)
                    availCoords.RemoveWhere(_coord => room.GetWallCoords(true).Contains(_coord));

                availCoords.UnionWith(m_Tilemap.m_Rooms.Anchors);

                double distance = m_Tilemap.Dims.x + m_Tilemap.Dims.y;
                var closestCoords = Support2D.GetNearestCoords(new HashSet<IVector2> {_entryPt}, availCoords,
                    ref distance);

                var closestRoom = m_Tilemap.m_Rooms.Get(closestCoords[1].Value);
                var excludeList = new List<Room>();
                if (closestRoom != null)
                    excludeList.Add(closestRoom);

                var turnCost = Settings.CalcRoutingStrength*Settings.CalcTurnCost;
                var entryRouteGraph = CreateRoutingCostMap(excludeList,
                    new IVector2(map.Dims.x, map.Dims.y), Settings.RoomBufferMax, Settings.TunnelPathMag,
                    Settings.CalcRoutingStrength);
                /*  //UNITY
                yield return TileMap2D.instance.StartCoroutine(AStar2D.AStar(entryRouteGraph, _entryPt, closestCoords[1].Value,
                    turnCost, Settings.TunnelPathMag,
                    AStar2D.ManhattanDistance));
                 * */
                AStar2D.AStar(entryRouteGraph, _entryPt, closestCoords[1].Value,
                    turnCost, Settings.TunnelPathMag,
                    AStar2D.ManhattanDistance);
                var entryConnect = AStar2D.AStarPath;

                //TODO make entry point a room?
                /*var entryRoom = new Room(new Bounds(_entryPt.x - 1, _entryPt.y - 1, _entryPt.x + 1, _entryPt.y + 1),
                    Room.ShapeType.Rectangle);
                m_Tilemap.m_Rooms.Add(entryRoom);*/

                var entryPath = new TunnelNetwork.IPath(entryConnect, null, closestRoom);
                TrimTunnelAtRooms(ref entryPath);
                AddPathToNetwork(entryPath);
            }

            Msg.LogDebug(TAG_DEBUG, "Total routing graph time: " + debugRoutingTime, MsgPriLvl.HIGH);
            Msg.LogDebug(TAG_DEBUG, "Total AStar time: " + debugAStarTime, MsgPriLvl.HIGH);
        }

        private static void AddPathToNetwork(TunnelNetwork.IPath _path)
        {
            //Get all nets that this path crosses/touches
            var crossedNets = new HashSet<TunnelNetwork>();
            foreach (var tNet in m_Tilemap.m_Tunnels)
                if (tNet.Intersects(_path))
                    crossedNets.Add(tNet);

            //If path doesn't cross any existing nets then add it to a new one.
            if (crossedNets.Count == 0)
            {
                m_Tilemap.m_Tunnels.Add(new TunnelNetwork(_path));
                return;
            }

            //Remove crossed nets from map and Create new net with them + this path 
            var newNet = new TunnelNetwork(crossedNets, _path);

            foreach (var tNet in crossedNets)
                m_Tilemap.m_Tunnels.Remove(tNet);

            m_Tilemap.m_Tunnels.Add(newNet);
        }

        private static void TrimTunnelAtRooms(ref TunnelNetwork.IPath _path)
        {
            //Trim tunnel at room walls
            for (var roomSelect = 0; roomSelect <= 1; ++roomSelect)
            {
                var curRoom = _path.Rooms[roomSelect];

                if (curRoom == null)
                    continue;
                var walls = curRoom.GetWallCoords(true);

                var tunScanIdx = 0;
                var scanDir = 1;
                if (roomSelect == 1)
                {
                    tunScanIdx = _path.Points.Count - 1;
                    scanDir = -1;
                }

                var skip = false;
                //Scan for innermost wall from end of path in room and store for end trim
                while (!walls.Contains(_path.Points[tunScanIdx]))
                {
                    tunScanIdx = tunScanIdx + scanDir;
                    if (tunScanIdx == -1 || tunScanIdx == _path.Points.Count)
                    {
                        skip = true;
                        break;
                    }
                }
                if (skip) continue;

                var trimIdx = tunScanIdx;

                if (roomSelect == 0)
                {
                    _path.Points.RemoveRange(0, trimIdx);
                }
                else
                {
                    _path.Points.RemoveRange(trimIdx + 1, (_path.Points.Count - 1) - trimIdx);
                }
            }
        }

        public static Graph2d CreateRoutingCostMap(List<Room> _exclude, IVector2 _size, int _roomBuffer,
            int _tunnelMagBuffer, double _tunnelRoutingStrength)
        {
            var defCost = _tunnelRoutingStrength*2d + 1;

            var routingGraph = new Graph2d(_size, defCost, false);


            foreach (var room in m_Tilemap.m_Rooms)
            {
                if (_exclude.Contains(room))
                {
                    //Add special case routing per room shape
                    switch (room.Shape)
                    {
                        //Rect rooms add corners as impassable
                        case Room.ShapeType.Rectangle:
                            foreach (var corner in room.Corners)
                                routingGraph.SetNodeWeight(corner, double.MaxValue);
                            break;
                        default:
                            break;
                    }
                    continue;
                }

                //Add room walls as impassable with specified buffer
                List<IVector2> bufferPoly;

                //Pre-calculate costs for room buffer
                var cost = new double[_roomBuffer + 1];
                for (var buffer = _roomBuffer; buffer > 0; --buffer)
                {
                    cost[buffer] = defCost + defCost*(_roomBuffer - buffer);
                }
                cost[0] = double.MaxValue;

                //Add room buffer costs
                for (var buffer = _roomBuffer; buffer >= 0; --buffer)
                {
                    if (buffer != 0)
                    {
                        var co = new ClipperOffset();
                        var wallCoords = room.GetWallCoords(true);
                        co.AddPath(Support2D.ToIntPoint(wallCoords), JoinType.jtMiter,
                            EndType.etClosedPolygon);
                        var pt = new PolyTree();
                        co.Execute(ref pt, buffer);

                        bufferPoly = Support2D.ToIVector2(pt.Childs[0].Contour);

                        var path = Support2D.CreatePath(bufferPoly, true);

                        foreach (var coord in path)
                            routingGraph.SetNodeWeight(coord, cost[buffer] + routingGraph.GetNodeWeight(coord));
                    }
                    else
                    {
                        foreach (var coord in room.GetWallCoords(true))
                            routingGraph.SetNodeWeight(coord, cost[0]);
                    }
                }
            }


            if (_tunnelMagBuffer >= 0 && Settings.CalcRoutingStrength != 0)
            {
                //Pre-calcluate costs for tunnel magnetism
                var tunnelCost = new double[_tunnelMagBuffer + 1];
                tunnelCost[0] = 1d;
                for (var tScaleIdx = 1; tScaleIdx <= _tunnelMagBuffer; ++tScaleIdx)
                    tunnelCost[tScaleIdx] = tunnelCost[0] + _tunnelRoutingStrength +
                                            (tScaleIdx - 1)*(defCost - 1 - _tunnelRoutingStrength)/_tunnelMagBuffer;
                foreach (var tunnel in m_Tilemap.m_Tunnels)
                {
                    for (var tOffset = _tunnelMagBuffer; tOffset >= 0; --tOffset)
                    {
                        //double cost = _defaultCost - _defaultCost/(tOffset + 1);
                        foreach (var path in tunnel.Paths)
                        {
                            if (tOffset > 0)
                            {
                                var solution = new PolyTree();
                                var co = new ClipperOffset();
                                co.AddPath(Support2D.ToIntPoint(path.Points), JoinType.jtMiter,
                                    EndType.etOpenButt);
                                //co.AddPath(Support2D.ToIntPoint(path.PointsReverse), JoinType.jtSquare,
                                //    EndType.etOpenButt);
                                co.Execute(ref solution, tOffset);


                                foreach (var child in solution.Childs)
                                {
                                    var tunnelPath = Support2D.CreatePath(Support2D.ToIVector2(child.Contour), true);


                                    foreach (var coord in tunnelPath)
                                    {
                                        if (routingGraph.GetNodeWeight(coord) != double.MaxValue)
                                        {
                                            var curCost = routingGraph.GetNodeWeight(coord);
                                            if (curCost <= defCost && curCost > tunnelCost[tOffset])
                                                routingGraph.SetNodeWeight(coord, tunnelCost[tOffset]);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                foreach (var coord in path.Points)
                                    if (routingGraph.GetNodeWeight(coord) != double.MaxValue)
                                        routingGraph.SetNodeWeight(coord, tunnelCost[0]);
                                //routingGraph.ScaleNodeWeight(coord, 0.1f * (float)_defaultCost);
                            }
                        }
                    }
                }
            }
            return routingGraph;
        }

        public static class Settings
        {
            public enum MagStrength : long
            {
                None = 0,
                Minimum = 1000000000000,
                Medium = 1000000,
                Maximum = 1
            }

            public enum TTCost
            {
                High = 4,
                Normal = 2,
                Low = 1,
                None = 0
            }

            //Dungeon Seed (null = random by system time)
            public static int? RndSeed;

            //Action for messages generated for the user
            public static Action<string> ActionForUserMessages = (Console.WriteLine);

            //Overall aspect ratio target for the entire map (0 = random)
            public static double AspectRatio = 0;

            //Room count and size
            public static int RoomCountMin = 5;
            public static int RoomCountMax = 10;
            public static int RoomSizeMin = 6;
            public static int RoomSizeMax = 10;

            //Buffer between rooms
            public static int RoomBufferMin = 5;
            public static int RoomBufferMax = 8;

            //Outer map buffer (empty space)
            public static int MapOuterBuffer = 3;

            //Sets whether or not routing is done between closest available points on the map or only room to room.
            public static bool ClosestConnect = true;

            //Buffer push from room edge to tunnel routing
            public static int TunnelBuffer = 1;

            //Tunnel routing attraction (higher should pull tunnels into eachother)
            public static int TunnelPathMag = 3;

            //Tunnel routing strength (higher should mean less of a web / exisiting tunnels used more)
            public static MagStrength TunnelRoutingStrength = MagStrength.Medium;

            //Percentage of extra room connections beyond MST
            public static double ExtraTunnelMinPct = 0.2;
            public static double ExtraTunnelMaxPct = 0.4;

            //Sets cost multiplier for making turns when routing tunnels
            public static TTCost TunnelTurningCost = TTCost.Normal;

            //Allowable room shapes
            public static List<Room.ShapeType> AllowedRoomShapes = new List<Room.ShapeType>
            {
                Room.ShapeType.Rectangle,
                Room.ShapeType.Ellipse
            };

            //Room shape percentage (Needs to add up to 1.0 and match the count in AllowedRoomShapes)
            //public static List<float> AllowedRoomMixPct = new List<float> {0.8f, 0.2f};


            public static RSType RoomSpread = RSType.Tight;

            public enum RSType : byte
            {
                Tight,
                Loose
            }


            internal static double CalcRoutingStrength
            {
                get
                {
                    if (TunnelRoutingStrength == 0)
                        return 0;
                    return (1d/(long) TunnelRoutingStrength)*100;
                }
            }

            internal static float CalcTurnCost
            {
                get { return (float) TunnelTurningCost/2f; }
            }
        }
    }
}