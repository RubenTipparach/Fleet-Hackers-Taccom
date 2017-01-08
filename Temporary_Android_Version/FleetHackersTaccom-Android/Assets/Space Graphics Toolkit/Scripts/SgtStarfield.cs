using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

public class SgtStarfield_Editor<T> : SgtEditor<T>
	where T : SgtStarfield
{
	protected void DrawMaterialBasic(ref bool updateMaterial)
	{
		DrawDefault("Color", ref updateMaterial);
		BeginError(Any(t => t.Brightness < 0.0f));
			DrawDefault("Brightness", ref updateMaterial);
		EndError();
		DrawDefault("RenderQueue", ref updateMaterial);
		DrawDefault("RenderQueueOffset", ref updateMaterial);
	}

	protected void DrawMaterialTexture(ref bool updateMaterial, ref bool updateMesh)
	{
		BeginError(Any(t => t.MainTex == null));
			DrawDefault("MainTex", ref updateMaterial);
		EndError();
		DrawDefault("Layout", ref updateMesh);
		BeginIndent();
			if (Any(t => t.Layout == SgtStarfieldLayoutType.Grid))
			{
				BeginError(Any(t => t.LayoutColumns <= 0));
					DrawDefault("LayoutColumns", ref updateMesh);
				EndError();
				BeginError(Any(t => t.LayoutRows <= 0));
					DrawDefault("LayoutRows", ref updateMesh);
				EndError();
			}

			if (Any(t => t.Layout == SgtStarfieldLayoutType.Custom))
			{
				DrawDefault("Rects", ref updateMesh);
			}
		EndIndent();
	}
	
	protected override void OnInspector()
	{
		var updateMaterial = false;
		var updateMeshesAndModels     = false;

		DrawMaterialBasic(ref updateMaterial);
		
		Separator();

		DrawMaterialTexture(ref updateMaterial, ref updateMeshesAndModels);

		Separator();

		DrawDefault("Softness", ref updateMaterial);
		
		if (Any(t => t.Softness > 0.0f))
		{
			foreach (var camera in Camera.allCameras)
			{
				if (camera.depthTextureMode == DepthTextureMode.None)
				{
					EditorGUILayout.HelpBox("You have enabled soft particles, but none of your cameras write depth textures.", MessageType.Error);
				}
			}
		}

		Separator();

		DrawDefault("Age");
		DrawDefault("TimeScale");
		
		Separator();
		
		DrawDefault("FadeNear", ref updateMaterial);
		
		if (Any(t => t.FadeNear == true))
		{
			BeginIndent();
				BeginError(Any(t => t.FadeNearRadius < 0.0f));
					DrawDefault("FadeNearRadius", ref updateMaterial);
				EndError();
				BeginError(Any(t => t.FadeNearThickness <= 0.0f));
					DrawDefault("FadeNearThickness", ref updateMaterial);
				EndError();
			EndIndent();
		}
		
		DrawDefault("FadeFar", ref updateMaterial);
		
		if (Any(t => t.FadeFar == true))
		{
			BeginIndent();
				BeginError(Any(t => t.FadeFarRadius < 0.0f));
					DrawDefault("FadeFarRadius", ref updateMaterial);
				EndError();
				BeginError(Any(t => t.FadeFarThickness <= 0.0f));
					DrawDefault("FadeFarThickness", ref updateMaterial);
				EndError();
			EndIndent();
		}
		
		if (Any(t => t is SgtWrappedStarfield == false))
		{
			DrawDefault("FollowCameras", ref updateMaterial);
		}
		
		DrawDefault("StretchToObservers", ref updateMaterial);
		
		if (Any(t => t.StretchToObservers == true))
		{
			BeginIndent();
				BeginError(Any(t => t.StretchScale <= 0.0f));
					DrawDefault("StretchScale", ref updateMaterial);
				EndError();
				DrawDefault("StretchOverride", ref updateMaterial);
				
				if (Any(t => t.StretchOverride == true))
				{
					DrawDefault("StretchVector", ref updateMaterial);
				}
			EndIndent();
		}
		
		DrawDefault("AllowPulse", ref updateMaterial);
		
		Separator();

		if (updateMaterial        == true) DirtyEach(t => t.UpdateMaterial       ());
		if (updateMeshesAndModels == true) DirtyEach(t => t.UpdateMeshesAndModels());
	}
}
#endif

