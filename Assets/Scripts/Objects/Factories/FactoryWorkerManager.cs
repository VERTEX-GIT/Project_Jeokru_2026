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

    private void Awake()
    {
        if (workArea == null)
        {
            workArea =
                GetComponent<FactoryWorkArea>();
        }

        if (occupancyManager == null)
        {
            occupancyManager =
                TileOccupancyManager.Instance;

            if (occupancyManager == null)
            {
                occupancyManager =
                    FindAnyObjectByType<
                        TileOccupancyManager>();
            }
        }
    }

    /* =< 작업 타일 관련 메서드 >================================================================================ */

    // 점유되지 않은 작업 타일 좌표 반환
    public List<Vector3Int> GetAvailableWorkCells()
    {
        List<Vector3Int> availableCells =
            new();

        if (workArea == null ||
            occupancyManager == null ||
            !workArea.IsRegistered)
        {
            return availableCells;
        }

        // 점유되지 않은 작업 타일 탐색
        foreach (Vector3Int workCell
                 in workArea.WorkCells)
        {
            if (occupancyManager
                .HasOccupant(
                    workCell))
            {
                continue;
            }

            if (occupancyManager
                .IsReserved(
                    workCell))
            {
                continue;
            }

            availableCells.Add(
                workCell);
        }

        return availableCells;
    }

    // 실제로 이 공장에서 작업 중인 아군 유닛 반환
    public List<UnitCore> GetWorkingUnits()
    {
        List<UnitCore> workingUnits =
            new();

        if (workArea == null ||
            occupancyManager == null ||
            !workArea.IsRegistered)
        {
            return workingUnits;
        }

        foreach (Vector3Int workCell
                 in workArea.WorkCells)
        {
            if (!occupancyManager
                    .TryGetOccupant(
                        workCell,
                        out TileObjectPlacement
                            occupant))
            {
                continue;
            }

            if (occupant.ObjectType !=
                TileObjectType.Unit)
            {
                continue;
            }

            UnitCore unitCore =
                occupant.GetComponent<
                    UnitCore>();

            if (unitCore == null ||
                !unitCore.IsGameplayAvailable ||
                unitCore.Data == null ||
                unitCore.Data.Team !=
                    UnitTeam.Ally ||
                unitCore.CurrentTarget !=
                    gameObject)
            {
                continue;
            }

            workingUnits.Add(
                unitCore);
        }

        return workingUnits;
    }

    /* =< 상태 확인 메서드 >================================================================================ */

    // 점유되지 않은 작업 타일 좌표 개수
    public int AvailableWorkCellCount() =>
        GetAvailableWorkCells().Count;

    // 점유되지 않은 작업 타일 좌표 존재 여부
    public bool IsFull() =>
        AvailableWorkCellCount() == 0;

    // 작업 중인 Unit 개수
    public int WorkingUnitCount() =>
        GetWorkingUnits().Count;
}
