using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CounselingRoomCountView :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private CounselingRoom counselingRoom;

    [SerializeField]
    private TMP_Text countText;

    [Header("Text")]
    [SerializeField]
    private string prefix = "";

    private void Awake()
    {
        if (counselingRoom == null)
        {
            counselingRoom =
                GetComponentInParent<
                    CounselingRoom>();
        }

        if (countText == null)
        {
            countText =
                GetComponent<TMP_Text>();
        }
    }

    private void OnEnable()
    {
        if (counselingRoom != null)
        {
            counselingRoom
                .CounselingCountChanged +=
                HandleCountChanged;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (counselingRoom != null)
        {
            counselingRoom
                .CounselingCountChanged -=
                HandleCountChanged;
        }
    }

    private void HandleCountChanged(
        int count)
    {
        SetCount(
            count);
    }

    private void Refresh()
    {
        int count =
            counselingRoom != null
                ? counselingRoom
                    .CurrentCounselingCount
                : 0;

        SetCount(
            count);
    }

    private void SetCount(
        int count)
    {
        if (countText == null)
        {
            return;
        }

        countText.text =
            prefix +
            count;
    }
}