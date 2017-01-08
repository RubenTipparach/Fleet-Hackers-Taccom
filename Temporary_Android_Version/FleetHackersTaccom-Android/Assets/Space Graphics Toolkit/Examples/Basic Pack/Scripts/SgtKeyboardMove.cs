using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtKeyboardMove))]
public class SgtKeyboardMove_Editor : SgtEditor<SgtKeyboardMove>
{
	protected override void OnInspector()
	{
		DrawDefault("Require");
		BeginError(Any(t => t.Speed <= 0.0f));
			DrawDefault("Speed");
		EndError();
		BeginError(Any(t => t.Dampening < 0.0f));
			DrawDefault("Dampening");
		EndError();

		Separator();

		DrawDefault("CheckTerrains");

		if (Any(t => t.CheckTerrains == true))
		{
			BeginIndent();
				DrawDefault("RepelDistance");
				BeginError(Any(t => t.SlowSpeed <= 0.0f));
					DrawDefault("SlowSpeed");
				EndError();
				BeginError(Any(t => t.SlowThickness <= 0.0f));
					DrawDefault("SlowThickness");
				EndError();
			EndIndent();
		}
	}
}
#endif

// This component handles keyboard movement when attached to the camera
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Keyboard Move")]
public class SgtKeyboardMove : MonoBehaviour
{
	[Tooltip("The key that needs to be held down to move")]
	public KeyCode Require = KeyCode.None;
	
	[Tooltip("The maximum speed of the movement")]
	public float Speed = 1.0f;
	
	[Tooltip("How quickly this accelerates toward the target position")]
	public float Dampening = 5.0f;

	[Tooltip("")]
	public bool CheckTerrains = true;

	[Tooltip("")]
	public float RepelDistance = 0.1f;

	[Tooltip("")]
	public float SlowSpeed = 0.1f;

	[Tooltip("")]
	public float SlowThickness = 10.0f;
	
	private Vector3 targetPosition;
	
	protected virtual void Start()
	{
		targetPosition = transform.position;
	}
	
	protected virtual void Update()
	{
		var maxSpeed = CalculateMaxSpeed();
		
		if (Require == KeyCode.None || Input.GetKey(Require) == true)
		{
			targetPosition += transform.forward * Input.GetAxisRaw("Vertical") * maxSpeed * Time.deltaTime;
			
			targetPosition += transform.right * Input.GetAxisRaw("Horizontal") * maxSpeed * Time.deltaTime;
		}
		
		transform.position = SgtHelper.Dampen3(transform.position, targetPosition, Dampening, Time.deltaTime, 0.1f);

		RepelPositions();
	}

	private float CalculateMaxSpeed()
	{
		var maxSpeed = Speed;
		
		if (CheckTerrains == true && SlowThickness > 0.0f)
		{
			for (var i = SgtTerrain.AllTerrains.Count - 1; i >= 0; i--)
			{
				var terrain      = SgtTerrain.AllTerrains[i];
				var height       = terrain.GetSurfaceHeightWorld(targetPosition);
				var targetVector = targetPosition - terrain.transform.position;
				var height01     = Mathf.Clamp01((targetVector.magnitude - height) / SlowThickness);
				var newSpeed     = Mathf.SmoothStep(SlowSpeed, Speed, height01);
				
				if (newSpeed < maxSpeed)
				{
					maxSpeed = newSpeed;
				}
			}
		}

		return maxSpeed;
	}

	private void RepelPositions()
	{
		if (CheckTerrains == true)
		{
			for (var i = SgtTerrain.AllTerrains.Count - 1; i >= 0; i--)
			{
				var terrain      = SgtTerrain.AllTerrains[i];
				var height       = terrain.GetSurfaceHeightWorld(targetPosition) + RepelDistance;
				var targetVector = targetPosition - terrain.transform.position;
				var localVector  = transform.position - terrain.transform.position;

				if (targetVector.magnitude < height)
				{
					targetPosition = terrain.transform.position + targetVector.normalized * height;
				}

				if (localVector.magnitude < height)
				{
					transform.position = terrain.transform.position + localVector.normalized * height;
				}
			}
		}
	}
}