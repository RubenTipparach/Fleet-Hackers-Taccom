using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtJovian))]
public class SgtJovian_Editor : SgtEditor<SgtJovian>
{
	protected override void OnInspector()
	{
		var updateRimLut      = false;
		var updateLightingLut = false;
		var updateMaterial    = false;
		var updateModels      = false;

		BeginError(Any(t => t.Lights != null && t.Lights.Exists(l => l == null)));
			DrawDefault("Lights", ref updateMaterial);
		EndError();
		BeginError(Any(t => t.Shadows != null && t.Shadows.Exists(s => s == null)));
			DrawDefault("Shadows", ref updateMaterial);
		EndError();

		Separator();

		DrawDefault("Color", ref updateMaterial);
		BeginError(Any(t => t.Brightness < 0.0f));
			DrawDefault("Brightness", ref updateMaterial);
		EndError();
		DrawDefault("RenderQueue", ref updateMaterial);
		DrawDefault("RenderQueueOffset", ref updateMaterial);
		DrawDefault("Smooth", ref updateMaterial);

		Separator();

		BeginError(Any(t => t.MainTex == null));
			DrawDefault("MainTex", ref updateMaterial);
		EndError();
		DrawDefault("Scattering", ref updateMaterial);

		if (Any(t => t.Scattering == true))
		{
			BeginIndent();
				DrawDefault("MieSharpness", ref updateMaterial);
				DrawDefault("MieStrength", ref updateMaterial);
				DrawDefault("LimitAlpha", ref updateMaterial);
			EndIndent();
		}

		Separator();
		
		DrawDefault("LightingBrightness", ref updateLightingLut);
		DrawDefault("LightingColor", ref updateLightingLut);
		DrawDefault("RimColor", ref updateRimLut);

		Separator();

		DrawDefault("DensityMode", ref updateMaterial);
		BeginError(Any(t => t.Density < 0.0f));
			DrawDefault("Density", ref updateMaterial);
		EndError();
		BeginError(Any(t => t.Power < 0.0f));
			DrawDefault("Power", ref updateMaterial);
		EndError();
		BeginError(Any(t => t.MeshRadius <= 0.0f));
			DrawDefault("MeshRadius", ref updateModels);
		EndError();
		BeginError(Any(t => t.Meshes != null && t.Meshes.Count == 0));
			DrawDefault("Meshes", ref updateModels);
		EndError();

		RequireObserver();

		if (updateRimLut      == true) DirtyEach(t => t.UpdateRimLut     ());
		if (updateLightingLut == true) DirtyEach(t => t.UpdateLightingLut());
		if (updateMaterial    == true) DirtyEach(t => t.UpdateMaterial   ());
		if (updateModels      == true) DirtyEach(t => t.UpdateModels     ());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu("Space Graphics Toolkit/SGT Jovian")]
public class SgtJovian : MonoBehaviour
{
	// All currently active and enabled jovians
	public static List<SgtJovian> AllJovians = new List<SgtJovian>();

	[Tooltip("The lights shining on this jovian")]
	public List<Light> Lights;

	[Tooltip("The shadows casting on this jovian")]
	public List<SgtShadow> Shadows;

	[Tooltip("The radius of the jovian meshes specified below")]
	public float MeshRadius = 1.0f;

	[Tooltip("The meshes used to build the jovian (should be a sphere)")]
	public List<Mesh> Meshes;

	[Tooltip("The color tint of this jovian")]
	public Color Color = Color.white;

	[Tooltip("The color brightness of this jovian")]
	public float Brightness = 1.0f;

	[Tooltip("The render queue group for this jovian")]
	public SgtRenderQueue RenderQueue = SgtRenderQueue.Transparent;

	[Tooltip("The render queue offset for this jovian")]
	public int RenderQueueOffset;

	[Tooltip("Should the final alpha curve be modified with smoothstep?")]
	public bool Smooth = true;

	[Tooltip("Should lights scatter through the jovian?")]
	public bool Scattering;

	[Tooltip("The Mie phase sharpness")]
	[SgtRange(0.0f, 5.0f)]
	public float MieSharpness = 2.0f;

	[Tooltip("The Mie phase strength")]
	[SgtRange(0.0f, 10.0f)]
	public float MieStrength = 1.0f;

	[Tooltip("Should the final alpha be clamped?")]
	public bool LimitAlpha = true;

	[Tooltip("The main cube map texture used to render the jovian")]
	public Cubemap MainTex;

	[Tooltip("The power/sharpness of the atmosphe relative to its altitude")]
	public float Power = 3.0f;

	[Tooltip("The density of the atmosphe")]
	public float Density = 10.0f;

	[Tooltip("The atmospheric density distribution model")]
	public SgtOutputMode DensityMode;

	[Tooltip("The color of the jovian based on how lit it is")]
	public Gradient LightingColor;

	[Tooltip("The brightness of the jovian based on how lit it is")]
	public Gradient LightingBrightness;
	
	[Tooltip("The color of the edge of the jovian")]
	public Gradient RimColor;
	
