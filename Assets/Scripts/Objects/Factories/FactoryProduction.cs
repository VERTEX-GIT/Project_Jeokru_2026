using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(FactoryCore))]
[RequireComponent(typeof(FactoryWorkerManager))]
[RequireComponent(typeof(FactoryHealth))]

public class FactoryProduction : MonoBehaviour
{
    /* =< 변수 >============================================================================================== */
    
    // 최소 생산 시간 (초)
    private const float MinProductionTime = 1f;

    private FactoryCore factoryCore;
    private FactoryWorkerManager workerManager;
    private FactoryHealth factoryHealth;
    private ResourceInventory resourceInventory;

    // 생산 진행 시간 (초)
    private float productionProgress;
    // 한 번 생산 시 요구되는 시간
    private float productionTime;
    // 이전 프레임에서 확인된 작업자 수
    private int workerUnitCount;
    // 생산 사이클 활성화 여부
    private bool isProductionCycleActive;

    /* -------<공개 값>----------------------------------------------------*/
    public float ProductionProgress => productionProgress;
    public float CalculatedProductionTime => productionTime;
    public int WorkingUnitCount => workerUnitCount;

    // 생산 진행률
    public float ProductionProgressRate => productionTime > 0f ? productionProgress / productionTime : 0f;
    // 생산 중 여부
    public bool IsProducing => factoryHealth != null && factoryHealth.IsAlive && workerUnitCount > 0 && isProductionCycleActive;

    /* =< 기본 메서드 >======================================================================================== */

    private void Awake()
    {
        factoryCore = GetComponent<FactoryCore>();
        workerManager = GetComponent<FactoryWorkerManager>();
        factoryHealth = GetComponent<FactoryHealth>();

        resourceInventory = ResourceInventory.Inventory;

        productionTime = CalculateProductionTime(0);
    }

    // 실시간 워커 수 변화 감지 및 처리
    private void Update()
    {
        int curWorkerCount = workerManager.WorkingUnitCount();

        // 워커 수에 변화가 감지되었을 때
        if (workerUnitCount != curWorkerCount)
        {
            SyncProductionProgress(curWorkerCount);
        }

        // 공장 파괴 시 생산 진행도 초기화
        if (factoryHealth.IsDestroyed)
        {
            productionProgress = 0f;
            isProductionCycleActive = false;
            return;
        }

        // 워커 수가 0일 경우
        if (workerUnitCount <= 0)
        {
            return;
        }

        if (!isProductionCycleActive && !TryStartProduction())
        {
            return;
        }

        // 생산 진행도 증가
        productionProgress += Time.deltaTime;

        // 생산 시간 경과 시
        if (productionProgress >= productionTime)
        {
            CompleteProduction();
        }
    }

    /* =< 생산 관련 메서드 >==================================================================================== */

    // 생산 시작 시 재료 지불
    private bool TryStartProduction()
    {
        if (resourceInventory == null)
        {
            resourceInventory = ResourceInventory.Inventory;
        }

        if (resourceInventory == null)
        {
            return false;
        }

        IReadOnlyList<ResourceCost> costs = factoryCore.Definition.ProductionCosts;

        // 빈 리스트는 무료 생산
        if (costs.Count > 0 && !resourceInventory.Spend(costs, false))
        {
            return false;
        }

        isProductionCycleActive = true;

        return true;
    }


    // 생산 시간 계산
    private float CalculateProductionTime(float workerCount)
    {
        float productionTime = factoryCore.Definition.BaseProductionTime - workerCount * 2f;

        return Mathf.Max(MinProductionTime, productionTime);
    }

    // 생산 진행도 비율 유지
    private void SyncProductionProgress(int newWorkerCount)
    {
        if (workerUnitCount == newWorkerCount)
        {
            return;
        }

        float progressRate = productionTime > 0f ? productionProgress / productionTime : 0f; // 0으로 나누는 상황 방지

        workerUnitCount = newWorkerCount;
        productionTime = CalculateProductionTime(workerUnitCount);

        productionProgress = Mathf.Floor(productionTime * progressRate);    // Mathf.Floor: 소수점 버림
    }

    // 생산 완료 시 처리
    private void CompleteProduction()
    {
        // 인벤토리 자원 추가
        resourceInventory.Add(factoryCore.Definition.ProductionType, factoryCore.Definition.ProductionAmount);

        productionProgress -= productionTime;   // ~~progress = 0f; 를 하면 함수 실행 후 추가적으로 더해진 시간 날아감
        isProductionCycleActive = false;

        // 다음 생산 재료가 부족하면 진행도 0에서 대기
        if (!TryStartProduction())
        {
            productionProgress = 0f;
        }
    }
}
