using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtThrusterFlame))]
public class SgtThrusterFlame_Editor : SgtEditor<SgtThrusterFlame>
{
	protected override void OnInspector()
	{
		DrawDefault("CurrentScale");

		Separator();

		BeginDisabled();
			DrawDefault("Thruster");
		EndDisabled();
	}
}
#endif

// This component is created and managed by the SgtThruster component
[ExecuteInEditMode]
[AddComponentMenu("")]
[RequireComponent(typeof(SpriteRenderer))]
public class SgtThrusterFlame : MonoBehaviour
{
	[Tooltip("The thruster this belongs to")]
	public SgtThruster Thruster;

	[Tooltip("The current scale of this effect")]
	public Vector3 CurrentScale;

	[SerializeField]
	[FormerlySerializedAs("spriteRenderer")]
	public SpriteRenderer SpriteRenderer;

	private static Material flameMaterial;

	[System.NonSerialized]
	private bool tempSet;

	[System.NonSerialized]
	private Quaternion tempRotation;

	// This returns the default shared flame material
	public static Material FlameMaterial
	{
		get
		{
			if (flameMaterial == null)
			{
				flameMaterial = SgtHelper.CreateTempMaterial(SgtHelper.ShaderNamePrefix + "ThrusterFlame");
			}

			return flameMaterial;
		}
	}

	public static SgtThrusterFlame Create(SgtThruster thruster)
	{
		var flame = SgtComponentPool<SgtThrusterFlame>.Pop(thruster.transform, "Flame", thruster.gameObject.layer);

		flame.Thruster = thruster;

		return flame;
	}

	public static void Pool(SgtThrusterFlame flame)
	{
		if (flame != null)
		{
			flame.Thruster = null;

			SgtComponentPool<SgtThrusterFlame>.Add(flame);
		}
	}

	public static void MarkForDestruction(SgtThrusterFlame flame)
	{
		if (flame != null)
		{
			flame.Thruster = null;

			flame.gameObject.SetActive(true);
		}
	}

	public void UpdateFlame(Sprite sprite, Vector3 targetScale, float flicker, float dampening)
	{
		// Get or add SpriteRenderer?
		if (SpriteRenderer == null)
		{
			SpriteRenderer = GetComponent<SpriteRenderer>();

			SpriteRenderer.sharedMaterial = FlameMaterial;
		}

		// Assign the default material?
		if (SpriteRenderer.sharedMaterial == null)
		{
			SgtHelper.BeginStealthSet(SpriteRenderer);
			{
				SpriteRenderer.sharedMaterial = FlameMaterial;
			}
			SgtHelper.EndStealthSet();
		}

		// Assign the current sprite?
		if (SpriteRenderer.sprite != sprite)
		{
			SpriteRenderer.sprite = sprite;
		}

		// Transition scale
		CurrentScale = SgtHelper.Dampen3(CurrentScale, targetScale, dampening, Time.deltaTime, 0.1f);

		transform.localScale = CurrentScale * (1.0f - flicker);
	}

	protected virtual void OnEnable()
	{
		Camera.onPreCull    += CameraPreCull;
		Camera.onPostRender += CameraPostRender;
	}

	protected virtual void OnDisable()
	{
		Camera.onPreCull    -= CameraPreCull;
		Camera.onPostRender -= CameraPostRender;
	}

	protected virtual void Update()
	{
		if (Thruster == null)
		{
			Pool(this);
		}
	}

	private void CameraPreCull(Camera camera)
	{
		if (Thruster != null)
		{
			var thrusterTransform = Thruster.transform;
			var direction         = thrusterTransform.forward;
			var adjacent          = thrusterTransform.position - camera.transform.position;
			var cross             = Vector3.Cross(direction, adjacent);
			
			if (cross != Vector3.zero)
			{
				var rotation = Quaternion.LookRotation(cross, direction) * Quaternion.Euler(0.0f, 90.0f, 90.0f);

				// Rotate flame to camera
				if (tempSet == false)
				{
					tempSet      = true;
					tempRotation = transform.rotation;
				}

				transform.rotation = rotation;
			}
		}
	}

	private void CameraPostRender(Camera camera)
	{
		if (tempSet == true)
		{
			tempSet = false;

			transform.rotation = tempRotation;
		}
	}
}
