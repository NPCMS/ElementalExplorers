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
	[Input] public int padding = 16;
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
		List<Texture2D> mains = new List<Texture2D>();
		List<Texture2D> masks = new List<Texture2D>();
		List<Texture2D> normals = new List<Texture2D>();
		foreach (KeyValuePair<Material,List<MeshRenderer>> instance in renderers)
		{
			Material material = instance.Key;
			mains.Add((Texture2D)material.GetTexture("_MainTexture"));
			masks.Add((Texture2D)material.GetTexture("_Mask"));
			normals.Add((Texture2D)material.GetTexture("_Normal"));
		}

		Material mat = new Material(GetInputValue("copy", copy));
		int texSize = GetInputValue("textureSize", textureSize);
		int pad = GetInputValue("padding", padding);
		
		Texture2D mainTexAtlas = new Texture2D(texSize, texSize, DefaultFormat.LDR,
			TextureCreationFlags.MipChain);
		Rect[] mainRects = mainTexAtlas.PackTextures(mains.ToArray(), pad);
		Texture2D maskAtlas = new Texture2D(texSize, texSize, DefaultFormat.LDR,
			TextureCreationFlags.MipChain);
		Rect[] maskRects = maskAtlas.PackTextures(masks.ToArray(), pad);
		Texture2D normalAtlas = new Texture2D(texSize, texSize, DefaultFormat.LDR,
			TextureCreationFlags.MipChain);
		Rect[] normalRects = normalAtlas.PackTextures(normals.ToArray(), pad);
		
		mat.SetTexture("_MainTexture", mainTexAtlas);
		mat.SetTexture("_Mask", maskAtlas);
		mat.SetTexture("_Normal", normalAtlas);
		int index = 0;
		foreach (KeyValuePair<Material,List<MeshRenderer>> instance in renderers)
		{
			float scale = instance.Key.GetFloat("_Scale");
			foreach (MeshRenderer renderer in instance.Value)
			{
				SetUVs(renderer, mainRects[index], mat, scale);
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