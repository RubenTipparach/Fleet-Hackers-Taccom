using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtAccretion))]
public class SgtAccretion_Editor : SgtEditor<SgtAccretion>
{
	protected override void OnInspector()
	{
		var updateMaterial = false;
		var updateSegments = false;
		var updateMesh     = false;

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
		DrawDefault("Age", ref updateMaterial);
		DrawDefault("TimeScale", ref updateMaterial);

		Separator();

		BeginError(Any(t => t.MainTex == null));
			DrawDefault("MainTex", ref updateMaterial);
		EndError();
		BeginError(Any(t => t.DustTex == null));
			DrawDefault("DustTex", ref updateMaterial);
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

		Separator();

		DrawDefault("Twist", ref updateMaterial);
		DrawDefault("TwistBias", ref updateMaterial);
		DrawDefault("ReverseTwist", ref updateMaterial);

		if (updateMaterial == true) DirtyEach(t => t.UpdateMaterial());
		if (updateMesh     == true) DirtyEach(t => t.UpdateMesh    ());
		if (updateSegments == true) DirtyEach(t => t.UpdateSegments());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Accretion")]
public class SgtAccretion : SgtRing
{
	// All active and enabled accretions in the scene
	public static List<SgtAccretion> AllAccretions = new List<SgtAccretion>();

	[Tooltip("The dust texture applied to the accretion and gets animated as it spirals into the center")]
	public Texture DustTex;

	[Tooltip("The amount of seconds this accretion has been animating for")]
	public float Age;

	[Tooltip("The time scale of the animation")]
	public float TimeScale = 0.125f;

	[Tooltip("How sharp the twisting effect is")]
	[Range(0.0f, 10.0f)]
	public float Twist = 2.0f;

	[Tooltip("How quickly the twisting effect fades out near the outer edge of the ring")]
	[Range(1.0f, 10.0f)]
	public float TwistBias = 2.0f;

	[Tooltip("Invert the twist direction?")]
	public bool ReverseTwist;

	protected override string ShaderName
	{
		get
		{
			return SgtHelper.ShaderNamePrefix + "Accretion";
		}
	}

	public static SgtAccretion CreateAccretion(int layer, Transform parent = null)
	{
		return CreateAccretion(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtAccretion CreateAccretion(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Accretion", layer, parent, localPosition, localRotation, localScale);
		var accretion  = gameObject.AddComponent<SgtAccretion>();

		return accretion;
	}

	protected override void OnEnable()
	{
		base.OnEnable();

		AllAccretions.Add(this);
	}

	protected override void OnDisable()
	{
		base.OnDisable();

		AllAccretions.Remove(this);
	}

	protected virtual void Update()
	{
		Age += Time.deltaTime * TimeScale;

		if (Material != null)
		{
			Material.SetFloat("_Age", Age);
		}
	}

	public override void UpdateMaterial()
	{
		base.UpdateMaterial();

		Material.SetTexture("_DustTex", DustTex);
		Material.SetFloat("_Twist", ReverseTwist == true ? -Twist : Twist);
		Material.SetFloat("_TwistBias", TwistBias);
		Material.SetFloat("_Age", Age);
	}

	public override void UpdateMesh()
	{
		updateMeshCalled = true;

		GetOrClearMesh();
		
		if (SegmentDetail >= 2)
		{
			var detail     = Mathf.Min(SegmentDetail, SgtHelper.QuadsPerMesh / 2); // Limit the amount of vertices that get made
			var positions  = new Vector3[detail * 2 + 2];
			var normals    = new Vector3[detail * 2 + 2];
			var coords1    = new Vector2[detail * 2 + 2];
			var coords2    = new Vector2[detail * 2 + 2];
			var angleTotal = SgtHelper.Divide(Mathf.PI * 2.0f, SegmentCount);
			var angleStep  = SgtHelper.Divide(angleTotal, detail);
			var coordStep  = SgtHelper.Reciprocal(detail);
			
			for (var i = 0; i <= detail; i++)
			{
				var coord = coordStep * i;
				var angle = angleStep * i;
				var sin   = Mathf.Sin(angle);
				var cos   = Mathf.Cos(angle);
				var offV  = i * 2;
				
				positions[offV + 0] = new Vector3(sin * InnerRadius, 0.0f, cos * InnerRadius);
				positions[offV + 1] = new Vector3(sin * OuterRadius, 0.0f, cos * OuterRadius);

				normals[offV + 0] = Vector3.up;
				normals[offV + 1] = Vector3.up;

				coords1[offV + 0] = new Vector2(0.0f, coord * InnerRadius);
				coords1[offV + 1] = new Vector2(1.0f, coord * OuterRadius);

				coords2[offV + 0] = new Vector2(InnerRadius, 0.0f);
				coords2[offV + 1] = new Vector2(OuterRadius, 0.0f);
			}
			
			Mesh.vertices  = positions;
			Mesh.normals   = normals;
			Mesh.uv        = coords1;
			Mesh.uv2       = coords2;
			
			WriteIndicesAndBounds(detail);
		}
	}

#if UNITY_EDITOR
	[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Accretion", false, 10)]
	public static void CreateAccretionMenuItem()
	{
		var parent    = SgtHelper.GetSelectedParent();
		var accretion = CreateAccretion(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(accretion);
	}
#endif
}
