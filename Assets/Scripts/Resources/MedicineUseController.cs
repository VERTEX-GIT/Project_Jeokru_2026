using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

[DisallowMultipleComponent]
public sealed class MedicineUseController : MonoBehaviour
{
    //================================================
    // 변수
    //================================================

    // 공격력 증가 수치
    [SerializeField, Min(0f)]
    private float attackIncreasePercent;

    // 방어력 증가 수치
    [SerializeField, Min(0f)]
    private float defenseIncreasePercent;

    // 공격 쿨타임 감소 수치
    [SerializeField, Range(0f, 100f)]
    private float cooldownDecreasePercent;

    // 회복량
    [SerializeField, Min(0f)]
    private float healAmount;

    //===============================================

    // 최대 공격력
    [SerializeField, Min(0f)]
    private float maxAttackPower;

    // 최대 방어력
    [SerializeField, Min(0f)]
    private float maxDefense;

    // 최소 공격 쿨타임
    [SerializeField, Min(0f)]
    private float minAttackCooldown;

    public string FailureReason { get; private set; } = "";
    private bool isApplying;

    public static MedicineUseController Instance { get; private set; }
    public ResourceType? SelectedMedicine { get; private set; }
    public bool InputConsumedThisFrame => consumedInputFrame == Time.frameCount;
    public bool BlocksWorldInput => SelectedMedicine.HasValue || InputConsumedThisFrame;

    private int consumedInputFrame = -1;
    private Camera worldCamera;
    private ObjectPlacementController placementController;
    private readonly List<RaycastResult> uiHits = new();

    private void Awake()
    {
        Instance = this;
        worldCamera = Camera.main;
        placementController = FindAnyObjectByType<ObjectPlacementController>();
    }

