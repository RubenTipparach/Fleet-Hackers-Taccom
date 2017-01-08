using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtAtmosphere))]
public class SgtAtmosphere_Editor : SgtEditor<SgtAtmosphere>
{
	protected override void OnInspector()
	{
		var updateAtmosphereLut  = false;
		var updateLightingLut    = false;
		var updateMaterials      = false;
		var updateInnerRenderers = false;
		var updateOuters         = false;

		BeginError(Any(t => t.Lights != null && t.Lights.Exists(l => l == null)));
			DrawDefault("Lights", ref updateMaterials);
		EndError();
		BeginError(Any(t => t.Shadows != null && t.Shadows.Exists(s => s == null)));
			DrawDefault("Shadows", ref updateMaterials);
		EndError();

		Separator();

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

		DrawDefault("Scattering", ref updateMaterials);

		if (Any(t => t.Scattering == true))
		{
			BeginIndent();
				DrawDefault("MieSharpness", ref updateMaterials);
				DrawDefault("MieStrength", ref updateMaterials);
				DrawDefault("RayleighStrength", ref updateMaterials);
				DrawDefault("LimitBrightness", ref updateMaterials);
				DrawDefault("GroundScattering", ref updateMaterials);

				if (Any(t => t.GroundScattering == true))
				{
					BeginIndent();
						DrawDefault("GroundPower", ref updateMaterials);
						DrawDefault("GroundMieSharpness", ref updateMaterials);
						DrawDefault("GroundMieStrength", ref updateMaterials);
					EndIndent();
				}
			EndIndent();
		}

		Separator();

		BeginError(Any(t => t.Height <= 0.0f));
			DrawDefault("Height", ref updateMaterials, ref updateOuters);
		EndError();
		BeginError(Any(t => t.InnerPower < 0.0f));
			DrawDefault("InnerPower", ref updateMaterials);
		EndError();
		BeginError(Any(t => t.InnerMeshRadius <= 0.0f));
			DrawDefault("InnerMeshRadius", ref updateInnerRenderers, false);
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
		BeginError(Any(t => t.OuterMeshes.Count == 0 || t.OuterMeshes.Exists(m => m == null) == true));
			DrawDefault("OuterMeshes", ref updateOuters);
		EndError();

		Separator();
		
		DrawDefault("LightingBrightness", ref updateLightingLut);
		DrawDefault("LightingColor", ref updateLightingLut);
		DrawDefault("DensityColor", ref updateAtmosphereLut);
		BeginError(Any(t => t.DensityScale < 0.0f));
			DrawDefault("DensityScale", ref updateMaterials);
		EndError();
		
		if (updateAtmosphereLut == true) DirtyEach(t => t.UpdateAtmosphereLut());
		if (updateLightingLut   == true) DirtyEach(t => t.UpdateLightingLut  ());
		if (updateMaterials     == true) DirtyEach(t => t.UpdateMaterials    ());
		if (updateOuters        == true) DirtyEach(t => t.UpdateOuters       ());
		
		if (updateInnerRenderers == true)
		{
			Each(t => t.RemoveInnerMaterial());

			serializedObject.ApplyModifiedProperties();
			
			DirtyEach(t => t.ApplyInnerMaterial());
		}
	}

	private static bool InvalidInnerRenderers(SgtAtmosphere t)
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
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Atmosphere")]
public class SgtAtmosphere : SgtCorona
{
	// All active and enabled atmospheres in the scene
	public static List<SgtAtmosphere> AllAtmospheres = new List<SgtAtmosphere>();

	[Tooltip("The lights shining on this atmosphere")]
	public List<Light> Lights;

	[Tooltip("The shadows casting on this atmosphere")]
	public List<SgtShadow> Shadows;

	[Tooltip("Should lights scatter through the atmosphere?")]
	public bool Scattering;

