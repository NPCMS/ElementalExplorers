using UnityEngine;
using XNode;

public class TextureGeneratorNode : Node
{
	[Input] public Vector2 offset;
	[Input] public float scale;
	[Output] public Texture2D texture;

	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		return null; // Replace this
	}
}