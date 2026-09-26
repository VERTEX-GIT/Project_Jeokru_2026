using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class DebugUnitShortcuts : MonoBehaviour
{
    [SerializeField] private UnitData allyData;
    [SerializeField] private UnitData enemyData;

    private UnitData pendingPlacement;
    private bool accelerating;
    private float speed = 2f;

    private void Update()
    {
        var keys = Keyboard.current;
        if (keys == null) return;

        bool fast = keys.fKey.isPressed && keys.aKey.isPressed;
        if (fast && !accelerating) speed = 2f;
        if (fast && keys.leftArrowKey.wasPressedThisFrame) speed = Mathf.Max(0.5f, speed - 0.5f);
        if (fast && keys.rightArrowKey.wasPressedThisFrame) speed += 0.5f;
        if (!GameplayPauseController.IsPaused)
        {
            if (fast) Time.timeScale = speed;
            else if (accelerating) Time.timeScale = 1f;
        }
        accelerating = fast;

        if (GameplayPauseController.IsPaused) return;
        bool one = keys.digit1Key.isPressed;
        if (one && (keys.digit1Key.wasPressedThisFrame || keys.qKey.wasPressedThisFrame) && keys.qKey.isPressed)
            pendingPlacement = allyData;
        else if (one && (keys.digit1Key.wasPressedThisFrame || keys.wKey.wasPressedThisFrame) && keys.wKey.isPressed)
            pendingPlacement = enemyData;
        if (keys.escapeKey.wasPressedThisFrame || Mouse.current?.rightButton.wasPressedThisFrame == true)
            pendingPlacement = null;
        if (Mouse.current?.leftButton.wasPressedThisFrame != true ||
            (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return;

        var camera = Camera.main;
        if (camera == null) return;
        Vector2 screen = Mouse.current.position.ReadValue();
        Vector3 world = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -camera.transform.position.z));
        world.z = 0f;

        if (pendingPlacement != null)
        {
            Place(pendingPlacement, world);
            pendingPlacement = null;
            return;
        }

        bool stress = keys.sKey.isPressed && keys.tKey.isPressed;
        bool kill = keys.dKey.isPressed && keys.eKey.isPressed;
        if (!stress && !kill) return;
        foreach (var hit in Physics2D.OverlapPointAll(world))
        {
            var core = hit.GetComponentInParent<UnitCore>();
            if (core == null || core.Data == null) continue;
            if (stress && core.Data.Team == UnitTeam.Ally && core.TryGetComponent(out UnitStress unitStress))
            {
                unitStress.SetStress(UnitStress.MaxStress);
                break;
            }
            if (kill && core.TryGetComponent(out UnitHealth health) && health.IsAlive)
            {
                health.TakeDamage(health.CurrentHp + core.Defense, null);
                break;
            }
        }
    }

    private static void Place(UnitData data, Vector3 world)
    {
        var coordinates = TileOccupancyManager.Instance?.CoordinateManager;
        if (coordinates == null || data.UnitPrefab == null) return;
        Vector3Int cell = coordinates.WorldToCell(world);
        if (!coordinates.HasTile(cell)) return;
        var unit = Instantiate(data.UnitPrefab, coordinates.CellToWorldCenter(cell), Quaternion.identity);
        var placement = unit.GetComponent<TileObjectPlacement>();
        if (placement == null || !placement.TryPlace(cell))
        {
            Destroy(unit);
            return;
        }
        unit.GetComponent<UnitCore>()?.SetData(data);
    }

    private void OnDestroy()
    {
        if (accelerating && !GameplayPauseController.IsPaused) Time.timeScale = 1f;
    }
}
