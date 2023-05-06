using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using XNode;

public class PlaceDropship : SyncExtendedNode
{

	[Input] public GameObject dropShip;
	[Input] public List<GeoCoordinate> pointsOfInterest;
	[Input] public GeoCoordinate dropShipPosition;
	[Input] public ElevationData elevationData;

	private ElevationData eData;
	
	[Output] public GameObject positionedDropShip;
	// Use this for initialization
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port)
	{


		if (port.fieldName == "positionedDropShip")
		{
			return positionedDropShip;
			
		}
		else
		{
			Debug.Log("wrong port name received. This should never happen");
			return null;
		}
		 // Replace this
	}

	public override IEnumerator CalculateOutputs(Action<bool> callback)
	{
		
		List<GeoCoordinate> pois = GetInputValue("pointsOfInterest", pointsOfInterest);
		GameObject actualDropShip = GetInputValue("dropShip", dropShip);
		GeoCoordinate position = GetInputValue("dropShipPosition", dropShipPosition);
		eData = GetInputValue("elevationData", elevationData);

		if (!MultiPlayerWrapper.isGameHost)
		{
			callback.Invoke(true);
			yield break;
		}
		
		Debug.Log("Spawning dropship");
		
		Vector3 poi = getPositionFromGeoCoord(pois[0]);
		Vector3 pos = getPositionFromGeoCoord(position);
		int numIterations = 0;
		RaycastHit hit;
		if (Physics.SphereCast(pos, 10, Vector3.down, out hit))
		{
			pos.y = hit.point.y + 1;
		}
		//make dropship face poi
		var positionRotation = poi - pos;
		positionRotation.y = 0;
		var rotation = Quaternion.LookRotation(positionRotation, Vector3.up);
		rotation *= Quaternion.Euler(0, -90, 0);
		actualDropShip = Instantiate(actualDropShip, pos, rotation);
		actualDropShip.GetComponent<NetworkObject>().Spawn();
		callback.Invoke(true);
		yield break;
	}
	
	private Vector3 getPositionFromGeoCoord(GeoCoordinate geoCoordinate)
	{
		GenerateBuildingClassesNode node = CreateInstance<GenerateBuildingClassesNode>();
		Vector2 meterpoint = node.ConvertGeoCoordToMeters(geoCoordinate, eData.box);
		float height = (float)eData.SampleHeightFromPosition(new Vector3(meterpoint.x, 0, meterpoint.y)) + 4000f;
		return new Vector3(meterpoint.x, height, meterpoint.y);
	}
	

	public override void Release()
	{
		pointsOfInterest = null;
		elevationData = null;
	}
}