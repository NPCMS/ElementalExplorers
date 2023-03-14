using UnityEngine;
using System.Collections;

public class InitDemo_1 : MonoBehaviour {

	public GameObject[] Prefabs;
	public int PrefabNum;
	public float PosY;
	
	void Awake() {
		GenerateLevel();
	}
	
	void GenerateLevel()
	{
		Vector3 prefabPos;
		GameObject go;
		
		for (var i = 0; i < PrefabNum; i++)
		{
			prefabPos = new Vector3(Random.Range(70f, 930f), PosY, Random.Range(70f, 930f));
			go = Instantiate(Prefabs[Random.Range(0, Prefabs.Length)], prefabPos, Quaternion.identity) as GameObject;
			go.transform.Rotate(Vector3.up, Random.Range(0f, 360f));
		}
	}
}
