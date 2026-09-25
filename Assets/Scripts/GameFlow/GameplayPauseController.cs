using System;

public static class GameplayPauseController
{
    [Flags]
    public enum PauseReason
    {
        None = 0,
        ManualPause = 1 << 0,
        Dialogue = 1 << 1,
        GameOver = 1 << 2
    }

    private static PauseReason activeReasons =
        PauseReason.None;

    public static bool IsPaused =>
        activeReasons !=
        PauseReason.None;

    public static PauseReason ActiveReasons =>
        activeReasons;

    public static event Action<PauseReason>
        PauseStateChanged;

    public static bool HasReason(
        PauseReason reason)
    {
        return
            (activeReasons & reason) !=
            PauseReason.None;
    }

    public static void SetPaused(
        PauseReason reason,
        bool paused)
    {
        if (reason ==
            PauseReason.None)
        {
            return;
        }

        PauseReason previousReasons =
            activeReasons;

        if (paused)
        {
            activeReasons |=
                reason;
        }
        else
        {
            activeReasons &=
                ~reason;
        }

        if (previousReasons ==
            activeReasons)
        {
            return;
        }

        PauseStateChanged?.Invoke(
            activeReasons);
    }

    public static void Clear()
    {
        if (activeReasons ==
            PauseReason.None)
        {
            return;
        }

        activeReasons =
            PauseReason.None;

        PauseStateChanged?.Invoke(
            activeReasons);
    }
}
