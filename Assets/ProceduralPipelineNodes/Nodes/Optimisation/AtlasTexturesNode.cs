using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using XNode;

public class AtlasTexturesNode : SyncExtendedNode
{
	[Input] public Shader useShader;
	[Input] public int textureSize = 1024;
	[Input] public Material copy;
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

	private void SetUVs(MeshRenderer renderer, int index, Material mat)
	{
		MeshFilter filter = renderer.GetComponent<MeshFilter>();
		Mesh mesh = filter.sharedMesh;
		List<Vector3> uvs = new List<Vector3>();
		mesh.GetUVs(0, uvs);
		for (int i = 0; i < uvs.Count; i++)
		{
			uvs[i] = new Vector3(uvs[i].x, uvs[i].y, index);
		}

		mesh.SetUVs(0, uvs);
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
		Material mat = new Material(GetInputValue("copy", copy));
		int texSize = GetInputValue("textureSize", textureSize);
		Texture2DArray mainTexArray = new Texture2DArray(texSize, texSize, renderers.Count, DefaultFormat.LDR,
			TextureCreationFlags.MipChain);
		Texture2DArray maskArray = new Texture2DArray(texSize, texSize, renderers.Count, DefaultFormat.LDR,
			TextureCreationFlags.MipChain);
		Texture2DArray normalArray = new Texture2DArray(texSize, texSize, renderers.Count, DefaultFormat.LDR,
			TextureCreationFlags.MipChain);
		int index = 0;
		foreach (KeyValuePair<Material,List<MeshRenderer>> instance in renderers)
		{
			Material material = instance.Key;
			Texture2D mainTex = Resize((Texture2D)material.GetTexture("_MainTexture"), textureSize, textureSize);
			Texture2D mask = Resize((Texture2D)material.GetTexture("_Mask"), textureSize,textureSize);
			Texture2D normal = Resize((Texture2D)material.GetTexture("_Normal"), textureSize, textureSize);
			mainTexArray.SetPixels(mainTex.GetPixels(), index);
			maskArray.SetPixels(mask.GetPixels(), index);
			normalArray.SetPixels(normal.GetPixels(), index);
			foreach (MeshRenderer renderer in instance.Value)
			{
				SetUVs(renderer, index, mat);
			}
			index++;

			if (wait.YieldIfTimePassed())
			{
				yield return null;
			}
		}

		mat.SetTexture("_MainTextureArray", mainTexArray);
		mat.SetTexture("_MaskArray", maskArray);
		mat.SetTexture("_NormalArray", normalArray);
		output = gos;

		callback.Invoke(true);
	}

	public override void Release()
	{
		input = null;
		output = null;
	}
}