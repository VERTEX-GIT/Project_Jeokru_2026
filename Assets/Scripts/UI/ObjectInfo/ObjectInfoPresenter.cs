using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ObjectInfoPresenter :
    MonoBehaviour
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

        // 공장 Hover는 선택된 유닛 정보보다
        // 일시적으로 우선한다.
        if (TryShowFactoryHover())
        {
            return;
        }

        if (validSelectedUnits.Count >= 2)
        {
            MultiSelectionInfoData info =
                BuildMultiSelectionInfo(
                    validSelectedUnits);

            popupController.ShowMulti(info);

            return;
        }

        if (validSelectedUnits.Count == 1)
        {
            UnitSelectable selected =
                validSelectedUnits[0];

            if (TryGetHoverProvider(
                    selected.gameObject,
                    out IHoverInfoProvider
                        provider))
            {
                popupController.ShowSingle(
                    provider.GetHoverInfo());

                return;
            }
        }

        if (hoverDetector != null &&
            hoverDetector.CurrentProvider !=
                null)
        {
            popupController.ShowSingle(
                hoverDetector
                    .CurrentProvider
                    .GetHoverInfo());

            return;
        }

        popupController.Hide();
    }

    private bool TryShowFactoryHover()
    {
        if (hoverDetector == null ||
            hoverDetector.CurrentProvider == null)
        {
            return false;
        }

        if (hoverDetector.CurrentProvider
            is not FactoryHoverInfoProvider)
        {
            return false;
        }

        popupController.ShowSingle(
            hoverDetector
                .CurrentProvider
                .GetHoverInfo());

        return true;
    }

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

    private static MultiSelectionInfoData
        BuildMultiSelectionInfo(
            List<UnitSelectable> units)
    {
        int totalCount =
            units.Count;

        float hpRatioSum = 0f;
        int hpUnitCount = 0;

        float stressSum = 0f;
        int stressUnitCount = 0;

        int movingCount = 0;
        int combatCount = 0;
        int workingCount = 0;
        int idleCount = 0;

        int rangedCount = 0;
        int meleeCount = 0;

        UnitData firstData = null;

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

            if (selectable.TryGetComponent(
                    out UnitStress stress))
            {
                stressSum +=
                    stress.CurrentStress;

                stressUnitCount++;
            }

            switch (data.AttackType)
            {
                case UnitAttackType.Ranged:
                    rangedCount++;
                    break;

                case UnitAttackType.Melee:
                    meleeCount++;
                    break;
            }

            if (!core.IsActive)
            {
                idleCount++;
            }
            else if (
                UnitInfoStateUtility
                    .IsWorkingAtFactory(core))
            {
                workingCount++;
            }
            else if (
                UnitInfoStateUtility
                    .IsInCombat(core))
            {
                combatCount++;
            }
            else if (
                UnitInfoStateUtility
                    .IsMoving(core))
            {
                movingCount++;
            }
            else
            {
                idleCount++;
            }
        }

        string title =
            BuildTitle(
                firstData,
                allSameUnitData,
                totalCount);

        string averageHp =
            BuildAverageHp(
                hpRatioSum,
                hpUnitCount);

        string averageStress =
            BuildAverageStress(
                stressSum,
                stressUnitCount);

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

    private static string BuildTitle(
        UnitData firstData,
        bool allSameUnitData,
        int totalCount)
    {
        if (!allSameUnitData ||
            firstData == null)
        {
            return
                totalCount +
                "개 유닛 선택";
        }

        string unitName =
            firstData.UnitName;

        if (string.IsNullOrWhiteSpace(
                unitName))
        {
            unitName =
                "유닛";
        }

        return
            unitName +
            " ×" +
            totalCount;
    }

    private static string BuildAverageHp(
        float hpRatioSum,
        int hpUnitCount)
    {
        if (hpUnitCount <= 0)
        {
            return "-";
        }

        float averageHpRatio =
            hpRatioSum /
            hpUnitCount;

        int averageHpPercent =
            Mathf.RoundToInt(
                averageHpRatio *
                100f);

        return
            averageHpPercent +
            "%";
    }

    private static string BuildAverageStress(
        float stressSum,
        int stressUnitCount)
    {
        if (stressUnitCount <= 0)
        {
            return "-";
        }

        float averageStress =
            stressSum /
            stressUnitCount;

        return
            $"{averageStress:0.#}/" +
            $"{UnitStress.MaxStress:0}";
    }

    private static bool
        TryGetHoverProvider(
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
                MonoBehaviour>(true);

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