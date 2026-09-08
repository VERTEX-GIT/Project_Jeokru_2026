using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitCore))]
[RequireComponent(typeof(UnitHealth))]
public sealed class UnitStress :
    MonoBehaviour
{
    public const float MaxStress = 100f;

    [Header("Settings")]
    [SerializeField]
    private UnitStressSettings settings;

    [Header("Runtime")]
    [field: SerializeField]
    public float CurrentStress { get; private set; }

    [field: SerializeField]
    public bool IsLowHealthStressActive
    {
        get;
        private set;
    }

    [field: SerializeField]
    public bool IsWorkStressActive
    {
        get;
        private set;
    }

    [SerializeField]
    private float lowHealthTimer;

    [SerializeField]
    private float workTimer;

    public UnitStressSettings Settings =>
        settings;

    public float StressRatio =>
        Mathf.Clamp01(
            CurrentStress /
            MaxStress);

    public bool IsMaxStress =>
        CurrentStress >=
        MaxStress;

    public event Action<float, float>
        StressChanged;

    private UnitCore unitCore;
    private UnitHealth unitHealth;

    private void Awake()
    {
        unitCore =
            GetComponent<UnitCore>();

        unitHealth =
            GetComponent<UnitHealth>();

        if (settings == null)
        {
            Debug.LogError(
                $"{name}: UnitStressSettings가 지정되지 않았습니다.",
                this);
        }

        CurrentStress = 0f;
    }

    private void Start()
    {
        NotifyStressChanged();
    }

    private void Update()
    {
        if (!CanAccumulateStress())
        {
            ResetInactiveTimers();
            return;
        }

        UpdateLowHealthStress();
        UpdateWorkStress();
    }

    private bool CanAccumulateStress()
    {
        if (unitCore == null ||
            unitCore.Data == null ||
            unitHealth == null ||
            settings == null)
        {
            return false;
        }

        if (unitCore.Data.Team !=
            UnitTeam.Ally)
        {
            return false;
        }

        if (!unitCore.IsActive)
        {
            return false;
        }

        if (!unitHealth.IsAlive)
        {
            return false;
        }

        if (IsMaxStress)
        {
            return false;
        }

        return true;
    }

    private void UpdateLowHealthStress()
    {
        IsLowHealthStressActive =
            IsLowHealth();

        if (!IsLowHealthStressActive)
        {
            lowHealthTimer = 0f;
            return;
        }

        lowHealthTimer +=
            Time.deltaTime;

        float interval =
            settings.LowHealthStressInterval;

        while (lowHealthTimer >= interval)
        {
            lowHealthTimer -= interval;

            AddStress(1f);

            if (IsMaxStress)
            {
                break;
            }
        }
    }

    private void UpdateWorkStress()
    {
        IsWorkStressActive =
            IsWorkingAtFactory();

        if (!IsWorkStressActive)
        {
            workTimer = 0f;
            return;
        }

        workTimer +=
            Time.deltaTime;

        float interval =
            settings.WorkStressInterval;

        while (workTimer >= interval)
        {
            workTimer -= interval;

            AddStress(1f);

            if (IsMaxStress)
            {
                break;
            }
        }
    }

    private bool IsLowHealth()
    {
        if (unitHealth == null ||
            settings == null)
        {
            return false;
        }

        return
            unitHealth.CurrentHp <=
            settings.LowHealthThreshold;
    }

    private bool IsWorkingAtFactory()
    {
        if (unitCore == null ||
            unitCore.Data == null ||
            unitCore.CurrentTarget == null)
        {
            return false;
        }

        if (unitCore.Data.Team !=
            UnitTeam.Ally)
        {
            return false;
        }

        if (!unitCore.CurrentTarget
                .TryGetComponent(
                    out FactoryWorkerManager
                        workerManager))
        {
            return false;
        }

        return workerManager
            .GetWorkingUnits()
            .Contains(unitCore);
    }

    private void ResetInactiveTimers()
    {
        IsLowHealthStressActive =
            false;

        IsWorkStressActive =
            false;

        lowHealthTimer = 0f;
        workTimer = 0f;
    }

    public void AddStress(
        float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        SetStress(
            CurrentStress +
            amount);
    }

    public void ReduceStress(
        float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        SetStress(
            CurrentStress -
            amount);
    }

    public void SetStress(
        float value)
    {
        float newStress =
            Mathf.Clamp(
                value,
                0f,
                MaxStress);

        if (Mathf.Approximately(
                newStress,
                CurrentStress))
        {
            return;
        }

        CurrentStress =
            newStress;

        NotifyStressChanged();
    }

    public void ClearStress()
    {
        SetStress(0f);
    }

    private void NotifyStressChanged()
    {
        StressChanged?.Invoke(
            CurrentStress,
            MaxStress);
    }
}