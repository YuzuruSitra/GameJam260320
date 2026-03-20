using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerShell : MonoBehaviour
{
    [Tooltip("Mob に当たったときの花粉加算量")]
    [SerializeField] private float _pollenForMob = 10f;
    private const string TagMob    = "Mob";
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(TagMob))
        {
            var gauge = other.GetComponentInParent<Pollen.PollenGauge>();
            if (gauge != null)
                gauge.Add(_pollenForMob);
            else
                Debug.LogWarning($"[PollenBall] '{other.name}' およびその親に PollenGauge が見つかりません。");
        }
    }
}
