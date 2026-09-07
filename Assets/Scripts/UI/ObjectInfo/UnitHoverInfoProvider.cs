using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitCore))]
[RequireComponent(typeof(UnitHealth))]
public sealed class UnitHoverInfoProvider :
    MonoBehaviour,
    IHoverInfoProvider
{
    private UnitCore unitCore;
    private UnitHealth unitHealth;

    private void Awake()
    {
        unitCore =
            GetComponent<UnitCore>();

        unitHealth =
            GetComponent<UnitHealth>();
    }

    public HoverInfoData GetHoverInfo()
    {
        if (unitCore == null ||
            unitCore.Data == null)
        {
            return new HoverInfoData(
                name,
                "-",
                "HP",
                "-",
                "스트레스",
                "-",
                "공격력",
                "-",
                "방어력",
                "-",
                "공격 속도",
                "-",
                string.Empty);
        }

        UnitData data =
            unitCore.Data;

        return new HoverInfoData(
            BuildUnitName(data),
            UnitInfoStateUtility
                .GetStateText(unitCore),

            "HP",
            BuildHpText(),

            "스트레스",
            "-",

            "공격력",
            data.AttackPower.ToString("0.#"),

            "방어력",
            data.Defense.ToString("0.#"),

            "공격 속도",
            $"{data.AttackCooldown:0.#}s",

            BuildDescription());
    }

    private static string BuildUnitName(
        UnitData data)
    {
        if (data == null ||
            string.IsNullOrWhiteSpace(
                data.UnitName))
        {
            return "유닛";
        }

        return data.UnitName;
    }

    private string BuildHpText()
    {
        if (unitHealth == null ||
            unitCore == null ||
            unitCore.Data == null)
        {
            return "-";
        }

        return
            $"{unitHealth.CurrentHp:0}/" +
            $"{unitCore.Data.MaxHp:0}";
    }

    private string BuildDescription()
    {
        if (unitCore == null)
        {
            return string.Empty;
        }

        if (!unitCore.IsActive)
        {
            return
                "현재 활동할 수 없는 상태";
        }

        if (UnitInfoStateUtility
            .IsWorkingAtFactory(unitCore))
        {
            return
                BuildFactoryWorkDescription();
        }

        if (UnitInfoStateUtility
            .IsInCombat(unitCore))
        {
            return
                BuildCombatDescription();
        }

        if (UnitInfoStateUtility
            .IsMoving(unitCore))
        {
            return
                "지정된 위치로 이동 중";
        }

        return "대기 중";
    }

    private string
        BuildFactoryWorkDescription()
    {
        if (unitCore == null ||
            unitCore.CurrentTarget == null ||
            !unitCore.CurrentTarget
                .TryGetComponent(
                    out FactoryCore factory))
        {
            return "공장에서 작업 중";
        }

        string factoryName =
            factory.Definition != null &&
            !string.IsNullOrWhiteSpace(
                factory.Definition.DisplayName)
                ? factory.Definition.DisplayName
                : "공장";

        return
            $"{factoryName}에서 작업 중";
    }

    private string BuildCombatDescription()
    {
        if (unitCore == null ||
            unitCore.CurrentTarget == null)
        {
            return "전투 중";
        }

        GameObject target =
            unitCore.CurrentTarget;

        if (target.TryGetComponent(
                out UnitCore targetUnit))
        {
            string targetName =
                targetUnit.Data != null &&
                !string.IsNullOrWhiteSpace(
                    targetUnit.Data.UnitName)
                    ? targetUnit.Data.UnitName
                    : "적 유닛";

            return
                $"{targetName}과 전투 중";
        }

        if (target.TryGetComponent(
                out FactoryCore factory))
        {
            string factoryName =
                factory.Definition != null &&
                !string.IsNullOrWhiteSpace(
                    factory.Definition.DisplayName)
                    ? factory.Definition.DisplayName
                    : "공장";

            return
                $"{factoryName} 공격 중";
        }

        return "전투 중";
    }
}