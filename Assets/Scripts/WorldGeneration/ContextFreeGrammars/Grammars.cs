using System.Collections.Generic;

public static class Grammars
{
   public static List<string> detachedHouse = new List<string>()
   {
      //"glass door", "tented canopy", "Level", "bay window", "bay window", "epsilon", "pitched roof"
      "glass door", "bay window", "pitched roof"
   };

   public static List<string> relations = new List<string>()
   {
      "glass door", "bay window",
   };
   
   public static List<string> museum = new List<string>()
   {
      "wooden door", "decorated window", "pitched roof"
   };
   
}
