using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtCorona))]
public class SgtCorona_Editor : SgtEditor<SgtCorona>
{
	protected override void OnInspector()
	{
		var updateAtmosphereLut  = false;
		var updateMaterials      = false;
		var updateInnerRenderers = false;
		var updateOuters         = false;

		DrawDefault("Color", ref updateMaterials);
		BeginError(Any(t => t.Brightness < 0.0f));
			DrawDefault("Brightness", ref updateMaterials);
		EndError();
		DrawDefault("RenderQueue", ref updateMaterials);
		DrawDefault("RenderQueueOffset", ref updateMaterials);
		BeginError(Any(t => t.Fog >= 1.0f));
			DrawDefault("Fog", ref updateMaterials);
		EndError();
		DrawDefault("Smooth", ref updateMaterials);
		
		Separator();
		
		BeginError(Any(t => t.Height <= 0.0f));
			DrawDefault("Height", ref updateMaterials, ref updateOuters);
		EndError();
		BeginError(Any(t => t.InnerPower < 0.0f));
			DrawDefault("InnerPower", ref updateMaterials);
		EndError();
		BeginError(Any(t => t.InnerMeshRadius <= 0.0f));
			DrawDefault("InnerMeshRadius", ref updateMaterials);
		EndError();
		BeginError(Any(InvalidInnerRenderers));
			DrawDefault("InnerRenderers", ref updateInnerRenderers, false);
		EndError();
		
		Separator();
		
		BeginError(Any(t => t.MiddlePower < 0.0f));
			DrawDefault("MiddlePower", ref updateMaterials);
		EndError();
		BeginError(Any(t => t.MiddleRatio >= 1.0f));
			DrawDefault("MiddleRatio", ref updateMaterials);
		EndError();
		
		Separator();
		
		BeginError(Any(t => t.OuterPower < 0.0f));
			DrawDefault("OuterPower", ref updateMaterials);
		EndError();
		BeginError(Any(t => t.OuterMeshRadius <= 0.0f));
			DrawDefault("OuterMeshRadius", ref updateOuters);
		EndError();
		BeginError(Any(t => t.OuterMeshes != null &&(t.OuterMeshes.Count == 0 || t.OuterMeshes.Exists(m => m == null) == true)));
			DrawDefault("OuterMeshes", ref updateOuters);
		EndError();
		
		Separator();
		
		DrawDefault("DensityColor", ref updateAtmosphereLut);
		BeginError(Any(t => t.DensityScale < 0.0f));
			DrawDefault("DensityScale", ref updateMaterials);
		EndError();
		
		if (updateAtmosphereLut == true) DirtyEach(t => t.UpdateAtmosphereLut());
		if (updateMaterials     == true) DirtyEach(t => t.UpdateMaterials    ());
		if (updateOuters        == true) DirtyEach(t => t.UpdateOuters       ());

		if (updateInnerRenderers == true)
		{
			Each(t => t.RemoveInnerMaterial());

			serializedObject.ApplyModifiedProperties();
			
			DirtyEach(t => t.ApplyInnerMaterial());
		}
	}

