using UnityEngine;
using System.Collections;
using Improbable.Unity;
using Improbable.Unity.Visualizer;

// Enables this in client.
[EngineType(EnginePlatform.Client)]
public class BasicRTSCamera : MonoBehaviour {

	public AnimationCurve Distance;
	public AnimationCurve Angle;

	private Transform ourTransform;


	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
 