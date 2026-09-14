using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MedicineCursorView : MonoBehaviour
{
    private readonly Dictionary<ResourceType, Sprite> sprites = new();
    private MedicineUseController controller;
    private Canvas canvas;
    private Image icon;
    private RectTransform canvasRect;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    // 처음 실행하거나 인게임에 다시 들어올 때 자동 연결합니다.
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var controller = MedicineUseController.Instance;
        if (controller == null || FindAnyObjectByType<MedicineCursorView>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        MedicineCursorView view = null;
        foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentTarget(i) != controller || button.image == null)
                {
                    continue;
                }

                ResourceType? medicine = GetMedicine(button.onClick.GetPersistentMethodName(i));
                var parentCanvas = button.GetComponentInParent<Canvas>(true);
                if (!medicine.HasValue || parentCanvas == null)
                {
                    continue;
                }

                if (view == null)
                {
                    var cursor = new GameObject("MedicineCursorIcon", typeof(RectTransform), typeof(Image));
                    cursor.layer = parentCanvas.gameObject.layer;
                    cursor.transform.SetParent(parentCanvas.rootCanvas.transform, false);
                    // 관리 스크립트는 켜 두어야 비활성 아이콘을 다시 켤 수 있습니다.
                    view = parentCanvas.rootCanvas.gameObject.AddComponent<MedicineCursorView>();
                    view.controller = controller;
                    view.canvas = parentCanvas.rootCanvas;
                    view.canvasRect = (RectTransform)view.canvas.transform;
                    view.icon = cursor.GetComponent<Image>();
                    view.icon.raycastTarget = false;
                    view.icon.preserveAspect = true;
                    view.icon.rectTransform.anchorMin = view.canvasRect.pivot;
                    view.icon.rectTransform.anchorMax = view.canvasRect.pivot;
                    view.icon.rectTransform.sizeDelta = new Vector2(32f, 32f);
                    cursor.SetActive(false);
                }

                view.sprites[medicine.Value] = button.image.overrideSprite;
            }
        }
    }

    private void LateUpdate()
    {
        if (icon == null) return;

        ResourceType? selected = controller != null ? controller.SelectedMedicine : null;
        bool visible = selected.HasValue && Mouse.current != null && Application.isFocused &&
            !PauseMenu.IsPaused && sprites.TryGetValue(selected.Value, out var sprite) && sprite != null;
        if (!visible)
        {
            icon.gameObject.SetActive(false);
            return;
        }

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (!new Rect(0f, 0f, Screen.width, Screen.height).Contains(screenPosition) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, camera, out var local))
        {
            icon.gameObject.SetActive(false);
            return;
        }

        icon.sprite = sprites[selected.Value];
        icon.rectTransform.anchoredPosition = GetIconPosition(local, canvasRect.rect);
        icon.transform.SetAsLastSibling();
        icon.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (icon != null) icon.gameObject.SetActive(false);
    }

    private static ResourceType? GetMedicine(string method) => method switch
    {
        nameof(MedicineUseController.SelectRedMedicine) => ResourceType.RedMedicine,
        nameof(MedicineUseController.SelectBlueMedicine) => ResourceType.BlueMedicine,
        nameof(MedicineUseController.SelectPurpleMedicine) => ResourceType.PurpleMedicine,
        nameof(MedicineUseController.SelectGreenMedicine) => ResourceType.GreenMedicine,
        _ => null
    };

    private static Vector2 GetIconPosition(Vector2 pointer, Rect bounds) => new(
        Mathf.Clamp(pointer.x + 24f, bounds.xMin + 16f, bounds.xMax - 16f),
        Mathf.Clamp(pointer.y - 24f, bounds.yMin + 16f, bounds.yMax - 16f));

#if UNITY_EDITOR
    [ContextMenu("Check Medicine Cursor")]
    private void CheckCursor()
    {
        var bounds = new Rect(-400f, -300f, 800f, 600f);
        bool passed = GetMedicine("SelectRedMedicine") == ResourceType.RedMedicine &&
            GetMedicine("SelectBlueMedicine") == ResourceType.BlueMedicine &&
            GetMedicine("SelectPurpleMedicine") == ResourceType.PurpleMedicine &&
            GetMedicine("SelectGreenMedicine") == ResourceType.GreenMedicine &&
            GetMedicine("SelectFactory") == null &&
            GetIconPosition(Vector2.zero, bounds) == new Vector2(24f, -24f) &&
            GetIconPosition(new Vector2(400f, -300f), bounds) == new Vector2(384f, -284f);
        if (!passed) throw new System.Exception("Medicine cursor check failed");
        Debug.Log("MEDICINE_CURSOR_CHECK PASS: four icons, offset and screen-edge clamp");
    }
#endif
}
