using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ObjectInfoPresenter
    : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private UnitSelectionController
        selectionController;

    [SerializeField]
    private ObjectHoverDetector
        hoverDetector;

    [SerializeField]
    private ObjectInfoPopupController
        popupController;

    private readonly List<UnitSelectable>
        validSelectedUnits = new();

    private void Awake()
    {
        if (selectionController == null)
        {
            selectionController =
                FindAnyObjectByType<
                    UnitSelectionController>();
        }

        if (hoverDetector == null)
        {
            hoverDetector =
                FindAnyObjectByType<
                    ObjectHoverDetector>();
        }

        if (popupController == null)
        {
            popupController =
                FindAnyObjectByType<
                    ObjectInfoPopupController>();
        }
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (popupController == null)
        {
            return;
        }

        CollectValidSelectedUnits();

        // =========================
        // 다중 선택
        // =========================

        if (validSelectedUnits.Count >= 2)
        {
            MultiSelectionInfoData info =
                BuildMultiSelectionInfo(
                    validSelectedUnits);

            popupController.ShowMulti(
                info);

            return;
        }

        // =========================
        // 단일 선택
        // =========================

        if (validSelectedUnits.Count == 1)
        {
            UnitSelectable selected =
                validSelectedUnits[0];

            if (TryGetHoverProvider(
                    selected.gameObject,
                    out IHoverInfoProvider provider))
            {
                popupController.ShowSingle(
                    provider.GetHoverInfo());

                return;
            }
        }

        // =========================
        // Hover
        // =========================

        if (hoverDetector != null &&
            hoverDetector.CurrentProvider != null)
        {
            popupController.ShowSingle(
                hoverDetector
                    .CurrentProvider
                    .GetHoverInfo());

            return;
        }

        popupController.Hide();
    }

    // =========================
    // Selection
    // =========================

    private void CollectValidSelectedUnits()
    {
        validSelectedUnits.Clear();

        if (selectionController == null ||
            selectionController.SelectedUnits ==
                null)
        {
            return;
        }

        IReadOnlyList<UnitSelectable>
            selectedUnits =
                selectionController
                    .SelectedUnits;

        foreach (UnitSelectable selectable
                 in selectedUnits)
        {
            if (selectable == null)
            {
                continue;
            }

            if (!selectable.TryGetComponent(
                    out UnitCore core) ||
                core == null ||
                core.Data == null)
            {
                continue;
            }

            validSelectedUnits.Add(
                selectable);
        }
    }

    // =========================
    // Multi Selection
    // =========================

    private static MultiSelectionInfoData
        BuildMultiSelectionInfo(
            List<UnitSelectable> units)
    {
        int totalCount =
            units.Count;

        float hpRatioSum =
            0f;

        int hpUnitCount =
            0;

        int movingCount =
            0;

        int combatCount =
            0;

        int workingCount =
            0;

        int idleCount =
            0;

        int rangedCount =
            0;

        int meleeCount =
            0;

        UnitData firstData =
            null;

        bool allSameUnitData =
            true;

        foreach (UnitSelectable selectable
                 in units)
        {
            if (selectable == null ||
                !selectable.TryGetComponent(
                    out UnitCore core) ||
                core.Data == null)
            {
                continue;
            }

            UnitData data =
                core.Data;

            // =========================
            // 같은 종류인지 검사
            // =========================

            if (firstData == null)
            {
                firstData =
                    data;
            }
            else if (firstData != data)
            {
                allSameUnitData =
                    false;
            }

            // =========================
            // HP 평균
            // =========================

            if (selectable.TryGetComponent(
                    out UnitHealth health) &&
                data.MaxHp > 0f)
            {
                float hpRatio =
                    health.CurrentHp /
                    data.MaxHp;

                hpRatioSum +=
                    Mathf.Clamp01(
                        hpRatio);

                hpUnitCount++;
            }

            // =========================
            // 유닛 구성
            // =========================

            switch (data.AttackType)
            {
                case UnitAttackType.Ranged:
                    rangedCount++;
                    break;

                case UnitAttackType.Melee:
                    meleeCount++;
                    break;
            }

            // =========================
            // 현재 상태
            // =========================

            if (IsWorkingAtFactory(core))
            {
                workingCount++;
            }
            else if (core.IsAutoCombat)
            {
                combatCount++;
            }
            else if (
                core.IsPlayerMoveCommandActive ||
                core.isMoving)
            {
                movingCount++;
            }
            else
            {
                idleCount++;
            }
        }

        // =========================
        // 제목
        // =========================

        string title;

        if (allSameUnitData &&
            firstData != null)
        {
            title =
                firstData.UnitName +
                " ×" +
                totalCount;
        }
        else
        {
            title =
                totalCount +
                "개 유닛 선택";
        }

        // =========================
        // 평균 HP
        // =========================

        string averageHp;

        if (hpUnitCount > 0)
        {
            float averageHpRatio =
                hpRatioSum /
                hpUnitCount;

            int averageHpPercent =
                Mathf.RoundToInt(
                    averageHpRatio *
                    100f);

            averageHp =
                averageHpPercent +
                "%";
        }
        else
        {
            averageHp =
                "-";
        }

        // =========================
        // 평균 스트레스
        // =========================

        // 스트레스 시스템 구현 후
        // 실제 평균값으로 교체
        string averageStress =
            "-";

        return new MultiSelectionInfoData(
            title,
            averageHp,
            averageStress,
            movingCount,
            combatCount,
            workingCount,
            idleCount,
            rangedCount,
            meleeCount);
    }

    private static bool IsWorkingAtFactory(
        UnitCore core)
    {
        if (core == null ||
            core.CurrentTarget == null)
        {
            return false;
        }

        return core.CurrentTarget
            .TryGetComponent<
                FactoryCore>(
                out _);
    }

    // =========================
    // Provider
    // =========================

    private static bool TryGetHoverProvider(
        GameObject target,
        out IHoverInfoProvider provider)
    {
        provider = null;

        if (target == null)
        {
            return false;
        }

        MonoBehaviour[] behaviours =
            target.GetComponents<
                MonoBehaviour>();

        foreach (MonoBehaviour behaviour
                 in behaviours)
        {
            if (behaviour is
                IHoverInfoProvider
                hoverProvider)
            {
                provider =
                    hoverProvider;

                return true;
            }
        }

        behaviours =
            target.GetComponentsInChildren<
                MonoBehaviour>(
                true);

        foreach (MonoBehaviour behaviour
                 in behaviours)
        {
            if (behaviour is
                IHoverInfoProvider
                hoverProvider)
            {
                provider =
                    hoverProvider;

                return true;
            }
        }

        return false;
    }
}