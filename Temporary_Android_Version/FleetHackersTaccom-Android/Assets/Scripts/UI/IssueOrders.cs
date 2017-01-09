using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class IssueOrders : MonoBehaviour, IPointerClickHandler, IDragHandler
{

	Plane gamePlane;

	public void OnDrag(PointerEventData eventData)
	{
		var ships = GetComponent<DragSelection>().SelectedShips;
		float hitdist = 0;

		Vector3 hitPos = Vector3.zero;
		var position = Input.mousePosition;
		Ray r = Camera.main.ScreenPointToRay(position);

		if (gamePlane.Raycast(Camera.main.ScreenPointToRay(position), out hitdist))
		{
			Debug.DrawLine(r.GetPoint(hitdist), r.origin, Color.red);
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{

		// These input eventually need to use some sort of android time step.
		if (eventData.button != PointerEventData.InputButton.Right )
		{
			return;
		}

		// Debug.Log("Pointer clicked");

		var ships = GetComponent<DragSelection>().SelectedShips;
		float hitdist = 0;

		Vector3 hitPos = Vector3.zero;
		var position = Input.mousePosition;
		Ray r = Camera.main.ScreenPointToRay(position);

		if (gamePlane.Raycast(r, out hitdist))
		{
			hitPos = r.GetPoint(hitdist);

			// Debug.Log("OrderIssued  to " + ships);

			Vector3 leftOffset = Vector3.zero;
			Vector3 rightOffset = Vector3.zero;

            bool first = true;
			bool left = false; //else right

			foreach (var ship in ships)
			{
				// we need to set offsets here.
				Vector3 offset = Vector3.zero;

				if(first)
				{
					leftOffset = offset;
					rightOffset = ship.GetComponent<SphereCollider>().radius * transform.right;

					first = false;
                }

				if (left)
				{
					ship.MoveShip(hitPos + leftOffset);
					leftOffset += ship.GetComponent<SphereCollider>().radius * (ship.LookAtTarget * -Vector3.right);
                }
				else
				{
					ship.MoveShip(hitPos + rightOffset);
					rightOffset += ship.GetComponent<SphereCollider>().radius * ( ship.LookAtTarget * Vector3.right );
				}

				left = !left;
            }
		}
		
	}

	// Use this for initialization
	void Start()
	{
		gamePlane = new Plane(Vector3.up, 0);
	}

	// Update is called once per frame
	void Update()
	{
		
	}
}
