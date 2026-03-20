using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Moves the 2D player toward the mouse cursor in both X and Y (new Input System).
/// The sprite always stays upright. Attach CameraFollow to the camera separately.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MouseFollowMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Units per second the player moves toward the cursor.")]
    public float moveSpeed = 5f;

    [Header("Stop Radius")]
    [Tooltip("Player stops moving when within this distance of the cursor.")]
    public float stopRadius = 0.5f;

    // ── private ──────────────────────────────────────────────────────────────
    private Rigidbody2D _rb;
    private Camera _cam;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _cam = Camera.main;

        // Only freeze rotation — both axes are free
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        // Disable gravity — movement is fully cursor-driven
        _rb.gravityScale = 0f;
    }

    void FixedUpdate()
    {
        Vector2 mouseWorld = GetMouseWorldPosition();
        Vector2 toMouse = mouseWorld - (Vector2)transform.position;
        float distance = toMouse.magnitude;

        if (distance > stopRadius)
        {
            _rb.linearVelocity = toMouse.normalized * moveSpeed;
        }
        else
        {
            _rb.linearVelocity = Vector2.zero;
        }

        // Keep sprite upright regardless of any external forces
        transform.rotation = Quaternion.identity;
    }

    // ─────────────────────────────────────────────────────────────────────────
    private Vector2 GetMouseWorldPosition()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        return _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -_cam.transform.position.z));
    }

    // ─────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, stopRadius);
    }
#endif
}