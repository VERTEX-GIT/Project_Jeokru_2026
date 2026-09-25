using UnityEngine;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class InGameLoadController :
    MonoBehaviour
{
    [SerializeField]
    private GameTimeManager gameTimeManager;

    public bool IsInitialized
    {
        get;
        private set;
    }

    public bool WasLoadedFromSave
    {
        get;
        private set;
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        InitializeGame();
    }

    private void ResolveReferences()
    {
        if (gameTimeManager == null)
        {
            gameTimeManager =
                GameTimeManager.Instance;
        }

        if (gameTimeManager == null)
        {
            gameTimeManager =
                FindAnyObjectByType<
                    GameTimeManager>();
        }
    }

    private void InitializeGame()
    {
        if (IsInitialized)
        {
            return;
        }

        ResolveReferences();

        if (gameTimeManager == null)
        {
            Debug.LogError(
                "InGameLoadController: " +
                "GameTimeManager를 찾을 수 없습니다.",
                this);

            IsInitialized = true;
            return;
        }

        if (GameSession.StartMode ==
            GameStartMode.Continue)
        {
            LoadSavedGame();
        }

        IsInitialized = true;
    }

    private void LoadSavedGame()
    {
        if (!SaveManager.HasSave)
        {
            Debug.LogWarning(
                "InGameLoadController: " +
                "이어하기로 진입했지만 세이브 파일이 없습니다. " +
                "새 게임 상태로 시작합니다.",
                this);

            GameSession.StartNewGame();
            return;
        }

        if (!SaveManager.TryLoad(
                gameTimeManager))
        {
            Debug.LogError(
                "InGameLoadController: " +
                "세이브 데이터를 복원하지 못했습니다. " +
                "새 게임 상태로 시작합니다.",
                this);

            GameSession.StartNewGame();
            return;
        }

        WasLoadedFromSave =
            true;
    }
}
