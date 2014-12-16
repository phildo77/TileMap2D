TileMap2D
=====

2014/11/17:
Working at a beta level.  Generation now reads a struct Settings for dungeon varition.

2014/05/23:
Operating at a basic level now.  Buggy, does square rooms, 1 unit width tunnels, and dungeon type only, and many planned features moving forward.  Either something is wrong in tunnel building or the Clipper Library doesn't like something about the tunnel paths when offset.


Usage
===

static void Main(string[] args)
{
    
    //Set data defaults for floor and walls.
    Generate.SetDefMats(1, 2);
    //Generate.Settings.RndSeed = 985;

    //Override some of the default settings.
    Generate.Settings.TunnelBuffer = 3;
    Generate.Settings.TunnelPathMag = 1;
    Generate.Settings.CurvyTunnels = false;

    Generate.Settings.RoomCountMin = 10;
    Generate.Settings.RoomCountMax = 20;
    Generate.Settings.RoomSizeMin = 8;
    Generate.Settings.RoomSizeMax = 12;

    //Create the dungeon
    Generate.Dungeon();

    //Monitor the message log
    while(!Generate.GenerateDone)
    {
        if (Generate.ExLog.Count > 0)
            System.Console.WriteLine(Generate.ExLog.Dequeue());
    }

    //Get the map and display text representation on the console
    TileMap2D map = Generate.GetGeneratedMap();
    System.Console.WriteLine(map.DebugToString());
    System.Console.ReadLine();
           

}