public abstract class SgtStarfield : MonoBehaviour
{
	// All enabled starfields in the scene
	public static List<SgtStarfield> AllStarfields = new List<SgtStarfield>();

	[Tooltip("The color of this starfield")]
	public Color Color = Color.white;

	[Tooltip("The brightness of this starfield")]
	public float Brightness = 1.0f;

	[Tooltip("The main texture of this starfield")]
	public Texture MainTex;

	[Tooltip("The layout of stars in the MainTex and HeightTex")]
	public SgtStarfieldLayoutType Layout = SgtStarfieldLayoutType.Grid;

	[Tooltip("The amount of columns in the MainTex")]
	public int LayoutColumns = 1;

	[Tooltip("The amount of rows in the MainTex")]
	public int LayoutRows = 1;
	
	[Tooltip("The rects of each star in the MainTex")]
	public List<Rect> Rects;

	[Tooltip("The render queue group for this starfield")]
	public SgtRenderQueue RenderQueue = SgtRenderQueue.Transparent;

	[Tooltip("The render queue offset for this starfield")]
	public int RenderQueueOffset;

	[Tooltip("The amount of seconds this starfield has been animating")]
	public float Age;

	[Tooltip("The animation speed of this starfield")]
	public float TimeScale = 1.0f;

	[Tooltip("Should the star particles fade out ")]
	[SgtRange(0.0f, 1000.0f)]
	public float Softness;
	
	[Tooltip("Should the stars stretch if an observer moves?")]
	public bool StretchToObservers;

	[Tooltip("Do you want to manually set the stretching?")]
	public bool StretchOverride;

	[Tooltip("The vector of the stretching")]
	public Vector3 StretchVector;

	[Tooltip("The scale of the stretching relative to the velocity")]
	public float StretchScale = 1.0f;

	[Tooltip("Should the stars fade out when the camera gets near?")]
	public bool FadeNear;

	[Tooltip("The distance at which the disappear in local coordinates")]
	public float FadeNearRadius = 1.0f;

	[Tooltip("The thickness of the fading effect in local coordinates")]
	public float FadeNearThickness = 2.0f;

	[Tooltip("Should the stars fade out when the camera gets too far away?")]
	public bool FadeFar;

	[Tooltip("The distance at which the disappear in local coordinates")]
	public float FadeFarRadius = 10.0f;

	[Tooltip("The thickness of the fading effect in local coordinates")]
	public float FadeFarThickness = 2.0f;

	[Tooltip("Should the stars automatically be placed on top of the currently rendering camera?")]
	[FormerlySerializedAs("FollowObservers")]
	public bool FollowCameras;

	[Tooltip("Should the stars pulse in size over time?")]
	public bool AllowPulse;

	// The models used to render the full belt
	[HideInInspector]
	public List<SgtStarfieldModel> Models;

	// The material applied to all models
	[System.NonSerialized]
	public Material Material;

	[SerializeField]
	[HideInInspector]
	protected bool startCalled;

	[System.NonSerialized]
	protected bool updateMaterialCalled;

	[System.NonSerialized]
	protected bool updateMeshesAndModelsCalled;
	
	protected static SgtStarfieldStar tempStar = new SgtStarfieldStar();

	protected static List<Vector4> tempCoords = new List<Vector4>();

	protected virtual string ShaderName
	{
		get
		{
			return SgtHelper.ShaderNamePrefix + "Starfield";
		}
	}

