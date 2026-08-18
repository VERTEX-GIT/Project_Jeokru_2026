using UnityEngine;

public enum UnitAttackType
{
    Melee,
    Ranged
}

[CreateAssetMenu(
    fileName = "NewUnitData",
    menuName = "Project Jeokru/Unit Data")]
public sealed class UnitData : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField]
    private string unitName;

    [SerializeField]
    private GameObject unitPrefab;

    [Header("능력치")]
    [SerializeField]
    [Min(1f)]
    private float maxHp = 100f;

    [SerializeField]
    [Min(0f)]
    private float moveSpeed = 3f;

    [SerializeField]
    [Min(0f)]
    private float attackPower = 10f;

    [SerializeField]
    [Min(0f)]
    private float defense;

    [SerializeField]
    [Min(0f)]
    private float attackCooldown = 1f;

    [Header("공격 설정")]
    [SerializeField]
    private UnitAttackType attackType;

    [SerializeField]
    [Min(0f)]
    private float attackRange = 1f;

    [SerializeField]
    [Min(0f)]
    private float preferredDistance = 1f;

    public string UnitName => unitName;
    public GameObject UnitPrefab => unitPrefab;

    public float MaxHp => maxHp;
    public float MoveSpeed => moveSpeed;
    public float AttackPower => attackPower;
    public float Defense => defense;
    public float AttackCooldown => attackCooldown;

    public UnitAttackType AttackType => attackType;
    public float AttackRange => attackRange;
    public float PreferredDistance => preferredDistance;
}