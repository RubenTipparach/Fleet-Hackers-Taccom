using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtStarfieldModel))]
public class SgtStarfieldModel_Editor : SgtEditor<SgtStarfieldModel>
{
	protected override void OnInspector()
	{
		BeginDisabled();
			DrawDefault("Starfield");
		EndDisabled();
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu("")]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SgtStarfieldModel : MonoBehaviour
{
	[Tooltip("The starfield this belongs to")]
	public SgtStarfield Starfield;
	
	[System.NonSerialized]
	private MeshFilter meshFilter;
	
	[System.NonSerialized]
	private MeshRenderer meshRenderer;

	[System.NonSerialized]
	private Mesh mesh;

	[System.NonSerialized]
	private Material material;
	
	[System.NonSerialized]
	private bool tempSet;

	[System.NonSerialized]
	private Vector3 tempPosition;

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

	public void SavePosition()
	{
		if (tempSet == false)
		{
			tempSet      = true;
			tempPosition = transform.position;
		}
	}

	public void LoadPosition()
	{
		if (tempSet == true)
		{
			tempSet            = false;
			transform.position = tempPosition;
		}
	}

	public static SgtStarfieldModel Create(SgtStarfield starfield)
	{
		var model = SgtComponentPool<SgtStarfieldModel>.Pop(starfield.transform, "Model", starfield.gameObject.layer);

		model.Starfield = starfield;
		
		return model;
	}

	public static void Pool(SgtStarfieldModel model)
	{
		if (model != null)
		{
			model.Starfield = null;

			model.PoolMeshNow();

			SgtComponentPool<SgtStarfieldModel>.Add(model);
		}
	}

	public static void MarkForDestruction(SgtStarfieldModel model)
	{
		if (model != null)
		{
			model.Starfield = null;

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
		if (Starfield == null)
		{
			Pool(this);
		}
	}
}
