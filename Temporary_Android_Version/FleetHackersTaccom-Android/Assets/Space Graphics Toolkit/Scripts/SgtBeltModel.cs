using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtBeltModel))]
public class SgtBeltModel_Editor : SgtEditor<SgtBeltModel>
{
	protected override void OnInspector()
	{
		BeginDisabled();
			DrawDefault("Belt");
		EndDisabled();
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu("")]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SgtBeltModel : MonoBehaviour
{
	[Tooltip("The belt this belongs to")]
	public SgtBelt Belt;
	
	[System.NonSerialized]
	private MeshRenderer meshRenderer;
	
	[System.NonSerialized]
	private MeshFilter meshFilter;

	[System.NonSerialized]
	private Mesh mesh;

	[System.NonSerialized]
	private Material material;
	
	public Mesh Mesh
	{
		get
		{
			return mesh;
		}
	}

	public void PoolMeshNow()
	{
		if (mesh != null)
		{
			if (meshFilter == null) meshFilter = gameObject.GetComponent<MeshFilter>();

			mesh = meshFilter.sharedMesh = SgtObjectPool<Mesh>.Add(mesh, m => m.Clear());
		}
	}

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

	public static SgtBeltModel Create(SgtBelt belt)
	{
		var model = SgtComponentPool<SgtBeltModel>.Pop(belt.transform, "Model", belt.gameObject.layer);

		model.Belt = belt;

		return model;
	}
	
	public static void Pool(SgtBeltModel model)
	{
		if (model != null)
		{
			model.Belt = null;

			model.PoolMeshNow();

			SgtComponentPool<SgtBeltModel>.Add(model);
		}
	}

	public static void MarkForDestruction(SgtBeltModel model)
	{
		if (model != null)
		{
			model.Belt = null;

			model.PoolMeshNow();

			model.gameObject.SetActive(true);
		}
	}
	
	protected virtual void OnDestroy()
	{
		PoolMeshNow();
	}

	protected virtual void Update()
	{
		if (Belt == null)
		{
			Pool(this);
		}
	}
}
