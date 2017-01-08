using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtStaticStarfield))]
public class SgtStaticStarfield_Editor : SgtStarfield_Editor<SgtStaticStarfield>
{
	protected override void OnInspector()
	{
		var updateMaterial        = false;
		var updateMeshesAndModels = false;

		DrawMaterialBasic(ref updateMaterial);

		Separator();

		DrawMaterialTexture(ref updateMaterial, ref updateMeshesAndModels);
		
		Separator();

		DrawDefault("Age");
		DrawDefault("TimeScale");

		Separator();

		DrawDefault("Seed", ref updateMeshesAndModels);
		BeginError(Any(t => t.Radius <= 0.0f));
			DrawDefault("Radius", ref updateMeshesAndModels);
		EndError();
		DrawDefault("Symmetry", ref updateMeshesAndModels);
		
		Separator();
		
		BeginError(Any(t => t.StarCount < 0));
			DrawDefault("StarCount", ref updateMeshesAndModels);
		EndError();
		BeginError(Any(t => t.StarRadiusMin < 0.0f || t.StarRadiusMin > t.StarRadiusMax));
			DrawDefault("StarRadiusMin", ref updateMeshesAndModels);
		EndError();
		BeginError(Any(t => t.StarRadiusMax < 0.0f || t.StarRadiusMin > t.StarRadiusMax));
			DrawDefault("StarRadiusMax", ref updateMeshesAndModels);
		EndError();
		DrawDefault("StarColors", ref updateMeshesAndModels);
		
		RequireObserver();

		if (updateMaterial        == true) DirtyEach(t => t.UpdateMaterial       ());
		if (updateMeshesAndModels == true) DirtyEach(t => t.UpdateMeshesAndModels());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Static Starfield")]
public class SgtStaticStarfield : SgtStarfield
{
	[Tooltip("The random seed used when generating the stars")]
	[SgtSeed]
	public int Seed;

	[Tooltip("The radius of the starfield")]
	public float Radius = 1.0f;

	[Tooltip("Should more stars be placed near the horizon?")]
	[SgtRange(0.0f, 1.0f)]
	public float Symmetry = 1.0f;

	[Tooltip("The amount of stars that will be generated in the starfield")]
	public int StarCount = 1000;

	[Tooltip("The minimum radius of stars in the starfield")]
	public float StarRadiusMin = 0.0f;

	[Tooltip("The maximum radius of stars in the starfield")]
	public float StarRadiusMax = 0.05f;

	[Tooltip("Each star is given a random color from this gradient")]
	public Gradient StarColors;
	
	protected override string ShaderName
	{
		get
		{
			return SgtHelper.ShaderNamePrefix + "StaticStarfield";
		}
	}

	public static SgtStaticStarfield CreateStaticStarfield(int layer = 0, Transform parent = null)
	{
		return CreateStaticStarfield(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtStaticStarfield CreateStaticStarfield(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Static Starfield", layer, parent, localPosition, localRotation, localScale);
		var starfield  = gameObject.AddComponent<SgtStaticStarfield>();

		return starfield;
	}

	protected override void BuildMesh(Mesh mesh, int starIndex, int starCount)
	{
		var positions = new Vector3[starCount * 4];
		var colors    = new Color[starCount * 4];
		var coords1   = new Vector2[starCount * 4];
		var indices   = new int[starCount * 6];
		var minMaxSet = false;
		var min       = default(Vector3);
		var max       = default(Vector3);
		
		for (var i = 0; i < starCount; i++)
		{
			NextStar(ref tempStar, starIndex + i);

			var offV     = i * 4;
			var offI     = i * 6;
			var position = tempStar.Position;
			var radius   = tempStar.Radius;
			var uv       = tempCoords[SgtHelper.Mod(tempStar.Variant, tempCoords.Count)];
			var rotation = Quaternion.FromToRotation(Vector3.back, position.normalized) * Quaternion.Euler(0.0f, 0.0f, tempStar.Angle);
			var up       = rotation * Vector3.up    * radius;
			var right    = rotation * Vector3.right * radius;

			ExpandBounds(ref minMaxSet, ref min, ref max, position, radius);
			
			positions[offV + 0] = position - up - right;
			positions[offV + 1] = position - up + right;
			positions[offV + 2] = position + up - right;
			positions[offV + 3] = position + up + right;

			colors[offV + 0] =
			colors[offV + 1] =
			colors[offV + 2] =
			colors[offV + 3] = tempStar.Color;
			
			coords1[offV + 0] = new Vector2(uv.x, uv.y);
			coords1[offV + 1] = new Vector2(uv.z, uv.y);
			coords1[offV + 2] = new Vector2(uv.x, uv.w);
			coords1[offV + 3] = new Vector2(uv.z, uv.w);
			
			indices[offI + 0] = offV + 0;
			indices[offI + 1] = offV + 1;
			indices[offI + 2] = offV + 2;
			indices[offI + 3] = offV + 3;
			indices[offI + 4] = offV + 2;
			indices[offI + 5] = offV + 1;
		}
		
		mesh.name      = "Belt";
		mesh.vertices  = positions;
		mesh.colors    = colors;
		mesh.uv        = coords1;
		mesh.triangles = indices;
		mesh.bounds    = SgtHelper.NewBoundsFromMinMax(min, max);
	}

	protected override int BeginStars()
	{
		SgtHelper.BeginRandomSeed(Seed);
		
		return StarCount;
	}

	protected override void NextStar(ref SgtStarfieldStar star, int starIndex)
	{
		var position = Random.insideUnitSphere;

		position.y *= Symmetry;

		star.Variant  = Random.Range(int.MinValue, int.MaxValue);
		star.Radius   = Random.Range(StarRadiusMin, StarRadiusMax);
		star.Angle    = Random.Range(-180.0f, 180.0f);
		star.Position = position.normalized * Radius;

		if (StarColors != null)
		{
			star.Color = StarColors.Evaluate(Random.value);
		}
		else
		{
			star.Color    = Color.white;
		}
	}

	protected override void EndStars()
	{
		SgtHelper.EndRandomSeed();
	}

	protected override void CameraPreCull(Camera camera)
	{
		// Make sure the starfield follows the camera
		FollowCameras = true;

		base.CameraPreCull(camera);
	}

#if UNITY_EDITOR
	protected virtual void OnDrawGizmosSelected()
	{
		Gizmos.matrix = transform.localToWorldMatrix;

		Gizmos.DrawWireSphere(Vector3.zero, Radius);
	}
#endif
	
#if UNITY_EDITOR
	[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Static Starfield", false, 10)]
	private static void CreateStaticStarfieldMenuItem()
	{
		var parent    = SgtHelper.GetSelectedParent();
		var starfield = CreateStaticStarfield(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(starfield);
	}
#endif
}
