using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

public abstract class SgtBelt_Editor<T> : SgtEditor<T>
	where T : SgtBelt
{
	protected override void OnInspector()
	{
		var updateMaterial        = false;
		var updateMeshesAndModels = false;
		
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
		DrawDefault("Age"); // Updated automatically
		DrawDefault("TimeScale"); // Updated automatically

		Separator();
		
		BeginError(Any(t => t.MainTex == null));
			DrawDefault("MainTex", ref updateMaterial);
		EndError();
		BeginError(Any(t => t.HeightTex == null));
			DrawDefault("HeightTex", ref updateMaterial);
		EndError();
		DrawDefault("Layout", ref updateMeshesAndModels);
		
		BeginIndent();
			if (Any(t => t.Layout == SgtBeltLayoutType.Grid))
			{
				BeginError(Any(t => t.LayoutColumns <= 0));
					DrawDefault("LayoutColumns", ref updateMeshesAndModels);
				EndError();
				BeginError(Any(t => t.LayoutRows <= 0));
					DrawDefault("LayoutRows", ref updateMeshesAndModels);
				EndError();
			}

			if (Any(t => t.Layout == SgtBeltLayoutType.Custom))
			{
				DrawDefault("Rects", ref updateMeshesAndModels);
			}
		EndIndent();
		
		if (updateMaterial        == true) DirtyEach(t => t.UpdateMaterial       ());
		if (updateMeshesAndModels == true) DirtyEach(t => t.UpdateMeshesAndModels());
	}
}
#endif

public abstract class SgtBelt : MonoBehaviour
{
	// All active and enabled belts in the scene
	public static List<SgtBelt> AllBelts = new List<SgtBelt>();

	[Tooltip("The lights shining on this belt")]
	public List<Light> Lights;

	[Tooltip("The shadows casting on this belt")]
	public List<SgtShadow> Shadows;
	
	[Tooltip("The base color of this belt")]
	public Color Color = Color.white;
	
	[Tooltip("The base brightness of this belt")]
	public float Brightness = 1.0f;

	[Tooltip("The main texture of this belt")]
	public Texture MainTex;

	[Tooltip("The height texture of this belt")]
	public Texture HeightTex;

	[Tooltip("The layout of asteroids in the MainTex and HeightTex")]
	public SgtBeltLayoutType Layout = SgtBeltLayoutType.Grid;

	[Tooltip("The amount of columns in the MainTex and HeightTex")]
	public int LayoutColumns = 1;

	[Tooltip("The amount of rows in the MainTex and HeightTex")]
	public int LayoutRows = 1;
	
	[Tooltip("The rects of each asteroid in the MainTex and HeightTex")]
	public List<Rect> Rects;

	[Tooltip("The render queue group for this belt")]
	public SgtRenderQueue RenderQueue = SgtRenderQueue.Geometry;
	
	[Tooltip("The render queue offset for this belt")]
	public int RenderQueueOffset;

	[Tooltip("The amount of seconds this belt has been animating for")]
	public float Age;

	[Tooltip("The animation speed of this belt")]
	public float TimeScale = 1.0f;
	
	// The models used to render the full belt
	[HideInInspector]
	public List<SgtBeltModel> Models;

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
	
	protected static SgtBeltAsteroid tempAsteroid = new SgtBeltAsteroid();

	protected static List<Vector4> tempCoords = new List<Vector4>();

	protected string ShaderName
	{
		get
		{
			return SgtHelper.ShaderNamePrefix + "Belt";
		}
	}

