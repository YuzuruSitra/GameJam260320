using UnityEngine;

/// <summary>
/// Destroys the GameObject once its ParticleSystem has finished playing.
/// Also lets you scale all child ParticleSystems from one place.
/// Attach to the particle prefab root.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class AutoDestroy : MonoBehaviour
{
    [Header("Size")]
    [Tooltip("Multiplier applied to the Start Size of every ParticleSystem in this prefab.")]
    public float sizeMultiplier = 1f;

    private ParticleSystem[] _allSystems;
    private float[] _originalMin;
    private float[] _originalMax;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _allSystems = GetComponentsInChildren<ParticleSystem>(includeInactive: true);
        _originalMin = new float[_allSystems.Length];
        _originalMax = new float[_allSystems.Length];

        // Snapshot the original sizes ONCE before touching anything
        for (int i = 0; i < _allSystems.Length; i++)
        {
            var startSize = _allSystems[i].main.startSize;
            _originalMin[i] = startSize.constantMin;
            _originalMax[i] = startSize.constantMax != 0f
                                   ? startSize.constantMax
                                   : startSize.constant;
        }

        ApplySize();
    }

    void Update()
    {
        if (!_allSystems[0].IsAlive(withChildren: true))
            Destroy(gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────────
    void ApplySize()
    {
        for (int i = 0; i < _allSystems.Length; i++)
        {
            var main = _allSystems[i].main;
            var startSize = main.startSize;

            switch (startSize.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    main.startSize = new ParticleSystem.MinMaxCurve(
                        _originalMax[i] * sizeMultiplier);
                    break;

                case ParticleSystemCurveMode.TwoConstants:
                    main.startSize = new ParticleSystem.MinMaxCurve(
                        _originalMin[i] * sizeMultiplier,
                        _originalMax[i] * sizeMultiplier);
                    break;
            }
        }
    }
}