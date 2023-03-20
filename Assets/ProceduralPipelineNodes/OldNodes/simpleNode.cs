using System;
using XNode;

namespace ProceduralPipelineNodes.Nodes
{
	public class SimpleNode : ExtendedNode {

		// Use this for initialization
		protected override void Init() {
			base.Init();
		}

		// define inputs and outputs for the node using the tags
		[Input] public float value;
		[Input] public float[] someReferenceCreatedByPipeline;
		[Output] public float result1;
		[Output] public float result2;
	
		// Return the correct value of an output port when requested
		public override object GetValue(NodePort port)
		{
			// Check which output is being requested. 
			if (port.fieldName == "result1")
			{
				// Return precomputed result1 (value + 1)
				return result1;
			}
			if (port.fieldName == "result2")
			{
				// Return precomputed result2 (value - 1)
				return result2;
			}
			// Hopefully this won't ever happen, but we need to return something
			// in the odd case that the port isn't "result"
			else return null;
		}

		//this is where the values are computed
		//run by the ProceduralManager script when building the pipeline
		public override void CalculateOutputs(Action<bool> callback)
		{
			//get the single input
			//string is variable name followed by the variable as a fallback
			//value can be set by the editor if no connection is made, so must be set as the fallback parameter
			float inputValue = GetInputValue("value", value);
			result1 = inputValue + 1;
			result2 = inputValue - 1;
			//callback when the computation has finished
			//true if the node successfully executed, false otherwise
			//false will stop the execution of the pipeline
			callback.Invoke(true);
		}

		//Set any reference values created by the pipeline (e.g. textures, api data) to null
		//So that garbage collection collects and reduces pipeline size/memory footprint
		public override void Release()
		{
			someReferenceCreatedByPipeline = null;

		}
	}
}