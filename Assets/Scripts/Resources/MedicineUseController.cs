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

    //================================================

    // 회복량
    [SerializeField, Min(0f)]
    private float healAmount;

    // 최대 공격력
    [SerializeField, Min(0f)]
    private float maxAttackPower;

    // 최대 방어력
    [SerializeField, Min(0f)]
    private float maxDefense;

    // 최소 공격 쿨타임
    [SerializeField, Min(0f)]
    private float minAttackCooldown;

    // 좌클릭을 누르고 있을 때 약을 반복 적용하는 간격(초)
    [SerializeField, Min(0.05f)]
    private float repeatInterval = 0.3f;

    private float nextUseTime;

    //================================================

    public string FailureReason { get; private set; } = "";
    private bool isApplying;

    //================================================

    // 싱글톤
    public static MedicineUseController Instance { get; private set; }

    // 현재 선택된 약 종류
    public ResourceType? SelectedMedicine { get; private set; }
    
    /* ----------------------------
    약 사용에 사용된 마우스 처리를 다른 작업에도 사용하지 못하도록 하는 변수

    InputConsumedThisFrame: 이번 프레임에 약 사용을 위해 마우스 입력이 소비되었는지 여부
    BlocksWorldInput: 약 사용 직전이거나 약 사용 시 다른 커멘드 막기
    ---------------------------- */
    public bool InputConsumedThisFrame => consumedInputFrame == Time.frameCount;
    public bool BlocksWorldInput => SelectedMedicine.HasValue || InputConsumedThisFrame;

    // 약 사용 시 마우스 입력이 소비된 프레임
    private int consumedInputFrame = -1;

    private Camera worldCamera;
    private ObjectPlacementController placementController;

    // UI Raycast 검사 결과를 재사용하기 위한 리스트
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

    //================================================
    // 알약 선택 메서드
    //================================================

    // 약 선택
    private void SelectMedicine(ResourceType medicine)
    {
        if (!isActiveAndEnabled || PauseMenu.IsPaused)
        {
            return;
        }

        placementController?.CancelPlacement();
        SelectedMedicine = medicine;
        nextUseTime = 0f;
        consumedInputFrame = Time.frameCount;
        FailureReason = "";
    }

    // 약 선택 취소
    public bool CancelMedicineSelection()
    {
        if (!SelectedMedicine.HasValue)
        {
            return false;
        }

        SelectedMedicine = null;
        nextUseTime = 0f;
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

        if (InputConsumedThisFrame || Mouse.current == null)
        {
            return;
        }

        var leftButton = Mouse.current.leftButton;
        if (!ShouldApplyMedicine(leftButton.wasPressedThisFrame, leftButton.isPressed, Time.unscaledTime))
        {
            return;
        }

        TryUseAtScreenPosition(Mouse.current.position.ReadValue());
    }

    // 첫 클릭은 즉시, 누르고 있으면 간격마다 한 번만 시도합니다.
    private bool ShouldApplyMedicine(bool pressedThisFrame, bool isHeld, float now)
    {
        if (!pressedThisFrame && !isHeld)
        {
            nextUseTime = 0f;
            return false;
        }

        if (!pressedThisFrame && now < nextUseTime)
        {
            return false;
        }

        nextUseTime = now + Mathf.Max(0.05f, repeatInterval);
        return true;
    }

    // 약 사용 시도
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

        if (!TryUseMedicine(SelectedMedicine.Value, target))
        {
            Debug.Log(FailureReason, this);
        }
    }

    // UI 위에 마우스가 있는지 검사
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
            Debug.Log("Current Inventory List: " + ResourceInventory.Inventory, this);
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

    //================================================
    // 알약별 효과 적용 메서드
    //================================================

    // 붉은 약 사용 - 공격력 증가
    private bool UseRedMedicine(UnitCore target)
    {
        float next = Increase(target.AttackPower, attackIncreasePercent, maxAttackPower);

        if (!TrySpendMedicine(ResourceType.RedMedicine, target.AttackPower, next))
        {
            return false;
        }

        Debug.Log($"[MedicineUseController] {target.name} 공격력 {target.AttackPower} -> {next}", this);

        target.SetCombatStats(next, target.Defense, target.AttackCooldown);
        return true;
    }

    // 푸른 약 사용 - 방어력 증가
    private bool UseBlueMedicine(UnitCore target)
    {
        float next = Increase(target.Defense, defenseIncreasePercent, maxDefense);

        if (!TrySpendMedicine(ResourceType.BlueMedicine, target.Defense, next))
        {
            return false;
        }

        Debug.Log($"[MedicineUseController] {target.name} 방어력 {target.Defense} -> {next}", this);

        target.SetCombatStats(target.AttackPower, next, target.AttackCooldown);
        return true;
    }

    // 보라 약 사용 - 공격 쿨타임 감소
    private bool UsePurpleMedicine(UnitCore target)
    {
        float next = Decrease(target.AttackCooldown, cooldownDecreasePercent, minAttackCooldown);

        if (!TrySpendMedicine(ResourceType.PurpleMedicine, target.AttackCooldown, next))
        {
            return false;
        }

        Debug.Log($"[MedicineUseController] {target.name} 공격 쿨타임 {target.AttackCooldown} -> {next}", this);

        target.SetCombatStats(target.AttackPower, target.Defense, next);
        return true;
    }

    // 초록 약 사용 - 체력 회복
    private bool UseGreenMedicine(UnitCore target, UnitHealth health)
    {
        float next = Mathf.Max(health.CurrentHp,
            Mathf.Min(health.MaxHp, health.CurrentHp + Mathf.Max(0f, healAmount)));

        if (!TrySpendMedicine(ResourceType.GreenMedicine, health.CurrentHp, next))
        {
            return false;
        }

        Debug.Log($"[MedicineUseController] {target.name} 체력 {health.CurrentHp} -> {next}", this);

        health.EnsureMinimumHp(next);
        if (!target.IsActive && health.IsAlive && target.Data.IsBasicUnit)
        {
            target.SetUnitActive(true);
        }

        return true;
    }

    // 변화가 없거나 재고가 부족하면 소비하지 않음
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

    // 스탯 증가 계산
    private static float Increase(float current, float percent, float maximum)
    {
        float increased = current * (1f + Mathf.Max(0f, percent) / 100f);
        return Mathf.Max(current, Mathf.Min(maximum, increased));
    }

    // 스탯 감소 계산
    private static float Decrease(float current, float percent, float minimum)
    {
        float decreased = current * (1f - Mathf.Clamp01(percent / 100f));
        return Mathf.Min(current, Mathf.Max(Mathf.Max(0f, minimum), decreased));
    }

    // 유한 수인지 검사
    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    // 약 사용 실패 시 실패 이유를 설정하고 false 반환
    private bool Fail(string reason)
    {
        FailureReason = reason;
        return false;
    }

// -----------------------------------------------------------------------------------------------------

#if UNITY_EDITOR
    [ContextMenu("Check Medicine Repeat Timing")]
    private void CheckRepeatTiming()
    {
        float savedTime = nextUseTime;
        float savedInterval = repeatInterval;
        try
        {
            repeatInterval = 0.5f;
            bool passed = ShouldApplyMedicine(true, true, 10f) &&
                !ShouldApplyMedicine(false, true, 10.49f) &&
                ShouldApplyMedicine(false, true, 10.5f) &&
                !ShouldApplyMedicine(false, true, 10.5f) &&
                !ShouldApplyMedicine(false, false, 10.6f) &&
                ShouldApplyMedicine(true, true, 10.61f);
            repeatInterval = 0f;
            passed &= ShouldApplyMedicine(true, true, 20f) &&
                !ShouldApplyMedicine(false, true, 20f);
            if (!passed)
            {
                throw new System.Exception("Medicine repeat timing check failed");
            }
            Debug.Log("MEDICINE_REPEAT_CHECK PASS: immediate click, interval, release, re-click and minimum interval");
        }
        finally
        {
            nextUseTime = savedTime;
            repeatInterval = savedInterval;
        }
    }

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
