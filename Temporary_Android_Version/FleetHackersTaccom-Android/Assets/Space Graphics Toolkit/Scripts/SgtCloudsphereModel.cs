using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtCloudsphereModel))]
public class SgtCloudsphereModel_Editor : SgtEditor<SgtCloudsphereModel>
{
	protected override void OnInspector()
	{
		BeginDisabled();
			DrawDefault("Cloudsphere");
		EndDisabled();
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu("")]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SgtCloudsphereModel : MonoBehaviour
{
	[Tooltip("The cloudsphere this belongs to")]
	public SgtCloudsphere Cloudsphere;

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

	public void SetScale(float scale)
	{
		SgtHelper.SetLocalScale(transform, scale);
	}

	public static SgtCloudsphereModel Create(SgtCloudsphere cloudsphere)
	{
		var model = SgtComponentPool<SgtCloudsphereModel>.Pop(cloudsphere.transform, "Model", cloudsphere.gameObject.layer);

		model.Cloudsphere = cloudsphere;

		return model;
	}

	public static void Pool(SgtCloudsphereModel model)
	{
		if (model != null)
		{
			model.Cloudsphere = null;

			SgtComponentPool<SgtCloudsphereModel>.Add(model);
		}
	}

	public static void MarkForDestruction(SgtCloudsphereModel model)
	{
		if (model != null)
		{
			model.Cloudsphere = null;

			model.gameObject.SetActive(true);
		}
	}
	
	protected virtual void Update()
	{
		if (Cloudsphere == null)
		{
			Pool(this);
		}
	}
}
