using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.IO;
using XNode;

[CreateNodeMenu("Buildings/Buildify/Buildify")]
public class BuildifyNode : AsyncExtendedNode
{
    const string blenderPath = "C:/Program Files/Blender Foundation/Blender 3.2/blender.exe";
    const string generatorPrep =  "C:/Users/nf20792/ElementalExplorers/Non_Unity/Blender/generators/";
    const string blenderArgEnd =  " -b --python C:/Users/nf20792/ElementalExplorers/Non_Unity/Blender/pythonScript.py";
    const string oldBlenderArgs =
        "C:/Users/nf20792/ElementalExplorers/Non_Unity/Blender/generators/generator.blend -b --python C:/Users/cv20549/Documents/ElementalExplorers/Non_Unity/Blender/pythonScript.py";
	const string inputPath = "C:/Users/nf20792/ElementalExplorers/Non_Unity/Blender/inputs/input.json";
	const string outputPath = "C:/Users/nf20792/ElementalExplorers/Non_Unity/Blender/outputs/output.json";
    [Input] public BuildifyFootprintList footprintList;

	[Output] public BuildifyCityData city;

	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "city")
		{
			return city;
		}
		return null; // Replace this
	}

	private BuildifyCityData Buildify(BuildifyFootprint[] list, string generator)
    {
        Debug.Log(3 + " " + list.Length);
        string blenderArgs = generatorPrep + generator + blenderArgEnd;
		Debug.Log(blenderArgs + " / " + oldBlenderArgs);
		blenderArgs = oldBlenderArgs;
		File.WriteAllText(inputPath, JsonConvert.SerializeObject(new BuildifyFootprints(list)));
		ProcessStartInfo processStart = new ProcessStartInfo(blenderPath, blenderArgs);
		processStart.UseShellExecute = false;
		processStart.CreateNoWindow = true;

		Debug.Log("Started blender");
		var process = Process.Start(processStart);

		process.WaitForExit();
        process.Close();
		Debug.Log("Closed blender");
		BuildifyCityData buildifyCityData = (BuildifyCityData)JsonConvert.DeserializeObject(File.ReadAllText(outputPath), typeof(BuildifyCityData));
		for (int i = 0; i < buildifyCityData.buildings.Length; i++)
		{
			buildifyCityData.buildings[i].generator = generator;
		}
		return buildifyCityData;
    }

	protected override void CalculateOutputsAsync(Action<bool> callback)
	{
		BuildifyFootprintList list = GetInputValue("footprintList", footprintList);
		
		//call all the different types of generator here
		string defaultGenerator = "generator.blend";
		string universityGenerator = "UniversityBuilding/UniversityBuilding.blend";
		string retailGenerator = "generator.blend";
		string carParkGenerator = "CarPark/CarPark.blend";

		List<BuildifyBuildingData> buildings = new List<BuildifyBuildingData>();
		
		
		city = Buildify(list.defaultFootprints, retailGenerator);
		buildings.AddRange(city.buildings);
        buildings.AddRange(Buildify(list.retailFootprints, retailGenerator).buildings);
        buildings.AddRange(Buildify(list.carParkFootprints, carParkGenerator).buildings);
		buildings.AddRange(Buildify(list.universityFootprints, universityGenerator).buildings);
		city.buildings = buildings.ToArray();
        callback.Invoke(true);
	}

	protected override void ReleaseData()
	{
		footprintList = null;
		city = null;

    }
}