using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtLightning))]
public class SgtLightning_Editor : SgtEditor<SgtLightning>
{
	protected override void OnInspector()
	{
		DrawDefault("Age");
		BeginError(Any(t => t.Life < 0.0f));
			DrawDefault("Life");
		EndError();

		Separator();

		BeginDisabled();
			DrawDefault("LightningSpawner");
		EndDisabled();
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu("")]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SgtLightning : MonoBehaviour
{
	[Tooltip("The lightning spawner this was spawned from")]
	public SgtLightningSpawner LightningSpawner;

	[Tooltip("The maximum amount of seconds this lightning has been active for")]
	public float Age;

	[Tooltip("The maximum amount of seconds this lightning can be active for")]
	public float Life;
	
	[System.NonSerialized]
	private MeshFilter meshFilter;
	
	[System.NonSerialized]
	private MeshRenderer meshRenderer;

	[System.NonSerialized]
	private Mesh mesh;

	[System.NonSerialized]
	private Material material;

	public Material Material
	{
		get
		{
			return material;
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

	public static SgtLightning Create(SgtLightningSpawner lightningSpawner)
	{
		var model = SgtComponentPool<SgtLightning>.Pop(lightningSpawner.transform, "Lightning", lightningSpawner.gameObject.layer);

		model.LightningSpawner = lightningSpawner;

		return model;
	}

	public static void Pool(SgtLightning model)
	{
		if (model != null)
		{
			model.LightningSpawner = null;

			SgtComponentPool<SgtLightning>.Add(model);
		}
	}

	public static void MarkForDestruction(SgtLightning model)
	{
		if (model != null)
		{
			model.LightningSpawner = null;

			model.gameObject.SetActive(true);
		}
	}

	protected virtual void OnDestroy()
	{
		SgtHelper.Destroy(material);
	}
	
	protected virtual void Update()
	{
		if (LightningSpawner == null)
		{
			Pool(this);
		}
		else
		{
			Age += Time.deltaTime;

			if (Age >= Life)
			{
				SgtComponentPool<SgtLightning>.Add(this);
			}
			else if (material != null)
			{
				material.SetFloat("_Age", SgtHelper.Divide(Age, Life));
			}
		}
	}
}