	public SgtCustomStarfield MakeEditableCopy(int layer = 0, Transform parent = null)
	{
		return MakeEditableCopy(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public SgtCustomStarfield MakeEditableCopy(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
#if UNITY_EDITOR
		SgtHelper.BeginUndo("Create Editable Starfield Copy");
#endif

		var gameObject      = SgtHelper.CreateGameObject("Editable Starfield Copy", layer, parent, localPosition, localRotation, localScale);
		var customStarfield = SgtHelper.AddComponent<SgtCustomStarfield>(gameObject, false);
		var stars           = new List<SgtStarfieldStar>();
		var starCount       = BeginStars();

		for (var i = 0; i < starCount; i++)
		{
			var star = SgtClassPool<SgtStarfieldStar>.Pop() ?? new SgtStarfieldStar();

			NextStar(ref star, i);

			stars.Add(star);
		}

		EndStars();

		// Copy common settings
		customStarfield.Color              = Color;
		customStarfield.Brightness         = Brightness;
		customStarfield.MainTex            = MainTex;
		customStarfield.Layout             = Layout;
		customStarfield.LayoutColumns      = LayoutColumns;
		customStarfield.LayoutRows         = LayoutRows;
		customStarfield.RenderQueue        = RenderQueue;
		customStarfield.RenderQueueOffset  = RenderQueueOffset;
		customStarfield.Age                = Age;
		customStarfield.TimeScale          = TimeScale;
		customStarfield.Softness           = Softness;
		customStarfield.StretchToObservers = StretchToObservers;
		customStarfield.StretchOverride    = StretchOverride;
		customStarfield.StretchVector      = StretchVector;
		customStarfield.StretchScale       = StretchScale;
		customStarfield.FadeNear           = FadeNear;
		customStarfield.FadeNearRadius     = FadeNearRadius;
		customStarfield.FadeNearThickness  = FadeNearThickness;
		customStarfield.FadeFar            = FadeFar;
		customStarfield.FadeFarRadius      = FadeFarRadius;
		customStarfield.FadeFarThickness   = FadeFarThickness;
		customStarfield.FollowCameras      = FollowCameras;
		customStarfield.AllowPulse         = AllowPulse;

		// Copy custom settings
		customStarfield.Stars = stars;

		// Update
		customStarfield.UpdateMaterial();
		customStarfield.UpdateMeshesAndModels();

		return customStarfield;
	}
	
	[ContextMenu("Update Material")]
	public void UpdateMaterial()
	{
		updateMaterialCalled = true;

		if (Material == null)
		{
			Material = SgtHelper.CreateTempMaterial(ShaderName);

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
		}
		
		BuildMaterial();
	}
	
	[ContextMenu("Update Meshes and Models")]
	public void UpdateMeshesAndModels()
	{
		updateMeshesAndModelsCalled = true;

		var starCount  = BeginStars();
		var modelCount = 0;
		
		// Build meshes and models until starCount reaches 0
		if (starCount > 0)
		{
			BuildRects();
			ConvertRectsToCoords();

			while (starCount > 0)
			{
				var quadCount = Mathf.Min(starCount, SgtHelper.QuadsPerMesh);
				var model     = GetOrNewModel(modelCount);
				var mesh      = GetOrNewMesh(model);
				
				model.SetMaterial(Material);

				BuildMesh(mesh, modelCount * SgtHelper.QuadsPerMesh, quadCount);
				
				modelCount += 1;
				starCount  -= quadCount;
			}
		}

		// Remove any excess
		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= modelCount; i--)
			{
				SgtStarfieldModel.Pool(Models[i]);

				Models.RemoveAt(i);
			}
		}
		
		EndStars();
	}

#if UNITY_EDITOR
	[ContextMenu("Make Editable Copy")]
	public void MakeEditableCopyContext()
	{
		var customStarfield = MakeEditableCopy(gameObject.layer, transform.parent, transform.localPosition, transform.localRotation, transform.localScale);

		SgtHelper.SelectAndPing(customStarfield);
	}
#endif

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
		CheckUpdateCalls();
	}
	
	protected virtual void Update()
	{
		Age += Time.deltaTime * TimeScale;

		if (Material != null)
		{
			if (AllowPulse == true)
			{
				Material.SetFloat("_Age", Age);
			}
		}
	}

	protected virtual void OnEnable()
	{
#if UNITY_EDITOR
		if (AllStarfields.Count == 0)
		{
			SgtHelper.RepaintAll();
		}
#endif
		AllStarfields.Add(this);
		
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

		Camera.onPreCull    += CameraPreCull;
		Camera.onPostRender += CameraPostRender;
	}

	protected virtual void OnDisable()
	{
		AllStarfields.Remove(this);
		
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

		Camera.onPreCull    -= CameraPreCull;
		Camera.onPostRender -= CameraPostRender;
	}

	protected virtual void OnDestroy()
	{
		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= 0; i--)
			{
				SgtStarfieldModel.MarkForDestruction(Models[i]);
			}
		}

		SgtHelper.Destroy(Material);
	}
	
	protected abstract int BeginStars();

	protected abstract void NextStar(ref SgtStarfieldStar star, int starIndex);

	protected abstract void EndStars();

	protected virtual void BuildMaterial()
	{
		Material.renderQueue = (int)RenderQueue + RenderQueueOffset;

		Material.SetTexture("_MainTex", MainTex);
		Material.SetColor("_Color", SgtHelper.Brighten(Color, Color.a * Brightness));
		Material.SetFloat("_Scale", transform.lossyScale.x);

		if (AllowPulse == true)
		{
			SgtHelper.EnableKeyword("LIGHT_1", Material);

			// This is also set in Update
			Material.SetFloat("_Age", Age);
		}
		else
		{
			SgtHelper.DisableKeyword("LIGHT_1", Material);
		}

		if (Softness > 0.0f)
		{
			SgtHelper.EnableKeyword("LIGHT_2", Material);

			Material.SetFloat("_InvFade", SgtHelper.Reciprocal(Softness));
		}
		else
		{
			SgtHelper.DisableKeyword("LIGHT_2", Material);
		}

		if (StretchToObservers == true)
		{
			SgtHelper.EnableKeyword("SGT_C", Material);
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_C", Material);
		}

		if (FadeNear == true)
		{
			SgtHelper.EnableKeyword("SGT_D", Material);

			Material.SetFloat("_FadeNearRadius", FadeNearRadius);
			Material.SetFloat("_FadeNearScale", SgtHelper.Reciprocal(FadeNearThickness));
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_D", Material);
		}

		if (FadeFar == true)
		{
			SgtHelper.EnableKeyword("SGT_E", Material);

			Material.SetFloat("_FadeFarRadius", FadeFarRadius);
			Material.SetFloat("_FadeFarScale", SgtHelper.Reciprocal(FadeFarThickness));
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_E", Material);
		}
	}

	protected virtual void BuildRects()
	{
		if (Layout == SgtStarfieldLayoutType.Grid)
		{
			if (Rects == null) Rects = new List<Rect>();

			Rects.Clear();

			if (LayoutColumns > 0 && LayoutRows > 0)
			{
				var invX = SgtHelper.Reciprocal(LayoutColumns);
				var invY = SgtHelper.Reciprocal(LayoutRows   );

				for (var y = 0; y < LayoutRows; y++)
				{
					var offY = y * invY;

					for (var x = 0; x < LayoutColumns; x++)
					{
						var offX = x * invX;
						var rect = new Rect(offX, offY, invX, invY);

						Rects.Add(rect);
					}
				}
			}
		}
	}
	
	protected virtual void BuildMesh(Mesh mesh, int starIndex, int starCount)
	{
		var positions = new Vector3[starCount * 4];
		var colors    = new Color[starCount * 4];
		var normals   = new Vector3[starCount * 4];
		var tangents  = new Vector4[starCount * 4];
		var coords1   = new Vector2[starCount * 4];
		var coords2   = new Vector2[starCount * 4];
		var indices   = new int[starCount * 6];
		var minMaxSet = false;
		var min       = default(Vector3);
		var max       = default(Vector3);
		
		for (var i = 0; i < starCount; i++)
		{
			NextStar(ref tempStar, starIndex + i);

			var offV     = i * 4;
			var offI     = i * 6;
			var position = tempStar.Position;
			var radius   = tempStar.Radius;
			var angle    = Mathf.Repeat(tempStar.Angle / 180.0f, 2.0f) - 1.0f;
			var uv       = tempCoords[SgtHelper.Mod(tempStar.Variant, tempCoords.Count)];
			
			ExpandBounds(ref minMaxSet, ref min, ref max, position, radius);
			
			positions[offV + 0] =
			positions[offV + 1] =
			positions[offV + 2] =
			positions[offV + 3] = position;

			colors[offV + 0] =
			colors[offV + 1] =
			colors[offV + 2] =
			colors[offV + 3] = tempStar.Color;
			
			normals[offV + 0] = new Vector3(-1.0f,  1.0f, angle);
			normals[offV + 1] = new Vector3( 1.0f,  1.0f, angle);
			normals[offV + 2] = new Vector3(-1.0f, -1.0f, angle);
			normals[offV + 3] = new Vector3( 1.0f, -1.0f, angle);

			tangents[offV + 0] =
			tangents[offV + 1] =
			tangents[offV + 2] =
			tangents[offV + 3] = new Vector4(tempStar.PulseOffset, tempStar.PulseSpeed, tempStar.PulseRange, 0.0f);
			
			coords1[offV + 0] = new Vector2(uv.x, uv.y);
			coords1[offV + 1] = new Vector2(uv.z, uv.y);
			coords1[offV + 2] = new Vector2(uv.x, uv.w);
			coords1[offV + 3] = new Vector2(uv.z, uv.w);
			
			coords2[offV + 0] = new Vector2(radius,  0.5f);
			coords2[offV + 1] = new Vector2(radius, -0.5f);
			coords2[offV + 2] = new Vector2(radius,  0.5f);
			coords2[offV + 3] = new Vector2(radius, -0.5f);

			indices[offI + 0] = offV + 0;
			indices[offI + 1] = offV + 1;
			indices[offI + 2] = offV + 2;
			indices[offI + 3] = offV + 3;
			indices[offI + 4] = offV + 2;
			indices[offI + 5] = offV + 1;
		}
		
		mesh.name      = "Belt";
		mesh.vertices  = positions;
		mesh.colors    = colors;
		mesh.normals   = normals;
		mesh.tangents  = tangents;
		mesh.uv        = coords1;
		mesh.uv2       = coords2;
		mesh.triangles = indices;
		mesh.bounds    = SgtHelper.NewBoundsFromMinMax(min, max);
	}

	protected virtual void CameraPreCull(Camera camera)
	{
		if (Material != null)
		{
			var observer = SgtObserver.Find(camera);

			if (observer != null)
			{
				Material.SetFloat("_CameraRollAngle", observer.RollAngle * Mathf.Deg2Rad);

				if (StretchToObservers == true)
				{
					var velocity = (StretchOverride == true ? StretchVector : observer.Velocity) * StretchScale;

					Material.SetVector("_StretchVector", velocity);
					Material.SetVector("_StretchDirection", velocity.normalized);
					Material.SetFloat("_StretchLength", velocity.magnitude);
				}
			}
		}

		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= 0; i--)
			{
				var model = Models[i];

				if (model != null)
				{
					if (FollowCameras == true)
					{
						model.SavePosition();

						model.transform.position = camera.transform.position;
					}
				}
			}
		}
	}

	protected void CameraPostRender(Camera camera)
	{
		if (Material != null)
		{
			Material.SetFloat("_CameraRollAngle", 0.0f);
		}

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

	protected static void ExpandBounds(ref bool minMaxSet, ref Vector3 min, ref Vector3 max, Vector3 position, float radius)
	{
		var radius3 = new Vector3(radius, radius, radius);

		if (minMaxSet == false)
		{
			minMaxSet = true;

			min = position - radius3;
			max = position + radius3;
		}

		min = Vector3.Min(min, position - radius3);
		max = Vector3.Max(max, position + radius3);
	}

	private void ConvertRectsToCoords()
	{
		tempCoords.Clear();

		if (Rects != null)
		{
			for (var i = 0; i < Rects.Count; i++)
			{
				var rect = Rects[i];

				tempCoords.Add(new Vector4(rect.xMin, rect.yMin, rect.xMax, rect.yMax));
			}
		}

		if (tempCoords.Count == 0) tempCoords.Add(default(Vector4));
	}

	private SgtStarfieldModel GetOrNewModel(int index)
	{
		var model = default(SgtStarfieldModel);

		if (Models == null)
		{
			Models = new List<SgtStarfieldModel>();
		}

		if (index < Models.Count)
		{
			model = Models[index];
		}
		else
		{
			Models.Add(model);
		}

		if (model == null)
		{
			model = Models[index] = SgtStarfieldModel.Create(this);
			
			model.SetMaterial(Material);
		}

		return model;
	}

	private Mesh GetOrNewMesh(SgtStarfieldModel model)
	{
		var mesh = model.Mesh;
		
		if (mesh == null)
		{
			mesh = SgtHelper.CreateTempMesh("Stars");

			model.SetMesh((Mesh)mesh);
		}
		else
		{
			mesh.Clear();
		}

		return mesh;
	}

	private void CheckUpdateCalls()
	{
		if (updateMaterialCalled == false)
		{
			UpdateMaterial();
		}

		if (updateMeshesAndModelsCalled == false)
		{
			UpdateMeshesAndModels();
		}
	}
}
