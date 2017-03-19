using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
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

	[SerializeField]
	float smooth;

	float velocity;

	float currentSpeed;

    public string ShipId;

	public Vector2 CurrentGridPositionInt
	{
		get;
		set;
	}
	public HealthBar HealthBar
	{
		get;
		set;
	}

	public int GroupId
	{
		get
		{
			return groupId;
		}
	}

    public TeamStance TeamDatabase
    {
        get;
        set;
    }

	public Shipclass ShipClass
	{
		get
		{
			return shipClass;
		}
	}

	private GridManager _gridManagerInstance;

	// always be int!
	public Vector2 GridPosition
	{
		get;
		set;
	}
		
	public bool Initialized
	{
		get;
		private set;
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

		Initialized = false;

		_gridManagerInstance = GameObject.FindGameObjectWithTag("GridManager").GetComponent<GridManager>();

        var dragSelection = GameObject.FindGameObjectWithTag("CameraController").GetComponent<DragSelection>();
        dragSelection.RegisterShip(this);

        transform.position = _gridManagerInstance.PlaceOnGrid(this, transform.position);

		Initialized = true;
    }

	// Update is called once per frame
	void Update() {
		
		// This will be implementing basic movement mechanics.

		if (movingToTarget)
		{

			lastPosition = transform.position;
			Quaternion targetAngle = this.LookAtTarget;
			float angularDist = Quaternion.Angle(transform.rotation, targetAngle) ;// (Quaternion.Angle(transform.rotation, targetAngle) - 90 )/180;

			float arrivalDist = Vector3.Distance(transform.position, target);

			//Debug.Log(angularDist);
			if (angularDist < 10 || arrivalDist < GetComponent<SphereCollider>().radius * 2)
			{
				transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
				currentSpeed = 0;

				// These two things go together!
				delta = Vector3.Distance(lastPosition, transform.position);

				// Slick movement stop criteria. Works like a charm!
				if (deltaThresholdToStop > delta)
				{
					movingToTarget = false;
					transform.position = target;
                }
			}
			else
			{
				//Debug.Log("cur speed " + currentSpeed);
				currentSpeed = Mathf.SmoothDamp(currentSpeed, moveSpeed, ref velocity, smooth);
				transform.transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime, Space.Self);
			}

            transform.rotation = Quaternion.Lerp(
				transform.rotation,
				targetAngle,
				rotationSpeed * Time.deltaTime);

			Debug.DrawLine(transform.position, target, Color.green);
			
        }

	}

	public void MoveShip(Vector3 destination)
	{
		target = _gridManagerInstance.PlaceOnGrid(this, destination);
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
