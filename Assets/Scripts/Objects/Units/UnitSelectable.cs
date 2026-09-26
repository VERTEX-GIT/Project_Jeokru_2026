using UnityEngine;

// 배치된 유닛의 선택 상태와 외곽선 표시를 관리
[DisallowMultipleComponent]
[RequireComponent(
    typeof(TileObjectPlacement),
    typeof(UnitCore),
    typeof(SpriteRenderer))]
public sealed class UnitSelectable : MonoBehaviour
{
    [Header("Selection Outline")]
    [SerializeField]
    private Material selectionOutlineMaterial;

    [SerializeField]
    private Color outlineColor =
        new Color(
            0f,
            0.654902f,
            0.87451f,
            1f);

    [SerializeField]
    [Min(0f)]
    private float outlineThickness = 0.005f;

    [SerializeField]
    [Range(0f, 1f)]
    private float highlightIntensity = 1f;

    [Header("Legacy")]
    [SerializeField]
    private GameObject selectionIndicator;

    public bool IsSelected
    {
        get;
        private set;
    }

    private static readonly int MainTexId =
        Shader.PropertyToID(
            "_MainTex");

    private static readonly int OutlineColorId =
        Shader.PropertyToID(
            "_OutlineColor");

    private static readonly int ThicknessId =
        Shader.PropertyToID(
            "_Thickness");

    private static readonly int HighlightIntensityId =
        Shader.PropertyToID(
            "_HighlightIntensity");

    private TileObjectPlacement placement;
    private UnitCore unitCore;
    private SpriteRenderer spriteRenderer;
    private Material originalMaterial;
    private MaterialPropertyBlock propertyBlock;
    private Texture lastSelectionTexture;

    // 필요한 컴포넌트와 원래 머티리얼을 저장하고 선택 상태를 초기화합니다.
    private void Awake()
    {
        placement =
            GetComponent<TileObjectPlacement>();

        unitCore =
            GetComponent<UnitCore>();

        spriteRenderer =
            GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalMaterial =
                spriteRenderer.sharedMaterial;
        }

        propertyBlock =
            new MaterialPropertyBlock();

        IsSelected =
            false;

        // 기존 타일 선택 표시는 더 이상 사용하지 않습니다.
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(
                false);
        }

        RestoreOriginalVisual();
    }

    // 애니메이션으로 스프라이트 텍스처가 변경되면 선택 머티리얼에도 반영합니다.
    private void LateUpdate()
    {
        if (!IsSelected ||
            spriteRenderer == null ||
            spriteRenderer.sprite == null)
        {
            return;
        }

        Texture currentTexture =
            spriteRenderer.sprite.texture;

        if (currentTexture ==
            lastSelectionTexture)
        {
            return;
        }

        RefreshSelectionProperties();
    }

    // 타일에 정상 배치된 아군 유닛인지 확인합니다.
    public bool CanSelect()
    {
        return placement != null &&
            placement.ObjectType ==
                TileObjectType.Unit &&
            unitCore != null &&
            unitCore.IsActive &&
            unitCore.Data != null &&
            unitCore.Data.Team ==
                UnitTeam.Ally;
    }

    // 선택 가능한 유닛을 선택 상태로 변경하고 외곽선을 표시합니다.
    public void Select()
    {
        if (IsSelected ||
            !CanSelect())
        {
            return;
        }

        IsSelected =
            true;

        ApplySelectionOutline();
    }

    // 선택 상태를 해제하고 원래 렌더링 상태로 복원합니다.
    public void Deselect()
    {
        if (!IsSelected)
        {
            return;
        }

        IsSelected =
            false;

        RestoreOriginalVisual();
    }

    // 비활성화된 유닛이 선택 상태로 남지 않도록 표시를 복원합니다.
    private void OnDisable()
    {
        IsSelected =
            false;

        RestoreOriginalVisual();
    }

    // 선택용 Shader Graph 머티리얼을 적용합니다.
    private void ApplySelectionOutline()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (selectionOutlineMaterial == null)
        {
            Debug.LogWarning(
                $"{name}: 선택 외곽선 Material이 지정되지 않았습니다.",
                this);

            return;
        }

        spriteRenderer.sharedMaterial =
            selectionOutlineMaterial;

        RefreshSelectionProperties();
    }

    // 현재 스프라이트 텍스처와 외곽선 값을 MaterialPropertyBlock에 반영합니다.
    private void RefreshSelectionProperties()
    {
        if (spriteRenderer == null ||
            spriteRenderer.sprite == null ||
            propertyBlock == null)
        {
            return;
        }

        Texture currentTexture =
            spriteRenderer.sprite.texture;

        spriteRenderer.GetPropertyBlock(
            propertyBlock);

        propertyBlock.SetTexture(
            MainTexId,
            currentTexture);

        propertyBlock.SetColor(
            OutlineColorId,
            outlineColor);

        propertyBlock.SetFloat(
            ThicknessId,
            outlineThickness);

        propertyBlock.SetFloat(
            HighlightIntensityId,
            highlightIntensity);

        spriteRenderer.SetPropertyBlock(
            propertyBlock);

        lastSelectionTexture =
            currentTexture;
    }

    // 선택 이전의 머티리얼과 프로퍼티 상태로 복원합니다.
    private void RestoreOriginalVisual()
    {
        lastSelectionTexture =
            null;

        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sharedMaterial =
            originalMaterial;

        if (propertyBlock == null)
        {
            return;
        }

        propertyBlock.Clear();

        spriteRenderer.SetPropertyBlock(
            propertyBlock);
    }
}
