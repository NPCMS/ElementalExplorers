using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Flags
{
    public Hashtable table;

    public Dictionary<string, double> windowsDistribution;
    public string windowPath;
    public string doorPath;
    public bool roof;
    public Flags()
    {
        this.windowsDistribution = new Dictionary<string, double>();
        this.windowPath = "";
        this.doorPath = "";
        this.roof = true;
    }
}
