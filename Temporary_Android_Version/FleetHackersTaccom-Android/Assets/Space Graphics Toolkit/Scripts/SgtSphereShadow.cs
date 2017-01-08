using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtSphereShadow))]
public class SgtSphereShadow_Editor : SgtEditor<SgtSphereShadow>
{
	protected override void OnInspector()
	{
		var updatePenumbraLut = false;

		BeginError(Any(t => t.Light == null));
			DrawDefault("Light");
		EndError();
		BeginError(Any(t => t.InnerRadius < 0.0f || t.InnerRadius >= t.OuterRadius));
			DrawDefault("InnerRadius");
		EndError();
		BeginError(Any(t => t.OuterRadius < 0.0f || t.InnerRadius >= t.OuterRadius));
			DrawDefault("OuterRadius");
		EndError();
		DrawDefault("PenumbraBrightness", ref updatePenumbraLut);
		DrawDefault("PenumbraColor", ref updatePenumbraLut);

		if (updatePenumbraLut == true) DirtyEach(t => t.UpdatePenumbraLut());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Sphere Shadow")]
public class SgtSphereShadow : SgtShadow
{
	[Tooltip("The inner radius of the sphere in local coordinates")]
	public float InnerRadius = 1.0f;
	
	[Tooltip("The outer radius of the sphere in local coordinates")]
	public float OuterRadius = 2.0f;
	
	[Tooltip("The color of the semi shadowed area")]
	public Gradient PenumbraColor;
	
	[Tooltip("The brightness of the semi shadowed area")]
	public Gradient PenumbraBrightness;
	
	// The penumbra look up table applied to shadowed objects
	[System.NonSerialized]
	public Texture2D PenumbraLut;
	
	[SerializeField]
	[HideInInspector]
	private bool startCalled;
	
	[System.NonSerialized]
	private bool updatePenumbraLutCalled;

	private static GradientColorKey[] defaultLightingBrightness = new GradientColorKey[] { new GradientColorKey(Color.black, 0.0f), new GradientColorKey(Color.white, 1.0f) };
	
	private static GradientColorKey[] defaultLightingColor = new GradientColorKey[] { new GradientColorKey(Color.red, 0.0f), new GradientColorKey(Color.white, 1.0f) };
	
	private static Vector3[] vectors = new Vector3[3];
	
	private static float[] magnitudes = new float[3];
	
	public override Texture GetTexture()
	{
		if (updatePenumbraLutCalled == false)
		{
			UpdatePenumbraLut();
		}
		
		return PenumbraLut;
	}

	[ContextMenu("Update Penumbra LUT")]
	public void UpdatePenumbraLut()
	{
		updatePenumbraLutCalled = true;

		if (PenumbraLut == null || PenumbraLut.width != 1 || PenumbraLut.height != 64)
		{
			SgtHelper.Destroy(PenumbraLut);
			
			PenumbraLut = SgtHelper.CreateTempTexture2D("Penumbra LUT", 1, 64);
		}
		
		for (var y = 0; y < PenumbraLut.height; y++)
		{
			var t = y / (float)PenumbraLut.height;
			var a = PenumbraBrightness.Evaluate(t);
			var b = PenumbraColor.Evaluate(t);
			var c = a * b;
			
			c.a = c.grayscale;
			
			PenumbraLut.SetPixel(0, y, c);
		}
		
		// Make sure the last pixel is white
		PenumbraLut.SetPixel(0, PenumbraLut.height - 1, Color.white);
		
		PenumbraLut.wrapMode = TextureWrapMode.Clamp;

		PenumbraLut.Apply();
	}
	
	// TODO: Make this work correctly
	public override bool CalculateShadow()
	{
		if (base.CalculateShadow() == true)
		{
			var direction = default(Vector3);
			var position  = default(Vector3);
			var color     = default(Color);
			
			SgtHelper.CalculateLight(Light, transform.position, null, null, ref position, ref direction, ref color);
			
			var rotation = Quaternion.FromToRotation(direction, Vector3.back);
			
			SetVector(0, rotation * transform.right   * transform.lossyScale.x * OuterRadius);
			SetVector(1, rotation * transform.up      * transform.lossyScale.y * OuterRadius);
			SetVector(2, rotation * transform.forward * transform.lossyScale.z * OuterRadius);
			
			SortVectors();
			
			var spin  = Quaternion.LookRotation(Vector3.forward, new Vector2(-vectors[1].x, vectors[1].y)); // Orient the shadow ellipse
			var scale = SgtHelper.Reciprocal3(new Vector3(magnitudes[0], magnitudes[1], 1.0f));
			
			var shadowT = SgtHelper.Translation(-transform.position);
			var shadowR = SgtHelper.Rotation(spin * rotation);
			var shadowS = SgtHelper.Scaling(scale);
			
			Matrix = shadowS * shadowR * shadowT;
			Ratio  = SgtHelper.Divide(OuterRadius, OuterRadius - InnerRadius);
			
			return true;
		}
		
		return false;
	}
	
	protected virtual void OnEnable()
	{
		if (startCalled == true)
		{
			CheckUpdateCalls();
		}
	}

	protected override void Start()
	{
		if (startCalled == false)
		{
			startCalled = true;
			
			if (PenumbraBrightness == null)
			{
				PenumbraBrightness = new Gradient();
				PenumbraBrightness.colorKeys = defaultLightingBrightness;
			}

			if (PenumbraColor == null)
			{
				PenumbraColor = new Gradient();
				PenumbraColor.colorKeys = defaultLightingColor;
			}
			
			CheckUpdateCalls();
		}
	}
	
	protected virtual void OnDestroy()
	{
		SgtHelper.Destroy(PenumbraLut);
	}
	
#if UNITY_EDITOR
	protected virtual void OnDrawGizmosSelected()
	{
		if (SgtHelper.Enabled(this) == true)
		{
			Gizmos.matrix = transform.localToWorldMatrix;
			
			Gizmos.DrawWireSphere(Vector3.zero, InnerRadius);
			Gizmos.DrawWireSphere(Vector3.zero, OuterRadius);
			
			if (CalculateShadow() == true)
			{
				Gizmos.matrix = Matrix.inverse;
				
				Gizmos.DrawWireCube(new Vector3(0,0,5), new Vector3(2,2,10));
			}
		}
	}
#endif
	
	private void SetVector(int index, Vector3 vector)
	{
		vectors[index] = vector;
		
		magnitudes[index] = new Vector2(vector.x, vector.y).magnitude;
	}
	
	// Put the highest magnitude vectors in indices 0 & 1
	private void SortVectors()
	{
		// Lowest is 0 or 2
		if (magnitudes[0] < magnitudes[1])
		{
			// Lowest is 0
			if (magnitudes[0] < magnitudes[2])
			{
				vectors[0] = vectors[2]; magnitudes[0] = magnitudes[2];
			}
		}
		// Lowest is 1 or 2
		else
		{
			// Lowest is 1
			if (magnitudes[1] < magnitudes[2])
			{
				vectors[1] = vectors[2]; magnitudes[1] = magnitudes[2];
			}
		}
	}

	private void CheckUpdateCalls()
	{
		if (updatePenumbraLutCalled == false)
		{
			UpdatePenumbraLut();
		}
	}
}