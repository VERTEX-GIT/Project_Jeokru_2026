using TMPro;
using UnityEngine;

public class ResourceAmountView : MonoBehaviour
{
    // 알약 자원 뷰어
    public TMP_Text RedMedAmountText;      // 붉은 약
    public TMP_Text BlueMedAmountText;     // 푸른 약
    public TMP_Text PurpleMedAmountText;   // 보라색 약
    public TMP_Text GreenMedAmountText;    // 녹색 약

    // 기본 생산 자원 뷰어
    public TMP_Text IronAmountText;        // 철

    private void Start()
    {   
        // 이벤트(ResourceAmountChanged) 호출 시 작동할 함수 리스트 추가
        ResourceInventory.Inventory.ResourceAmountChanged += OnAmountChanged;

        // 초기화
        UpdateResourceAmount(ResourceType.Iron, ResourceInventory.Inventory.GetResourceAmount(ResourceType.Iron));
        UpdateResourceAmount(ResourceType.RedMedicine, ResourceInventory.Inventory.GetResourceAmount(ResourceType.RedMedicine));
        UpdateResourceAmount(ResourceType.BlueMedicine, ResourceInventory.Inventory.GetResourceAmount(ResourceType.BlueMedicine));
        UpdateResourceAmount(ResourceType.PurpleMedicine, ResourceInventory.Inventory.GetResourceAmount(ResourceType.PurpleMedicine));
        UpdateResourceAmount(ResourceType.GreenMedicine, ResourceInventory.Inventory.GetResourceAmount(ResourceType.GreenMedicine));
    }

    // 자원 개수 정보 표시
    private void UpdateResourceAmount(ResourceType resourceType, int amount)
    {
        switch (resourceType)
        {
            case ResourceType.Iron:
                IronAmountText.text = amount.ToString();
                break;
            case ResourceType.RedMedicine:
                RedMedAmountText.text = amount.ToString();
                break;
            case ResourceType.BlueMedicine:
                BlueMedAmountText.text = amount.ToString();
                break;
            case ResourceType.PurpleMedicine:
                PurpleMedAmountText.text = amount.ToString();
                break;
            case ResourceType.GreenMedicine:
                GreenMedAmountText.text = amount.ToString();
                break;
        }
    }

    // 이벤트(ResourceAmountChanged) 호출 시 자원 개수 업데이트
    public void OnAmountChanged(ResourceType resourceType, int amount)
    {
        UpdateResourceAmount(resourceType, amount);
    }
}
