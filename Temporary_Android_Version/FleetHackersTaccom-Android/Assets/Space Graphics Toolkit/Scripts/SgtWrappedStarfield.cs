using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtWrappedStarfield))]
public class SgtWrappedStarfield_Editor : SgtStarfield_Editor<SgtWrappedStarfield>
{
	protected override void OnInspector()
	{
		base.OnInspector();

		var updateMesh     = false;
		var updateMaterial = false;
		
		DrawDefault("Seed", ref updateMesh);
		BeginError(Any(t => t.Size.x <= 0.0f || t.Size.y <= 0.0f || t.Size.z <= 0.0f));
			DrawDefault("Size", ref updateMesh);
		EndError();
		DrawDefault("Wrap3D", ref updateMaterial);
		
		Separator();
		
		BeginError(Any(t => t.StarCount < 0));
			DrawDefault("StarCount", ref updateMesh);
		EndError();
		BeginError(Any(t => t.StarRadiusMin < 0.0f || t.StarRadiusMin > t.StarRadiusMax));
			DrawDefault("StarRadiusMin", ref updateMesh);
		EndError();
		BeginError(Any(t => t.StarRadiusMax < 0.0f || t.StarRadiusMin > t.StarRadiusMax));
			DrawDefault("StarRadiusMax", ref updateMesh);
		EndError();
		DrawDefault("StarPulseMax", ref updateMesh);
		
		RequireObserver();

		if (updateMesh     == true) DirtyEach(t => t.UpdateMeshesAndModels());
		if (updateMaterial == true) DirtyEach(t => t.UpdateMaterial       ());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Wrapped Starfield")]
public class SgtWrappedStarfield : SgtStarfield
{
	[Tooltip("The random seed for the starfield")]
	[SgtSeed]
	public int Seed;
	
	[Tooltip("The local size of the starfield")]
	public Vector3 Size = new Vector3(1.0f, 1.0f, 1.0f);

	[Tooltip("Wrap the starfield stars in 2D or 3D?")]
	public bool Wrap3D = true;

	[Tooltip("The amount of stars that will be generated in the starfield")]
	public int StarCount = 1000;

	[Tooltip("The minimum radius of stars in the starfield")]
	public float StarRadiusMin = 0.0f;

	[Tooltip("The maximum radius of stars in the starfield")]
	public float StarRadiusMax = 0.05f;

	[Tooltip("The maximum amount a star's size can pulse over time. A value of 1 means the star can potentially pulse between its maximum size, and 0")]
	[SgtRange(0.0f, 1.0f)]
	public float StarPulseMax = 1.0f;
	
	public static SgtWrappedStarfield CreateWrappedStarfield(int layer = 0, Transform parent = null)
	{
		return CreateWrappedStarfield(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtWrappedStarfield CreateWrappedStarfield(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Wrapped Starfield", layer, parent, localPosition, localRotation, localScale);
		var starfield  = gameObject.AddComponent<SgtWrappedStarfield>();
		
		return starfield;
	}
	
	// Shift all bounds on top of the observer, so it never exits the view frustum
	protected override void CameraPreCull(Camera camera)
	{
		// Make sure this is disabled, else the wrapping will never be seen
		FollowCameras = false;

		base.CameraPreCull(camera);

		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= 0; i--)
			{
				var model = Models[i];

				if (model != null)
				{
					var mesh = model.Mesh;

					if (mesh != null)
					{
						var center = model.transform.InverseTransformPoint(camera.transform.position);

						mesh.bounds = new Bounds(center, Size);
					}
				}
			}
		}
	}

	protected override void BuildMaterial()
	{
		base.BuildMaterial();

		Material.SetVector("_WrapSize", Size);

		if (Wrap3D == true)
		{
			SgtHelper.DisableKeyword("SGT_A", Material); // 2D
			SgtHelper.EnableKeyword("SGT_B", Material); // 3D
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_B", Material); // 3D
			SgtHelper.EnableKeyword("SGT_A", Material); // 2D
		}
	}

	protected override int BeginStars()
	{
		SgtHelper.BeginRandomSeed(Seed);
		
		return StarCount;
	}

	protected override void NextStar(ref SgtStarfieldStar star, int starIndex)
	{
		var position = default(Vector3);

		position.x = Random.Range(-Size.x, Size.x);
		position.y = Random.Range(-Size.y, Size.y);
		position.z = Random.Range(-Size.z, Size.z);

		star.Variant     = Random.Range(int.MinValue, int.MaxValue);
		star.Color       = Color.white;
		star.Radius      = Random.Range(StarRadiusMin, StarRadiusMax);
		star.Angle       = Random.Range(-180.0f, 180.0f);
		star.Position    = position;
		star.PulseRange  = Random.value * StarPulseMax;
		star.PulseSpeed  = Random.value;
		star.PulseOffset = Random.value;
	}

	protected override void EndStars()
	{
		SgtHelper.EndRandomSeed();
	}

#if UNITY_EDITOR
	[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Wrapped Starfield", false, 10)]
	private static void CreateWrappedStarfieldMenuItem()
	{
		var parent    = SgtHelper.GetSelectedParent();
		var starfield = CreateWrappedStarfield(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(starfield);
	}
#endif
}