	private static bool InvalidInnerRenderers(SgtCorona t)
	{
		var terrain = t.GetComponent<SgtTerrain>();

		if (terrain != null && terrain.Corona == t)
		{
			return false;
		}

		return t.InnerRenderers == null || t.InnerRenderers.Count == 0 || t.InnerRenderers.Exists(r => r == null) == true;
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Corona")]
public class SgtCorona : MonoBehaviour
{
	// All active and enabled coronas in the scene
	public static List<SgtCorona> AllCoronas = new List<SgtCorona>();

	[Tooltip("The color tint of this corona")]
	public Color Color = Color.white;

	[Tooltip("The color brightness of this corona")]
	public float Brightness = 1.0f;

	[Tooltip("The render queue group of this corona")]
	public SgtRenderQueue RenderQueue = SgtRenderQueue.Transparent;

	[Tooltip("The render queue offset of this corona")]
	public int RenderQueueOffset;
	
	[Tooltip("The amount of extra atmospheric thickness added")]
	[SgtRange(0.0f, 1.0f)]
	public float Fog = 0.0f;

	[Tooltip("Should the final atmosphere color be smoothstepped to make it less linear?")]
	public bool Smooth = true;

	[Tooltip("The atmospheric falloff for the inner atmosphere (surface)")]
	public float InnerPower = 3.0f;

	[Tooltip("The radius of the inner renderers (surface) in local coordinates")]
	public float InnerMeshRadius = 1.0f;

	[Tooltip("The renderers that are used to render the inner atmosphere (surface)")]
	public List<MeshRenderer> InnerRenderers;

	[Tooltip("The atmospheric falloff of the sky when viewed from within the atmosphere")]
	public float MiddlePower = 0.0f;
	
	[Tooltip("The point where the atmosphere fully transitions between OuterPower and MiddlePower (0 = surface, 1 = space)")]
	[SgtRange(0.0f, 1.0f)]
	public float MiddleRatio = 0.5f;

	[Tooltip("The atmospheric falloff of the sky when viewed from space")]
	public float OuterPower = 3.0f;

	[Tooltip("The radius of the meshes used to render the sky in local coordinates")]
	public float OuterMeshRadius = 1.0f;

	[Tooltip("The meshes used to render the sky")]
	public List<Mesh> OuterMeshes;

	[Tooltip("The color applied to the sky based on its current optical depth")]
	public Gradient DensityColor;

	[Tooltip("The scale of the relationship between the optical depth and the density color mapping")]
	public float DensityScale = 1.0f;

	[Tooltip("The height of the sky above the surface in local coordinates")]
	public float Height = 0.1f;
	
	// The GameObjects used to render the sky
	[FormerlySerializedAs("outers")]
	public List<SgtCoronaOuter> Outers;

	// The atmosphere color look up table
	[System.NonSerialized]
	public Texture2D AtmosphereLut;

	// The material applied to the surface
	[System.NonSerialized]
	public Material InnerMaterial;

	// The material applied to the sky
	[System.NonSerialized]
	public Material OuterMaterial;
	
	[SerializeField]
	protected bool awakeCalled;

	[SerializeField]
	[HideInInspector]
	protected bool startCalled;
	
	[System.NonSerialized]
	protected bool updateAtmosphereLutCalled;

	[System.NonSerialized]
	protected bool updateMaterialsCalled;
	
	[System.NonSerialized]
	protected bool updateOutersCalled;

	private static GradientColorKey[] defaultDensityColor = new GradientColorKey[] { new GradientColorKey(Color.yellow, 0.5f) };
	
	public float OuterRadius
	{
		get
		{
			return InnerMeshRadius + Height;
		}
	}

	public float MiddleHeight
	{
		get
		{
			return MiddleRatio * Height;
		}
	}

	public float MiddleRadius
	{
		get
		{
			return InnerMeshRadius + MiddleRatio * Height;
		}
	}

	protected virtual string InnerShaderName
	{
		get
		{
			return SgtHelper.ShaderNamePrefix + "CoronaInner";
		}
	}

	protected virtual string OuterShaderName
	{
		get
		{
			return SgtHelper.ShaderNamePrefix + "CoronaOuter";
		}
	}

	[ContextMenu("Update Atmosphere LUT")]
	public virtual void UpdateAtmosphereLut()
	{
		if (AtmosphereLut == null || AtmosphereLut.width != 1 || AtmosphereLut.height != 64)
		{
			SgtHelper.Destroy(AtmosphereLut);

			AtmosphereLut = SgtHelper.CreateTempTexture2D("Atmosphere LUT", 1, 64);
		}

		for (var y = 0; y < AtmosphereLut.height; y++)
		{
			var t = y / (float)AtmosphereLut.height;

			AtmosphereLut.SetPixel(0, y, DensityColor.Evaluate(t));
		}

		AtmosphereLut.wrapMode = TextureWrapMode.Clamp;

		AtmosphereLut.Apply();

		if (InnerMaterial != null)
		{
			InnerMaterial.SetTexture("_AtmosphereLut", AtmosphereLut);
		}

		if (OuterMaterial != null)
		{
			OuterMaterial.SetTexture("_AtmosphereLut", AtmosphereLut);
		}
	}

	[ContextMenu("Update Materials")]
	public virtual void UpdateMaterials()
	{
		updateMaterialsCalled = true;

		if (InnerMaterial == null)
		{
			InnerMaterial = SgtHelper.CreateTempMaterial(InnerShaderName);

			if (InnerRenderers != null)
			{
				for (var i = InnerRenderers.Count - 1; i >= 0; i--)
				{
					var innerRenderer = InnerRenderers[i];

					if (innerRenderer != null)
					{
						SgtHelper.AddMaterial(innerRenderer, InnerMaterial);
					}
				}
			}
			
			if (AtmosphereLut != null)
			{
				InnerMaterial.SetTexture("_AtmosphereLut", AtmosphereLut);
			}
		}

		if (OuterMaterial == null)
		{
			OuterMaterial = SgtHelper.CreateTempMaterial(OuterShaderName);

			if (Outers != null)
			{
				for (var i = Outers.Count - 1; i >= 0; i--)
				{
					var outer = Outers[i];

					if (outer != null)
					{
						outer.SetMaterial(OuterMaterial);
					}
				}
			}

			if (AtmosphereLut != null)
			{
				OuterMaterial.SetTexture("_AtmosphereLut", AtmosphereLut);
			}
		}
		
		var color = SgtHelper.Brighten(Color, Brightness);

		InnerMaterial.renderQueue = OuterMaterial.renderQueue = (int)RenderQueue + RenderQueueOffset;

		InnerMaterial.SetColor("_Color", color);
		InnerMaterial.SetFloat("_AtmosphereScale", DensityScale);
		InnerMaterial.SetFloat("_Power", InnerPower);
		InnerMaterial.SetFloat("_SkyRadius", OuterRadius);
		InnerMaterial.SetFloat("_SkyRadiusRecip", SgtHelper.Reciprocal(OuterRadius));
		
		OuterMaterial.SetColor("_Color", color);
		OuterMaterial.SetFloat("_AtmosphereScale", DensityScale);
		
		SgtHelper.SetTempMaterial(InnerMaterial, OuterMaterial);
		
		if (Smooth == true)
		{
			SgtHelper.EnableKeyword("SGT_B");
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_B");
		}

		UpdateMaterialNonSerialized();
	}

	private SgtTerrain terrain;

	[ContextMenu("Apply Inner Material")]
	public void ApplyInnerMaterial()
	{
		if (InnerRenderers != null)
		{
			for (var i = InnerRenderers.Count - 1; i >= 0; i--)
			{
				SgtHelper.AddMaterial(InnerRenderers[i], InnerMaterial);
			}
		}

		if (UpdateTerrain() == true)
		{
			terrain.UpdateMaterials();
		}
	}

	[ContextMenu("Remove Inner Material")]
	public void RemoveInnerMaterial()
	{
		if (InnerRenderers != null)
		{
			for (var i = InnerRenderers.Count - 1; i >= 0; i--)
			{
				SgtHelper.RemoveMaterial(InnerRenderers[i], InnerMaterial);
			}
		}

		if (UpdateTerrain() == true)
		{
			terrain.UpdateMaterials();
		}
	}

	private bool UpdateTerrain()
	{
		if (terrain != null && terrain.Corona == this)
		{
			terrain = null;
		}

		if (terrain == null)
		{
			terrain = GetComponent<SgtTerrain>();
		}

		return terrain != null;
	}

	public void AddInnerRenderer(MeshRenderer renderer)
	{
		if (renderer != null)
		{
			if (InnerRenderers == null)
			{
				InnerRenderers = new List<MeshRenderer>();
			}

			if (InnerRenderers.Contains(renderer) == false)
			{
				SgtHelper.AddMaterial(renderer, InnerMaterial);

				InnerRenderers.Add(renderer);
			}
		}
	}

	public void RemoveRenderer(MeshRenderer renderer)
	{
		if (renderer != null && InnerRenderers != null)
		{
			SgtHelper.RemoveMaterial(renderer, InnerMaterial);
			
			InnerRenderers.Remove(renderer);
		}
	}
	
	[ContextMenu("Update Outers")]
	public void UpdateOuters()
	{
		updateOutersCalled = true;

		var meshCount  = OuterMeshes != null ? OuterMeshes.Count : 0;
		var outerScale = SgtHelper.Divide(OuterRadius, OuterMeshRadius);

		for (var i = 0; i < meshCount; i++)
		{
			var outerMesh = OuterMeshes[i];
			var outer     = GetOrAddOuter(i);

			outer.SetMesh(outerMesh);
			outer.SetMaterial(OuterMaterial);
			outer.SetScale(outerScale);
		}

		// Remove any excess
		if (Outers != null)
		{
			for (var i = Outers.Count - 1; i >= meshCount; i--)
			{
				var outer = Outers[i];

				if (outer != null)
				{
					SgtCoronaOuter.Pool(outer);
				}

				Outers.RemoveAt(i);
			}
		}
	}
	
	protected virtual float CalculateOuterPower(Vector3 cameraPosition, float clampedAltitude)
	{
		return Mathf.Lerp(MiddlePower, OuterPower, clampedAltitude);
	}
	
	protected virtual void OnEnable()
	{
#if UNITY_EDITOR
		if (AllCoronas.Count == 0)
		{
			SgtHelper.RepaintAll();
		}
#endif
		AllCoronas.Add(this);

		Camera.onPreRender += CameraPreRender;

		if (InnerRenderers != null)
		{
			for (var i = InnerRenderers.Count - 1; i >= 0; i--)
			{
				SgtHelper.ReplaceMaterial(InnerRenderers[i], InnerMaterial);
			}
		}

		if (Outers != null)
		{
			for (var i = Outers.Count - 1; i >= 0; i--)
			{
				var outer = Outers[i];

				if (outer != null)
				{
					outer.gameObject.SetActive(true);
				}
			}
		}

		if (startCalled == true)
		{
			CheckUpdateCalls();
		}

		ApplyInnerMaterial();
	}

	protected virtual void Start()
	{
		if (startCalled == false)
		{
			startCalled = true;
			
			StartOnce();
		}
	}

	protected virtual void StartOnce()
	{
		if (DensityColor == null)
		{
			DensityColor = new Gradient();
			DensityColor.colorKeys = defaultDensityColor;
		}

		// Is this corona being added to a terrain?
		var terrain = GetComponent<SgtTerrain>();

		if (terrain != null)
		{
			terrain.Corona = this;

			terrain.UpdateMaterialsDirty();
		}
		// Is this corona being added to a sphere?
		else
		{
			if (InnerRenderers == null)
			{
				var meshRenderer = GetComponent<MeshRenderer>();

				if (meshRenderer != null)
				{
					var meshFilter = GetComponent<MeshFilter>();

					if (meshFilter != null)
					{
						var mesh = meshFilter.sharedMesh;

						if (mesh != null)
						{
							var min = mesh.bounds.min;
							var max = mesh.bounds.max;
							var avg = Mathf.Abs(min.x) + Mathf.Abs(min.y) + Mathf.Abs(min.z) + Mathf.Abs(max.x) + Mathf.Abs(max.y) + Mathf.Abs(max.z);

							InnerMeshRadius = avg / 6.0f;
							InnerRenderers  = new List<MeshRenderer>();

							InnerRenderers.Add(meshRenderer);
						}
					}
				}
			}
		}
		
#if UNITY_EDITOR
		// Add an outer mesh?
		if (OuterMeshes == null)
		{
			var mesh = SgtHelper.LoadFirstAsset<Mesh>("Geosphere40 t:mesh");

			if (mesh != null)
			{
				OuterMeshes = new List<Mesh>();

				OuterMeshes.Add(mesh);
			}
		}
#endif
		CheckUpdateCalls();
	}

	protected virtual void OnDisable()
	{
		AllCoronas.Remove(this);

		Camera.onPreRender -= CameraPreRender;

		RemoveInnerMaterial();

		if (Outers != null)
		{
			for (var i = Outers.Count - 1; i >= 0; i--)
			{
				var outer = Outers[i];

				if (outer != null)
				{
					outer.gameObject.SetActive(false);
				}
			}
		}
	}

	protected virtual void OnDestroy()
	{
		if (Outers != null)
		{
			for (var i = Outers.Count - 1; i >= 0; i--)
			{
				SgtCoronaOuter.MarkForDestruction(Outers[i]);
			}
		}

		SgtHelper.Destroy(AtmosphereLut);
		SgtHelper.Destroy(OuterMaterial);
		SgtHelper.Destroy(InnerMaterial);
	}
	
	protected virtual void CameraPreRender(Camera camera)
	{
		if (InnerMaterial != null && OuterMaterial != null)
		{
			var cameraPosition       = camera.transform.position;
			var localCameraPosition  = transform.InverseTransformPoint(cameraPosition);
			var localDistance        = localCameraPosition.magnitude;
			var clampedAltitude      = Mathf.InverseLerp(MiddleRadius, OuterRadius, localDistance);
			var innerAtmosphereDepth = default(float);
			var outerAtmosphereDepth = default(float);
			var radiusRatio          = SgtHelper.Divide(InnerMeshRadius, OuterRadius);
			var innerHeightRatio     = SgtHelper.Divide(MiddleHeight, OuterRadius);
			var scaleDistance        = SgtHelper.Divide(localDistance, OuterRadius);
			var fog                  = 1.0f - Fog;

			SgtHelper.CalculateAtmosphereThicknessAtHorizon(radiusRatio, 1.0f, scaleDistance, out innerAtmosphereDepth, out outerAtmosphereDepth);

			// Make the fog level reduce when you get closer than the ground height
			innerAtmosphereDepth = Mathf.Max(innerAtmosphereDepth, innerHeightRatio);

			SgtHelper.SetTempMaterial(InnerMaterial, OuterMaterial);

			if (scaleDistance > 1.0f)
			{
				SgtHelper.EnableKeyword("SGT_A");
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_A");
			}
		
			InnerMaterial.SetFloat("_HorizonLengthRecip", SgtHelper.Reciprocal(innerAtmosphereDepth * fog));
			OuterMaterial.SetFloat("_HorizonLengthRecip", SgtHelper.Reciprocal(outerAtmosphereDepth * fog));

			OuterMaterial.SetFloat("_Power", CalculateOuterPower(cameraPosition, clampedAltitude));

			UpdateMaterialNonSerialized();
		}
	}

	protected virtual void CheckUpdateCalls()
	{
		if (updateAtmosphereLutCalled == false)
		{
			UpdateAtmosphereLut();
		}

		if (updateMaterialsCalled == false)
		{
			UpdateMaterials();
		}

		if (updateOutersCalled == false)
		{
			UpdateOuters();
		}
	}

	protected virtual void UpdateMaterialNonSerialized()
	{
		var scale        = SgtHelper.Divide(OuterMeshRadius, OuterRadius);
		var worldToLocal = SgtHelper.Scaling(scale) * transform.worldToLocalMatrix;

		InnerMaterial.SetMatrix("_WorldToLocal", worldToLocal);
		OuterMaterial.SetMatrix("_WorldToLocal", worldToLocal);
	}

#if UNITY_EDITOR
	protected virtual void OnDrawGizmosSelected()
	{
		if (SgtHelper.Enabled(this) == true)
		{
			var r0 = InnerMeshRadius;
			var r1 = MiddleRadius;
			var r2 = OuterRadius;

			SgtHelper.DrawSphere(transform.position, transform.right * transform.lossyScale.x * r0, transform.up * transform.lossyScale.y * r0, transform.forward * transform.lossyScale.z * r0);
			SgtHelper.DrawSphere(transform.position, transform.right * transform.lossyScale.x * r1, transform.up * transform.lossyScale.y * r1, transform.forward * transform.lossyScale.z * r1);
			SgtHelper.DrawSphere(transform.position, transform.right * transform.lossyScale.x * r2, transform.up * transform.lossyScale.y * r2, transform.forward * transform.lossyScale.z * r2);
		}
	}
#endif
	
	private SgtCoronaOuter GetOrAddOuter(int index)
	{
		var outer = default(SgtCoronaOuter);

		if (Outers == null)
		{
			Outers = new List<SgtCoronaOuter>();
		}

		if (index < Outers.Count)
		{
			outer = Outers[index];

			if (outer == null)
			{
				outer = SgtCoronaOuter.Create(this);

				Outers[index] = outer;
			}
		}
		else
		{
			outer = SgtCoronaOuter.Create(this);

			Outers.Add(outer);
		}

		return outer;
	}
}
