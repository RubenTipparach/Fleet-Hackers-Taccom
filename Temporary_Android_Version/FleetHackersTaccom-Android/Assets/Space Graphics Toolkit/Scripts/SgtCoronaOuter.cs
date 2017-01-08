using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtCoronaOuter))]
public class SgtCoronaOuter_Editor : SgtEditor<SgtCoronaOuter>
{
	protected override void OnInspector()
	{
		BeginDisabled();
			DrawDefault("Corona");
		EndDisabled();
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu("")]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SgtCoronaOuter : MonoBehaviour
{
	[Tooltip("The corona this belongs to")]
	public SgtCorona Corona;

	[System.NonSerialized]
	private MeshFilter meshFilter;

	[System.NonSerialized]
	private MeshRenderer meshRenderer;

	public void SetMesh(Mesh mesh)
	{
		if (meshFilter == null) meshFilter = gameObject.GetComponent<MeshFilter>();

		if (meshFilter.sharedMesh != mesh)
		{
			SgtHelper.BeginStealthSet(meshFilter);
			{
				meshFilter.sharedMesh = mesh;
			}
			SgtHelper.EndStealthSet();
		}
	}

	public void SetMaterial(Material material)
	{
		if (meshRenderer == null) meshRenderer = gameObject.GetComponent<MeshRenderer>();

		if (meshRenderer.sharedMaterial != material)
		{
			SgtHelper.BeginStealthSet(meshRenderer);
			{
				meshRenderer.sharedMaterial = material;
			}
			SgtHelper.EndStealthSet();
		}
	}

	public void SetScale(float scale)
	{
		SgtHelper.SetLocalScale(transform, scale);
	}

	public static SgtCoronaOuter Create(SgtCorona corona)
	{
		var outer = SgtComponentPool<SgtCoronaOuter>.Pop(corona.transform, "Outer", corona.gameObject.layer);

		outer.Corona = corona;

		return outer;
	}

	public static void Pool(SgtCoronaOuter outer)
	{
		if (outer != null)
		{
			outer.Corona = null;

			SgtComponentPool<SgtCoronaOuter>.Add(outer);
		}
	}

	public static void MarkForDestruction(SgtCoronaOuter outer)
	{
		if (outer != null)
		{
			outer.Corona = null;

			outer.gameObject.SetActive(true);
		}
	}

	protected virtual void Update()
	{
		if (Corona == null)
		{
			Pool(this);
		}
	}
}
