using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtNebulaStarfield))]
public class SgtNebulaStarfield_Editor : SgtStarfield_Editor<SgtNebulaStarfield>
{
	protected override void OnInspector()
	{
		base.OnInspector();

		var updateMesh = false;
		
		DrawDefault("Seed", ref updateMesh);
		BeginError(Any(t => t.SourceTex == null));
			DrawDefault("SourceTex", ref updateMesh);
		EndError();
		DrawDefault("Threshold", ref updateMesh);
		DrawDefault("Jitter", ref updateMesh);
		DrawDefault("Samples", ref updateMesh);
		DrawDefault("HeightSource", ref updateMesh);
		DrawDefault("ScaleSource", ref updateMesh);
		BeginError(Any(t => t.Size.x <= 0.0f || t.Size.y <= 0.0f || t.Size.z <= 0.0f));
			DrawDefault("Size", ref updateMesh);
		EndError();

		Separator();

		BeginError(Any(t => t.HorizontalBrightness < 0.0f));
			DrawDefault("HorizontalBrightness");
		EndError();
		BeginError(Any(t => t.HorizontalPower < 0.0f));
			DrawDefault("HorizontalPower");
		EndError();

		Separator();

		BeginError(Any(t => t.StarRadiusMin < 0.0f || t.StarRadiusMin > t.StarRadiusMax));
			DrawDefault("StarRadiusMin", ref updateMesh);
		EndError();
		BeginError(Any(t => t.StarRadiusMax < 0.0f || t.StarRadiusMin > t.StarRadiusMax));
			DrawDefault("StarRadiusMax", ref updateMesh);
		EndError();
		DrawDefault("StarPulseMax", ref updateMesh);
		DrawDefault("StarCount", ref updateMesh);

		RequireObserver();

		if (updateMesh == true) DirtyEach(t => t.UpdateMeshesAndModels());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Nebula Starfield")]
public class SgtNebulaStarfield : SgtStarfield
{
	[Tooltip("The random seed used when generating the stars")]
	[SgtSeed]
	public int Seed;

	[Tooltip("This texture used to color the nebula particles")]
	public Texture SourceTex;
	
	[Tooltip("This brightness of the sampled SourceTex pixel for a particle to be spawned")]
	[SgtRange(0.0f, 1.0f)]
	public float Threshold = 0.1f;

	[Tooltip("The amount of times a nebula point is randomly sampled, before the brightest sample is used")]
	[SgtRange(1, 5)]
	public int Samples = 2;
	
	[Tooltip("This allows you to randomly offset each nebula particle position")]
	[SgtRange(0.0f, 1.0f)]
	public float Jitter;

	[Tooltip("The calculation used to find the height offset of a particle in the nebula")]
	public SgtNebulaSource HeightSource = SgtNebulaSource.None;

	[Tooltip("The calculation used to find the scale modified of each particle in the nebula")]
	public SgtNebulaSource ScaleSource = SgtNebulaSource.None;

	[Tooltip("The size of the generated nebula")]
	public Vector3 Size = new Vector3(1.0f, 1.0f, 1.0f);

	[Tooltip("The brightness of the nebula when viewed from the side (good for galaxies)")]
	public float HorizontalBrightness = 0.25f;

	[Tooltip("The relationship between the Brightness and HorizontalBrightness relative to the viweing angle")]
	public float HorizontalPower = 1.0f;

	[Tooltip("The amount of stars that will be generated in the starfield")]
	public int StarCount = 1000;

	[Tooltip("The minimum radius of stars in the starfield")]
	public float StarRadiusMin = 0.0f;

	[Tooltip("The maximum radius of stars in the starfield")]
	public float StarRadiusMax = 0.05f;

	[Tooltip("The maximum amount a star's size can pulse over time. A value of 1 means the star can potentially pulse between its maximum size, and 0")]
	[SgtRange(0.0f, 1.0f)]
	public float StarPulseMax = 1.0f;

	// Temp vars used during generation
	private static Texture2D sourceTex2D;
	private static Vector3   halfSize;

	public static SgtNebulaStarfield CreateNebulaStarfield(int layer = 0, Transform parent = null)
	{
		return CreateNebulaStarfield(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtNebulaStarfield CreateNebulaStarfield(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Nebula Starfield", layer, parent, localPosition, localRotation, localScale);
		var starfield  = gameObject.AddComponent<SgtNebulaStarfield>();

		return starfield;
	}

	protected override int BeginStars()
	{
		SgtHelper.BeginRandomSeed(Seed);

		sourceTex2D = SourceTex as Texture2D;

		if (sourceTex2D != null && Samples > 0)
		{
#if UNITY_EDITOR
			SgtHelper.MakeTextureReadable(sourceTex2D);
			SgtHelper.MakeTextureTruecolor(sourceTex2D);
#endif
			halfSize = Size * 0.5f;

			return StarCount;
		}

		return 0;
	}

	protected override void NextStar(ref SgtStarfieldStar star, int starIndex)
	{
		for (var i = Samples - 1; i >= 0; i--)
		{
			var sampleX = Random.Range(0.0f, 1.0f);
			var sampleY = Random.Range(0.0f, 1.0f);
			var pixel   = sourceTex2D.GetPixelBilinear(sampleX, sampleY);
			var gray    = pixel.grayscale;

			if (gray > Threshold || i == 0)
			{
				var position = -halfSize + Random.insideUnitSphere * Jitter * StarRadiusMax;

				position.x += Size.x * sampleX;
				position.y += Size.y * GetWeight(HeightSource, pixel, 0.5f);
				position.z += Size.z * sampleY;

				star.Variant     = Random.Range(int.MinValue, int.MaxValue);
				star.Color       = pixel;
				star.Radius      = Random.Range(StarRadiusMin, StarRadiusMax) * GetWeight(ScaleSource, pixel, 1.0f);
				star.Angle       = Random.Range(-180.0f, 180.0f);
				star.Position    = position;
				star.PulseRange  = Random.value * StarPulseMax;
				star.PulseSpeed  = Random.value;
				star.PulseOffset = Random.value;

				return;
			}
		}
	}

	protected override void EndStars()
	{
		SgtHelper.EndRandomSeed();
	}

	protected override void CameraPreCull(Camera camera)
	{
		base.CameraPreCull(camera);

		// Change brightness based on viewing angle?
		if (Material != null)
		{
			var dir    = (transform.position - camera.transform.position).normalized;
			var theta  = Mathf.Abs(Vector3.Dot(transform.up, dir));
			var bright = Mathf.Lerp(HorizontalBrightness, Brightness, Mathf.Pow(theta, HorizontalPower));
			var color  = SgtHelper.Brighten(Color, Color.a * bright);
			
			Material.SetColor("_Color", color);
		}
	}

#if UNITY_EDITOR
	protected virtual void OnDrawGizmosSelected()
	{
		Gizmos.matrix = transform.localToWorldMatrix;

		Gizmos.DrawWireCube(Vector3.zero, Size);
	}
#endif

	private float GetWeight(SgtNebulaSource source, Color pixel, float defaultWeight)
	{
		switch (source)
		{
			case SgtNebulaSource.Red: return pixel.r;
			case SgtNebulaSource.Green: return pixel.g;
			case SgtNebulaSource.Blue: return pixel.b;
			case SgtNebulaSource.Alpha: return pixel.a;
			case SgtNebulaSource.AverageRgb: return (pixel.r + pixel.g + pixel.b) / 3.0f;
			case SgtNebulaSource.MinRgb: return Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b));
			case SgtNebulaSource.MaxRgb: return Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b));
		}

		return defaultWeight;
	}

#if UNITY_EDITOR
	[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Nebula Starfield", false, 10)]
	private static void CreateNebulaStarfieldMenuItem()
	{
		var parent    = SgtHelper.GetSelectedParent();
		var starfield = CreateNebulaStarfield(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(starfield);
	}
#endif
}
