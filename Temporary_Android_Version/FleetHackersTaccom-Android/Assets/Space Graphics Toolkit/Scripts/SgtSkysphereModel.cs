using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtSkysphereModel))]
public class SgtSkysphereModel_Editor : SgtEditor<SgtSkysphereModel>
{
	protected override void OnInspector()
	{
		BeginDisabled();
			DrawDefault("Skysphere");
		EndDisabled();
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu("")]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SgtSkysphereModel : MonoBehaviour
{
	[Tooltip("The skysphere this belongs to")]
	public SgtSkysphere Skysphere;

	[System.NonSerialized]
	private MeshFilter meshFilter;

	[System.NonSerialized]
	private MeshRenderer meshRenderer;

	[System.NonSerialized]
	private bool tempSet;

	[System.NonSerialized]
	private Vector3 tempPosition;

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

	public static SgtSkysphereModel Create(SgtSkysphere skysphere)
	{
		var model = SgtComponentPool<SgtSkysphereModel>.Pop(skysphere.transform, "Model", skysphere.gameObject.layer);

		model.Skysphere = skysphere;

		return model;
	}

	public static void Pool(SgtSkysphereModel model)
	{
		if (model != null)
		{
			model.Skysphere = null;

			SgtComponentPool<SgtSkysphereModel>.Add(model);
		}
	}

	public static void MarkForDestruction(SgtSkysphereModel model)
	{
		if (model != null)
		{
			model.Skysphere = null;

			model.gameObject.SetActive(true);
		}
	}

	protected virtual void Update()
	{
		if (Skysphere == null)
		{
			Pool(this);
		}
	}
}
