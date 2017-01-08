using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtSimpleBelt))]
public class SgtSimpleBelt_Editor : SgtBelt_Editor<SgtSimpleBelt>
{
	protected override void OnInspector()
	{
		base.OnInspector();
		
		var updateMeshesAndModels = false;
		
		DrawDefault("Seed", ref updateMeshesAndModels);
		DrawDefault("Thickness", ref updateMeshesAndModels);
		BeginError(Any(t => t.InnerRadius < 0.0f || t.InnerRadius > t.OuterRadius));
			DrawDefault("InnerRadius", ref updateMeshesAndModels);
		EndError();
		DrawDefault("InnerSpeed", ref updateMeshesAndModels);
		BeginError(Any(t => t.OuterRadius < 0.0f || t.InnerRadius > t.OuterRadius));
			DrawDefault("OuterRadius", ref updateMeshesAndModels);
		EndError();
		DrawDefault("OuterSpeed", ref updateMeshesAndModels);
		
		Separator();
		
		BeginError(Any(t => t.AsteroidCount < 0));
			DrawDefault("AsteroidCount", ref updateMeshesAndModels);
		EndError();
		DrawDefault("AsteroidSpin", ref updateMeshesAndModels);
		BeginError(Any(t => t.AsteroidRadiusMin < 0.0f || t.AsteroidRadiusMin >= t.AsteroidRadiusMax));
			DrawDefault("AsteroidRadiusMin", ref updateMeshesAndModels);
		EndError();
		BeginError(Any(t => t.AsteroidRadiusMax < 0.0f || t.AsteroidRadiusMin >= t.AsteroidRadiusMax));
			DrawDefault("AsteroidRadiusMax", ref updateMeshesAndModels);
		EndError();

		if (updateMeshesAndModels == true) DirtyEach(t => t.UpdateMeshesAndModels());
		
		RequireObserver();
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Simple Belt")]
public class SgtSimpleBelt : SgtBelt
{
	[Tooltip("The seed used to generate the asteroids")]
	[SgtSeed]
	public int Seed;

	[Tooltip("The thickness of the belt in local coordinates")]
	public float Thickness;

	[Tooltip("The radius of the inner edge of the belt in local coordinates")]
	public float InnerRadius = 1.0f;

	[Tooltip("The speed of asteroids orbiting on the inner edge of the belt in radians")]
	public float InnerSpeed = 0.1f;

	[Tooltip("The radius of the outer edge of the belt in local coordinates")]
	public float OuterRadius = 2.0f;

	[Tooltip("The speed of asteroids orbiting on the outer edge of the belt in radians")]
	public float OuterSpeed = 0.05f;

	[Tooltip("The amount of asteroids generated in the belt")]
	public int AsteroidCount = 1000;

	[Tooltip("The maximum amount of angular velcoity each asteroid has")]
	public float AsteroidSpin = 1.0f;

	[Tooltip("The minimum asteroid radius in local coordinates")]
	public float AsteroidRadiusMin = 0.025f;

	[Tooltip("The maximum asteroid radius in local coordinates")]
	public float AsteroidRadiusMax = 0.05f;
	
	public static SgtSimpleBelt CreateSimpleBelt(int layer = 0, Transform parent = null)
	{
		return CreateSimpleBelt(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtSimpleBelt CreateSimpleBelt(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Simple Belt", layer, parent, localPosition, localRotation, localScale);
		var belt       = gameObject.AddComponent<SgtSimpleBelt>();

		return belt;
	}

	protected override int BeginAsteroids()
	{
		SgtHelper.BeginRandomSeed(Seed);

		return AsteroidCount;
	}
	
	protected override void NextAsteroid(ref SgtBeltAsteroid asteroid, int asteroidIndex)
	{
		var distance01 = Random.value;

		asteroid.Variant       = Random.Range(int.MinValue, int.MaxValue);
		asteroid.Color         = Color.white;
		asteroid.Radius        = Random.Range(AsteroidRadiusMin, AsteroidRadiusMax);
		asteroid.Height        = Random.Range(-Thickness, Thickness);
		asteroid.Angle         = Random.Range(0.0f, Mathf.PI * 2.0f);
		asteroid.Spin          = Random.Range(-AsteroidSpin, AsteroidSpin);
		asteroid.OrbitAngle    = Random.Range(0.0f, Mathf.PI * 2.0f);
		asteroid.OrbitSpeed    = Mathf.Lerp(InnerSpeed, OuterSpeed, distance01);
		asteroid.OrbitDistance = Mathf.Lerp(InnerRadius, OuterRadius, distance01);
	}

	protected override void EndAsteroids()
	{
		SgtHelper.EndRandomSeed();
	}

#if UNITY_EDITOR
	[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Simple Belt", false, 10)]
	public static void CreateSimpleBeltMenuItem()
	{
		var parent = SgtHelper.GetSelectedParent();
		var belt   = CreateSimpleBelt(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(belt);
	}
#endif
}
