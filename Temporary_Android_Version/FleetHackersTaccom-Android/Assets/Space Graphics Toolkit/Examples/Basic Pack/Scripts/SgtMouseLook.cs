using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtMouseLook))]
public class SgtMouseLook_Editor : SgtEditor<SgtMouseLook>
{
	protected override void OnInspector()
	{
		DrawDefault("Require");
		DrawDefault("Sensitivity");
		DrawDefault("TargetPitch");
		DrawDefault("TargetYaw");
		BeginError(Any(t => t.Dampening < 0.0f));
			DrawDefault("Dampening");
		EndError();
	}
}
#endif

// This component handles mouselook when attached to the camera
[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Mouse Look")]
public class SgtMouseLook : MonoBehaviour
{
	[Tooltip("The key that needs to be held down to look")]
	public KeyCode Require = KeyCode.Mouse0;

	[Tooltip("How quickly this rotates relative to the mouse movement")]
	public float Sensitivity = 2.0f;

	[Tooltip("The target X rotation")]
	public float TargetPitch;

	[Tooltip("The target Y rotation")]
	public float TargetYaw;

	[Tooltip("The speed at which this approaches the target rotation")]
	public float Dampening = 10.0f;

	private float currentPitch;

	private float currentYaw;

	protected virtual void Awake()
	{
		currentPitch = TargetPitch;
		currentYaw   = TargetYaw;
	}

	protected virtual void Update()
	{
		TargetPitch = Mathf.Clamp(TargetPitch, -89.9f, 89.9f);

		if (Require == KeyCode.None || Input.GetKey(Require) == true)
		{
			TargetPitch -= Input.GetAxisRaw("Mouse Y") * Sensitivity;

			TargetYaw += Input.GetAxisRaw("Mouse X") * Sensitivity;
		}

		currentPitch = SgtHelper.Dampen(currentPitch, TargetPitch, Dampening, Time.deltaTime, 0.1f);
		currentYaw   = SgtHelper.Dampen(currentYaw  , TargetYaw  , Dampening, Time.deltaTime, 0.1f);

		var rotation = Quaternion.Euler(currentPitch, currentYaw, 0.0f);

		SgtHelper.SetLocalRotation(transform, rotation);
	}
}
