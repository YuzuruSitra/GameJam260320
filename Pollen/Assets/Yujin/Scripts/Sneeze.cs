using UnityEngine;
using UnityEngine.InputSystem;
using Pollen;

public class ProjectileSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject projectilePrefab;

    [Header("Timing")]
    [Tooltip("Spawn interval when pollen is empty — slowest.")]
    public float maxSpawnInterval = 3f;
    [Tooltip("Spawn interval when pollen is full — fastest.")]
    public float minSpawnInterval = 0.2f;

    [Header("Spawn Size")]
    [Tooltip("Prefab scale when pollen is empty.")]
    public float minSpawnSize = 0.3f;
    [Tooltip("Prefab scale when pollen is full.")]
    public float maxSpawnSize = 2f;

    [Header("Spawn Offset")]
    public float spawnDistance = 0.6f;

    // ── private ──────────────────────────────────────────────────────────────
    private Camera _cam;
    private float _timer;
    private float _spawnInterval;
    private float _spawnSize;
    private PollenGauge _gauge;
    private SpriteRenderer _sr;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _cam = Camera.main;
        _gauge = GetComponent<PollenGauge>();
        _sr = GetComponent<SpriteRenderer>();
        _spawnInterval = maxSpawnInterval;
        _spawnSize = minSpawnSize;
        _timer = _spawnInterval;
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
    void Update()
    {
        _timer -= Time.deltaTime;

        // Lerp sprite color white → red as timer counts down to zero
        if (_sr != null)
        {
            float t = 1f - Mathf.Clamp01(_timer / _spawnInterval);
            _sr.color = Color.Lerp(Color.white, Color.red, t);
        }

        if (_timer <= 0f)
        {
            _timer = _spawnInterval;
            Spawn();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    void OnPollenChanged(float current, float max)
    {
        float t = max > 0f ? current / max : 0f;
        _spawnInterval = Mathf.Lerp(maxSpawnInterval, minSpawnInterval, t);
        _spawnSize = Mathf.Lerp(minSpawnSize, maxSpawnSize, t);
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
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle - 90f);

        GameObject spawned = Instantiate(projectilePrefab, spawnPos, rotation);
        spawned.transform.localScale = Vector3.one * _spawnSize;
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
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, spawnDistance);
    }
#endif
}