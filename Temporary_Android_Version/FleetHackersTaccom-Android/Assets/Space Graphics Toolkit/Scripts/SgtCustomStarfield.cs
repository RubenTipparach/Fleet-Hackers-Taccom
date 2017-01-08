using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtCustomStarfield))]
public class SgtCustomStarfield_Editor : SgtStarfield_Editor<SgtCustomStarfield>
{
	protected override void OnInspector()
	{
		base.OnInspector();

		var updateMeshesAndModels = false;
		
		DrawDefault("Stars", ref updateMeshesAndModels);
		
		RequireObserver();

		if (updateMeshesAndModels == true) DirtyEach(t => t.UpdateMeshesAndModels());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Custom Starfield")]
public class SgtCustomStarfield : SgtStarfield
{
	[Tooltip("The stars that will be rendered by this starfield")]
	public List<SgtStarfieldStar> Stars;

	public static SgtCustomStarfield CreateCustomStarfield(int layer = 0, Transform parent = null)
	{
		return CreateCustomStarfield(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtCustomStarfield CreateCustomStarfield(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Custom Starfield", layer, parent, localPosition, localRotation, localScale);
		var starfield  = gameObject.AddComponent<SgtCustomStarfield>();

		return starfield;
	}
	
	protected override int BeginStars()
	{
		if (Stars != null)
		{
			return Stars.Count;
		}

		return 0;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		if (Stars != null)
		{
			for (var i = Stars.Count - 1; i >= 0; i--)
			{
				SgtClassPool<SgtStarfieldStar>.Add(Stars[i]);
			}
		}
	}

	protected override void NextStar(ref SgtStarfieldStar star, int starIndex)
	{
		star.CopyFrom(Stars[starIndex]);
	}

	protected override void EndStars()
	{
	}

#if UNITY_EDITOR
	[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Custom Starfield", false, 10)]
	private static void CreateCustomStarfieldMenuItem()
	{
		var parent    = SgtHelper.GetSelectedParent();
		var starfield = CreateCustomStarfield(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(starfield);
	}
#endif
}
