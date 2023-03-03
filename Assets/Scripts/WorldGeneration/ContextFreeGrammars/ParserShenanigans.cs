using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ParserShenanigans : MonoBehaviour {

    public void Start()
    {
     List<string> tokens = new List<string>(){
     "glass door", "tented canopy", "Level", "rounded window", "rounded window", "epsilon", "Level", "arched window",
     "slit window", "strip window", "bay window", "epsilon", "pitched roof"
    };
    //AbstractDescentParser parser = new DetachedHouseDescentParser(tokens);
    //bool success = parser.Parse();
    //Debug.Log("success?" + success);
    }



}