	[Tooltip("The Mie phase sharpness")]
	[SgtRange(0.0f, 5.0f)]
	public float MieSharpness = 2.0f;

	[Tooltip("The Mie phase strength")]
	[SgtRange(0.0f, 10.0f)]
	public float MieStrength = 1.0f;
	
	[Tooltip("The Rayleigh phase strength")]
	[SgtRange(0.0f, 10.0f)]
	public float RayleighStrength = 0.1f;

	[Tooltip("Should the ground also get scattered?")]
	public bool GroundScattering;

	[Tooltip("The falloff of the scattering on the ground")]
	public float GroundPower = 2.0f;

	[Tooltip("The Mie phase sharpness")]
	[SgtRange(0.0f, 5.0f)]
	public float GroundMieSharpness = 1.0f;

	[Tooltip("The Mie phase strength")]
	[SgtRange(0.0f, 10.0f)]
	public float GroundMieStrength = 0.5f;

	[Tooltip("Should the scattering only fill in the remaining rgba so it goes up to 1?")]
	public bool LimitBrightness = true;

	[Tooltip("The brightness of the atmosphere reltive to how close it is to facing the light (left = dark side, right = light side)")]
	public Gradient LightingBrightness;

	[Tooltip("The color of the atmosphere reltive to how close it is to facing the light (left = dark side, right = light side)")]
	public Gradient LightingColor;

	// The lighting look up table
	[System.NonSerialized]
	public Texture2D LightingLut;

	[System.NonSerialized]
	private bool updateLightingLutCalled;
	
	private static GradientColorKey[] defaultLightingBrightness = new GradientColorKey[] { new GradientColorKey(Color.black, 0.4f), new GradientColorKey(Color.white, 0.6f) };

	private static GradientColorKey[] defaultLightingColor = new GradientColorKey[] { new GradientColorKey(Color.red, 0.25f), new GradientColorKey(Color.white, 0.5f) };

	private static GradientColorKey[] defaultDensityColor = new GradientColorKey[] { new GradientColorKey(Color.cyan, 0.0f), new GradientColorKey(Color.white, 1.0f) };

	protected override string InnerShaderName
	{
		get
		{
			return SgtHelper.ShaderNamePrefix + "AtmosphereInner";
		}
	}

	protected override string OuterShaderName
	{
		get
		{
			return SgtHelper.ShaderNamePrefix + "AtmosphereOuter";
		}
	}

	protected override void StartOnce()
	{
		if (LightingBrightness == null)
		{
			LightingBrightness = new Gradient();
			LightingBrightness.colorKeys = defaultLightingBrightness;
		}

		if (LightingColor == null)
		{
			LightingColor = new Gradient();
			LightingColor.colorKeys = defaultLightingColor;
		}

		if (DensityColor == null)
		{
			DensityColor = new Gradient();
			DensityColor.colorKeys = defaultDensityColor;
		}

		base.StartOnce();
	}

	protected override void OnEnable()
	{
		base.OnEnable();

		AllAtmospheres.Add(this);
	}

	protected virtual void LateUpdate()
	{
		// The lights and shadows may have moved, so write them
		if (InnerMaterial != null && OuterMaterial != null)
		{
			SgtHelper.SetTempMaterial(InnerMaterial, OuterMaterial);

			SgtHelper.WriteLights(Lights, 2, transform.position, transform, null);
			SgtHelper.WriteShadows(Shadows, 2);
		}
	}

	protected override void OnDisable()
	{
		base.OnDisable();

		AllAtmospheres.Remove(this);
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		SgtHelper.Destroy(LightingLut);
	}
	
