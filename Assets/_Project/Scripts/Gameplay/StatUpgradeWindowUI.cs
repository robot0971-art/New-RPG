using UnityEngine;

public sealed class StatUpgradeWindowUI : MonoBehaviour
{
    [SerializeField] private GameObject windowRoot;
    [SerializeField] private StatUpgradePanelUI[] panels;

    private void Awake()
    {
        if (windowRoot == null)
        {
            windowRoot = gameObject;
        }

        if (panels == null || panels.Length == 0)
        {
            panels = GetComponentsInChildren<StatUpgradePanelUI>(true);
        }
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Open()
    {
        SetVisible(true);
    }

    public void Close()
    {
        SetVisible(false);
    }

    public void Toggle()
    {
        SetVisible(windowRoot == null || !windowRoot.activeSelf);
    }

    public void Refresh()
    {
        if (panels == null)
        {
            return;
        }

        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
            {
                panels[i].Refresh();
            }
        }
    }

    private void SetVisible(bool visible)
    {
        if (windowRoot != null)
        {
            windowRoot.SetActive(visible);
        }
    }
}
