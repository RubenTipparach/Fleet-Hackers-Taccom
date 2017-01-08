using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtEllipticalStarfield))]
public class SgtEllipticalStarfield_Editor : SgtStarfield_Editor<SgtEllipticalStarfield>
{
	protected override void OnInspector()
	{
		base.OnInspector();

		var updateMesh = false;
		
		DrawDefault("Seed", ref updateMesh);
		BeginError(Any(t => t.Radius <= 0.0f));
			DrawDefault("Radius", ref updateMesh);
		EndError();
		DrawDefault("Symmetry", ref updateMesh);
		DrawDefault("Offset", ref updateMesh);
		DrawDefault("Inverse", ref updateMesh);
		
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

		if (updateMesh == true) DirtyEach(t => t.UpdateMeshesAndModels());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Elliptical Starfield")]
public class SgtEllipticalStarfield : SgtStarfield
{
	[Tooltip("The random seed used when generating the stars")]
	[SgtSeed]
	public int Seed;

	[Tooltip("The radius of the starfield")]
	public float Radius = 1.0f;

	[Tooltip("Should more stars be placed near the horizon?")]
	[SgtRange(0.0f, 1.0f)]
	public float Symmetry = 1.0f;

	[Tooltip("How far from the center the distribution begins")]
	[SgtRange(0.0f, 1.0f)]
	public float Offset = 0.0f;

	[Tooltip("Invert the distribution?")]
	public bool Inverse;

	[Tooltip("The amount of stars that will be generated in the starfield")]
	public int StarCount = 1000;

	[Tooltip("The minimum radius of stars in the starfield")]
	public float StarRadiusMin = 0.0f;

	[Tooltip("The maximum radius of stars in the starfield")]
	public float StarRadiusMax = 0.05f;

	[Tooltip("The maximum amount a star's size can pulse over time. A value of 1 means the star can potentially pulse between its maximum size, and 0")]
	[SgtRange(0.0f, 1.0f)]
	public float StarPulseMax = 1.0f;
	
	public static SgtEllipticalStarfield CreateEllipticalStarfield(int layer = 0, Transform parent = null)
	{
		return CreateEllipticalStarfield(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtEllipticalStarfield CreateEllipticalStarfield(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Elliptical Starfield", layer, parent, localPosition, localRotation, localScale);
		var starfield  = gameObject.AddComponent<SgtEllipticalStarfield>();

		return starfield;
	}

	protected override int BeginStars()
	{
		SgtHelper.BeginRandomSeed(Seed);

		return StarCount;
	}

	protected override void NextStar(ref SgtStarfieldStar star, int starIndex)
	{
		var position  = Random.insideUnitSphere;
		var magnitude = Offset;

		if (Inverse == true)
		{
			magnitude += (1.0f - position.magnitude) * (1.0f - Offset);
		}
		else
		{
			magnitude += position.magnitude * (1.0f - Offset);
		}

		position.y *= Symmetry;

		star.Variant     = Random.Range(int.MinValue, int.MaxValue);
		star.Color       = Color.white;
		star.Radius      = Random.Range(StarRadiusMin, StarRadiusMax);
		star.Angle       = Random.Range(-180.0f, 180.0f);
		star.Position    = position.normalized * magnitude * Radius;
		star.PulseRange  = Random.value * StarPulseMax;
		star.PulseSpeed  = Random.value;
		star.PulseOffset = Random.value;
	}

	protected override void EndStars()
	{
		SgtHelper.EndRandomSeed();
	}

#if UNITY_EDITOR
	protected virtual void OnDrawGizmosSelected()
	{
		Gizmos.matrix = transform.localToWorldMatrix;

		Gizmos.DrawWireSphere(Vector3.zero, Radius);

		Gizmos.DrawWireSphere(Vector3.zero, Radius * Offset);
	}
#endif

#if UNITY_EDITOR
	[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Elliptical Starfield", false, 10)]
	private static void CreateEllipticalStarfieldMenuItem()
	{
		var parent    = SgtHelper.GetSelectedParent();
		var starfield = CreateEllipticalStarfield(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(starfield);
	}
#endif
}