	[Tooltip("The models used to render the full jovian")]
	[FormerlySerializedAs("models")]
	public List<SgtJovianModel> Models;

	// The lighting color look up table
	[System.NonSerialized]
	public Texture2D LightingLut;

	// The rim color look up table
	[System.NonSerialized]
	public Texture2D RimLut;
	
	// The material applied to all models
	[System.NonSerialized]
	public Material Material;

	[SerializeField]
	[HideInInspector]
	private bool startCalled;
	
	[System.NonSerialized]
	private bool updateRimLutCalled;

	[System.NonSerialized]
	private bool updateLightingLutCalled;

	[System.NonSerialized]
	private bool updateMaterialCalled;

	[System.NonSerialized]
	private bool updateModelsCalled;
	
	private static GradientColorKey[] defaultLightingBrightness = new GradientColorKey[] { new GradientColorKey(Color.black, 0.4f), new GradientColorKey(Color.white, 0.6f) };

	private static GradientColorKey[] defaultLightingColor = new GradientColorKey[] { new GradientColorKey(Color.red, 0.25f), new GradientColorKey(Color.white, 0.5f) };

	private static GradientColorKey[] defaultRimColor = new GradientColorKey[] { new GradientColorKey(Color.blue, 0.0f), new GradientColorKey(Color.white, 0.5f) };

	[ContextMenu("Update Rim LUT")]
	public void UpdateRimLut()
	{
		updateRimLutCalled = true;

		if (RimLut == null || RimLut.width != 1 || RimLut.height != 64)
		{
			SgtHelper.Destroy(RimLut);

			RimLut = SgtHelper.CreateTempTexture2D("Rim LUT", 1, 64);

			if (Material != null)
			{
				Material.SetTexture("_RimLut", RimLut);
			}
		}

		for (var y = 0; y < RimLut.height; y++)
		{
			var t = y / (float)RimLut.height;

			RimLut.SetPixel(0, y, RimColor.Evaluate(t));
		}

		RimLut.wrapMode = TextureWrapMode.Clamp;

		RimLut.Apply();
	}

	[ContextMenu("Update Lighting LUT")]
	public void UpdateLightingLut()
	{
		updateLightingLutCalled = true;

		if (LightingLut == null || LightingLut.width != 1 || LightingLut.height != 64)
		{
			SgtHelper.Destroy(LightingLut);

			LightingLut = SgtHelper.CreateTempTexture2D("Lighting LUT", 1, 64);

			if (Material != null)
			{
				Material.SetTexture("_LightingLut", LightingLut);
			}
		}

		for (var y = 0; y < LightingLut.height; y++)
		{
			var t = y / (float)LightingLut.height;

			LightingLut.SetPixel(0, y, LightingBrightness.Evaluate(t) * LightingColor.Evaluate(t));
		}

		LightingLut.wrapMode = TextureWrapMode.Clamp;

		LightingLut.Apply();
	}

	[ContextMenu("Update Material")]
	public void UpdateMaterial()
	{
		updateMaterialCalled = true;

		if (Material == null)
		{
			Material = SgtHelper.CreateTempMaterial(SgtHelper.ShaderNamePrefix + "Jovian");

			if (Models != null)
			{
				for (var i = Models.Count - 1; i >= 0; i--)
				{
					var model = Models[i];

					if (model != null)
					{
						model.SetMaterial(Material);
					}
				}
			}

			if (RimLut != null)
			{
				Material.SetTexture("_RimLut", RimLut);
			}

			if (LightingLut != null)
			{
				Material.SetTexture("_LightingLut", LightingLut);
			}
		}

		Material.renderQueue = (int)RenderQueue + RenderQueueOffset;

		Material.SetTexture("_MainTex", MainTex);
		Material.SetColor("_Color", SgtHelper.Brighten(Color, Brightness));
		Material.SetFloat("_Power", Power);
		Material.SetFloat("_Density", Density);
		
		SgtHelper.SetTempMaterial(Material);

		if (Smooth == true)
		{
			SgtHelper.EnableKeyword("SGT_B");
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_B");
		}
			
		switch (DensityMode)
		{
			case SgtOutputMode.Linear:      SgtHelper.DisableKeyword("SGT_C"); break;
			case SgtOutputMode.Logarithmic: SgtHelper. EnableKeyword("SGT_C"); break;
		}

		if (Scattering == true)
		{
			SgtHelper.EnableKeyword("SGT_D");
				
			SgtHelper.WriteMie(MieSharpness, MieStrength);

			if (LimitAlpha == true)
			{
				SgtHelper.EnableKeyword("SGT_E");
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_E");
			}
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_D");
			SgtHelper.DisableKeyword("SGT_E");
		}

		UpdateMaterialNonSerialized();
	}
	
