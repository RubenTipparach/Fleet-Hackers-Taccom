using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicShip : MonoBehaviour {

	Vector3 target;

	bool movingToTarget = false;

	[SerializeField]
	float moveSpeed = 1f;

	[SerializeField]
	float rotationSpeed = 1f;

	[SerializeField]
	float startingHealth = 100;

	public float currentHealth = 100;

	public HealthBar HealthBar
	{
		get;
		set;
	}

	// Use this for initialization
	void Start () {
		currentHealth = startingHealth;	
	}

	// Update is called once per frame
	void Update() {
		
		// This will be implementing basic movement mechanics.

		if (movingToTarget)
		{
			transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
			transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(target), rotationSpeed * Time.deltaTime);
		}


	}

	public void MoveShip(Vector3 destination)
	{
		target = destination;
	}

}
