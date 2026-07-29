using System.Collections.Generic;
using UnityEngine;

// 유닛과 공장의 타일 점유 범위를 색상 셀로 표시
[DisallowMultipleComponent]
public sealed class PlacementPreview : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private TileCoordinateManager coordinateManager;
    [SerializeField]
    private GameObject previewCellPrefab;

    [Header("Preview Colors")]
    [SerializeField]
    private Color validPreviewColor = // 초록색 : 배치 가능
        new Color(0f, 1f, 0f, 0.5f);
    
    [SerializeField]
    private Color invalidPreviewColor = // 빨간색 : 배치 불가능
        new Color(1f, 0f, 0f, 0.5f);
    
    [SerializeField]
    private Color factoryWorkAreaColor = // 주황색 : 공장 작업 영역
        new Color(1f, 0.5f, 0f, 0.5f);

    private readonly List<SpriteRenderer> factoryBodyCells = new();
    private readonly List<SpriteRenderer> factoryWorkCells = new();

    private SpriteRenderer unitCell;

    // 재사용할 미리보기 셀을 생성하고 초기 상태에서 숨김
    private void Awake()
    {
        CreatePreviewObjects();
        Hide();
    }

    // 유닛의 1×1 배치 셀을 유효성 색상으로 표시
    public void ShowUnit(Vector3Int cell, bool isValid)
    {
        if (unitCell == null || coordinateManager == null)
        {
            return;
        }

        SetCellPosition(unitCell, cell);

        unitCell.color = isValid
            ? validPreviewColor
            : invalidPreviewColor;

        unitCell.gameObject.SetActive(true);

        SetListActive(factoryBodyCells, false);
        SetListActive(factoryWorkCells, false);
    }

    // 공장 본체 3×3과 주변 작업 영역 16칸을 표시
    public void ShowFactory(
        Vector3Int anchorCell,
        bool isValid)
    {
        if (coordinateManager == null ||
            factoryBodyCells.Count != 9 ||
            factoryWorkCells.Count != 16)
        {
            return;
        }

        if (unitCell != null)
        {
            unitCell.gameObject.SetActive(false);
        }

        Color bodyColor = isValid
            ? validPreviewColor
            : invalidPreviewColor;

        int bodyIndex = 0;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int cell =
                    anchorCell + new Vector3Int(x, y, 0);

                SpriteRenderer preview =
                    factoryBodyCells[bodyIndex];

                SetCellPosition(preview, cell);
                preview.color = bodyColor;
                preview.gameObject.SetActive(true);

                bodyIndex++;
            }
        }

        int workIndex = 0;

        for (int x = -2; x <= 2; x++)
        {
            for (int y = -2; y <= 2; y++)
            {
                bool isFactoryBody =
                    Mathf.Abs(x) <= 1 &&
                    Mathf.Abs(y) <= 1;

                if (isFactoryBody)
                {
                    continue;
                }

                Vector3Int cell =
                    anchorCell + new Vector3Int(x, y, 0);

                SpriteRenderer preview =
                    factoryWorkCells[workIndex];

                SetCellPosition(preview, cell);
                preview.color = factoryWorkAreaColor;
                preview.gameObject.SetActive(true);

                workIndex++;
            }
        }
    }

    // 생성된 모든 미리보기 셀 숨김
    public void Hide()
    {
        if (unitCell != null)
        {
            unitCell.gameObject.SetActive(false);
        }

        SetListActive(factoryBodyCells, false);
        SetListActive(factoryWorkCells, false);
    }

    // 유닛 1칸, 공장 본체 9칸, 작업 영역 16칸을 미리 생성
    private void CreatePreviewObjects()
    {
        if (previewCellPrefab == null)
        {
            Debug.LogError(
                $"{name}: Preview Cell Prefab이 연결되지 않았습니다.",
                this);

            return;
        }

        unitCell = CreatePreviewCell("Unit Preview Cell");

        for (int i = 0; i < 9; i++)
        {
            factoryBodyCells.Add(
                CreatePreviewCell($"Factory Body Preview {i}"));
        }

        for (int i = 0; i < 16; i++)
        {
            factoryWorkCells.Add(
                CreatePreviewCell($"Factory Work Preview {i}"));
        }
    }

    // 미리보기 프리팹을 자식으로 생성하고 SpriteRenderer 반환
    private SpriteRenderer CreatePreviewCell(string objectName)
    {
        GameObject previewObject =
            Instantiate(previewCellPrefab, transform);

        previewObject.name = objectName;

        if (!previewObject.TryGetComponent(
                out SpriteRenderer spriteRenderer))
        {
            Debug.LogError(
                $"{previewCellPrefab.name}에 SpriteRenderer가 없습니다.",
                previewCellPrefab);

            previewObject.SetActive(false);
            return null;
        }

        previewObject.SetActive(false);
        return spriteRenderer;
    }

    // 셀 좌표를 월드 중심 좌표로 변환해 미리보기 이동
    private void SetCellPosition(
        SpriteRenderer preview,
        Vector3Int cell)
    {
        if (preview == null)
        {
            return;
        }

        Vector3 worldPosition =
            coordinateManager.CellToWorldCenter(cell);

        worldPosition.z = transform.position.z;
        preview.transform.position = worldPosition;
    }

    // 미리보기 목록의 활성 상태를 일괄 변경
    private static void SetListActive(
        List<SpriteRenderer> previews,
        bool isActive)
    {
        foreach (SpriteRenderer preview in previews)
        {
            if (preview != null)
            {
                preview.gameObject.SetActive(isActive);
            }
        }
    }
}
