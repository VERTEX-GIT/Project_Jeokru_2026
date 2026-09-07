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

        if (Data == null)
        {
            Debug.LogError(
                $"{name}: UnitCore에 UnitData가 지정되지 않았습니다.",
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