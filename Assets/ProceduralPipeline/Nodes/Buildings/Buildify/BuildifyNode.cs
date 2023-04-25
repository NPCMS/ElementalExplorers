using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.IO;
using System.Linq;
using UnityEngine;
using XNode;
using QuikGraph;

[CreateNodeMenu("Buildings/Buildify/Buildify")]
public class BuildifyNode : AsyncExtendedNode
{
    const string blenderPath = "C:/Program Files/Blender Foundation/Blender 3.2/blender.exe";
    const string generatorPrep =  "C:/Users/uq20042/Documents/ElementalExplorers/Non_Unity/Blender/generators/";
    const string blenderArgEnd =  " -b --python C:/Users/uq20042/Documents/ElementalExplorers/Non_Unity/Blender/pythonScript.py";
    const string oldBlenderArgs =
        "C:/Users/uq20042/Documents//ElementalExplorers/Non_Unity/Blender/generators/generator.blend -b --python C:/Users/uq20042/Documents//ElementalExplorers/Non_Unity/Blender/pythonScript.py";
	const string inputPath = "C:/Users/uq20042/Documents/ElementalExplorers/Non_Unity/Blender/inputs/input.json";
	const string outputPath = "C:/Users/uq20042/Documents/ElementalExplorers/Non_Unity/Blender/outputs/output.json";
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
		Debug.Log("Starting generator: " + generator);
        string blenderArgs = generatorPrep + generator + blenderArgEnd;
		File.WriteAllText(inputPath, JsonConvert.SerializeObject(new BuildifyFootprints(list)));
		ProcessStartInfo processStart = new ProcessStartInfo(blenderPath, blenderArgs);
		processStart.RedirectStandardOutput = true;
		//processStart.RedirectStandardError = true;
        processStart.UseShellExecute = false;
		processStart.CreateNoWindow = true;

		var process = Process.Start(processStart);
        Debug.Log(process.StandardOutput.ReadToEnd());
        //Debug.LogAssertion(process.StandardError.ReadToEnd());
        process.WaitForExit();
        process.Close();
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
		string retailGenerator = "retailgenerator.blend";
		string carParkGenerator = "CarPark/CarPark.blend";
		string officeGenerator = "office.blend";
		string apartmentGenerator = "ApartmentComplex/ApartmentComplex.blend";
		string coffeeShopGenerator = "CoffeeShop/CoffeeShop.blend";
		string detachedHouseGenerator = "DetachedHouse/DetachedHouse.blend";

		List<BuildifyBuildingData> buildings = new List<BuildifyBuildingData>();
		
		
		city = Buildify(list.defaultFootprints, defaultGenerator);
		buildings.AddRange(city.buildings);
        buildings.AddRange(Buildify(list.retailFootprints, retailGenerator).buildings);
        buildings.AddRange(Buildify(list.carParkFootprints, carParkGenerator).buildings);
		buildings.AddRange(Buildify(list.universityFootprints, universityGenerator).buildings);
		buildings.AddRange(Buildify(list.officeFootprints, officeGenerator).buildings);
		buildings.AddRange(Buildify(list.apartmentFootprints, apartmentGenerator).buildings);
		buildings.AddRange(Buildify(list.coffeeShopFootprints, coffeeShopGenerator).buildings);
		buildings.AddRange(Buildify(list.detachedHouseFootprints, detachedHouseGenerator).buildings);
		city.buildings = buildings.ToArray();
        callback.Invoke(true);
	}

	protected override void ReleaseData()
	{
		footprintList = null;
		city = null;

    }
}