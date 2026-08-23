using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(FactoryWorkArea))]

public sealed class FactoryWorkerManager : MonoBehaviour
{
    /* =< 변수 >============================================================================================== */

    // FactoryWorkArea 컴포넌트
    private FactoryWorkArea workArea;
    // TileOccupancyManager 컴포넌트
    private TileOccupancyManager occupancyManager;

    /* =< 기본 메서드 >======================================================================================== */

    void Awake()
    {
        if (workArea == null)
        {
            workArea = GetComponent<FactoryWorkArea>();
        }

        if (occupancyManager == null)
        {
            occupancyManager = TileOccupancyManager.Instance;

            if (occupancyManager == null)
            {
                occupancyManager = FindAnyObjectByType<TileOccupancyManager>();
            }
        }
    }

    /* =< 작업 타일 관련 메서드 >================================================================================ */

    // 점유되지 않은 작업 타일 좌표 반환
    public List<Vector3Int> GetAvailableWorkCells()
    {
        List<Vector3Int> availableCells = new();

        if (workArea == null || occupancyManager == null || !workArea.IsRegistered)
        {
            return availableCells;
        }

        // 점유되지 않은 작업 타일 탐색
        foreach (Vector3Int workCell in workArea.WorkCells)
        {
            if (occupancyManager.HasOccupant(workCell))
            {
                continue;
            }

            if (occupancyManager.IsReserved(workCell))
            {
                continue;
            }

            availableCells.Add(workCell);
        }

        return availableCells;
    }

    // 작업 중인 UnitCore 리스트 반환
    public List<UnitCore> GetWorkingUnits()
    {
        List<UnitCore> workingUnits = new();

        if (workArea == null || occupancyManager == null || !workArea.IsRegistered)
        {
            return workingUnits;
        }

        foreach (Vector3Int workCell in workArea.WorkCells)
        {
            if (occupancyManager.TryGetOccupant(workCell, out TileObjectPlacement occupant))
            {
                // 타일 점유 오브젝트가 Unit인지 확인
                /* <!> 나중에 피아 구분도 해야할 수 있음 */
                if (occupant.ObjectType != TileObjectType.Unit)
                {
                    continue;
                }

                UnitCore unitCore = occupant.GetComponent<UnitCore>();

                /*
                ==============================================================================
                <if문 확인 내용>
                - Unit이 활성화되어 있는가?
                - Unit의 CurrentTarget이 이 FactoryWorkArea를 소유한 Factory인가?
                - 해당 공장의 실제 작업 타일을 점유하고 있는가?
                ==============================================================================
                */
                if (unitCore == null || !unitCore.IsActive || unitCore.CurrentTarget != gameObject)
                {
                    continue;
                }

                workingUnits.Add(unitCore);
            }
        }

        return workingUnits;
    }

    /* =< 상태 확인 메서드 >================================================================================ */

    // 점유되지 않은 작업 타일 좌표 개수
    public int AvailableWorkCellCount() => GetAvailableWorkCells().Count;
    // 점유되지 않은 작업 타일 좌표 존재 여부
    public bool IsFull() => AvailableWorkCellCount() == 0;

    // 작업 중인 Unit 개수
    public int WorkingUnitCount() => GetWorkingUnits().Count;
}
