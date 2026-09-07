using UnityEngine;

[DisallowMultipleComponent]
public sealed class AppRoot : MonoBehaviour
{
    public static AppRoot Instance
    {
        get;
        private set;
    }

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(
            gameObject);
    }
}