using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class CircleGestureInput : MonoBehaviour
{
    [Header("Gesture Settings")]
    [Tooltip("Minimum number of points to evaluate a circle gesture.")]
    public int minPointsForGesture = 12;

    [Tooltip("Size of the sliding window (points). Keeping this close to one circle's worth " +
             "of points means multiple loops won't confuse the fitter.")]
    public int windowSize = 60;

    [Tooltip("Minimum circle radius in screen pixels.")]
    public float minRadius = 50f;

    [Tooltip("Maximum allowed radius standard deviation (relative to mean).")]
    [Range(0.01f, 0.35f)]
    public float radiusTolerance = 0.22f;

    [Tooltip("Minimum net clockwise sweep (degrees) to declare a circle.")]
    [Range(180f, 360f)]
    public float degreesRequired = 320f;

    [Tooltip("Maximum seconds of inactivity before buffer reset.")]
    public float maxIdleSeconds = 0.5f;

    [Tooltip("Minimum pixels the mouse must move before a new point is recorded.")]
    public float minMoveDistance = 3f;

    [Tooltip("Cooldown in seconds after a detection before the next one can fire.")]
    public float detectionCooldown = 0.5f;

    [Header("Event")]
    public UnityEvent OnCircleComplete;

    // ── private ───────────────────────────────────────────────────────────────
    // Ring buffer — avoids List.RemoveAt(0) on every frame
    private Vector2[] _ring;
    private int _head;        // index of the oldest point
    private int _count;       // how many valid points are in the buffer

    private float _lastInputTime;
    private Vector2 _lastRecordedPoint;
    private float _cooldownTimer;
    private CircleFitResult _lastFit;


    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _ring = new Vector2[windowSize];
    }

    void OnEnable() => ClearGesture();
    void OnDisable() => ClearGesture();

    // ─────────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.unscaledDeltaTime;
            return;
        }

        // Reset on idle
        if (Time.unscaledTime - _lastInputTime > maxIdleSeconds)
            ClearGesture();

        if (!CollectPointerInput(out Vector2 screenPoint))
            return;

        // Skip if mouse hasn't moved enough
        if (_count > 0 && Vector2.Distance(screenPoint, _lastRecordedPoint) < minMoveDistance)
            return;

        _lastInputTime = Time.unscaledTime;
        _lastRecordedPoint = screenPoint;
        AddPoint(screenPoint);

        if (_count < minPointsForGesture)
            return;

        // Evaluate the current window
        List<Vector2> window = GetWindow();
        _lastFit = FitCircle(window);

        if (_lastFit.radius < minRadius)
            return;

        float radiusSd = CalculateRadiusStdDev(window, _lastFit.center, _lastFit.radius);
        if (radiusSd / _lastFit.radius > radiusTolerance)
            return;

        float sweep = CalculateClockwiseSweep(window, _lastFit.center);
        if (sweep >= degreesRequired)
        {
            _cooldownTimer = detectionCooldown;
            OnCircleComplete?.Invoke();
            ClearGesture();
        }
    }

    // ── Ring buffer helpers ───────────────────────────────────────────────────
    void AddPoint(Vector2 p)
    {
        int writeIndex = (_head + _count) % windowSize;
        _ring[writeIndex] = p;

        if (_count < windowSize)
            _count++;
        else
            _head = (_head + 1) % windowSize; // overwrite oldest
    }

    List<Vector2> GetWindow()
    {
        var list = new List<Vector2>(_count);
        for (int i = 0; i < _count; i++)
            list.Add(_ring[(_head + i) % windowSize]);
        return list;
    }

    // ─────────────────────────────────────────────────────────────────────────
    bool CollectPointerInput(out Vector2 pointer)
    {
        pointer = default;

        if (Input.touchCount > 0)
        {
            pointer = Input.GetTouch(0).position;
            return true;
        }

        if (Mouse.current != null)
        {
            pointer = Mouse.current.position.ReadValue();
            return true;
        }

        return false;
    }

    float CalculateRadiusStdDev(List<Vector2> pts, Vector2 center, float meanRadius)
    {
        float sum = 0f;
        for (int i = 0; i < pts.Count; i++)
        {
            float d = Vector2.Distance(pts[i], center);
            float delta = d - meanRadius;
            sum += delta * delta;
        }
        return Mathf.Sqrt(sum / pts.Count);
    }

    float CalculateClockwiseSweep(List<Vector2> pts, Vector2 center)
    {
        if (pts.Count < 2) return 0f;

        float sweep = 0f;
        float angle = Mathf.Atan2(pts[0].y - center.y, pts[0].x - center.x) * Mathf.Rad2Deg;

        for (int i = 1; i < pts.Count; i++)
        {
            float next = Mathf.Atan2(pts[i].y - center.y, pts[i].x - center.x) * Mathf.Rad2Deg;
            float diff = -Mathf.DeltaAngle(angle, next); // CW positive
            angle = next;

            if (diff > 0f) sweep += diff;
            else sweep = Mathf.Max(0f, sweep + diff);
        }

        return sweep;
    }

    CircleFitResult FitCircle(List<Vector2> pts)
    {
        Vector2 centroid = Vector2.zero;
        for (int i = 0; i < pts.Count; i++)
            centroid += pts[i];
        centroid /= pts.Count;

        float rSum = 0f;
        for (int i = 0; i < pts.Count; i++)
            rSum += Vector2.Distance(pts[i], centroid);

        return new CircleFitResult(centroid, rSum / pts.Count);
    }

    void ClearGesture()
    {
        _head = 0;
        _count = 0;
        _lastFit = default;
    }

}

public struct CircleFitResult
{
    public Vector2 center;
    public float radius;
    public CircleFitResult(Vector2 c, float r) { center = c; radius = r; }
}