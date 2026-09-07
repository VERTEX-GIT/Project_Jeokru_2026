using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitCore))]
[RequireComponent(typeof(UnitHealth))]
public sealed class UnitHoverInfoProvider
    : MonoBehaviour,
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
                "-",
                "-",
                "-",
                "-",
                "-",
                string.Empty);
        }

        UnitData data =
            unitCore.Data;

        return new HoverInfoData(
            data.UnitName,
            BuildStateText(),
            BuildHpText(),

            // 스트레스 시스템 구현 전
            "-",

            data.AttackPower
                .ToString("0.#"),

            data.Defense
                .ToString("0.#"),

            $"{data.AttackCooldown:0.#}s",

            BuildDescription());
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

    private string BuildStateText()
    {
        if (unitCore == null)
        {
            return "-";
        }

        if (!unitCore.IsActive)
        {
            return "활동 정지";
        }

        if (IsWorkingAtFactory())
        {
            return "작업 중";
        }

        if (unitCore.IsAutoCombat)
        {
            return "전투 중";
        }

        if (unitCore.IsPlayerMoveCommandActive ||
            unitCore.isMoving)
        {
            return "이동 중";
        }

        return "대기";
    }

    private string BuildDescription()
    {
        if (unitCore == null)
        {
            return string.Empty;
        }

        if (!unitCore.IsActive)
        {
            return "현재 활동할 수 없는 상태";
        }

        if (TryGetTargetFactory(
                out FactoryCore factory))
        {
            string factoryName =
                factory.Definition != null &&
                !string.IsNullOrWhiteSpace(
                    factory.Definition.DisplayName)
                    ? factory.Definition.DisplayName
                    : "공장";

            return
                $"{factoryName}에서 작업 중";
        }

        if (unitCore.IsAutoCombat)
        {
            return "적과 전투 중";
        }

        if (unitCore.IsPlayerMoveCommandActive ||
            unitCore.isMoving)
        {
            return "지정된 위치로 이동 중";
        }

        return "대기 중";
    }

    private bool IsWorkingAtFactory()
    {
        return TryGetTargetFactory(
            out _);
    }

    private bool TryGetTargetFactory(
        out FactoryCore factory)
    {
        factory = null;

        if (unitCore == null ||
            unitCore.CurrentTarget == null)
        {
            return false;
        }

        return unitCore.CurrentTarget
            .TryGetComponent(
                out factory);
    }
}