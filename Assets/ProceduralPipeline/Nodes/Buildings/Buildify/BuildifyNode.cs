using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.IO;
using UnityEngine;
using XNode;
using QuikGraph;

[CreateNodeMenu("Buildings/Buildify/Buildify")]
public class BuildifyNode : AsyncExtendedNode
{
    const string blenderPath = "C:/Program Files/Blender Foundation/Blender 3.2/blender.exe";
    const string blenderArgs =
        "C:/Users/uq20042/Documents/ElementalExplorers/Non_Unity/Blender/generators/generator.blend -b --python C:/Users/uq20042/Documents/ElementalExplorers/Non_Unity/Blender/pythonScript.py";
	const string inputPath = "C:/Users/uq20042/Documents/ElementalExplorers/Non_Unity/Blender/inputs/input.json";
	const string outputPath = "C:/Users/uq20042/Documents/ElementalExplorers/Non_Unity/Blender/outputs/output.json";
    [Input] public BuildifyFootprintList footprintList;
	[Input] public int batchSize = 10;

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

	private BuildifyCityData Buildify(BuildifyFootprintList list)
    {
		Debug.Log("Write JSON");
		File.WriteAllText(inputPath, JsonUtility.ToJson(list));
        Debug.Log("Start Process");
        ProcessStartInfo processStart = new ProcessStartInfo(blenderPath, blenderArgs);
        processStart.UseShellExecute = false;
        processStart.CreateNoWindow = true;

        Debug.Log("Started");
        var process = Process.Start(processStart);
        Debug.Log("Waiting");

        process.WaitForExit();
        Debug.Log("Waited");
        process.Close();
        Debug.Log("Closed");

        return (BuildifyCityData)JsonUtility.FromJson(File.ReadAllText(inputPath), typeof(BuildifyCityData));
    }

	protected override void CalculateOutputsAsync(Action<bool> callback)
	{
		BuildifyFootprintList list = GetInputValue("footprintList", footprintList);
		Queue<BuildifyFootprint> queue = new Queue<BuildifyFootprint>(list.footprints);
		List<BuildifyFootprint> batch = new List<BuildifyFootprint>();
		List<BuildifyBuildingData> buildings = new List<BuildifyBuildingData>();
		int size = GetInputValue("batchSize", batchSize);
		while (queue.Count > 0)
		{
			Debug.Log(queue.Count + " remaining");
			for (int i = 0; i < size; i++)
			{
				batch.Add(queue.Dequeue());
			}

			BuildifyCityData data = Buildify(new BuildifyFootprintList() { footprints = batch.ToArray() });
			buildings.AddRange(data.buildings);

			batch.Clear();
		}
		city = new BuildifyCityData() { buildings = buildings.ToArray() };
        Debug.Log("Deserialised");
        callback.Invoke(true);
	}

	protected override void ReleaseData()
	{
		footprintList = null;
		city = null;
	}
}