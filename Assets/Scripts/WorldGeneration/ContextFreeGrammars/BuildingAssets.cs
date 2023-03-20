using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections.Generic;
using Valve.Newtonsoft.Json.Utilities;

public static class BuildingAssets
{
    public static readonly List<string> materialsPaths = new List<string>()
    {
        "CFGAssets/Building_Materials/UrbanTerracotaBrick/Material/UrbanTerracotaBrickTriplanar",
        "CFGAssets/Building_Materials/Brickwall/Materials/Brickwall2x2mTriplanar", 
        "CFGAssets/Building_Materials/ClassicBrickwall1/Materials/ClassicBrickwall2x2Triplanar",
        "CFGAssets/Building_Materials/StoneBricks/Materials/StoneBricksTriplanar",
        "CFGAssets/Building_Materials/ColouredStoneBricks/Materials/ColouredStoneBricksTriplanar",
        "CFGAssets/Building_Materials/WornBrickwall/Materials/WornBrickwallTriplanar",
        "CFGAssets/Building_Materials/StoneBricks2/Materials/StoneBricks03Triplanar",
        "CFGAssets/Building_Materials/BrickwallWorn/Materials/BrickwallWorn1x2Triplanar",
        "CFGAssets/Building_Materials/ModernBrickwall/Materials/ModernBrickwallTriplanar",
        "CFGAssets/Building_Materials/ClassicBrickwall2/Materials/ClassicBrickwall2x2Var2Triplanar",
        "CFGAssets/Building_Materials/ConcreteDamaged/Materials/ConcreteDamagedTriplanar",
        "CFGAssets/Building_Materials/CastInSituRoughConcrete/Materials/CastInSituTriplanar"
    };

    public static readonly List<string> windowsPaths = new List<string>()
    {
        "CFGAssets/Window_Meshes/Var2/WindowVariation2",
        "CFGAssets/Window_Meshes/Var7/WindowVariation7",
        "CFGAssets/Window_Meshes/Var5/WindowVariation5",
        "CFGAssets/Window_Meshes/Var6/WindowVar6",
        "CFGAssets/Window_Meshes/Var9/WindowVariation9",
        "CFGAssets/Window_Meshes/Var3/WindowVariation3",
        "CFGAssets/Window_Meshes/Var10/WindowVariation10",
    };

    public static readonly List<string> doorsPaths = new List<string>()
    {
        "01_AssetStore/DoorPackFree/Prefab/DoorV1",
        "01_AssetStore/DoorPackFree/Prefab/DoorV2",
        "01_AssetStore/DoorPackFree/Prefab/DoorV3",
        "01_AssetStore/DoorPackFree/Prefab/DoorV4",
        "01_AssetStore/DoorPackFree/Prefab/DoorV5",
        "01_AssetStore/DoorPackFree/Prefab/DoorV6",
        "01_AssetStore/DoorPackFree/Prefab/DoorV7",

    };

    public static int getWindowIndex(double probability)
    {
        if (probability <= 0.2)
        {
            return 0;
        }
        else if (probability <= 0.4)
        {
            return 2;
        }
        else if (probability <= 0.6)
        {
            return 3;
        }
        else if (probability <= 0.8)
        {
            return 4;
        }
        else if (probability <= 0.95)
        {
            return 6;
        }
        else if (probability <= 0.98)
        {
            return 5;
        }
        else if (probability > 0.98)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
    
    
    public static int getMaterialIndex(double probability)
    {
        if (probability <= 0.1)
        {
            return 0;
        }
        else if (probability <= 0.2)
        {
            return 2;
        }
        else if (probability <= 0.3)
        {
            return 3;
        }
        else if (probability <= 0.4)
        {
            return 4;
        }
        else if (probability <= 0.5)
        {
            return 1;
        }
        else if (probability <= 0.6)
        {
            return 6;
        }
        else if (probability <= 0.7)
        {
            return 7;
        }
        else if (probability <= 0.8)
        {
            return 8;
        }
        else if (probability <= 0.9)
        {
            return 9;
        }
        else if (probability <= 0.95)
        {
            return 10;
        }
        else
        {
            return 11;
        }
    }

}

