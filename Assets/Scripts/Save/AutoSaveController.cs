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

        if (gameTimeManager != null)
        {
            gameTimeManager
                .DayCheckpointReached +=
                HandleDayCheckpointReached;
        }
    }

    private void OnDisable()
    {
        if (gameTimeManager != null)
        {
            gameTimeManager
                .DayCheckpointReached -=
                HandleDayCheckpointReached;
        }
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
}
