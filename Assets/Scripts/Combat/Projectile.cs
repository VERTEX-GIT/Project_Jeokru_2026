using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class Projectile : MonoBehaviour
{
    [SerializeField]
    [Min(0f)]
    private float speed = 8f;

    [SerializeField]
    [Min(0f)]
    private float lifetime = 5f;

    private GameObject owner;
    private Vector2 direction;
    private float attackPower;
    private UnitTeam ownerTeam;
    private bool initialized;

    public void Initialize(
        Vector2 shootDirection,
        float damage,
        UnitTeam team,
        GameObject projectileOwner)
    {
        if (shootDirection.sqrMagnitude <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        direction =
            shootDirection.normalized;

        attackPower = damage;
        ownerTeam = team;
        owner = projectileOwner;

        initialized = true;

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        transform.position +=
            (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        if (!initialized || other == null)
        {
            return;
        }

        // 유닛 충돌
        if (other.TryGetComponent(
                out UnitCore targetUnit))
        {
            if (targetUnit.Data == null)
            {
                return;
            }

            // 같은 팀은 완전히 통과
            if (targetUnit.Data.Team == ownerTeam)
            {
                return;
            }

            if (other.TryGetComponent(
                    out IDamageable damageable) &&
                damageable.IsAlive)
            {
                damageable.TakeDamage(
                    attackPower,
                    owner);

                Destroy(gameObject);
            }

            return;
        }

        // 공장 충돌
        if (other.TryGetComponent(
                out FactoryCore _))
        {
            // 현재 규칙상 아군 Projectile은
            // 공장을 공격하지 않는다.
            if (ownerTeam == UnitTeam.Ally)
            {
                return;
            }

            if (other.TryGetComponent(
                    out IDamageable damageable) &&
                damageable.IsAlive)
            {
                damageable.TakeDamage(
                    attackPower,
                    owner);

                Destroy(gameObject);
            }
        }
    }
}