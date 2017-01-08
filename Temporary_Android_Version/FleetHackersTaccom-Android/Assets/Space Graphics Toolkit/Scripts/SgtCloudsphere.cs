using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtCloudsphere))]
public class SgtCloudsphere_Editor : SgtEditor<SgtCloudsphere>
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

		Separator();

		BeginError(Any(t => t.MainTex == null));
			DrawDefault("MainTex", ref updateMaterial);
		EndError();
		BeginError(Any(t => t.Radius < 0.0f));
			DrawDefault("Radius", ref updateModels);
		EndError();
		DrawDefault("CameraOffset"); // Updated automatically
		DrawDefault("FadeNear", ref updateMaterial);

		if (Any(t => t.FadeNear == true))
		{
			BeginIndent();
				BeginError(Any(t => t.FadeInnerRadius < 0.0f || t.FadeInnerRadius >= t.FadeOuterRadius));
					DrawDefault("FadeInnerRadius", ref updateMaterial);
				EndError();
				BeginError(Any(t => t.FadeOuterRadius < 0.0f || t.FadeInnerRadius >= t.FadeOuterRadius));
					DrawDefault("FadeOuterRadius", ref updateMaterial);
				EndError();
			EndIndent();
		}

		Separator();
		
		DrawDefault("LightingBrightness", ref updateLightingLut);
		DrawDefault("LightingColor", ref updateLightingLut);
		DrawDefault("RimColor", ref updateRimLut);

		Separator();

		BeginError(Any(t => t.MeshRadius <= 0.0f));
			DrawDefault("MeshRadius", ref updateModels);
		EndError();
		BeginError(Any(t => t.Meshes != null && t.Meshes.Count == 0));
			DrawDefault("Meshes", ref updateModels);
		EndError();
		
		if (updateRimLut      == true) DirtyEach(t => t.UpdateRimLut     ());
		if (updateLightingLut == true) DirtyEach(t => t.UpdateLightingLut());
		if (updateMaterial    == true) DirtyEach(t => t.UpdateMaterial   ());
		if (updateModels      == true) DirtyEach(t => t.UpdateModels     ());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu("Space Graphics Toolkit/SGT Cloudsphere")]
public class SgtCloudsphere : MonoBehaviour
{
	// All active and enabled cloudspheres in the scene
	public static List<SgtCloudsphere> AllCloudspheres = new List<SgtCloudsphere>();

	[Tooltip("The lights shining on this cloudsphere")]
	public List<Light> Lights;

	[Tooltip("The shadows casting on this cloudsphere")]
	public List<SgtShadow> Shadows;

	[Tooltip("The radius of the cloudsphere meshes specified below")]
	public float MeshRadius = 1.0f;

	[Tooltip("The meshes used to build the cloudsphere (should be a sphere)")]
	public List<Mesh> Meshes;

	[Tooltip("The color tint of this cloudsphere")]
	public Color Color = Color.white;

	[Tooltip("The color brightness of this cloudsphere")]
	public float Brightness = 1.0f;

	[Tooltip("The render queue group for this cloudsphere")]
	public SgtRenderQueue RenderQueue = SgtRenderQueue.Transparent;

	[Tooltip("The render queue offset for this cloudsphere")]
	public int RenderQueueOffset;

	[Tooltip("The desired radius of the cloudsphere in local coordinates")]
	public float Radius = 1.5f;

	[Tooltip("Should the clouds fade out when the camera gets near?")]
	public bool FadeNear;

	[Tooltip("The distance when the clouds become become invisible")]
	public float FadeInnerRadius = 0.25f;

	[Tooltip("The distance when the clouds become fully visible")]
	public float FadeOuterRadius = 0.5f;

	[Tooltip("The amount the clouds get moved toward the current camera")]
	[FormerlySerializedAs("ObserverOffset")]
	public float CameraOffset;

	[Tooltip("The cubemap used to render the clouds")]
	public Cubemap MainTex;

	[Tooltip("The color of the cloudsphere based on how lit it is")]
	public Gradient LightingColor;

	[Tooltip("The brightness of the cloudsphere based on how lit it is")]
	public Gradient LightingBrightness;
	
	[Tooltip("The color of the edge of the cloudsphere")]
	public Gradient RimColor;
	
	// The material applied to all models
	[System.NonSerialized]
	public Material Material;

	// The lighting color look up table
	[System.NonSerialized]
	public Texture2D LightingLut;

	// The rim color look yp table
	[System.NonSerialized]
	public Texture2D RimLut;

