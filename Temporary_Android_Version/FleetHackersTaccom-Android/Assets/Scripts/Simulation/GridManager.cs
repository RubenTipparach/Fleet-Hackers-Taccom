using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour {

	/// <summary>
	/// TODO: 3D grid this thing!
	/// </summary>
	
	[SerializeField]
	float gridSpacing = .1f;

	[SerializeField]
	int gridSize = 100000;

	Vector3 girdStart;

	void Awake() {

		girdStart = new Vector3(-((float)gridSize) / 2.0f * gridSpacing, 0, -((float)gridSize) / 2.0f * gridSpacing);
    }

	void Start()
	{

	}
	
	// Update is called once per frame
	void Update () {
		
	}

	/// <summary>
	/// Places a ship the on grid via some input position.
	/// 
	/// Data is commuincated back to the presentation layer to represent the ship.
	/// </summary>
	/// <param name="position">The position.</param>
	/// <returns></returns>
	public Vector3 PlaceOnGrid(BasicShip ship, Vector3 position)
	{
		// we can just round it for now.

		var x = (int)position.x * (int)(1.0f / gridSpacing);
		var y = (int)position.z * (int)(1.0f / gridSpacing);

		Debug.Log(string.Format("Coordinates for {0} : {1},{2} "  , ship.gameObject.name, x, y));

		if(ship.Initialized)
		{
			Vector2 lastPosition = ship.GridPosition;
        }

		ship.GridPosition = new Vector2(x, y);

		// pass back the presentation stuff.
		return new Vector3(x*gridSpacing, 0, y * gridSpacing);
	}
}

// use for later
public struct Vector3Int
{

}


