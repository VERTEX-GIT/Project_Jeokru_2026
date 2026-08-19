using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitCore))]
[RequireComponent(typeof(UnitMovement))]
[RequireComponent(typeof(TileObjectPlacement))]
[RequireComponent(typeof(UnitSelectable))]
public sealed class UnitWorkRecovery : MonoBehaviour
{
    private UnitCore unitCore;
    private UnitMovement movement;
    private TileObjectPlacement placement;
    private UnitSelectable selectable;

    private UnitDestinationAssigner destinationAssigner;

    private FactoryCore interruptedFactory;

    public bool HasInterruptedWork =>
        interruptedFactory != null;

    private void Awake()
    {
        unitCore =
            GetComponent<UnitCore>();

        movement =
            GetComponent<UnitMovement>();

        placement =
            GetComponent<TileObjectPlacement>();

        selectable =
            GetComponent<UnitSelectable>();

        destinationAssigner =
            FindAnyObjectByType<UnitDestinationAssigner>();
    }

    private void Update()
    {
        if (interruptedFactory == null ||
            unitCore == null ||
            !unitCore.IsActive)
        {
            return;
        }

        // 아직 공격자가 살아 있으면 전투 계속
        if (IsCurrentCombatTargetAlive())
        {
            return;
        }

        ReturnToInterruptedFactory();
    }

    public void TrySaveCurrentWork()
    {
        if (unitCore == null ||
            movement == null ||
            placement == null ||
            movement.IsMoving ||
            !placement.IsPlaced)
        {
            return;
        }

        GameObject target =
            unitCore.CurrentTarget;

        if (target == null ||
            !target.TryGetComponent(
                out FactoryCore factory) ||
            !target.TryGetComponent(
                out FactoryWorkArea workArea))
        {
            return;
        }

        // 실제 작업 타일에 정지해 있는 경우만
        // "작업 중"으로 간주
        if (!workArea.Contains(
                placement.AnchorCell))
        {
            return;
        }

        interruptedFactory = factory;
    }

    public void ClearInterruptedWork()
    {
        interruptedFactory = null;
    }

    private bool IsCurrentCombatTargetAlive()
    {
        GameObject target =
            unitCore.CurrentTarget;

        if (target == null)
        {
            return false;
        }

        if (!target.TryGetComponent(
                out UnitCore targetUnit))
        {
            return false;
        }

        return targetUnit.IsActive &&
               targetUnit.Data != null &&
               targetUnit.Data.Team !=
                   unitCore.Data.Team;
    }

    private void ReturnToInterruptedFactory()
    {
        FactoryCore factory =
            interruptedFactory;

        if (factory == null)
        {
            interruptedFactory = null;
            return;
        }

        if (destinationAssigner == null ||
            selectable == null)
        {
            return;
        }

        UnitSelectable[] singleUnit =
        {
            selectable
        };

        destinationAssigner.IssueFactoryCommand(
            singleUnit,
            factory);

        // 실제로 공장 명령이 들어간 경우에만
        // 복귀 정보를 제거한다.
        if (unitCore.CurrentTarget ==
            factory.gameObject)
        {
            interruptedFactory = null;
        }
    }
}