[System.Serializable]
public class BuildingData
{

    private int id;
    private int tags;
    private double height;
    private string colour;
    private int levels;
    private bool building;
    private string part;
    private string tourism;
    private bool office;
    private string historic;
    private string name;
    private string wikiData;

    public BuildingData()
    {
    }

    public int Id { get => id; private set => id = value; }
    public int Tags { get => tags; set => tags = value; }
    public double Height { get => height; set => height = value; }
    public string Colour { get => colour; set => colour = value; }
    public int Levels { get => levels; set => levels = value; }
    public bool Building { get => building; set => building = value; }
    public string Part { get => part; set => part = value; }
    public string Tourism { get => tourism; set => tourism = value; }
    public bool Office { get => office; set => office = value; }
    public string Historic { get => historic; set => historic = value; }
    public string Name { get => name; set => name = value; }
    public string WikiData { get => wikiData; set => wikiData = value; }

 
}
