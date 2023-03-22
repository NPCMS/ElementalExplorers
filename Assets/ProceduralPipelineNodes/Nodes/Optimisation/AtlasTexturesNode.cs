using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using XNode;

public class AtlasTexturesNode : SyncExtendedNode
{
	private static readonly string[] BuildingTextureIdentifiers = new string[]{"_MainTexture","_Mask","_Normal"};
	private static readonly string[] LitTextureIdentifiers = new string[]{"_BaseMap","_NormalMap"};
	[Input] public Shader useShader;
	[Input] public int textureSize = 1024;
	[Input] public int padding = 16;
	[Input] public Material copy;
	[Input] public bool building;
	[Input] public GameObject[] input;
	[Output] public GameObject[] output;
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "output")
		{
			return output;
		}
		return null; // Replace this
	}

	private void SetUVs(MeshRenderer renderer, Rect rect, Material mat, float scale)
	{
		MeshFilter filter = renderer.GetComponent<MeshFilter>();
		Mesh mesh = filter.sharedMesh;
		List<Vector3> uvs = new List<Vector3>();
		mesh.GetUVs(0, uvs);
		for (int i = 0; i < uvs.Count; i++)
		{
			uvs[i] = new Vector3(uvs[i].x, uvs[i].y, scale);
		}
		List<Color> colors = new List<Color>();
		Color col = new Color(rect.xMin, rect.yMin, rect.width, rect.height);
		for (int i = 0; i < mesh.vertexCount; i++)
		{
			colors.Add(col);
		}

		mesh.SetUVs(0, uvs);
		mesh.SetColors(colors);
		filter.sharedMesh = mesh;
		renderer.sharedMaterial = mat;
	}
	
	Texture2D Resize(Texture2D texture2D,int targetX,int targetY)
	{
		RenderTexture rt=new RenderTexture(targetX, targetY,24);
		RenderTexture.active = rt;
		Graphics.Blit(texture2D,rt);
		Texture2D result=new Texture2D(targetX,targetY);
		result.ReadPixels(new Rect(0,0,targetX,targetY),0,0);
		result.Apply();
		return result;
	}

	public override IEnumerator CalculateOutputs(Action<bool> callback)
	{
		GameObject[] gos = GetInputValue("input", input);
		Shader shader = GetInputValue("useShader", useShader);
		Dictionary<Material, List<MeshRenderer>> renderers = new Dictionary<Material, List<MeshRenderer>>();
		SyncYieldingWait wait = new SyncYieldingWait();
		for (int i = 0; i < gos.Length; i++)
		{
			MeshRenderer[] meshes = gos[i].GetComponentsInChildren<MeshRenderer>();
			foreach (MeshRenderer renderer in meshes)
			{
				if (renderer.sharedMaterial.shader == shader)
				{
					if (!renderers.ContainsKey(renderer.sharedMaterial))
					{
						renderers.Add(renderer.sharedMaterial, new List<MeshRenderer>());
					}
					renderers[renderer.sharedMaterial].Add(renderer);
				}
			}

			if (wait.YieldIfTimePassed())
			{
				yield return null;
			}
		}

		bool isBuilding = GetInputValue("building", building);
		string[] texIDs = isBuilding ? BuildingTextureIdentifiers : LitTextureIdentifiers;
		List<Texture2D>[] textures = new List<Texture2D>[texIDs.Length];
		for (int i = 0; i < texIDs.Length; i++)
		{
			textures[i] = new List<Texture2D>();
		}
		foreach (KeyValuePair<Material,List<MeshRenderer>> instance in renderers)
		{
			Material material = instance.Key;
			for (int i = 0; i < texIDs.Length; i++)
			{
				textures[i].Add((Texture2D)material.GetTexture(texIDs[i]));;
			}
		}

		Material mat = new Material(GetInputValue("copy", copy));
		int texSize = GetInputValue("textureSize", textureSize);
		int pad = GetInputValue("padding", padding);

		Texture2D[] textureAtlases = new Texture2D[texIDs.Length];
		Rect[] rects = null;
		for (int i = 0; i < textureAtlases.Length; i++)
		{
			textureAtlases[i] = new Texture2D(texSize, texSize, DefaultFormat.LDR,
				TextureCreationFlags.MipChain);
			rects = textureAtlases[i].PackTextures(textures[i].ToArray(), pad);
		}

		for (int i = 0; i < texIDs.Length; i++)
		{
			mat.SetTexture(texIDs[i], textureAtlases[i]);
		}
		
		int index = 0;
		foreach (KeyValuePair<Material,List<MeshRenderer>> instance in renderers)
		{
			float scale = isBuilding ? 1 : instance.Key.GetFloat("_Scale");
			foreach (MeshRenderer renderer in instance.Value)
			{
				SetUVs(renderer, rects[index], mat, scale);
			}


			if (wait.YieldIfTimePassed())
			{
				yield return null;
			}

			index++;
		}
		output = gos;

		callback.Invoke(true);
	}

	public override void Release()
	{
		input = null;
		output = null;
	}
}