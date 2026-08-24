using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(FactoryCore))]
public sealed class FactoryHealth : MonoBehaviour, IDamageable
{
    private FactoryCore factoryCore;

    public float MaxHp => factoryCore != null && factoryCore.Definition != null ? factoryCore.Definition.MaxHealth : 0f;

    // [SerializeField]
    // [Min(0f)]
    // private float defense = 5f;

    [field: SerializeField]
    public float CurrentHp { get; private set; }

    [field: SerializeField]
    public bool IsDestroyed { get; private set; }

    public bool IsAlive => !IsDestroyed;

    private void Awake()
    {
        factoryCore = GetComponent<FactoryCore>();

        if (factoryCore == null || factoryCore.Definition == null)
        {
            Debug.LogError($"{name}: FactoryDefinition이 없습니다.", this);

            CurrentHp = 0f;
            IsDestroyed = true;
            
            return;
        }

        CurrentHp = factoryCore.Definition.MaxHealth;

        IsDestroyed = false;
    }

    public void TakeDamage(float attackPower, GameObject attacker)
    {
        if (!IsAlive)
        {
            return;
        }

        float damage = Mathf.Max(0f, attackPower - factoryCore.Definition.Defense);

        CurrentHp =Mathf.Max(0f, CurrentHp - damage);

        if (CurrentHp <= 0f)
        {
            HandleDestroyed();
        }
    }

    private void HandleDestroyed()
    {
        IsDestroyed = true;

        // 공장은 제거하지 않는다.
        // 추후 생산 정지 / 작업자 해제 / 수리 시스템과 연결.
    }

    // 공장 수리(HP 100% 회복)
    public bool Repair()
    {
        // 수리 불가 조건: factoryCore 또는 factoryCore.Definition이 null, 현재 HP가 최대 HP 이상
        if(factoryCore == null || factoryCore.Definition == null || CurrentHp >= factoryCore.Definition.MaxHealth)
        {
            return false;
        }

        CurrentHp = factoryCore.Definition.MaxHealth;
        IsDestroyed = false;

        return true;
    }
}