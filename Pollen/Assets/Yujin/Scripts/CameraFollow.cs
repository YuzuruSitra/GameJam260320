using UnityEngine;

/// <summary>
/// Smoothly follows a target (the player) in X and Y.
/// Attach this script to your Main Camera.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The transform the camera should follow (drag your player here).")]
    public Transform target;

    [Header("Follow Settings")]
    [Tooltip("Higher = snappier. Lower = smoother lag.")]
    public float smoothSpeed = 8f;

    [Tooltip("Offset from the player in world space. Z should stay negative (e.g. -10).")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    // ─────────────────────────────────────────────────────────────────────────
    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(target.position.x, target.position.y, 0f);
        Vector3 smoothed = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);

        // Z is never touched — keep the camera at its fixed depth
        transform.position = new Vector3(smoothed.x, smoothed.y, offset.z);
    }
}