using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitCore))]
[RequireComponent(typeof(UnitHealth))]
[RequireComponent(typeof(UnitStress))]
[RequireComponent(typeof(UnitMovement))]
[RequireComponent(typeof(TileObjectPlacement))]
public sealed class UnitCounseling :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private CounselingRoom counselingRoom;

    [Header("Runtime")]
    [field: SerializeField]
    public bool IsCounseling
    {
        get;
        private set;
    }

    [field: SerializeField]
    public Vector3Int ReturnCell
    {
        get;
        private set;
    }

    [SerializeField]
    private bool hasReturnCell;

    [SerializeField]
    private float recoveryTimer;

    private UnitCore unitCore;
    private UnitHealth unitHealth;
    private UnitStress unitStress;
    private UnitMovement movement;
    private TileObjectPlacement placement;
    private UnitSelectable selectable;
    private UnitWorkRecovery workRecovery;

    private void Awake()
    {
        unitCore =
            GetComponent<UnitCore>();

        unitHealth =
            GetComponent<UnitHealth>();

        unitStress =
            GetComponent<UnitStress>();

        movement =
            GetComponent<UnitMovement>();

        placement =
            GetComponent<TileObjectPlacement>();

        selectable =
            GetComponent<UnitSelectable>();

        workRecovery =
            GetComponent<UnitWorkRecovery>();

        if (counselingRoom == null)
        {
            counselingRoom =
                FindAnyObjectByType<
                    CounselingRoom>();
        }
    }

    private void Update()
    {
        if (!IsCounseling)
        {
            TryEnterCounseling();
            return;
        }

        UpdateCounseling();
    }

    private void TryEnterCounseling()
    {
        if (unitStress == null ||
            unitStress.CurrentStress <
                UnitStress.MaxStress)
        {
            return;
        }

        if (!CanEnterCounseling())
        {
            return;
        }

        EnterCounseling();
    }

    private bool CanEnterCounseling()
    {
        if (unitCore == null ||
            unitCore.Data == null ||
            unitHealth == null ||
            unitStress == null ||
            placement == null ||
            movement == null)
        {
            return false;
        }

        if (unitCore.Data.Team !=
            UnitTeam.Ally)
        {
            return false;
        }

        if (!unitCore.IsActive ||
            !unitHealth.IsAlive)
        {
            return false;
        }

        if (counselingRoom == null)
        {
            Debug.LogError(
                $"{name}: CounselingRoom을 찾을 수 없습니다.",
                this);

            return false;
        }

        return true;
    }

    private void EnterCounseling()
    {
        // 이동 중이라면 현재 위치 근처의
        // 유효한 타일에 먼저 정착시킨다.
        if (movement.IsMoving)
        {
            movement.CancelMovement();
        }

        if (!placement.IsPlaced)
        {
            Debug.LogError(
                $"{name}: 상담 진입 전에 " +
                "복귀 타일을 확보하지 못했습니다.",
                this);

            return;
        }

        ReturnCell =
            placement.AnchorCell;

        hasReturnCell =
            true;

        if (selectable != null)
        {
            selectable.Deselect();
        }

        unitCore.SetAutoCombat(false);

        unitCore
            .SetPlayerMoveCommandActive(
                false);

        unitCore.ClearTarget();

        if (workRecovery != null)
        {
            workRecovery
                .ClearInterruptedWork();
        }

        placement.RemoveFromTiles();

        unitCore.SetUnitActive(false);

        IsCounseling =
            true;

        recoveryTimer =
            0f;

        counselingRoom.Register(
            this);

        Vector3 processingPosition =
            counselingRoom
                .ProcessingPosition;

        processingPosition.z =
            transform.position.z;

        transform.position =
            processingPosition;
    }

    private void UpdateCounseling()
    {
        if (unitStress == null ||
            unitStress.Settings == null)
        {
            return;
        }

        if (unitStress.CurrentStress <= 0f)
        {
            TryExitCounseling();
            return;
        }

        recoveryTimer +=
            Time.deltaTime;

        float interval =
            unitStress
                .Settings
                .CounselingRecoveryInterval;

        while (recoveryTimer >= interval)
        {
            recoveryTimer -= interval;

            unitStress.ReduceStress(1f);

            if (unitStress.CurrentStress <= 0f)
            {
                TryExitCounseling();
                break;
            }
        }
    }

    private void TryExitCounseling()
    {
        if (!IsCounseling ||
            unitStress == null ||
            unitStress.CurrentStress > 0f ||
            !hasReturnCell ||
            placement == null)
        {
            return;
        }

        // 원래 타일이 다른 오브젝트에게
        // 점유됐다면 복귀를 기다린다.
        if (!placement.CanPlace(
                ReturnCell))
        {
            return;
        }

        if (!placement.TryPlace(
                ReturnCell))
        {
            return;
        }

        unitHealth.EnsureMinimumHp(
            31f);

        IsCounseling =
            false;

        hasReturnCell =
            false;

        recoveryTimer =
            0f;

        unitCore.SetUnitActive(
            true);

        counselingRoom.Unregister(
            this);
    }
}