using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbstractDescentParser
{
    private List<string> tokens;
    private int index;
    public AbstractDescentParser(List<string> tokens) {
        this.tokens = tokens;
        this.index = 0;
    }
    public bool Parse() {
        bool parsingSuccess = ParseFacade();
        //if we didn't parse all tokens then parsing failed
        Debug.Log(index);
        return parsingSuccess;
        
    }

    private bool ParseFacade() {
        bool entranceSuccess = ParseEntrance();
        bool windowsSuccess = ParseLevels();
        bool roofSuccess = ParseRoof();

        return entranceSuccess && windowsSuccess && roofSuccess;
    }


    private bool ParseEntrance() {
        bool doorSuccess = ParseDoor();
        bool canopySuccess = ParseCanopy();

        return doorSuccess && canopySuccess;
    }

    private bool ParseDoor() {
        if (tokens[index] == "glass door" || tokens[index] == "metal door" || tokens[index] == "sliding door" || tokens[index] == "automatic door") {
            index++;
            return true;
        } else {
            return false;
        }
    }

    private bool ParseCanopy() {    
        if (tokens[index] == "metal canopy" || tokens[index] == "tented canopy" || tokens[index] == "angled canopy") {
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
        bool anotherLevel = true;
        while (anotherLevel)
        {
            if(tokens[index] != "Level")
            {

                anotherLevel = false;
                return true;
            }
            index++;
            ParseLevel();
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
            index++;
            return true;
        } else {
            return false;   
        }
    }
}