	protected override float CalculateOuterPower(Vector3 cameraPosition, float clampedAltitude)
	{
		var cameraDir  = (cameraPosition - transform.position).normalized;
		var lightCount = 0;
		var maxLights  = 2;
		var strength   = 1.0f - clampedAltitude;

		if (Lights != null)
		{
			for (var i = Lights.Count - 1; i >= 0; i--)
			{
				var light = Lights[i];

				if (SgtHelper.Enabled(light) == true && light.intensity > 0.0f && lightCount < maxLights)
				{
					var direction = default(Vector3);
					var position  = default(Vector3);
					var color     = default(Color);

					SgtHelper.CalculateLight(light, transform.position, null, null, ref position, ref direction, ref color);

					var dot      = Vector3.Dot(direction, cameraDir);
					var lighting = LightingBrightness.Evaluate(dot * 0.5f + 0.5f);

					clampedAltitude += (1.0f - lighting.a) * strength;
				}
			}
		}

		return base.CalculateOuterPower(cameraPosition, clampedAltitude);
	}
	
	public override void UpdateMaterials()
	{
		base.UpdateMaterials();

		InnerMaterial.SetTexture("_LightingLut", LightingLut);

		OuterMaterial.SetTexture("_LightingLut", LightingLut);

		InnerMaterial.SetFloat("_ScatteringPower", GroundPower);

		if (Scattering == true)
		{
			SgtHelper.EnableKeyword("SGT_C", OuterMaterial);

			if (GroundScattering == true)
			{
				SgtHelper.EnableKeyword("SGT_C", InnerMaterial);
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_C", InnerMaterial);
			}

			if (LimitBrightness == true)
			{
				SgtHelper.EnableKeyword("SGT_D", InnerMaterial);
				SgtHelper.EnableKeyword("SGT_D", OuterMaterial);
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_D", InnerMaterial);
				SgtHelper.DisableKeyword("SGT_D", OuterMaterial);
			}

			SgtHelper.SetTempMaterial(OuterMaterial);

			SgtHelper.WriteMie(MieSharpness, MieStrength);
			SgtHelper.WriteRayleigh(RayleighStrength);

			if (GroundScattering == true)
			{
				SgtHelper.SetTempMaterial(InnerMaterial);

				SgtHelper.WriteMie(GroundMieSharpness, GroundMieStrength);
				SgtHelper.WriteRayleigh(RayleighStrength);
			}
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_C", InnerMaterial);
			SgtHelper.DisableKeyword("SGT_C", OuterMaterial);
			SgtHelper.DisableKeyword("SGT_D", InnerMaterial);
			SgtHelper.DisableKeyword("SGT_D", OuterMaterial);
		}
	}
	
	[ContextMenu("Update Lighting LUT")]
	public virtual void UpdateLightingLut()
	{
		updateLightingLutCalled = true;

		if (LightingLut == null || LightingLut.width != 1 || LightingLut.height != 64)
		{
			SgtHelper.Destroy(LightingLut);

			LightingLut = SgtHelper.CreateTempTexture2D("Lighting LUT", 1, 64);
		}

		for (var y = 0; y < LightingLut.height; y++)
		{
			var t = y / (float)LightingLut.height;
			var a = LightingBrightness.Evaluate(t);
			var b = LightingColor.Evaluate(t);
			var c = a * b;

			c.a = c.grayscale;

			LightingLut.SetPixel(0, y, c);
		}

		LightingLut.wrapMode = TextureWrapMode.Clamp;

		LightingLut.Apply();

		if (InnerMaterial != null)
		{
			InnerMaterial.SetTexture("_LightingLut", LightingLut);
		}

		if (OuterMaterial != null)
		{
			OuterMaterial.SetTexture("_LightingLut", LightingLut);
		}
	}

	protected override void CheckUpdateCalls()
	{
		base.CheckUpdateCalls();

		if (updateLightingLutCalled == false)
		{
			UpdateLightingLut();
		}
	}

	protected override void UpdateMaterialNonSerialized()
	{
		base.UpdateMaterialNonSerialized();

		SgtHelper.SetTempMaterial(InnerMaterial, OuterMaterial);
		
		SgtHelper.WriteShadowsNonSerialized(Shadows, 2);
	}
}
