using UnityEngine;

public sealed class StatUpgradeWindowUI : MonoBehaviour, IGameplayWindow
{
    [SerializeField] private GameObject windowRoot;
    [SerializeField] private StatUpgradePanelUI[] panels;

    public bool IsOpen => windowRoot != null && windowRoot.activeInHierarchy;

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

        GameplayWindowManager.Register(this);
    }

    private void OnDestroy()
    {
        GameplayWindowManager.Unregister(this);
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Open()
    {
        GameplayWindowManager.OpenExclusive(this);
        SetVisible(true);
        Refresh();
    }

    public void Close()
    {
        SetVisible(false);
    }

    public void Toggle()
    {
        if (IsOpen)
        {
            Close();
            return;
        }

        Open();
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
