using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TileObjectPlacement))]
public sealed class CounselingRoom :
    MonoBehaviour
{
    [Header("Placement")]
    [SerializeField]
    private Vector3Int anchorCell;

    [Header("Processing")]
    [SerializeField]
    private Transform processingPoint;

    private readonly HashSet<UnitCounseling>
        counselingUnits = new();

    private TileObjectPlacement placement;

    public int CurrentCounselingCount =>
        counselingUnits.Count;

    public Vector3 ProcessingPosition
    {
        get
        {
            if (processingPoint != null)
            {
                return processingPoint.position;
            }

            return transform.position;
        }
    }

    public event Action<int>
        CounselingCountChanged;

    private void Awake()
    {
        placement =
            GetComponent<
                TileObjectPlacement>();
    }

    private void Start()
    {
        RegisterFixedPlacement();

        NotifyCountChanged();
    }

    private void RegisterFixedPlacement()
    {
        if (placement == null)
        {
            Debug.LogError(
                $"{name}: TileObjectPlacement가 없습니다.",
                this);

            return;
        }

        if (placement.IsPlaced)
        {
            return;
        }

        if (!placement.TryPlace(
                anchorCell))
        {
            Debug.LogError(
                $"{name}: 상담실을 " +
                $"{anchorCell}에 배치하지 못했습니다.",
                this);
        }
    }

    public void Register(
        UnitCounseling counseling)
    {
        if (counseling == null)
        {
            return;
        }

        if (!counselingUnits.Add(
                counseling))
        {
            return;
        }

        NotifyCountChanged();
    }

    public void Unregister(
        UnitCounseling counseling)
    {
        if (counseling == null)
        {
            return;
        }

        if (!counselingUnits.Remove(
                counseling))
        {
            return;
        }

        NotifyCountChanged();
    }

    private void NotifyCountChanged()
    {
        CounselingCountChanged?.Invoke(
            CurrentCounselingCount);
    }

    private void OnDisable()
    {
        counselingUnits.Clear();

        NotifyCountChanged();
    }
}