    private void OnDisable()
    {
        CancelMedicineSelection();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // 버튼 On Click에 연결할 함수
    public void SelectRedMedicine() => SelectMedicine(ResourceType.RedMedicine);
    public void SelectBlueMedicine() => SelectMedicine(ResourceType.BlueMedicine);
    public void SelectPurpleMedicine() => SelectMedicine(ResourceType.PurpleMedicine);
    public void SelectGreenMedicine() => SelectMedicine(ResourceType.GreenMedicine);

    private void SelectMedicine(ResourceType medicine)
    {
        if (!isActiveAndEnabled || PauseMenu.IsPaused)
        {
            return;
        }

        placementController?.CancelPlacement();
        SelectedMedicine = medicine;
        consumedInputFrame = Time.frameCount;
        FailureReason = "";
    }

    public bool CancelMedicineSelection()
    {
        if (!SelectedMedicine.HasValue)
        {
            return false;
        }

        SelectedMedicine = null;
        consumedInputFrame = Time.frameCount;
        return true;
    }

    private void Update()
    {
        if (!SelectedMedicine.HasValue)
        {
            return;
        }

        if (PauseMenu.IsPaused ||
            (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            CancelMedicineSelection();
            return;
        }

        if (InputConsumedThisFrame || Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        TryUseAtScreenPosition(Mouse.current.position.ReadValue());
    }

    private void TryUseAtScreenPosition(Vector2 screenPosition)
    {
        if (!SelectedMedicine.HasValue || worldCamera == null ||
            !worldCamera.pixelRect.Contains(screenPosition) || IsPointerOverUI(screenPosition))
        {
            return;
        }

        Vector3 worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
        Collider2D hit = Physics2D.OverlapPoint(worldPosition);
        UnitCore target = hit != null ? hit.GetComponentInParent<UnitCore>() : null;

        if (TryUseMedicine(SelectedMedicine.Value, target))
        {
            CancelMedicineSelection();
        }
        else
        {
            Debug.Log(FailureReason, this);
        }
    }

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        var pointer = new PointerEventData(EventSystem.current) { position = screenPosition };
        uiHits.Clear();
        EventSystem.current.RaycastAll(pointer, uiHits);
        foreach (var hit in uiHits)
        {
            if (hit.module is GraphicRaycaster)
            {
                return true;
            }
        }

        return false;
    }

    //================================================
    // 알약 사용 메서드
    //================================================

    // 약 사용
    public bool TryUseMedicine(ResourceType medicine, UnitCore target)
    {
        FailureReason = "";
        if (isApplying || PauseMenu.IsPaused)
        {
            return Fail("현재 약을 사용할 수 없습니다.");
        }

        if (!CanUseOnTarget(medicine, target, out UnitHealth health))
        {
            return false;
        }

        isApplying = true;
        try
        {
            bool success = medicine switch
            {
                ResourceType.RedMedicine => UseRedMedicine(target),
                ResourceType.BlueMedicine => UseBlueMedicine(target),
                ResourceType.PurpleMedicine => UsePurpleMedicine(target),
                ResourceType.GreenMedicine => UseGreenMedicine(target, health),
                _ => Fail("알약 종류가 아닙니다.")
            };

            if (success)
            {
                FailureReason = "";
            }

            return success;
        }
        finally
        {
            isApplying = false;
        }
    }

    // 공통 대상 검사
    private bool CanUseOnTarget(ResourceType medicine, UnitCore target, out UnitHealth health)
    {
        health = null;
        if (target == null || !target.gameObject.activeInHierarchy ||
            target.Data == null || target.Data.Team != UnitTeam.Ally ||
            target.IsCounseling || !target.TryGetComponent(out health))
        {
            return Fail("약을 사용할 수 없는 대상입니다.");
        }

        bool canRevive = medicine == ResourceType.GreenMedicine && target.Data.IsBasicUnit;
        if ((!target.IsActive || !health.IsAlive) && !canRevive)
        {
            return Fail("활동 정지한 기본 유닛에는 회복약만 사용할 수 있습니다.");
        }

        if (!IsFinite(target.AttackPower) || !IsFinite(target.Defense) ||
            !IsFinite(target.AttackCooldown) || !IsFinite(health.CurrentHp))
        {
            return Fail("유닛 능력치를 확인해 주세요.");
        }

        return true;
    }

    // 약별 효과: 새 값 계산 → 변화 확인 및 차감 → 적용
    private bool UseRedMedicine(UnitCore target)
    {
        float next = Increase(target.AttackPower, attackIncreasePercent, maxAttackPower);
        if (!TrySpendMedicine(ResourceType.RedMedicine, target.AttackPower, next))
        {
            return false;
        }

        target.SetCombatStats(next, target.Defense, target.AttackCooldown);
        return true;
    }

    private bool UseBlueMedicine(UnitCore target)
    {
        float next = Increase(target.Defense, defenseIncreasePercent, maxDefense);
        if (!TrySpendMedicine(ResourceType.BlueMedicine, target.Defense, next))
        {
            return false;
        }

        target.SetCombatStats(target.AttackPower, next, target.AttackCooldown);
        return true;
    }

    private bool UsePurpleMedicine(UnitCore target)
    {
        float next = Decrease(target.AttackCooldown, cooldownDecreasePercent, minAttackCooldown);
        if (!TrySpendMedicine(ResourceType.PurpleMedicine, target.AttackCooldown, next))
        {
            return false;
        }

        target.SetCombatStats(target.AttackPower, target.Defense, next);
        return true;
    }

    private bool UseGreenMedicine(UnitCore target, UnitHealth health)
    {
        float next = Mathf.Max(health.CurrentHp,
            Mathf.Min(health.MaxHp, health.CurrentHp + Mathf.Max(0f, healAmount)));
        if (!TrySpendMedicine(ResourceType.GreenMedicine, health.CurrentHp, next))
        {
            return false;
        }

        health.EnsureMinimumHp(next);
        if (!target.IsActive && health.IsAlive && target.Data.IsBasicUnit)
        {
            target.SetUnitActive(true);
        }

        return true;
    }

    // 변화가 없거나 재고가 부족하면 소비하지 않습니다.
    private bool TrySpendMedicine(ResourceType medicine, float current, float next)
    {
        if (!IsFinite(next))
        {
            return Fail("약 효과 수치를 확인해 주세요.");
        }

        if (current == next)
        {
            return Fail("능력치가 변하지 않습니다. 효과 설정 또는 능력치 제한을 확인해 주세요.");
        }

        ResourceInventory inventory = ResourceInventory.Inventory;
        if (inventory == null)
        {
            return Fail("인벤토리가 없습니다.");
        }

        if (!inventory.Spend(medicine, 1))
        {
            return Fail("약이 부족합니다.");
        }

        return true;
    }

    // 이미 제한을 넘은 유닛에게 약을 사용해도 능력치가 역으로 나빠지지 않습니다.
    private static float Increase(float current, float percent, float maximum)
    {
        float increased = current * (1f + Mathf.Max(0f, percent) / 100f);
        return Mathf.Max(current, Mathf.Min(maximum, increased));
    }

    private static float Decrease(float current, float percent, float minimum)
    {
        float decreased = current * (1f - Mathf.Clamp01(percent / 100f));
        return Mathf.Min(current, Mathf.Max(Mathf.Max(0f, minimum), decreased));
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private bool Fail(string reason)
    {
        FailureReason = reason;
        return false;
    }

#if UNITY_EDITOR
    [ContextMenu("Check Medicine Selection (Play Mode)")]
    private void CheckSelection()
    {
        if (!Application.isPlaying || PauseMenu.IsPaused || SelectedMedicine.HasValue ||
            (placementController != null && placementController.CurrentMode != PlacementMode.None))
        {
            throw new System.Exception("실행 중인 배치/약 선택을 취소하고 플레이 모드에서 검사해 주세요.");
        }

        try
        {
            System.Action[] buttons = { SelectRedMedicine, SelectBlueMedicine, SelectPurpleMedicine, SelectGreenMedicine };
            ResourceType[] types = { ResourceType.RedMedicine, ResourceType.BlueMedicine, ResourceType.PurpleMedicine, ResourceType.GreenMedicine };
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i]();
                if (SelectedMedicine != types[i] || !BlocksWorldInput || !CancelMedicineSelection() ||
                    SelectedMedicine.HasValue || !InputConsumedThisFrame || CancelMedicineSelection())
                {
                    throw new System.Exception("Medicine selection/cancel check failed");
                }
            }
            Debug.Log("MEDICINE_SELECTION_CHECK PASS: four buttons, cancel and same-frame input guard");
        }
        finally
        {
            CancelMedicineSelection();
        }
    }

    [ContextMenu("Check Medicine Calculations")]
    private void CheckCalculations()
    {
        bool passed = Mathf.Approximately(Increase(100f, 20f, 110f), 110f) &&
            Increase(120f, 20f, 110f) == 120f && Increase(0f, 20f, 110f) == 0f &&
            Mathf.Approximately(Decrease(1f, 50f, 0.7f), 0.7f) &&
            Decrease(0.5f, 50f, 0.7f) == 0.5f && Decrease(1f, 0f, 0.1f) == 1f;
        if (!passed)
        {
            throw new System.Exception("Medicine calculation check failed");
        }
        Debug.Log("MEDICINE_CHECK PASS: percentage, limits, zero defense and no-change cases");
    }
#endif
}
