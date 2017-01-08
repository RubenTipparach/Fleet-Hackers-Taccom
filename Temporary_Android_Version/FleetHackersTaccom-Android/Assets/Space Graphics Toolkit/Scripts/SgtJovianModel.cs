using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtJovianModel))]
public class SgtJovianModel_Editor : SgtEditor<SgtJovianModel>
{
	protected override void OnInspector()
	{
		BeginDisabled();
			DrawDefault("Jovian");
		EndDisabled();
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu("")]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SgtJovianModel : MonoBehaviour
{
	[Tooltip("The jovian this belongs to")]
	public SgtJovian Jovian;

	[System.NonSerialized]
	private MeshFilter meshFilter;
	
	[System.NonSerialized]
	private MeshRenderer meshRenderer;

	[System.NonSerialized]
	private Mesh mesh;

	[System.NonSerialized]
	private Material material;

	public void SetMesh(Mesh newMesh)
	{
		if (newMesh != mesh)
		{
			if (meshFilter == null) meshFilter = gameObject.GetComponent<MeshFilter>();

			SgtHelper.BeginStealthSet(meshFilter);
			{
				mesh = meshFilter.sharedMesh = newMesh;
			}
			SgtHelper.EndStealthSet();
		}
	}

	public void SetMaterial(Material newMaterial)
	{
		if (newMaterial != material)
		{
			if (meshRenderer == null) meshRenderer = gameObject.GetComponent<MeshRenderer>();

			SgtHelper.BeginStealthSet(meshRenderer);
			{
				material = meshRenderer.sharedMaterial = newMaterial;
			}
			SgtHelper.EndStealthSet();
		}
	}

	public static SgtJovianModel Create(SgtJovian jovian)
	{
		var model = SgtComponentPool<SgtJovianModel>.Pop(jovian.transform, "Model", jovian.gameObject.layer);

		model.Jovian = jovian;

		return model;
	}

	public static void Pool(SgtJovianModel model)
	{
		if (model != null)
		{
			model.Jovian = null;

			SgtComponentPool<SgtJovianModel>.Add(model);
		}
	}

	public static void MarkForDestruction(SgtJovianModel model)
	{
		if (model != null)
		{
			model.Jovian = null;

			model.gameObject.SetActive(true);
		}
	}
	
	protected virtual void Update()
	{
		if (Jovian == null)
		{
			Pool(this);
		}
	}
}
