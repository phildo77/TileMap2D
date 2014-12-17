using System;
using System.Diagnostics;
using ioSoftSmiths.ioLog;
using ioSoftSmiths.TileMap;

class MainTest
{

    static void Main(string[] args)
    {

        Console.SetBufferSize(500, 2000);
        Console.SetWindowSize(155, 50);

        Msg.DebugVerbosity = LogVerbosity.LOW;
        Msg.DebugAction = (_group, _msg) => Debug.WriteLine(_group + " : " + _msg);
        Generate.Settings.ActionForUserMessages = Console.WriteLine;

        Generate.SetDefaultMaterials(1, 2);

        //Generate.Settings.RndSeed = -1708127739;

        Generate.Settings.AspectRatio = 1f;

        Generate.Settings.RoomBufferMin = 1;
        Generate.Settings.RoomBufferMax = 3;
        Generate.Settings.TunnelBuffer = 3;
        Generate.Settings.TunnelPathMag = 3;
        Generate.Settings.TunnelRoutingStrength = Generate.Settings.MagStrength.Minimum;
        Generate.Settings.ClosestConnect = false;
        Generate.Settings.TunnelTurningCost = Generate.Settings.TTCost.Normal;

        Generate.Settings.RoomCountMin = 50;
        Generate.Settings.RoomCountMax = 60;
        Generate.Settings.RoomSizeMin = 6;
        Generate.Settings.RoomSizeMax = 20;

        Generate.Settings.RoomSpread = Generate.Settings.RSType.Tight;

        Console.WriteLine(Generate.DebugSettingsToString());

        //System.Console.ReadLine();
        Generate.Dungeon();
            
        while(!Generate.GenerateDone)
        {
                
        }

        TileMap2D map = Generate.GetGeneratedMap();
        Console.WriteLine(map.DebugToString());
        //System.Console.ReadLine();
        Console.WriteLine("Closing...");
        Console.ReadLine();


    }
}
