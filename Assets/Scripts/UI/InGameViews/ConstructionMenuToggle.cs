using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ConstructionMenuToggle : MonoBehaviour
{
    [SerializeField]
    private GameObject[] viewers;

    [SerializeField]
    private TMP_Text buttonText;

    private void Start()
    {
        RefreshText();
    }

    private bool HasVisibleViewer()
    {
        if (viewers != null)
        {
            foreach (GameObject viewer in viewers)
            {
                if (viewer != null && viewer.activeSelf)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void RefreshText()
    {
        if (buttonText != null)
        {
            buttonText.text = HasVisibleViewer() ? "UI 끄기" : "UI 켜기";
        }
    }

    public void Toggle()
    {
        if (viewers == null)
        {
            return;
        }

        bool show = !HasVisibleViewer();

        foreach (GameObject viewer in viewers)
        {
            if (viewer != null)
            {
                viewer.SetActive(show);
            }
        }

        RefreshText();
    }
}
