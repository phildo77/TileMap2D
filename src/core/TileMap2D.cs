using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CDataArray;
using ioSoftSmiths.Collections;
using ioSoftSmiths.ioLog;
using ioSoftSmiths.ioVector;
using ioSoftSmiths.ioSupport;

namespace ioSoftSmiths.TileMap
{
    public class TileMap2D //: MonoBehaviour
    {

        // UNITY TileMapVisualizer Debug SPECIFIC
        /*
        public static MeshRenderer meshRenderer;
        public static TileMap2D instance;


        void Start()
        {

            meshRenderer = GetComponent<MeshRenderer>();
            instance = this;


            Msg.DebugVerbosity = LogVerbosity.LOW;
            Msg.DebugAction = (_group, _msg) => Debug.Log(_group + " : " + _msg);

            Generate.SetDefaultMaterials(1, 2);

            //Generate.Settings.RndSeed = -1708127739;

            Generate.Settings.AspectRatio = 0f;

            Generate.Settings.RoomBufferMin = 1;
            Generate.Settings.RoomBufferMax = 3;
            Generate.Settings.TunnelBuffer = 3;
            Generate.Settings.TunnelPathMag = 3;
            Generate.Settings.TunnelRoutingStrength = Generate.Settings.MagStrength.Minimum;
            Generate.Settings.ClosestConnect = false;
            Generate.Settings.TunnelTurningCost = Generate.Settings.TTCost.Normal;

            Generate.Settings.RoomCountMin = 10;
            Generate.Settings.RoomCountMax = 13;
            Generate.Settings.RoomSizeMin = 6;
            Generate.Settings.RoomSizeMax = 20;

            Generate.Settings.RoomSpreadFactor = 0;

            Debug.Log(Generate.DebugSettingsToString());

            //System.Console.ReadLine();
            StartCoroutine(Generate.Dungeon());



        }

        void Update()
        {

            if (Generate.GenerateDone)
            {
                var map = Generate.GetGeneratedMap();
                map.DebugToString();
                var ep = map.EntryPoint;
                var curTexture = (Texture2D)meshRenderer.material.mainTexture;
                curTexture.SetPixel(ep.x, ep.y, new Color(1, 0, 0, 1));
                curTexture.Apply();

                //tileMap = Generate.GetGeneratedMap();
                Debug.Log("Map generated.");

            }

            if (Input.GetMouseButtonDown(0))
                if (!Generate.Generating)
                    StartCoroutine(Generate.Dungeon());

        }

        */
        // END UNITY -----------------------------------------------------------------------------

        private const string TAG_DEBUG = "ioSoftSmiths.TileMap.TileMap2D";
        internal CDataArray2D<ulong> m_MapData;
        internal List<TunnelNetwork> m_Tunnels;
        internal RoomGroup m_Rooms;
        internal IVector2 m_EntryPoint;
        public IVector2 EntryPoint { get { return m_EntryPoint; } }
        public Graph2d m_PathingGraph; 

        public IVector2 Dims { get { return m_MapData.Dims; } }

        public TileMap2D(IVector2 _size, ulong _defaultData = 0)
        {
            m_MapData = new CDataArray2D<ulong>(_size.x, _size.y, _defaultData, true);
        }

        public ulong this[IVector2 _coord] { get { return this[_coord.x, _coord.y]; } set { this[_coord.x, _coord.y] = value; } }
        public ulong this[int x, int y]
        {
            get
            {
                return m_MapData[x, y];
            }

            set
            {
                m_MapData[x, y] = value;
            }
        }

        public bool InBounds(IVector2 _coord)
        {
            if (_coord.x < 0 || _coord.x >= Dims.x || _coord.y < 0 || _coord.y >= Dims.y) return false;
            return true;
        }

        public String DebugToString()
        {
            if (true)
            {
                StringBuilder sb = new StringBuilder();
                for (int y = Dims.y - 1; y >= 0; --y)
                {
                    for (int x = 0; x < Dims.x; ++x)
                    {
                        switch (m_MapData[x, y])
                        {
                            case 0: //Empty
                                sb.Append(' ');
                                break;
                            case 1: //Wall
                                sb.Append('#');
                                break;
                            case 2: //Floor
                                sb.Append('.');
                                break;
                            default:
                                sb.Append(' ');
                                break;
                        }
                    }
                    sb.AppendLine();
                }
                return sb.ToString();
            }/* UNITY SPECIFIC 
            else
            {
                var curTexture = (Texture2D)meshRenderer.material.mainTexture;
                for (int y = Dims.y - 1; y >= 0; --y)
                {
                    for (int x = 0; x < Dims.x; ++x)
                    {
                        switch (m_MapData[x, y])
                        {
                            case 0: //Empty
                                curTexture.SetPixel(x, y, new Color(0, 0, 0, 1));
                                break;
                            case 1: //Wall
                                curTexture.SetPixel(x, y, new Color(1f, 1f, 1f, 1));
                                break;
                            case 2: //Floor
                                curTexture.SetPixel(x, y, new Color(0.5f, 0.5f, 0.5f, 1));
                                break;
                            default:
                                curTexture.SetPixel(x, y, new Color(0, 0, 0, 1)); ;
                                break;
                        }
                    }

                }
                var doorways = new HashSet<IVector2>();
                foreach (var doorway in m_Rooms.SelectMany(room => room.Doorways))
                    curTexture.SetPixel(doorway.x, doorway.y, Color.green);
                curTexture.Apply();
                return "";
            }*/


        }

        public String DataToString()
        {
            return m_MapData.ToString();
        }

        public List<IVector2> GetAllCoordsMatching(ulong _data)
        {
            var coords = new List<IVector2>();
            
            for(int x = 0; x < Dims.x; ++x)
                for(int y = 0; y < Dims.y; ++y)
                    if(this[x,y].Equals(_data)) coords.Add(new IVector2(x,y));

            return coords;

        }


    }

    
}
