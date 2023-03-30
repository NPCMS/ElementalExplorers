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
using UnityEngine.UI;

[CreateNodeMenu("Buildings/Buildify/Buildify")]
public class BuildifyNode : AsyncExtendedNode
{
    const string blenderPath = "C:/Program Files/Blender Foundation/Blender 3.2/blender.exe";
    const string blenderArgs =
        "C:/Users/uq20042/Documents/ElementalExplorers/Non_Unity/Blender/generators/generator.blend -b --python C:/Users/uq20042/Documents/ElementalExplorers/Non_Unity/Blender/pythonScript.py";
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

	private BuildifyCityData Buildify(BuildifyFootprintList list)
    {
		File.WriteAllText(inputPath, JsonConvert.SerializeObject(list));
		ProcessStartInfo processStart = new ProcessStartInfo(blenderPath, blenderArgs);
		processStart.UseShellExecute = false;
		processStart.CreateNoWindow = true;

		Debug.Log("Started blender");
		var process = Process.Start(processStart);

		process.WaitForExit();
		process.Close();
		Debug.Log("Closed blender");
		return (BuildifyCityData)JsonConvert.DeserializeObject(File.ReadAllText(outputPath), typeof(BuildifyCityData));
    }

	protected override void CalculateOutputsAsync(Action<bool> callback)
	{
		BuildifyFootprintList list = GetInputValue("footprintList", footprintList);
		city = Buildify(list);
        callback.Invoke(true);
	}

	protected override void ReleaseData()
	{
		footprintList = null;
		city = null;

    }
}