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

        public string Key => key;
        public Sprite Sprite => sprite;
    }

    [Serializable]
    private sealed class CharacterEntry
    {
        [SerializeField]
        private string key;

        [SerializeField]
        private Sprite sprite;

        [Header("Default Size")]
        [SerializeField]
        private bool overrideHeight;

        [SerializeField, Min(0f)]
        private float height = 900f;

        public string Key => key;
        public Sprite Sprite => sprite;
        public bool OverrideHeight => overrideHeight;
        public float Height => height;
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
    private CharacterEntry[] characters;

    private readonly Dictionary<string, Sprite>
        backgroundLookup =
            new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, CharacterEntry>
        characterLookup =
            new(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        ResolveReferences();

        BuildBackgroundLookup();
        BuildCharacterLookup();

        HideAllVisuals();

        RegisterCommands();
    }

    private void OnDestroy()
    {
        if (dialogueRunner == null)
        {
            return;
        }

        dialogueRunner.RemoveCommandHandler(
            "background");

        dialogueRunner.RemoveCommandHandler(
            "character");

        dialogueRunner.RemoveCommandHandler(
            "character_position");

        dialogueRunner.RemoveCommandHandler(
            "character_height");
    }

    private void ResolveReferences()
    {
        if (dialogueRunner == null)
        {
            dialogueRunner =
                GetComponent<DialogueRunner>();
        }

        if (dialogueRunner == null)
        {
            dialogueRunner =
                GetComponentInParent<DialogueRunner>();
        }

        if (dialogueRunner == null)
        {
            dialogueRunner =
                FindAnyObjectByType<DialogueRunner>();
        }
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

        dialogueRunner.AddCommandHandler<string>(
            "background",
            SetBackground);

        dialogueRunner.AddCommandHandler<string>(
            "character",
            SetCharacter);

        dialogueRunner.AddCommandHandler<float, float>(
            "character_position",
            SetCharacterPosition);

        dialogueRunner.AddCommandHandler<float>(
            "character_height",
            SetCharacterHeight);
    }

    private void BuildBackgroundLookup()
    {
        backgroundLookup.Clear();

        if (backgrounds == null)
        {
            return;
        }

        foreach (VisualEntry entry in backgrounds)
        {
            if (entry == null ||
                string.IsNullOrWhiteSpace(entry.Key))
            {
                continue;
            }

            if (entry.Sprite == null)
            {
                Debug.LogWarning(
                    $"{name}: 배경 키 '{entry.Key}'에 Sprite가 없습니다.",
                    this);

                continue;
            }

            string normalizedKey =
                entry.Key.Trim();

            if (!backgroundLookup.TryAdd(
                    normalizedKey,
                    entry.Sprite))
            {
                Debug.LogWarning(
                    $"{name}: 배경 키 '{normalizedKey}'가 중복되었습니다.",
                    this);
            }
        }
    }

    private void BuildCharacterLookup()
    {
        characterLookup.Clear();

        if (characters == null)
        {
            return;
        }

        foreach (CharacterEntry entry in characters)
        {
            if (entry == null ||
                string.IsNullOrWhiteSpace(entry.Key))
            {
                continue;
            }

            if (entry.Sprite == null)
            {
                Debug.LogWarning(
                    $"{name}: 캐릭터 키 '{entry.Key}'에 Sprite가 없습니다.",
                    this);

                continue;
            }

            string normalizedKey =
                entry.Key.Trim();

            if (!characterLookup.TryAdd(
                    normalizedKey,
                    entry))
            {
                Debug.LogWarning(
                    $"{name}: 캐릭터 키 '{normalizedKey}'가 중복되었습니다.",
                    this);
            }
        }
    }

    private void SetBackground(
        string key)
    {
        if (backgroundImage == null)
        {
            Debug.LogError(
                $"{name}: 배경 Image가 연결되지 않았습니다.",
                this);

            return;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        string normalizedKey =
            key.Trim();

        if (normalizedKey.Equals(
                "none",
                StringComparison.OrdinalIgnoreCase))
        {
            HideVisual(
                backgroundImage);

            return;
        }

        if (!backgroundLookup.TryGetValue(
                normalizedKey,
                out Sprite sprite))
        {
            Debug.LogWarning(
                $"{name}: 등록되지 않은 배경 키 '{normalizedKey}'입니다.",
                this);

            return;
        }

        backgroundImage.sprite =
            sprite;

        backgroundImage.enabled =
            true;
    }

    private void SetCharacter(
        string key)
    {
        if (characterImage == null)
        {
            Debug.LogError(
                $"{name}: 캐릭터 Image가 연결되지 않았습니다.",
                this);

            return;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        string normalizedKey =
            key.Trim();

        if (normalizedKey.Equals(
                "none",
                StringComparison.OrdinalIgnoreCase))
        {
            HideVisual(
                characterImage);

            return;
        }

        if (!characterLookup.TryGetValue(
                normalizedKey,
                out CharacterEntry entry))
        {
            Debug.LogWarning(
                $"{name}: 등록되지 않은 캐릭터 키 '{normalizedKey}'입니다.",
                this);

            return;
        }

        characterImage.sprite =
            entry.Sprite;

        characterImage.enabled =
            true;

        if (entry.OverrideHeight)
        {
            ApplyCharacterHeight(
                entry.Height);
        }
    }

    private void SetCharacterPosition(
        float x,
        float y)
    {
        if (characterImage == null)
        {
            return;
        }

        characterImage.rectTransform
            .anchoredPosition =
            new Vector2(
                x,
                y);
    }

    private void SetCharacterHeight(
        float height)
    {
        ApplyCharacterHeight(
            height);
    }

    private void ApplyCharacterHeight(
        float height)
    {
        if (characterImage == null ||
            characterImage.sprite == null ||
            height <= 0f)
        {
            return;
        }

        Sprite sprite =
            characterImage.sprite;

        float spriteWidth =
            sprite.rect.width;

        float spriteHeight =
            sprite.rect.height;

        if (spriteHeight <= 0f)
        {
            return;
        }

        float aspectRatio =
            spriteWidth /
            spriteHeight;

        float width =
            height *
            aspectRatio;

        characterImage.rectTransform
            .sizeDelta =
            new Vector2(
                width,
                height);
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