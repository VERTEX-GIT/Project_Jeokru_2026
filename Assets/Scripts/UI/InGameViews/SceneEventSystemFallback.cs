using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(EventSystem))]
public sealed class SceneEventSystemFallback : MonoBehaviour
{
    private void Awake()
    {
        if (EventSystem.current != null &&
            EventSystem.current != GetComponent<EventSystem>())
        {
            Destroy(gameObject);
        }
    }
}
