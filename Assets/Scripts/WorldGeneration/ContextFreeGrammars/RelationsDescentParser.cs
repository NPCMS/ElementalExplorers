using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RelationsDescentParser : AbstractDescentParser
{
    
     private readonly List<string> tokens;
    private int index;
    private readonly GameObject parent;
    private OSMBuildingData buildingData;
    private ElevationData elevation;
    //private string[][] facade;

   public RelationsDescentParser(List<string> tokens, GameObject parent, OSMBuildingData buildingData) : base(tokens, parent)
    {
        this.tokens = tokens;
        this.index = 0;
        this.parent = parent;
        this.buildingData = buildingData;

    }
   
   public override bool Parse(ElevationData elevationData)
   {
       if (tokens.Count > 1)
       {
           this.elevation = elevationData;
           bool parsingSuccess = ParseFacade();
           //if we didn't parse all tokens then parsing failed
           if (this.index < tokens.Count)
           {
               return false;
           }
           return parsingSuccess;
       }
       return true;

   }
   
   private bool ParseFacade() {
        bool entranceSuccess = ParseEntrance();
        bool windowSuccess = ParseWindow();
        // bool windowsSuccess = ParseLevels();
        //bool roofSuccess = ParseRoof();

        return entranceSuccess && windowSuccess;
    }


    private bool ParseEntrance() {
        bool doorSuccess = ParseDoor();
        //bool windowSuccess = ParseWindow();

        return doorSuccess;
    }

    private bool ParseDoor() {
        if (tokens[index] == "glass door" || tokens[index] == "metal door" || tokens[index] == "sliding door" || tokens[index] == "automatic door") {
            //draw the door here.
            DataToObjects.CreateDoor(parent.GetComponent<MeshFilter>(), tokens[index],
                elevation);
            Debug.Log("making a door");
            index++;
            return true;
        } else {
            return false;
        }
    }

    private bool ParseWindow() {    
        if (tokens[index] == "floor-to-ceiling window" || tokens[index] == "bay window" || tokens[index] == "strip window" || tokens[index] == "slit window" || tokens[index] == "rounded window" || tokens[index] == "arched window") {
            DataToObjects.CreateWindow(parent.GetComponent<MeshFilter>(), tokens[index],
                elevation, 1, buildingData);
            Debug.Log("making a window");

            index++;
            return true;
        } else {
            return false;
        }
    }

    private bool ParseLevels()  {
        if(tokens[index] != "Level")
        {
            return false;
        }
        index++;
        ParseLevel();
        while (true)
        {
            if(tokens[index] != "Level")
            {
                return true;
            }
            index++;
            if (!ParseLevel())
            {
                break;
            }
        }
        index++;
        return true;
    }

    private bool ParseLevel() {
        if (tokens[index] == "floor-to-ceiling window" || tokens[index] == "bay window" || tokens[index] == "strip window" || tokens[index] == "slit window" || tokens[index] == "rounded window" || tokens[index] == "arched window") {
            index++;
            while ((tokens[index] == "floor-to-ceiling window" || tokens[index] == "bay window" || tokens[index] == "strip window" || tokens[index] == "slit window" || 
            tokens[index] == "rounded window" || tokens[index] == "arched window") && tokens[index] != "epsilon") {
                index++;
            }
            if(tokens[index] == "epsilon")
            {
                index++;
            }
            return true;
        } 
        else {
            return false;
        }
    }

    private bool ParseDecorations() {
        if (tokens[index] == "floor-to-ceiling window" || tokens[index] == "bay window" || tokens[index] == "strip window" || tokens[index] == "slit window" || tokens[index] == "rounded window" || tokens[index] == "arched window") {
            index++;
            while (tokens[index] == "floor-to-ceiling window" || tokens[index] == "bay window" || tokens[index] == "strip window" || tokens[index] == "slit window" || tokens[index] == "rounded window" || tokens[index] == "arched window") {
                index++;
            }
            return true;
        } else {
            return false;        
        }
    }

    private bool ParseRoof() {
        if (tokens[index] == "flat roof" || tokens[index] == "green roof" || tokens[index] == "sloped roof" || tokens[index] == "hip roof" || tokens[index] == "pitched roof") {
            DataToObjects.CreateRoof(parent, tokens[index],
                elevation, buildingData);
            index++;
            return true;
        } else {
            return false;   
        }
    }
    // public RelationsDescentParser(List<string> tokens, GameObject parent) : base(tokens, parent)
    // {
    //     
    // }
    
}
