using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtRing))]
public class SgtRing_Editor : SgtEditor<SgtRing>
{
	protected override void OnInspector()
	{
		var updateMaterial = false;
		var updateMesh     = false;
		var updateSegments = false;

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
		DrawDefault("LightingBias", ref updateMaterial);
		DrawDefault("LightingSharpness", ref updateMaterial);
		DrawDefault("Scattering", ref updateMaterial);

		if (Any(t => t.Scattering == true))
		{
			BeginIndent();
				DrawDefault("MieSharpness", ref updateMaterial);
				DrawDefault("MieStrength", ref updateMaterial);
			EndIndent();
		}

		Separator();
		
		BeginError(Any(t => t.InnerRadius == t.OuterRadius));
			DrawDefault("InnerRadius", ref updateMesh);
			DrawDefault("OuterRadius", ref updateMesh);
		EndError();
		BeginError(Any(t => t.SegmentCount < 1));
			DrawDefault("SegmentCount", ref updateMesh, ref updateSegments);
		EndError();
		BeginError(Any(t => t.SegmentDetail < 3));
			DrawDefault("SegmentDetail", ref updateMesh);
		EndError();
		BeginError(Any(t => t.BoundsShift < 0.0f));
			DrawDefault("BoundsShift", ref updateMesh);
		EndError();
		
		if (updateMaterial == true) DirtyEach(t => t.UpdateMaterial());
		if (updateMesh     == true) DirtyEach(t => t.UpdateMesh    ());
		if (updateSegments == true) DirtyEach(t => t.UpdateSegments());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Ring")]
public class SgtRing : MonoBehaviour
{
	// All currently active and enabled rings
	public static List<SgtRing> AllRings = new List<SgtRing>();

	[Tooltip("The lights shining on this ring")]
	public List<Light> Lights;

	[Tooltip("The shadows casting on this ring")]
	public List<SgtShadow> Shadows;

	[Tooltip("The texture applied to the ring (left side = inside, right side = outside)")]
	public Texture MainTex;

	[Tooltip("The color tint of this ring")]
	public Color Color = Color.white;

	[Tooltip("The color brightness of this ring")]
	public float Brightness = 1.0f;

	[Tooltip("The render queue group for this ring")]
	public SgtRenderQueue RenderQueue = SgtRenderQueue.Transparent;

	[Tooltip("The render queue offset for this ring")]
	public int RenderQueueOffset;

	[Tooltip("The radius of the inner edge of the ring in local coordinates")]
	public float InnerRadius = 1.0f;

	[Tooltip("The radius of the outer edge of the ring in local coordinates")]
	public float OuterRadius = 2.0f;

	[Tooltip("The amount of segments this ring is split into")]
	public int SegmentCount = 8;

	[Tooltip("The amount of quads used to build each segment")]
	public int SegmentDetail = 10;

	[Tooltip("How much light the rings reflect when not viewed head-on relative to the lighting direction")]
	[SgtRange(-1.0f, 1.0f)]
	public float LightingBias = 0.5f;

	[Tooltip("The sharpness of this lighting")]
	[SgtRange(0.0f, 1.0f)]
	public float LightingSharpness = 0.5f;

	[Tooltip("Should light scatter through the rings?")]
	public bool Scattering;

	[Tooltip("The mie phase sharpness of the scattering")]
	[SgtRange(0.0f, 5.0f)]
	public float MieSharpness = 2.0f;

	[Tooltip("The mie phase strength of the scattering")]
	[SgtRange(0.0f, 10.0f)]
	public float MieStrength = 1.0f;

	[Tooltip("The amount the ring mesh bounds should get pshed out by in local coordinates. This should be used with an 8+ SegmentCount")]
	public float BoundsShift;

	// The models used to render the full belt
	[HideInInspector]
	[UnityEngine.Serialization.FormerlySerializedAs("segments")]
	public List<SgtRingSegment> Segments;

	// The material applied to all segments
	[System.NonSerialized]
	public Material Material;

	// The mesh applied to all segments
	[System.NonSerialized]
	public Mesh Mesh;

	[SerializeField]
	[HideInInspector]
	private bool startCalled;

	[System.NonSerialized]
	private bool updateMaterialCalled;

	[System.NonSerialized]
	protected bool updateMeshCalled;

	[System.NonSerialized]
	private bool updateSegmentsCalled;

	public float Width
	{
		get
		{
			return OuterRadius - InnerRadius;
		}
	}

	protected virtual string ShaderName
	{
		get
		{
			return SgtHelper.ShaderNamePrefix + "Ring";
		}
	}
	
	[ContextMenu("Update Material")]
	public virtual void UpdateMaterial()
	{
		updateMaterialCalled = true;

		if (Material == null)
		{
			Material = SgtHelper.CreateTempMaterial(ShaderName);

			if (Segments != null)
			{
				for (var i = Segments.Count - 1; i >= 0; i--)
				{
					var segment = Segments[i];

					if (segment != null)
					{
						segment.SetMaterial(Material);
					}
				}
			}
		}

		var color       = SgtHelper.Brighten(Color, Brightness);
		var renderQueue = (int)RenderQueue + RenderQueueOffset;
		
		if (Material.renderQueue != renderQueue)
		{
			Material.renderQueue = renderQueue;
		}

		Material.SetTexture("_MainTex", MainTex);
		Material.SetColor("_Color", color);
		Material.SetFloat("_LightingBias", LightingBias);
		Material.SetFloat("_LightingSharpness", LightingSharpness);

		SgtHelper.SetTempMaterial(Material);

		if (Scattering == true)
		{
			SgtHelper.EnableKeyword("SGT_A");

			SgtHelper.WriteMie(MieSharpness, MieStrength);
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_A");
		}
		
		UpdateMaterialNonSerialized();
	}

	[ContextMenu("Update Mesh")]
	public virtual void UpdateMesh()
	{
		updateMeshCalled = true;

		GetOrClearMesh();
		
		if (SegmentDetail >= 2)
		{
			var detail     = Mathf.Min(SegmentDetail, SgtHelper.QuadsPerMesh / 2); // Limit the amount of vertices that get made
			var positions  = new Vector3[detail * 2 + 2];
			var normals    = new Vector3[detail * 2 + 2];
			var coords     = new Vector2[detail * 2 + 2];
			var angleTotal = SgtHelper.Divide(Mathf.PI * 2.0f, SegmentCount);
			var angleStep  = SgtHelper.Divide(angleTotal, detail);
			var coordStep  = SgtHelper.Reciprocal(detail);
			
			for (var i = 0; i <= detail; i++)
			{
				var coord  = coordStep * i;
				var angle  = angleStep * i;
				var sin    = Mathf.Sin(angle);
				var cos    = Mathf.Cos(angle);
				var offV   = i * 2;
				
				positions[offV + 0] = new Vector3(sin * InnerRadius, 0.0f, cos * InnerRadius);
				positions[offV + 1] = new Vector3(sin * OuterRadius, 0.0f, cos * OuterRadius);

				normals[offV + 0] = Vector3.up;
				normals[offV + 1] = Vector3.up;

				coords[offV + 0] = new Vector2(0.0f, coord);
				coords[offV + 1] = new Vector2(1.0f, coord);
			}
			
			Mesh.vertices  = positions;
			Mesh.normals   = normals;
			Mesh.uv        = coords;
			
			WriteIndicesAndBounds(detail);
		}
	}
	
	[ContextMenu("Update Segments")]
	public void UpdateSegments()
	{
		updateSegmentsCalled = true;
		
		var angleStep = SgtHelper.Divide(360.0f, SegmentCount);
		
		for (var i = 0; i < SegmentCount; i++)
		{
			var segment  = GetOrAddSegment(i);
			var angle    = angleStep * i;
			var rotation = Quaternion.Euler(0.0f, angle, 0.0f);

			segment.SetMesh(Mesh);
			segment.SetMaterial(Material);
			segment.SetRotation(rotation);
		}

		// Remove any excess
		if (Segments != null)
		{
			var min = Mathf.Max(0, SegmentCount);

			for (var i = Segments.Count - 1; i >= min; i--)
			{
				SgtRingSegment.Pool(Segments[i]);

				Segments.RemoveAt(i);
			}
		}
	}

	public static SgtRing CreateRing(int layer = 0, Transform parent = null)
	{
		return CreateRing(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtRing CreateRing(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Ring", layer, parent, localPosition, localRotation, localScale);
		var ring       = gameObject.AddComponent<SgtRing>();

		return ring;
	}

#if UNITY_EDITOR
	[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Ring", false, 10)]
	public static void CreateRingMenuItem()
	{
		var parent = SgtHelper.GetSelectedParent();
		var ring   = CreateRing(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(ring);
	}
#endif

	protected void GetOrClearMesh()
	{
		if (Mesh == null)
		{
			Mesh = SgtHelper.CreateTempMesh("Ring");

			if (Segments != null)
			{
				for (var i = Segments.Count - 1; i >= 0; i--)
				{
					var segment = Segments[i];

					if (segment != null)
					{
						segment.SetMesh(Mesh);
					}
				}
			}
		}
		else
		{
			Mesh.Clear();
		}
	}

	protected void WriteIndicesAndBounds(int detail)
	{
		var indices = new int[detail * 6];

		for (var i = 0; i < detail; i++)
		{
			var offV = i * 2;
			var offI = i * 6;

			indices[offI + 0] = offV + 0;
			indices[offI + 1] = offV + 1;
			indices[offI + 2] = offV + 2;
			indices[offI + 3] = offV + 3;
			indices[offI + 4] = offV + 2;
			indices[offI + 5] = offV + 1;
		}

		Mesh.triangles = indices;

		Mesh.RecalculateBounds();

		var bounds = Mesh.bounds;

		Mesh.bounds = SgtHelper.NewBoundsCenter(bounds, bounds.center + bounds.center.normalized * BoundsShift);
	}

	protected virtual void OnEnable()
	{
		AllRings.Add(this);

		Camera.onPreRender += CameraPreRender;

		if (Segments != null)
		{
			for (var i = Segments.Count - 1; i >= 0; i--)
			{
				var segment = Segments[i];

				if (segment != null)
				{
					segment.gameObject.SetActive(true);
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
		AllRings.Remove(this);

		Camera.onPreRender -= CameraPreRender;

		if (Segments != null)
		{
			for (var i = Segments.Count - 1; i >= 0; i--)
			{
				var segment = Segments[i];

				if (segment != null)
				{
					segment.gameObject.SetActive(false);
				}
			}
		}
	}

	protected virtual void OnDestroy()
	{
		if (Segments != null)
		{
			for (var i = Segments.Count - 1; i >= 0; i--)
			{
				SgtRingSegment.MarkForDestruction(Segments[i]);
			}
		}
		
		SgtObjectPool<Mesh>.Add(Mesh, m => m.Clear());
		
		SgtHelper.Destroy(Material);
	}

#if UNITY_EDITOR
	protected virtual void OnDrawGizmosSelected()
	{
		Gizmos.matrix = transform.localToWorldMatrix;

		SgtHelper.DrawCircle(Vector3.zero, Vector3.up, InnerRadius);
		SgtHelper.DrawCircle(Vector3.zero, Vector3.up, OuterRadius);
	}
#endif
	
	private void UpdateMaterialNonSerialized()
	{
		SgtHelper.SetTempMaterial(Material);

		SgtHelper.WriteShadowsNonSerialized(Shadows, 2);
	}

	private void CameraPreRender(Camera camera)
	{
		if (Material != null)
		{
			UpdateMaterialNonSerialized();
		}
	}

	private SgtRingSegment GetOrAddSegment(int index)
	{
		var segment = default(SgtRingSegment);

		if (Segments == null)
		{
			Segments = new List<SgtRingSegment>();
		}

		if (index < Segments.Count)
		{
			segment = Segments[index];

			if (segment == null)
			{
				segment = SgtRingSegment.Create(this);

				Segments[index] = segment;
			}
		}
		else
		{
			segment = SgtRingSegment.Create(this);

			Segments.Add(segment);
		}

		return segment;
	}

	private void CheckUpdateCalls()
	{
		if (updateMaterialCalled == false)
		{
			UpdateMaterial();
		}

		if (updateMeshCalled == false)
		{
			UpdateMesh();
		}

		if (updateSegmentsCalled == false)
		{
			UpdateSegments();
		}
	}
}
