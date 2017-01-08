using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(SgtDepthTextureMode))]
public class SgtDepthTextureMode_Editor : SgtEditor<SgtDepthTextureMode>
{
	protected override void OnInspector()
	{
		DrawDefault("DepthMode");
	}
}
#endif

// This component allows you to control a Camera component's depthTextureMode setting.
[ExecuteInEditMode]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Depth Texture Mode")]
public class SgtDepthTextureMode : MonoBehaviour
{
	public DepthTextureMode DepthMode = DepthTextureMode.None;

	private Camera thisCamera;

	protected virtual void Update()
	{
		if (thisCamera == null) thisCamera = GetComponent<Camera>();

		thisCamera.depthTextureMode = DepthMode;
	}
}