	public SgtCustomBelt MakeEditableCopy(int layer = 0, Transform parent = null)
	{
		return MakeEditableCopy(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public SgtCustomBelt MakeEditableCopy(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
#if UNITY_EDITOR
		SgtHelper.BeginUndo("Create Editable Belt Copy");
#endif
		var gameObject    = SgtHelper.CreateGameObject("Editable Belt Copy", layer, parent, localPosition, localRotation, localScale);
		var customBelt    = SgtHelper.AddComponent<SgtCustomBelt>(gameObject, false);
		var asteroids     = new List<SgtBeltAsteroid>();
		var asteroidCount = BeginAsteroids();

		for (var i = 0; i < asteroidCount; i++)
		{
			var asteroid = SgtClassPool<SgtBeltAsteroid>.Pop() ?? new SgtBeltAsteroid();

			NextAsteroid(ref asteroid, i);

			asteroids.Add(asteroid);
		}

		EndAsteroids();
		
		// Copy common settings
		if (Lights != null)
		{
			customBelt.Lights = new List<Light>(Lights);
		}

		if (Shadows != null)
		{
			customBelt.Shadows = new List<SgtShadow>(Shadows);
		}
		
		customBelt.Color         = Color;
		customBelt.Brightness    = Brightness;
		customBelt.MainTex       = MainTex;
		customBelt.HeightTex     = HeightTex;
		customBelt.Layout        = Layout;
		customBelt.LayoutColumns = LayoutColumns;
		customBelt.LayoutRows    = LayoutRows;

		if (Rects != null)
		{
			customBelt.Rects = new List<Rect>(Rects);
		}

		customBelt.RenderQueue       = RenderQueue;
		customBelt.RenderQueueOffset = RenderQueueOffset;
		customBelt.Age               = Age;
		customBelt.TimeScale         = TimeScale;

		// Copy custom settings
		customBelt.Asteroids  = asteroids;

		// Update
		customBelt.UpdateMaterial();
		customBelt.UpdateMeshesAndModels();

		return customBelt;
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

		var asteroidCount = BeginAsteroids();
		var modelCount    = 0;

		// Build meshes and models until asteroidCount reaches 0
		if (asteroidCount > 0)
		{
			BuildRects();
			ConvertRectsToCoords();

			while (asteroidCount > 0)
			{
				var quadCount = Mathf.Min(asteroidCount, SgtHelper.QuadsPerMesh);
				var model     = GetOrNewModel(modelCount);
				var modelMesh = GetOrNewMesh(model);
				
				model.SetMaterial(Material);

				BuildMesh(modelMesh, modelCount * SgtHelper.QuadsPerMesh, quadCount);
				
				modelCount    += 1;
				asteroidCount -= quadCount;
			}
		}

		// Remove any excess
		if (Models != null)
		{
			for (var i = Models.Count - 1; i >= modelCount; i--)
			{
				SgtBeltModel.Pool(Models[i]);

				Models.RemoveAt(i);
			}
		}
		
		EndAsteroids();
	}

#if UNITY_EDITOR
	[ContextMenu("Make Editable Copy")]
	public void MakeEditableCopyContext()
	{
		var customBelt = MakeEditableCopy(gameObject.layer, transform.parent, transform.localPosition, transform.localRotation, transform.localScale);

		SgtHelper.SelectAndPing(customBelt);
	}
#endif

	protected virtual void OnEnable()
	{
#if UNITY_EDITOR
		if (AllBelts.Count == 0)
		{
			SgtHelper.RepaintAll();
		}
#endif
		AllBelts.Add(this);

		SgtObserver.OnObserverPreRender += ObserverPreRender;

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

			StartOnce();
		}
	}

	protected virtual void StartOnce()
	{
		CheckUpdateCalls();
	}

	protected virtual void LateUpdate()
	{
		Age += Time.deltaTime * TimeScale;

		if (Material != null)
		{
			Material.SetFloat("_Age", Age);
		}

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
		AllBelts.Remove(this);

		SgtObserver.OnObserverPreRender -= ObserverPreRender;

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
				SgtBeltModel.MarkForDestruction(Models[i]);
			}
		}

