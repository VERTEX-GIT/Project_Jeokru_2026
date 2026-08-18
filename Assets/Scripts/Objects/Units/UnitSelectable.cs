using UnityEngine;

// 배치된 유닛의 선택 상태와 선택 표시 오브젝트를 관리
[DisallowMultipleComponent]
[RequireComponent(typeof(TileObjectPlacement))]
public sealed class UnitSelectable : MonoBehaviour
{
    [SerializeField]
    private GameObject selectionIndicator; // 선택 상태를 시각적으로 표시할 오브젝트

    public bool IsSelected { get; private set; }

    private TileObjectPlacement placement;
    private UnitCore unitCore;

    // 배치 컴포넌트를 가져오고 선택 상태 초기화
    private void Awake()
    {
        placement = GetComponent<TileObjectPlacement>();
        unitCore = GetComponent<UnitCore>();

        IsSelected = false;

        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }
    }

    // 타일에 정상 배치된 유닛인지 확인
    public bool CanSelect()
    {
        return placement != null &&
            placement.IsPlaced &&
            placement.ObjectType == TileObjectType.Unit &&
            unitCore != null &&
            unitCore.Data != null &&
            unitCore.Data.Team == UnitTeam.Ally;
    }

    // 선택 가능한 유닛을 선택 상태로 변경
    public void Select()
    {
        if (IsSelected || !CanSelect())
        {
            return;
        }

        IsSelected = true;

        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(true);
        }
    }

    // 선택 상태를 해제하고 표시 오브젝트 숨김
    public void Deselect()
    {
        if (!IsSelected)
        {
            return;
        }

        IsSelected = false;

        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }
    }

    // 비활성화된 유닛이 선택 목록에 남지 않도록 상태 해제
    private void OnDisable()
    {
        Deselect();
    }
}
