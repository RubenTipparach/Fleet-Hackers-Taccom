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

	[SerializeField]
	int groupId;

	[SerializeField]
	float deltaThresholdToStop = .05f;

	[SerializeField]
	Shipclass shipClass;

	float delta = 0;

	Vector3 lastPosition;

	public HealthBar HealthBar
	{
		get;
		set;
	}

	public int GetGroupId
	{
		get
		{
			return groupId;
		}
	}

	public Shipclass ShipClass
	{
		get
		{
			return shipClass;
		}
	}

	/// <summary>
	/// Quick operation to help with rotational stuff, and also to help with formations.
	/// </summary>
	public Quaternion LookAtTarget
	{
		get
		{
			return Quaternion.LookRotation(target - transform.position);
		}
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

			lastPosition = transform.position;

            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
			transform.rotation = Quaternion.Slerp(
				transform.rotation, 
				this.LookAtTarget,
				rotationSpeed * Time.deltaTime);

			Debug.DrawLine(transform.position, target, Color.green);
			
			delta = Vector3.Distance(lastPosition, transform.position);

			// Slick movement stop criteria. Works like a charm!
			if(deltaThresholdToStop > delta)
			{
				movingToTarget = false;
			}
        }

	}

	public void MoveShip(Vector3 destination)
	{
		target = destination;
		lastPosition = Vector3.zero;
		movingToTarget = true;
    }

	public enum Shipclass
	{
		Fighter,
		BattleFrigate,
		TroopTransport,
		Barge,
		ScienceVessel,
		UtilityShip
	}

}
