using UnityEngine;
using UnityEngine.InputSystem;
using Pollen;

[RequireComponent(typeof(Rigidbody2D))]
public class MouseFollowMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Stop Radius")]
    public float stopRadius = 0.5f;

    [Header("Pollen Speed Scaling")]
    [Tooltip("Player speed when pollen is empty.")]
    public float minSpeed = 2f;
    [Tooltip("Player speed when pollen is full.")]
    public float maxSpeed = 10f;

    // ── private ──────────────────────────────────────────────────────────────
    private Rigidbody2D _rb;
    private Camera _cam;
    private PollenGauge _gauge;
    private SpriteRenderer _sr;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _cam = Camera.main;
        _gauge = GetComponent<PollenGauge>();
        _sr = GetComponent<SpriteRenderer>();

        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _rb.gravityScale = 0f;
    }

    void OnEnable()
    {
        if (_gauge != null)
            _gauge.OnChanged += OnPollenChanged;
    }

    void OnDisable()
    {
        if (_gauge != null)
            _gauge.OnChanged -= OnPollenChanged;
    }

    // ─────────────────────────────────────────────────────────────────────────
    void FixedUpdate()
    {
        Vector2 mouseWorld = GetMouseWorldPosition();
        Vector2 toMouse = mouseWorld - (Vector2)transform.position;
        float distance = toMouse.magnitude;

        if (distance > stopRadius)
        {
            Vector2 dir = toMouse.normalized;
            _rb.linearVelocity = dir * moveSpeed;

            // Flip sprite based on horizontal direction
            if (_sr != null && Mathf.Abs(dir.x) > 0.01f)
                _sr.flipX = dir.x < 0f;
        }
        else
        {
            _rb.linearVelocity = Vector2.zero;
        }

        transform.rotation = Quaternion.identity;
    }

    // ─────────────────────────────────────────────────────────────────────────
    void OnPollenChanged(float current, float max)
    {
        float t = max > 0f ? current / max : 0f;
        moveSpeed = Mathf.Lerp(minSpeed, maxSpeed, t);
    }

    // ─────────────────────────────────────────────────────────────────────────
    private Vector2 GetMouseWorldPosition()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        return _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -_cam.transform.position.z));
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, stopRadius);
    }
#endif
}