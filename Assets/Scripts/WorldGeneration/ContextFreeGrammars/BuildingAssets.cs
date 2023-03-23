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
        "CFGAssets/Window_Meshes/Var3/WindowVariation3",
        "CFGAssets/Window_Meshes/Var5/WindowVariation5",
        "CFGAssets/Window_Meshes/Var6/WindowVar6",
        "CFGAssets/Window_Meshes/Var7/WindowVariation7",
        "CFGAssets/Window_Meshes/Var8/WindowVariation8",
        "CFGAssets/Window_Meshes/Var9/WindowVariation9",
        "CFGAssets/Window_Meshes/Var10/WindowVariation10",
        "CFGAssets/Window_Meshes/Var11/WindowVariation11",
        "CFGAssets/Window_Meshes/Var13/WindowVariation13",
        
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
        else if (probability <= 0.55)
        {
            return 3;
        }
        else if (probability <= 0.65)
        {
            return 7;
        }
        else if (probability <= 0.75)
        {
            return 8;
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
    
    
    public static List<double> getWindowDistribution()
    {
        return new List<double>()
        {
            0.2, 0.02, 0.2, 0.15, 0.01, 0.07, 0.1, 0.1, 0.1, 0.05
        };
    }

    public static List<double> getMaterialDistribution()
    {
        return new List<double>()
        {
            0.15, 0.1, 0.12, 0.05, 0.05, 0.12, 0.05, 0.1, 0.1, 0.1, 0.02, 0.04
        };
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
    
    
    public static List<double> generateWindowDistribution(Dictionary<string, double> dict)
    {
        List<double> list = getWindowDistribution();
        foreach (var entry in dict)
        {
            int index = windowsPaths.IndexOf(entry.Key);
            list[index] = entry.Value;
            double sumOfOtherProbs = 0;
            foreach (var prob in list)
            {
                if (list.IndexOf(prob) != index)
                {
                    sumOfOtherProbs += prob;
                }
            }

            double dividingFactor = sumOfOtherProbs / 1 - entry.Value;
            
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != index)
                {
                    list[i] = list[i] / dividingFactor;
                }
            }
        }
        Debug.Log(list);
        
        
        return list;
    }

    public static int getIndexFromDistribution(double prob, List<double> list)
    {
        double sum = 0;
        for (int i = 0; i < list.Count; i++)
        {
            if (prob < sum)
            {
                return i;
            }
            sum += list[i];
        }

        return 0;
    }

}