	// The models used to render this cloudsphere
	[SerializeField]
	[FormerlySerializedAs("models")]
	public List<SgtCloudsphereModel> Models;
	
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
			Material = SgtHelper.CreateTempMaterial(SgtHelper.ShaderNamePrefix + "Cloudsphere");

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
		
		if (FadeNear == true)
		{
			SgtHelper.EnableKeyword("SGT_A");

			Material.SetFloat("_FadeRadius", FadeInnerRadius);
			Material.SetFloat("_FadeScale", SgtHelper.Reciprocal(FadeOuterRadius - FadeInnerRadius));
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_A");
		}

		Material.renderQueue = (int)RenderQueue + RenderQueueOffset;

		Material.SetTexture("_MainTex", MainTex);
		Material.SetColor("_Color", SgtHelper.Brighten(Color, Brightness));
		Material.SetTexture("_RimLut", RimLut);
		Material.SetTexture("_LightingLut", LightingLut);
	}

	[ContextMenu("Update Models")]
	public void UpdateModels()
	{
		updateModelsCalled = true;

		var meshCount = Meshes != null ? Meshes.Count : 0;
		var scale     = SgtHelper.Divide(Radius, MeshRadius);

		for (var i = 0; i < meshCount; i++)
		{
			var mesh  = Meshes[i];
			var model = GetOrAddModel(i);

			model.SetMesh(mesh);
			model.SetMaterial(Material);
			model.SetScale(scale);
		}

		// Remove any excess
		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= meshCount; i--)
			{
				SgtCloudsphereModel.Pool(Models[i]);

				Models.RemoveAt(i);
			}
		}
	}

	public static SgtCloudsphere CreateCloudsphere(int layer = 0, Transform parent = null)
	{
		return CreateCloudsphere(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtCloudsphere CreateCloudsphere(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject  = SgtHelper.CreateGameObject("Cloudsphere", layer, parent, localPosition, localRotation, localScale);
		var cloudsphere = gameObject.AddComponent<SgtCloudsphere>();

		return cloudsphere;
	}

#if UNITY_EDITOR
	[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Cloudsphere", false, 10)]
	public static void CreateCloudsphereMenuItem()
	{
		var parent      = SgtHelper.GetSelectedParent();
		var cloudsphere = CreateCloudsphere(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(cloudsphere);
	}
#endif
	
	protected virtual void OnEnable()
	{
		AllCloudspheres.Add(this);

		Camera.onPreCull    += CameraPreCull;
		Camera.onPostRender += CameraPostRender;
		
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

			SgtHelper.WriteLights(Lights, 2, transform.position, null, null);
			SgtHelper.WriteShadows(Shadows, 2);
		}
	}

	protected virtual void OnDisable()
	{
		AllCloudspheres.Remove(this);

		Camera.onPreCull    -= CameraPreCull;
		Camera.onPostRender -= CameraPostRender;
		
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
				SgtCloudsphereModel.MarkForDestruction(Models[i]);
			}
		}

		SgtHelper.Destroy(Material);
		SgtHelper.Destroy(LightingLut);
		SgtHelper.Destroy(RimLut);
	}

	private void CameraPreCull(Camera camera)
	{
		if (Material != null)
		{
			UpdateMaterialNonSerialized();
		}

		if (CameraOffset != 0.0f)
		{
			if (Models != null)
			{
				for (var i = Models.Count - 1; i >= 0; i--)
				{
					var model = Models[i];

					if (model != null)
					{
						var modelTransform = model.transform;
						var cameraDir      = (modelTransform.position - camera.transform.position).normalized;

						model.SavePosition();
						
						modelTransform.position += cameraDir * CameraOffset;
					}
				}
			}
		}
	}

	private void UpdateMaterialNonSerialized()
	{
		SgtHelper.SetTempMaterial(Material);

		SgtHelper.WriteShadowsNonSerialized(Shadows, 2);
	}

	private void CameraPostRender(Camera camera)
	{
		if (CameraOffset != 0.0f)
		{
			if (Models != null)
			{
				for (var i = Models.Count - 1; i >= 0; i--)
				{
					var model = Models[i];

					if (model != null)
					{
						model.LoadPosition();
					}
				}
			}
		}
	}
	
	private SgtCloudsphereModel GetOrAddModel(int index)
	{
		var model = default(SgtCloudsphereModel);

		if (Models == null)
		{
			Models = new List<SgtCloudsphereModel>();
		}

		if (index < Models.Count)
		{
			model = Models[index];

			if (model == null)
			{
				model = SgtCloudsphereModel.Create(this);

				Models[index] = model;
			}
		}
		else
		{
			model = SgtCloudsphereModel.Create(this);

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
