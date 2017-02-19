using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class IssueOrders : MonoBehaviour, IPointerClickHandler, IDragHandler
{

    [SerializeField]
    LayerMask layerMask;

	Plane gamePlane;

    /// <summary>
    /// Deag event. Moving the camera around.
    /// </summary>
    /// <param name="eventData"></param>
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventData"></param>
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

		var position = Input.mousePosition;
		Ray r = Camera.main.ScreenPointToRay(position);

        RaycastHit hit;

        if(Physics.Raycast(r, out hit, 200, layerMask))
        {
            Debug.Log("game object got hit " + hit.transform.gameObject);

            var firstShip = ships.First();
            var radius = hit.transform.gameObject.GetComponent<SphereCollider>().radius;

            Vector3 uvect = (hit.transform.position - firstShip.transform.position).normalized; 

            Vector3 hitPos = hit.transform.position - uvect * radius;
            MoveShips(hitPos, ships);

            Debug.DrawLine(firstShip.transform.position, hitPos, Color.blue, 10);
        }
		else if (gamePlane.Raycast(r, out hitdist))
		{
            Vector3 hitPos = r.GetPoint(hitdist);
            MoveShips(hitPos, ships); // formation code is built in.
        }
	}

    private void MoveShips( Vector3 hitPos, IEnumerable<BasicShip> ships)
    {
        // Debug.Log("OrderIssued  to " + ships);

        Vector3 leftOffset = Vector3.zero;
        Vector3 rightOffset = Vector3.zero;

        bool first = true;
        bool left = false; //else right

        foreach (var ship in ships)
        {
            // we need to set offsets here.
            Vector3 offset = Vector3.zero;

            if (first)
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
                rightOffset += ship.GetComponent<SphereCollider>().radius * (ship.LookAtTarget * Vector3.right);
            }

            left = !left;
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
