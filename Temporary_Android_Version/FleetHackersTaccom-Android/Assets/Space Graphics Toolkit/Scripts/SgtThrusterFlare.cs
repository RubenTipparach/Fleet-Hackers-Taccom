using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtThrusterFlare))]
public class SgtThrusterFlare_Editor : SgtEditor<SgtThrusterFlare>
{
	protected override void OnInspector()
	{
		DrawDefault("CurrentScale");
		DrawDefault("Dampening");

		Separator();

		BeginDisabled();
			DrawDefault("Thruster");
		EndDisabled();
	}
}
#endif

// This component is created and managed by the SgtTHruster component
[ExecuteInEditMode]
[AddComponentMenu("")]
[RequireComponent(typeof(SpriteRenderer))]
public class SgtThrusterFlare : MonoBehaviour
{
	[Tooltip("The thruster this belongs to")]
	public SgtThruster Thruster;

	[Tooltip("The current scale of this effect")]
	public Vector3 CurrentScale;

	[Tooltip("How quickly this scales to the target size when it goes behind objects")]
	public float Dampening = 10.0f;

	[SerializeField]
	[FormerlySerializedAs("spriteRenderer")]
	public SpriteRenderer SpriteRenderer;

	// A visibility stores if a camera can see this flare or not
	private List<SgtThrusterVisibility> visibilities = new List<SgtThrusterVisibility>();

	[SerializeField]
	private Vector3 finalScale;

	private static Material flareMaterial;

	[System.NonSerialized]
	private bool tempSet;

	[System.NonSerialized]
	private Quaternion tempRotation;

	[System.NonSerialized]
	private Vector3 tempScale;

	// This returns the default shared flare material
	public static Material FlareMaterial
	{
		get
		{
			if (flareMaterial == null)
			{
				flareMaterial = SgtHelper.CreateTempMaterial(SgtHelper.ShaderNamePrefix + "ThrusterFlare");
			}

			return flareMaterial;
		}
	}

	public static SgtThrusterFlare Create(SgtThruster thruster)
	{
		var flare = SgtComponentPool<SgtThrusterFlare>.Pop(thruster.transform, "Flare", thruster.gameObject.layer);

		flare.Thruster = thruster;

		return flare;
	}

	public static void Pool(SgtThrusterFlare flare)
	{
		if (flare != null)
		{
			flare.Thruster = null;

			SgtComponentPool<SgtThrusterFlare>.Add(flare);
		}
	}

	public static void MarkForDestruction(SgtThrusterFlare flare)
	{
		if (flare != null)
		{
			flare.Thruster = null;

			flare.gameObject.SetActive(true);
		}
	}

	public void UpdateFlare(Sprite sprite, Vector3 targetScale, float flicker, float dampening)
	{
		// Get or add SpriteRenderer?
		if (SpriteRenderer == null)
		{
			SpriteRenderer = SgtHelper.GetOrAddComponent<SpriteRenderer>(gameObject);

			SpriteRenderer.sharedMaterial = FlareMaterial;
		}

		// Assign the default material?
		if (SpriteRenderer.sharedMaterial == null)
		{
			SgtHelper.BeginStealthSet(SpriteRenderer);
			{
				SpriteRenderer.sharedMaterial = FlareMaterial;
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
		finalScale   = CurrentScale * (1.0f - flicker);
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

	protected virtual void FixedUpdate()
	{
		for (var i = visibilities.Count - 1; i >= 0; i--)
		{
			var visibility = visibilities[i];
			var camera     = visibility.Camera;

			if (camera != null)
			{
				var origin    = camera.transform.position;
				var target    = transform.position;
				var direction = target - origin;

				// Intersect flare
				if (Physics.Raycast(origin, direction.normalized, direction.magnitude, Thruster.FlareMask) == true)
				{
					visibility.Visible = false;
				}
				else
				{
					visibility.Visible = true;
				}
			}
			else
			{
				visibilities.RemoveAt(i);
			}
		}
	}

	private SgtThrusterVisibility GetThrusterVisibility(Camera camera)
	{
		var visibility = visibilities.Find(v => v.Camera == camera);

		// Create?
		if (visibility == null)
		{
			visibility = new SgtThrusterVisibility();

			visibility.Camera = camera;

			visibilities.Add(visibility);
		}

		return visibility;
	}

	private void CameraPreCull(Camera camera)
	{
#if UNITY_EDITOR
		if (Application.isPlaying == false && Thruster != null)
		{
			FixedUpdate();
		}
#endif
		if (Thruster != null)
		{
			var visibility = GetThrusterVisibility(camera);
			var rotation   = camera.transform.rotation;

			// Resize
			visibility.Scale = SgtHelper.Dampen(visibility.Scale, visibility.Visible == true ? 1.0f : 0.0f, Dampening, Time.unscaledDeltaTime, 0.1f);

			// Point flare at camera
			if (tempSet == false)
			{
				tempSet      = true;
				tempRotation = transform.rotation;
				tempScale    = transform.localScale;
			}

			transform.rotation   = rotation;
			transform.localScale = finalScale * visibility.Scale;
		}
	}

	private void CameraPostRender(Camera camera)
	{
		if (tempSet == true)
		{
			tempSet = false;

			transform.rotation   = tempRotation;
			transform.localScale = tempScale;
		}
	}
}
