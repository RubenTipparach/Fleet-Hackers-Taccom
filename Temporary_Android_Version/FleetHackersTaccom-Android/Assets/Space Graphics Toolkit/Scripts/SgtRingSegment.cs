using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtRingSegment))]
public class SgtRingSegment_Editor : SgtEditor<SgtRingSegment>
{
	protected override void OnInspector()
	{
		BeginDisabled();
			DrawDefault("Ring");
		EndDisabled();
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu("")]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SgtRingSegment : MonoBehaviour
{
	[Tooltip("The ring this belongs to")]
	public SgtRing Ring;
	
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

	public void SetRotation(Quaternion rotation)
	{
		SgtHelper.SetLocalRotation(transform, rotation);
	}

	public static SgtRingSegment Create(SgtRing ring)
	{
		var segment = SgtComponentPool<SgtRingSegment>.Pop(ring.transform, "Segment", ring.gameObject.layer);

		segment.Ring = ring;

		return segment;
	}

	public static void Pool(SgtRingSegment segment)
	{
		if (segment != null)
		{
			segment.Ring = null;

			SgtComponentPool<SgtRingSegment>.Add(segment);
		}
	}

	public static void MarkForDestruction(SgtRingSegment segment)
	{
		if (segment != null)
		{
			segment.Ring = null;

			segment.gameObject.SetActive(true);
		}
	}

	protected virtual void Update()
	{
		if (Ring == null)
		{
			Pool(this);
		}
	}
}
