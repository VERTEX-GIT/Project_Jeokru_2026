using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(FactoryCore))]
[RequireComponent(typeof(FactoryHealth))]
public sealed class FactoryHoverInfoProvider :
    MonoBehaviour,
    IHoverInfoProvider
{
    private FactoryCore factoryCore;
    private FactoryHealth factoryHealth;
    private FactoryProduction factoryProduction;
    private FactoryWorkerManager workerManager;

    private void Awake()
    {
        factoryCore =
            GetComponent<FactoryCore>();

        factoryHealth =
            GetComponent<FactoryHealth>();

        factoryProduction =
            GetComponent<FactoryProduction>();

        workerManager =
            GetComponent<FactoryWorkerManager>();
    }

    public HoverInfoData GetHoverInfo()
    {
        if (factoryCore == null ||
            factoryCore.Definition == null)
        {
            return new HoverInfoData(
                name,
                "-",
                "HP",
                "-",
                "작업자",
                "-",
                "생산품",
                "-",
                "방어력",
                "-",
                "생산 시간",
                "-",
                string.Empty);
        }

        FactoryDefinition definition =
            factoryCore.Definition;

        return new HoverInfoData(
            BuildFactoryName(definition),
            BuildStateText(),

            "HP",
            BuildHpText(),

            "작업자",
            BuildWorkerText(),

            "생산품",
            BuildProductionTypeText(definition),

            "방어력",
            definition.Defense.ToString(),

            "생산 시간",
            BuildProductionTimeText(definition),

            BuildDescription());
    }

    private static string BuildFactoryName(
        FactoryDefinition definition)
    {
        if (definition == null ||
            string.IsNullOrWhiteSpace(
                definition.DisplayName))
        {
            return "공장";
        }

        return definition.DisplayName;
    }

    private string BuildStateText()
    {
        if (factoryHealth == null)
        {
            return "-";
        }

        if (factoryHealth.IsDestroyed)
        {
            return "파괴됨";
        }

        int workerCount =
            GetWorkingUnitCount();

        if (workerCount <= 0)
        {
            return "대기";
        }

        if (factoryProduction != null &&
            factoryProduction.IsProducing)
        {
            return "생산 중";
        }

        return "생산 대기";
    }

    private string BuildHpText()
    {
        if (factoryHealth == null)
        {
            return "-";
        }

        return
            $"{factoryHealth.CurrentHp:0}/" +
            $"{factoryHealth.MaxHp:0}";
    }

    private string BuildWorkerText()
    {
        int workerCount =
            GetWorkingUnitCount();

        return $"{workerCount}명";
    }

    private int GetWorkingUnitCount()
    {
        if (factoryProduction != null)
        {
            return
                factoryProduction
                    .WorkingUnitCount;
        }

        if (workerManager != null)
        {
            return
                workerManager
                    .WorkingUnitCount();
        }

        return 0;
    }

    private static string
        BuildProductionTypeText(
            FactoryDefinition definition)
    {
        if (definition == null)
        {
            return "-";
        }

        return
            definition
                .ProductionType
                .ToString();
    }

    private string BuildProductionTimeText(
        FactoryDefinition definition)
    {
        if (factoryProduction != null &&
            factoryProduction
                .CalculatedProductionTime > 0f)
        {
            return
                $"{factoryProduction.CalculatedProductionTime:0.#}s";
        }

        if (definition == null)
        {
            return "-";
        }

        return
            $"{definition.BaseProductionTime:0.#}s";
    }

    private string BuildDescription()
    {
        if (factoryHealth == null)
        {
            return string.Empty;
        }

        if (factoryHealth.IsDestroyed)
        {
            return
                "파괴되어 생산이 중단된 상태";
        }

        int workerCount =
            GetWorkingUnitCount();

        if (workerCount <= 0)
        {
            return
                "작업자 배치를 기다리는 중";
        }

        if (factoryProduction == null)
        {
            return
                $"{workerCount}명이 작업 중";
        }

        if (factoryProduction.IsProducing)
        {
            int progressPercent =
                Mathf.RoundToInt(
                    Mathf.Clamp01(
                        factoryProduction
                            .ProductionProgressRate) *
                    100f);

            return
                $"생산 진행도 {progressPercent}%";
        }

        return
            "생산 시작 조건을 기다리는 중";
    }
}