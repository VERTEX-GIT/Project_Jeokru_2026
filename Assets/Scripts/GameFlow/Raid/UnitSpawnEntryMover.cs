using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class UnitSpawnEntryMover : MonoBehaviour
{
    private Vector3Int entryCell;
    private TileOccupancyManager occupancyManager;
    private TileObjectPlacement placement;
    private UnitCore unitCore;
    private bool isEntering;
    private bool activateOnEntry = true;

    public event Action EntryCompleted;

    public void Initialize(
        Vector3Int targetEntryCell,
        TileOccupancyManager manager,
        bool shouldActivateOnEntry = true)
    {
        entryCell = targetEntryCell;
        occupancyManager = manager;
        activateOnEntry = shouldActivateOnEntry;

        placement = GetComponent<TileObjectPlacement>();
        unitCore = GetComponent<UnitCore>();

        if (placement == null ||
            unitCore == null ||
            occupancyManager == null ||
            occupancyManager.CoordinateManager == null)
        {
            enabled = false;
            return;
        }

        unitCore.SetUnitActive(false);

        isEntering = true;
        enabled = true;
    }

    private void Update()
    {
        if (!isEntering || PauseMenu.IsPaused)
        {
            return;
        }

        MoveTowardEntry();
    }

    private void OnDestroy()
    {
        if (isEntering &&
            occupancyManager != null &&
            placement != null)
        {
            occupancyManager.ReleaseReservation(
                entryCell,
                placement);
        }
    }

    private void MoveTowardEntry()
    {
        Vector3 targetPosition =
            occupancyManager.CoordinateManager
                .CellToWorldCenter(entryCell);

        targetPosition.z = transform.position.z;

        float moveSpeed =
            unitCore.Data != null
                ? unitCore.Data.MoveSpeed
                : 0f;

        transform.position =
            Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime);

        if (transform.position == targetPosition)
        {
            CompleteEntry();
        }
    }

    private void CompleteEntry()
    {
        occupancyManager.ReleaseReservation(
            entryCell,
            placement);

        if (!placement.TryPlace(entryCell))
        {
            Debug.LogError(
                $"{name}: 생성 진입 타일 {entryCell} 배치 실패.",
                this);

            Destroy(gameObject);
            return;
        }

        isEntering = false;

        if (activateOnEntry)
        {
            unitCore.SetUnitActive(true);
            unitCore.SetAutoCombat(true);
        }

        EntryCompleted?.Invoke();

        enabled = false;
    }
}
