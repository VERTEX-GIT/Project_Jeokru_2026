using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitMovement))]
public sealed class UnitCore : MonoBehaviour
{
    [field: Header("Unit Data")]
    [field: SerializeField]
    public UnitData Data
    {
        get;
        private set;
    }

    [field: Header("Runtime Combat Stats")]
    [field: SerializeField, Min(0f)]
    public float AttackPower { get; private set; }

    [field: SerializeField, Min(0f)]
    public float Defense { get; private set; }

    [field: SerializeField, Min(0f)]
    public float AttackCooldown { get; private set; }

    [field: Header("Runtime State")]
    [field: SerializeField]
    public bool IsActive
    {
        get;
        private set;
    } = true;

    [field: SerializeField]
    public bool IsAutoCombat
    {
        get;
        private set;
    }

    [field: SerializeField]
    public bool IsPlayerMoveCommandActive
    {
        get;
        private set;
    }

    [field: SerializeField]
    public GameObject CurrentTarget
    {
        get;
        private set;
    }

    [field: SerializeField]
    public bool isMoving
    {
        get;
        private set;
    }

    [field: SerializeField]
    public Vector3Int DestinationCell
    {
        get;
        private set;
    }

    private UnitMovement movement;
    private UnitCounseling counseling;

    public bool IsCounseling =>
        counseling != null &&
        counseling.IsCounseling;

    public bool IsGameplayAvailable =>
        IsActive &&
        !IsCounseling;

    private void Awake()
    {
        movement =
            GetComponent<UnitMovement>();

        counseling =
            GetComponent<UnitCounseling>();

        UpdateMovementState();

        if (Data != null)
            SetData(Data);

        if (Data == null)
        {
            Debug.LogWarning(
                $"{name}: UnitCore에 UnitData가 아직 지정되지 않았습니다. " +
                "동적 생성 유닛이라면 생성 직후 설정될 수 있습니다.",
                this);
        }
    }

    private void LateUpdate()
    {
        UpdateMovementState();
    }

    private void UpdateMovementState()
    {
        if (movement == null)
        {
            isMoving = false;
            DestinationCell = default;
            return;
        }

        isMoving =
            movement.IsMoving;

        DestinationCell =
            movement.DestinationCell;
    }

    public void SetData(
        UnitData data)
    {
        if (data == null)
        {
            Debug.LogError(
                $"{name}: null UnitData를 설정할 수 없습니다.",
                this);

            return;
        }

        Data = data;
        SetCombatStats(data.AttackPower, data.Defense, data.AttackCooldown);
    }

    // SO 기본값은 바꾸지 않고 이 유닛의 현재 능력치만 변경합니다.
    public void SetCombatStats(float attackPower, float defense, float attackCooldown)
    {
        AttackPower = Mathf.Max(0f, attackPower);
        Defense = Mathf.Max(0f, defense);
        AttackCooldown = Mathf.Max(0f, attackCooldown);
    }

    public void SetUnitActive(
        bool active)
    {
        IsActive =
            active;
    }

    public void SetAutoCombat(
        bool enabled)
    {
        IsAutoCombat =
            enabled;
    }

    public void SetPlayerMoveCommandActive(
        bool active)
    {
        IsPlayerMoveCommandActive =
            active;
    }

    public void SetTarget(
        GameObject target)
    {
        CurrentTarget =
            target;
    }

    public void ClearTarget()
    {
        CurrentTarget =
            null;
    }
}
