using UnityEngine;
using UnityEngine.UI;

public sealed class SkillWindowUI : MonoBehaviour, IGameplayWindow
{
    [SerializeField] private GameObject windowRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button toggleButton;
    [SerializeField] private SkillButtonUI[] skillButtons;
    [SerializeField] private bool openOnAwake = true;

    public bool IsOpen => windowRoot != null && windowRoot.activeInHierarchy;

    private void Awake()
    {
        if (windowRoot == null)
        {
            windowRoot = gameObject;
        }

        CacheSkillButtons();
        GameplayWindowManager.Register(this);

        if (openOnAwake)
        {
            Open();
        }
        else
        {
            SetVisible(false);
        }
    }

    private void OnDestroy()
    {
        GameplayWindowManager.Unregister(this);
    }

    private void OnEnable()
    {
        AddListeners();
        Refresh();
    }

    private void OnDisable()
    {
        RemoveListeners();
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
        CacheSkillButtons();

        if (skillButtons == null)
        {
            return;
        }

        for (int i = 0; i < skillButtons.Length; i++)
        {
            if (skillButtons[i] != null)
            {
                skillButtons[i].Refresh();
            }
        }
    }

    private void AddListeners()
    {
        if (openButton != null)
        {
            openButton.onClick.AddListener(Open);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(Toggle);
        }
    }

    private void RemoveListeners()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(Open);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
        }

        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(Toggle);
        }
    }

    private void CacheSkillButtons()
    {
        if (skillButtons == null || skillButtons.Length == 0)
        {
            skillButtons = GetComponentsInChildren<SkillButtonUI>(true);
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
