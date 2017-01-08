using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtProminencePlane))]
public class SgtProminencePlane_Editor : SgtEditor<SgtProminencePlane>
{
	protected override void OnInspector()
	{
		BeginDisabled();
			DrawDefault("Prominence");
		EndDisabled();
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu("")]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SgtProminencePlane : MonoBehaviour
{
	[Tooltip("The prominence this belongs to")]
	public SgtProminence Prominence;

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

	public void SetRotation(Quaternion rotation)
	{
		SgtHelper.SetLocalRotation(transform, rotation);
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

	public static SgtProminencePlane Create(SgtProminence prominence)
	{
		var plane = SgtComponentPool<SgtProminencePlane>.Pop(prominence.transform, "Plane", prominence.gameObject.layer);

		plane.Prominence = prominence;

		return plane;
	}

	public static void Pool(SgtProminencePlane plane)
	{
		if (plane != null)
		{
			plane.Prominence = null;

			SgtComponentPool<SgtProminencePlane>.Add(plane);
		}
	}

	public static void MarkForDestruction(SgtProminencePlane plane)
	{
		if (plane != null)
		{
			plane.Prominence = null;

			plane.gameObject.SetActive(true);
		}
	}

	protected virtual void Update()
	{
		if (Prominence == null)
		{
			Pool(this);
		}
	}
}
