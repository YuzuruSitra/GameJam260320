using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Spawns a prefab at a fixed interval in the direction of the mouse cursor.
/// Attach to the player. Assign a prefab in the Inspector.
/// </summary>
public class ProjectileSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("The prefab to spawn (e.g. a bullet).")]
    public GameObject projectilePrefab;

    [Header("Timing")]
    [Tooltip("Seconds between each spawn.")]
    public float spawnInterval = 1f;

    [Header("Spawn Offset")]
    [Tooltip("How far from the player center the prefab appears (so it doesn't overlap the sprite).")]
    public float spawnDistance = 0.6f;

    // ── private ──────────────────────────────────────────────────────────────
    private Camera _cam;
    private float _timer;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _cam = Camera.main;
        _timer = spawnInterval; // fire immediately on first tick if you prefer 0
    }

    void Update()
    {
        _timer -= Time.deltaTime;

        if (_timer <= 0f)
        {
            _timer = spawnInterval;
            Spawn();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    void Spawn()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("ProjectileSpawner: no prefab assigned!");
            return;
        }

        Vector2 mouseWorld = GetMouseWorldPosition();
        Vector2 direction = (mouseWorld - (Vector2)transform.position).normalized;
        Vector3 spawnPos = transform.position + (Vector3)(direction * spawnDistance);

        // Rotate so the prefab's "up" points toward the cursor
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle - 90f);

        Instantiate(projectilePrefab, spawnPos, rotation);
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
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, spawnDistance);
    }
#endif
}