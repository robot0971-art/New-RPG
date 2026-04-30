using UnityEngine;

public sealed class TogglePanelButton : MonoBehaviour
{
    [SerializeField] private GameObject targetPanel;

    public void Toggle()
    {
        if (targetPanel == null)
        {
            return;
        }

        targetPanel.SetActive(!targetPanel.activeSelf);
    }
}
