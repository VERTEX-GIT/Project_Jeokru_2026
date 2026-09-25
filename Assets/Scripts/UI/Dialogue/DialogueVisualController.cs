using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

[DisallowMultipleComponent]
public sealed class DialogueVisualController : MonoBehaviour
{
    [Serializable]
    private sealed class VisualEntry
    {
        [SerializeField]
        private string key;

        [SerializeField]
        private Sprite sprite;

        public string Key =>
            key;

        public Sprite Sprite =>
            sprite;
    }

    [Header("References")]
    [SerializeField]
    private DialogueRunner dialogueRunner;

    [SerializeField]
    private Image backgroundImage;

    [SerializeField]
    private Image characterImage;

    [Header("Backgrounds")]
    [SerializeField]
    private VisualEntry[] backgrounds;

    [Header("Characters")]
    [SerializeField]
    private VisualEntry[] characters;

    private readonly Dictionary<string, Sprite>
        backgroundLookup =
            new(
                StringComparer
                    .OrdinalIgnoreCase);

    private readonly Dictionary<string, Sprite>
        characterLookup =
            new(
                StringComparer
                    .OrdinalIgnoreCase);

    private GameplayInputBlocker
        gameplayInputBlocker;

    private bool dialogueStateInitialized;
    private bool lastDialogueRunning;

    private void Awake()
    {
        ResolveReferences();

        BuildLookup(
            backgrounds,
            backgroundLookup,
            "배경");

        BuildLookup(
            characters,
            characterLookup,
            "캐릭터");

        HideAllVisuals();

        RegisterCommands();

        ResolveGameplayInputBlocker();
    }

    private void Start()
    {
        RefreshDialogueState(
            true);
    }

    private void Update()
    {
        RefreshDialogueState(
            false);
    }

    private void OnDestroy()
    {
        if (dialogueRunner != null)
        {
            dialogueRunner
                .RemoveCommandHandler(
                    "background");

            dialogueRunner
                .RemoveCommandHandler(
                    "character");
        }

        ApplyDialogueBlocking(
            false);
    }

    private void ResolveReferences()
    {
        if (dialogueRunner == null)
        {
            dialogueRunner =
                GetComponent<
                    DialogueRunner>();
        }

        if (dialogueRunner == null)
        {
            dialogueRunner =
                GetComponentInParent<
                    DialogueRunner>();
        }

        if (dialogueRunner == null)
        {
            dialogueRunner =
                FindAnyObjectByType<
                    DialogueRunner>();
        }
    }

    private void ResolveGameplayInputBlocker()
    {
        if (gameplayInputBlocker != null)
        {
            return;
        }

        gameplayInputBlocker =
            FindAnyObjectByType<
                GameplayInputBlocker>();

        if (gameplayInputBlocker == null)
        {
            gameplayInputBlocker =
                gameObject.AddComponent<
                    GameplayInputBlocker>();
        }
    }

    private void RefreshDialogueState(
        bool force)
    {
        bool isDialogueRunning =
            dialogueRunner != null &&
            dialogueRunner
                .IsDialogueRunning;

        if (!force &&
            dialogueStateInitialized &&
            isDialogueRunning ==
            lastDialogueRunning)
        {
            return;
        }

        dialogueStateInitialized = true;
        lastDialogueRunning =
            isDialogueRunning;

        ApplyDialogueBlocking(
            isDialogueRunning);
    }

    private void ApplyDialogueBlocking(
        bool blocked)
    {
        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance
                .SetDialogueBlocking(
                    blocked);
        }

        ResolveGameplayInputBlocker();

        gameplayInputBlocker?
            .SetDialogueBlocked(
                blocked);
    }

    private void RegisterCommands()
    {
        if (dialogueRunner == null)
        {
            Debug.LogError(
                $"{name}: DialogueRunner를 찾을 수 없습니다.",
                this);

            return;
        }

        dialogueRunner
            .AddCommandHandler<string>(
                "background",
                SetBackground);

        dialogueRunner
            .AddCommandHandler<string>(
                "character",
                SetCharacter);
    }

    private void BuildLookup(
        VisualEntry[] entries,
        Dictionary<string, Sprite> lookup,
        string categoryName)
    {
        lookup.Clear();

        if (entries == null)
        {
            return;
        }

        foreach (VisualEntry entry
                 in entries)
        {
            if (entry == null ||
                string.IsNullOrWhiteSpace(
                    entry.Key))
            {
                continue;
            }

            if (entry.Sprite == null)
            {
                Debug.LogWarning(
                    $"{name}: {categoryName} 키 '{entry.Key}'에 Sprite가 없습니다.",
                    this);

                continue;
            }

            string normalizedKey =
                entry.Key.Trim();

            if (!lookup.TryAdd(
                    normalizedKey,
                    entry.Sprite))
            {
                Debug.LogWarning(
                    $"{name}: {categoryName} 키 '{normalizedKey}'가 중복되었습니다.",
                    this);
            }
        }
    }

    private void SetBackground(
        string key)
    {
        SetVisual(
            key,
            backgroundImage,
            backgroundLookup,
            "배경");
    }

    private void SetCharacter(
        string key)
    {
        SetVisual(
            key,
            characterImage,
            characterLookup,
            "캐릭터");
    }

    private void SetVisual(
        string key,
        Image targetImage,
        Dictionary<string, Sprite> lookup,
        string categoryName)
    {
        if (targetImage == null)
        {
            Debug.LogError(
                $"{name}: {categoryName} Image가 연결되지 않았습니다.",
                this);

            return;
        }

        if (string.IsNullOrWhiteSpace(
                key))
        {
            return;
        }

        string normalizedKey =
            key.Trim();

        if (normalizedKey.Equals(
                "none",
                StringComparison
                    .OrdinalIgnoreCase))
        {
            HideVisual(
                targetImage);

            return;
        }

        if (!lookup.TryGetValue(
                normalizedKey,
                out Sprite sprite))
        {
            Debug.LogWarning(
                $"{name}: 등록되지 않은 {categoryName} 키 '{normalizedKey}'입니다.",
                this);

            return;
        }

        targetImage.sprite =
            sprite;

        targetImage.enabled =
            true;
    }

    public void HideAllVisuals()
    {
        HideVisual(
            backgroundImage);

        HideVisual(
            characterImage);
    }

    private static void HideVisual(
        Image targetImage)
    {
        if (targetImage == null)
        {
            return;
        }

        targetImage.sprite =
            null;

        targetImage.enabled =
            false;
    }
}
