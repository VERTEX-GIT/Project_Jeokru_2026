using UnityEngine;

public static class UnitInfoStateUtility
{
    public static bool IsWorkingAtFactory(
        UnitCore core)
    {
        if (core == null ||
            core.Data == null ||
            core.CurrentTarget == null)
        {
            return false;
        }

        // 공장에서 작업하는 것은 아군만 해당
        if (core.Data.Team !=
            UnitTeam.Ally)
        {
            return false;
        }

        return core.CurrentTarget
            .TryGetComponent<FactoryCore>(
                out _);
    }

    public static bool IsInCombat(
        UnitCore core)
    {
        if (core == null ||
            core.Data == null ||
            core.CurrentTarget == null)
        {
            return false;
        }

        GameObject target =
            core.CurrentTarget;

        // 상대 유닛을 타겟으로 잡은 경우
        if (target.TryGetComponent(
                out UnitCore targetUnit))
        {
            return targetUnit.IsActive &&
                targetUnit.Data != null &&
                targetUnit.Data.Team !=
                    core.Data.Team;
        }

        // 공장은 적군만 공격 대상으로 취급
        if (core.Data.Team ==
                UnitTeam.Enemy &&
            target.TryGetComponent(
                out FactoryHealth factoryHealth))
        {
            return factoryHealth.IsAlive;
        }

        return false;
    }

    public static bool IsMoving(
        UnitCore core)
    {
        if (core == null)
        {
            return false;
        }

        return
            core.IsPlayerMoveCommandActive ||
            core.isMoving;
    }

    public static string GetStateText(
        UnitCore core)
    {
        if (core == null)
        {
            return "-";
        }

        if (!core.IsActive)
        {
            return "활동 정지";
        }

        if (IsWorkingAtFactory(core))
        {
            return "작업 중";
        }

        if (IsInCombat(core))
        {
            return "전투 중";
        }

        if (IsMoving(core))
        {
            return "이동 중";
        }

        return "대기";
    }
}