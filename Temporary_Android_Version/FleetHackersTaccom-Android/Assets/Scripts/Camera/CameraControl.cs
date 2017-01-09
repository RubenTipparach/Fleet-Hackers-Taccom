using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraControl : MonoBehaviour, IDragHandler
{
	[SerializeField]
	float smoothness = 1;

	[SerializeField]
	float scrollSensitivity = 1;

	[SerializeField]
	float distanceMin = 1;

	[SerializeField]
	float distanceMax = 10;

	[SerializeField]
	LayerMask physicsMask; // mask nothing!

	float velocity;

	[SerializeField]
	float distanceFromPlane = 10;//dont modify, just for viewing
	[SerializeField]
	float scrollDistance = 10;

	Plane gamePlane;

	Vector3 pivotLocation;
	[SerializeField]
	float panningToScrollFactor;
	public void OnDrag(PointerEventData eventData)
	{
		if(Input.GetMouseButton(2))//middle mouse
		{
			//Camera.main.transform.Translate(eventData.delta * Time.deltaTime);
			pivotLocation += new Vector3(-eventData.delta.x,0, -eventData.delta.y) * Time.deltaTime *(panningToScrollFactor *scrollDistance);
        }
	}

	// Use this for initialization
	void Start () {
		gamePlane = new Plane(Vector3.up, 0);

		float hitDis = 0;
		Ray r = Camera.main.ViewportPointToRay(Vector3.zero);
		if (gamePlane.Raycast(Camera.main.ViewportPointToRay(Vector3.zero),
			out hitDis))
		{
			distanceFromPlane = hitDis;
			scrollDistance = hitDis;
        }
    }


	// Update is called once per frame
	void Update ()
	{

		float hitDis = 0;
		//Ray r = Camera.main.ViewportPointToRay(Vector3.zero);
		//if (gamePlane.Raycast(Camera.main.ViewportPointToRay(Vector3.zero),
		//	out hitDis))
		//{
		//	//distanceFromPlane = hitDis;
		//	//pivotLocation = r.GetPoint(hitDis);
		//}
		//else
		//{
		//	return;
		//}

		
        float scroll = Input.GetAxis("Mouse ScrollWheel");
		if (scroll != 0)
		{
			scrollDistance -= scroll * Time.deltaTime * scrollSensitivity;
			scrollDistance = Mathf.Clamp(scrollDistance, distanceMin, distanceMax);
		}

		if (distanceFromPlane != scrollDistance)
		{
			distanceFromPlane = Mathf.SmoothDamp(distanceFromPlane, scrollDistance, ref velocity, Time.deltaTime * smoothness);
		}

		Camera.main.transform.position = CalculateCameraPosition();
	}

	private Vector3 CalculateCameraPosition()
	{
		var rotationalOffset = Camera.main.transform.rotation * (Vector3.back * distanceFromPlane);
        Debug.DrawLine(pivotLocation, rotationalOffset);
		return pivotLocation + rotationalOffset; //yoffset goes here.
    }
}
