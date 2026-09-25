using UnityEngine;

[DisallowMultipleComponent]
public sealed class AutoSaveController :
    MonoBehaviour
{
    [SerializeField]
    private GameTimeManager gameTimeManager;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (gameTimeManager == null)
        {
            return;
        }

        gameTimeManager
            .DayCheckpointReached +=
            HandleDayCheckpointReached;

        gameTimeManager
            .GameFinished +=
            HandleGameFinished;
    }

    private void OnDisable()
    {
        if (gameTimeManager == null)
        {
            return;
        }

        gameTimeManager
            .DayCheckpointReached -=
            HandleDayCheckpointReached;

        gameTimeManager
            .GameFinished -=
            HandleGameFinished;
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

    private void HandleDayCheckpointReached(
        int day)
    {
        if (gameTimeManager == null ||
            gameTimeManager.IsGameOver)
        {
            return;
        }

        if (!SaveManager.Save(
                gameTimeManager))
        {
            Debug.LogError(
                $"AutoSaveController: " +
                $"{day}일차 체크포인트 저장에 실패했습니다.",
                this);

            return;
        }

        Debug.Log(
            $"AutoSaveController: " +
            $"{day}일차 체크포인트 저장 완료.",
            this);
    }

    private void HandleGameFinished(
        bool victory)
    {
        if (!SaveManager.DeleteSave())
        {
            Debug.LogError(
                "AutoSaveController: " +
                "게임 종료 후 진행 세이브를 삭제하지 못했습니다.",
                this);

            return;
        }

        Debug.Log(
            "AutoSaveController: " +
            $"게임 {(victory ? "승리" : "패배")}로 " +
            "진행 세이브를 삭제했습니다.",
            this);
    }
}