		SgtHelper.Destroy(Material);
	}

	protected abstract int BeginAsteroids();

	protected abstract void NextAsteroid(ref SgtBeltAsteroid asteroid, int asteroidIndex);

	protected abstract void EndAsteroids();
	
	protected virtual void BuildMaterial()
	{
		Material.renderQueue = (int)RenderQueue + RenderQueueOffset;

		Material.SetTexture("_MainTex", MainTex);
		Material.SetTexture("_HeightTex", HeightTex);
		Material.SetColor("_Color", SgtHelper.Brighten(Color, Brightness));
		Material.SetFloat("_Scale", transform.lossyScale.x);
		Material.SetFloat("_Age", Age);
	}

	protected virtual void BuildRects()
	{
		if (Layout == SgtBeltLayoutType.Grid)
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

	protected virtual void BuildMesh(Mesh mesh, int asteroidIndex, int asteroidCount)
	{
		var positions = new Vector3[asteroidCount * 4];
		var colors    = new Color[asteroidCount * 4];
		var normals   = new Vector3[asteroidCount * 4];
		var tangents  = new Vector4[asteroidCount * 4];
		var coords1   = new Vector2[asteroidCount * 4];
		var coords2   = new Vector2[asteroidCount * 4];
		var indices   = new int[asteroidCount * 6];
		var maxWidth  = 0.0f;
		var maxHeight = 0.0f;
		
		for (var i = 0; i < asteroidCount; i++)
		{
			NextAsteroid(ref tempAsteroid, asteroidIndex + i);

			var offV     = i * 4;
			var offI     = i * 6;
			var radius   = tempAsteroid.Radius;
			var distance = tempAsteroid.OrbitDistance;
			var height   = tempAsteroid.Height;
			var uv       = tempCoords[SgtHelper.Mod(tempAsteroid.Variant, tempCoords.Count)];
			
			maxWidth  = Mathf.Max(maxWidth , distance + radius);
			maxHeight = Mathf.Max(maxHeight, height   + radius);
			
			positions[offV + 0] =
			positions[offV + 1] =
			positions[offV + 2] =
			positions[offV + 3] = new Vector3(tempAsteroid.OrbitAngle, distance, tempAsteroid.OrbitSpeed);

			colors[offV + 0] =
			colors[offV + 1] =
			colors[offV + 2] =
			colors[offV + 3] = tempAsteroid.Color;
			
			normals[offV + 0] = new Vector3(-1.0f,  1.0f, 0.0f);
			normals[offV + 1] = new Vector3( 1.0f,  1.0f, 0.0f);
			normals[offV + 2] = new Vector3(-1.0f, -1.0f, 0.0f);
			normals[offV + 3] = new Vector3( 1.0f, -1.0f, 0.0f);

			tangents[offV + 0] =
			tangents[offV + 1] =
			tangents[offV + 2] =
			tangents[offV + 3] = new Vector4(tempAsteroid.Angle / Mathf.PI, tempAsteroid.Spin / Mathf.PI, 0.0f, 0.0f);
			
			coords1[offV + 0] = new Vector2(uv.x, uv.y);
			coords1[offV + 1] = new Vector2(uv.z, uv.y);
			coords1[offV + 2] = new Vector2(uv.x, uv.w);
			coords1[offV + 3] = new Vector2(uv.z, uv.w);
					
			coords2[offV + 0] =
			coords2[offV + 1] =
			coords2[offV + 2] =
			coords2[offV + 3] = new Vector2(radius, height);

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
		mesh.bounds    = new Bounds(Vector3.zero, new Vector3(maxWidth * 2.0f, maxHeight * 2.0f, maxWidth * 2.0f));
	}
	
	private void ObserverPreRender(SgtObserver observer)
	{
		if (Material != null)
		{
			Material.SetFloat("_CameraRollAngle", observer.RollAngle * Mathf.Deg2Rad);
		}
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

	private SgtBeltModel GetOrNewModel(int index)
	{
		var model = default(SgtBeltModel);

		if (Models == null)
		{
			Models = new List<SgtBeltModel>();
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
			model = Models[index] = SgtBeltModel.Create(this);
		}

		return model;
	}

	private Mesh GetOrNewMesh(SgtBeltModel model)
	{
		var mesh = model.Mesh;
		
		if (mesh == null)
		{
			mesh = SgtHelper.CreateTempMesh("Asteroids");

			model.SetMesh((Mesh)mesh);
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
