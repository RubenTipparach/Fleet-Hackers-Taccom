using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Ship;
using Improbable.Unity;
using Improbable.Unity.Visualizer;

[EngineType(EnginePlatform.Client)]
public class ClientMove : MonoBehaviour {

	[Require]
	private ShipControls.Writer ShipControlsWriter;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
	{

		// we'll update this to specified raycast destination.
		ShipControlsWriter.Send(new ShipControls.Update()
			.SetTargetSpeed(Mathf.Clamp01(Input.GetAxis("Vertical")))
			.SetTargetSteering(Input.GetAxis("Horizontal")));
		
	}
}
