using UnityEngine;

[DisallowMultipleComponent]
public sealed class UnitRetreatMover
    : MonoBehaviour
{
    private enum RetreatPhase
    {
        None,
        MovingToEntry,
        MovingOutside
    }

    private UnitCore unitCore;
    private UnitMovement movement;
    private TileObjectPlacement placement;
    private UnitTargeting targeting;

    private EnemySpawnZone spawnZone;

    private Vector3Int exitCell;
    private Vector3 outsideDestination;

    private RetreatPhase phase =
        RetreatPhase.None;

    public bool IsRetreating =>
        phase != RetreatPhase.None;

    private void Awake()
    {
        unitCore =
            GetComponent<UnitCore>();

        movement =
            GetComponent<UnitMovement>();

        placement =
            GetComponent<
                TileObjectPlacement>();

        targeting =
            GetComponent<UnitTargeting>();
    }

    private void Update()
    {
        if (!IsRetreating ||
            PauseMenu.IsPaused)
        {
            return;
        }

        switch (phase)
        {
            case RetreatPhase.MovingToEntry:
                UpdateMovingToEntry();
                break;

            case RetreatPhase.MovingOutside:
                UpdateMovingOutside();
                break;
        }
    }

    public bool BeginRetreat(
        EnemySpawnZone zone)
    {
        if (zone == null ||
            unitCore == null ||
            movement == null ||
            placement == null)
        {
            return false;
        }

        if (IsRetreating)
        {
            return true;
        }

        spawnZone =
            zone;

        unitCore.ClearTarget();

        unitCore.SetAutoCombat(
            false);

        if (targeting != null)
        {
            targeting.enabled =
                false;
        }

        movement.CancelMovement();

        if (!spawnZone.TryMoveToEntry(
                movement,
                out exitCell))
        {
            Debug.LogWarning(
                $"{name}: 퇴각할 EntryPoint를 " +
                "확보하지 못했습니다.",
                this);

            return false;
        }

        phase =
            RetreatPhase.MovingToEntry;

        return true;
    }

    private void UpdateMovingToEntry()
    {
        if (movement == null ||
            movement.IsMoving)
        {
            return;
        }

        if (placement == null ||
            !placement.IsPlaced)
        {
            return;
        }

        if (placement.AnchorCell !=
            exitCell)
        {
            return;
        }

        if (!placement.RemoveFromTiles())
        {
            return;
        }

        outsideDestination =
            spawnZone
                .GetRetreatDestination(
                    transform.position);

        unitCore.SetUnitActive(
            false);

        phase =
            RetreatPhase.MovingOutside;
    }

    private void UpdateMovingOutside()
    {
        if (unitCore == null ||
            unitCore.Data == null)
        {
            return;
        }

        float moveSpeed =
            unitCore.Data.MoveSpeed;

        transform.position =
            Vector3.MoveTowards(
                transform.position,
                outsideDestination,
                moveSpeed *
                Time.deltaTime);

        if (transform.position !=
            outsideDestination)
        {
            return;
        }

        Destroy(gameObject);
    }
}