using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtDepth))]
public class SgtDepth_Editor : SgtEditor<SgtDepth>
{
	protected override void OnInspector()
	{
		DrawDefault("RenderQueue");
		DrawDefault("RenderQueueOffset");
		
		Separator();
		
		DrawDefault("Renderers");
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu("Space Graphics Toolkit/SGT Depth")]
public class SgtDepth : MonoBehaviour
{
	public static List<SgtDepth> AllDepthDrawers = new List<SgtDepth>();
	
	public SgtRenderQueue RenderQueue = SgtRenderQueue.Transparent;
	
	public int RenderQueueOffset = 1;
	
	public List<Renderer> Renderers;
	
	[SerializeField]
	private Material depthMaterial;
	
	public Material DepthMaterial
	{
		get
		{
			if (depthMaterial == null)
			{
				UpdateMaterial();
			}
			
			return depthMaterial;
		}
	}
	
	public void AddRenderer(Renderer renderer)
	{
		if (renderer != null)
		{
			if (Renderers == null) Renderers = new List<Renderer>();

			if (Renderers.Contains(renderer) == false)
			{
				if (depthMaterial != null)
				{
					SgtHelper.AddMaterial(renderer, depthMaterial);
				}
				
				Renderers.Add(renderer);
			}
		}
	}
	
	public void RemoveRenderer(Renderer renderer)
	{
		if (renderer != null)
		{
			if (depthMaterial != null)
			{
				SgtHelper.RemoveMaterial(renderer, depthMaterial);
			}
			
			if (Renderers == null) Renderers = new List<Renderer>();

			Renderers.Add(renderer);
		}
	}
	
	protected virtual void OnEnable()
	{
#if UNITY_EDITOR
		if (AllDepthDrawers.Count == 0)
		{
			SgtHelper.RepaintAll();
		}
#endif
		AllDepthDrawers.Add(this);
	}
	
	protected virtual void OnDisable()
	{
		AllDepthDrawers.Remove(this);
		
		RemoveMaterial();
	}
	
	protected virtual void OnDestroy()
	{
		RemoveMaterial();
		
		SgtHelper.Destroy(depthMaterial);
	}
	
	protected virtual void LateUpdate()
	{
		UpdateMaterial();
		
		if (Renderers != null)
		{
			for (var i = Renderers.Count - 1; i >= 0; i--)
			{
				var renderer = Renderers[i];
			
				SgtHelper.BeginStealthSet(renderer);
				{
					SgtHelper.AddMaterial(renderer, depthMaterial);
				}
				SgtHelper.EndStealthSet();
			}
		}
	}
	
	private void UpdateMaterial()
	{
		if (depthMaterial == null) depthMaterial = SgtHelper.CreateTempMaterial(SgtHelper.ShaderNamePrefix + "Depth");
		
		depthMaterial.renderQueue = (int)RenderQueue + RenderQueueOffset;
	}
	
	private void RemoveMaterial()
	{
		if (Renderers != null)
		{
			for (var i = Renderers.Count - 1; i >= 0; i--)
			{
				var renderer = Renderers[i];
			
				SgtHelper.BeginStealthSet(renderer);
				{
					SgtHelper.RemoveMaterial(renderer, depthMaterial);
				}
				SgtHelper.EndStealthSet();
			}
		}
	}
}