	[ContextMenu("Update Models")]
	public void UpdateModels()
	{
		updateModelsCalled = true;

		var meshCount = Meshes != null ? Meshes.Count : 0;
		
		for (var i = 0; i < meshCount; i++)
		{
			var mesh  = Meshes[i];
			var model = GetOrAddModel(i);

			model.SetMesh(mesh);
			model.SetMaterial(Material);
		}

		// Remove any excess
		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= meshCount; i--)
			{
				SgtJovianModel.Pool(Models[i]);

				Models.RemoveAt(i);
			}
		}
	}

	public static SgtJovian CreateJovian(int layer = 0, Transform parent = null)
	{
		return CreateJovian(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtJovian CreateJovian(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Jovian", layer, parent, localPosition, localRotation, localScale);
		var jovian     = gameObject.AddComponent<SgtJovian>();

		return jovian;
	}

#if UNITY_EDITOR
	[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Jovian", false, 10)]
	public static void CreateJovianMenuItem()
	{
		var parent = SgtHelper.GetSelectedParent();
		var jovian = CreateJovian(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(jovian);
	}
#endif

	protected virtual void OnEnable()
	{
#if UNITY_EDITOR
		if (AllJovians.Count == 0)
		{
			SgtHelper.RepaintAll();
		}
#endif
		AllJovians.Add(this);

		Camera.onPreRender += CameraPreRender;

		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= 0; i--)
			{
				var model = Models[i];

				if (model != null)
				{
					model.gameObject.SetActive(true);
				}
			}
		}

		if (startCalled == true)
		{
			CheckUpdateCalls();
		}
	}

	protected virtual void Start()
	{
		if (startCalled == false)
		{
			startCalled = true;
			
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

			if (RimColor == null)
			{
				RimColor = new Gradient();
				RimColor.colorKeys = defaultRimColor;
			}

			// Add a mesh?
#if UNITY_EDITOR
			if (Meshes == null)
			{
				var mesh = SgtHelper.LoadFirstAsset<Mesh>("Geosphere40 t:mesh");

				if (mesh != null)
				{
					Meshes = new List<Mesh>();

					Meshes.Add(mesh);
				}
			}
#endif

			CheckUpdateCalls();
		}
	}

	protected virtual void LateUpdate()
	{
		// The lights and shadows may have moved, so write them
		if (Material != null)
		{
			SgtHelper.SetTempMaterial(Material);

			SgtHelper.WriteLights(Lights, 2, transform.position, transform, null);
			SgtHelper.WriteShadows(Shadows, 2);
		}
	}

	protected virtual void OnDisable()
	{
		AllJovians.Remove(this);

		Camera.onPreRender -= CameraPreRender;

		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= 0; i--)
			{
				var model = Models[i];

				if (model != null)
				{
					model.gameObject.SetActive(false);
				}
			}
		}
	}

	protected virtual void OnDestroy()
	{
		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= 0; i--)
			{
				SgtJovianModel.MarkForDestruction(Models[i]);
			}
		}

		SgtHelper.Destroy(Material);
		SgtHelper.Destroy(LightingLut);
		SgtHelper.Destroy(RimLut);
	}

#if UNITY_EDITOR
	protected virtual void OnDrawGizmosSelected()
	{
		if (SgtHelper.Enabled(this) == true)
		{
			var r0 = transform.lossyScale;

			SgtHelper.DrawSphere(transform.position, transform.right * r0.x, transform.up * r0.y, transform.forward * r0.z);
		}
	}
#endif

	private void UpdateMaterialNonSerialized()
	{
		var localToWorld = transform.localToWorldMatrix * SgtHelper.Scaling(MeshRadius * 2.0f); // Double mesh radius so the max thickness caps at 1.0
		
		Material.SetMatrix("_WorldToLocal", localToWorld.inverse);

		Material.SetMatrix("_LocalToWorld", localToWorld);
		
		SgtHelper.SetTempMaterial(Material);

		SgtHelper.WriteShadowsNonSerialized(Shadows, 2);
	}
	
	private void CameraPreRender(Camera camera)
	{
		if (Material != null)
		{
			var cameraPosition      = camera.transform.position;
			var localCameraPosition = transform.InverseTransformPoint(cameraPosition);
			var localDistance       = localCameraPosition.magnitude;
			
			if (localDistance > MeshRadius)
			{
				SgtHelper.EnableKeyword("SGT_A", Material);
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_A", Material);
			}
			
			UpdateMaterialNonSerialized();
		}
	}

	private SgtJovianModel GetOrAddModel(int index)
	{
		var model = default(SgtJovianModel);

		if (Models == null)
		{
			Models = new List<SgtJovianModel>();
		}

		if (index < Models.Count)
		{
			model = Models[index];

			if (model == null)
			{
				model = SgtJovianModel.Create(this);

				Models[index] = model;
			}
		}
		else
		{
			model = SgtJovianModel.Create(this);

			Models.Add(model);
		}

		return model;
	}

	private void CheckUpdateCalls()
	{
		if (updateRimLutCalled == false)
		{
			UpdateRimLut();
		}

		if (updateLightingLutCalled == false)
		{
			UpdateLightingLut();
		}

		if (updateMaterialCalled == false)
		{
			UpdateMaterial();
		}

		if (updateModelsCalled == false)
		{
			UpdateModels();
		}
	}
}
