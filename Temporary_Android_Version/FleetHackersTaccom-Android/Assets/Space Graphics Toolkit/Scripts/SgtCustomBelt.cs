using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtCustomBelt))]
public class SgtCustomBelt_Editor : SgtBelt_Editor<SgtCustomBelt>
{
	protected override void OnInspector()
	{
		base.OnInspector();
		
		var updateMeshesAndModels = false;
		
		DrawDefault("Asteroids", ref updateMeshesAndModels);

		RequireObserver();

		if (updateMeshesAndModels == true) DirtyEach(t => t.UpdateMeshesAndModels());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Custom Belt")]
public class SgtCustomBelt : SgtBelt
{
	[Tooltip("The custom asteroids in this belt")]
	public List<SgtBeltAsteroid> Asteroids;

	public static SgtCustomBelt CreateCustomBelt(int layer = 0, Transform parent = null)
	{
		return CreateCustomBelt(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtCustomBelt CreateCustomBelt(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Custom Belt", layer, parent, localPosition, localRotation, localScale);
		var belt       = gameObject.AddComponent<SgtCustomBelt>();

		return belt;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		if (Asteroids != null)
		{
			for (var i = Asteroids.Count - 1; i >= 0; i--)
			{
				SgtClassPool<SgtBeltAsteroid>.Add(Asteroids[i]);
			}
		}
	}
	
	protected override int BeginAsteroids()
	{
		if (Asteroids != null)
		{
			return Asteroids.Count;
		}

		return 0;
	}

	protected override void NextAsteroid(ref SgtBeltAsteroid asteroid, int asteroidIndex)
	{
		asteroid.CopyFrom(Asteroids[asteroidIndex]);
	}

	protected override void EndAsteroids()
	{
	}

#if UNITY_EDITOR
	[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Custom Belt", false, 10)]
	public static void CreateCustomBeltMenuItem()
	{
		var parent = SgtHelper.GetSelectedParent();
		var belt   = CreateCustomBelt(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(belt);
	}
#endif
}
