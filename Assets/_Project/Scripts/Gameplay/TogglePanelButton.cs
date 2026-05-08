using UnityEngine;
using UnityEngine.UI;

public sealed class TogglePanelButton : MonoBehaviour
{
    [SerializeField] private GameObject targetPanel;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.RemoveListener(Toggle);
            button.onClick.AddListener(Toggle);
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(Toggle);
        }
    }

    public void Toggle()
    {
        if (targetPanel == null)
        {
            return;
        }

        StatUpgradeWindowUI statWindow = targetPanel.GetComponentInChildren<StatUpgradeWindowUI>(true);
        if (statWindow != null)
        {
            bool shouldOpenStatWindow = !targetPanel.activeSelf || !statWindow.IsOpen;
            if (shouldOpenStatWindow)
            {
                targetPanel.SetActive(true);
                targetPanel.transform.SetAsLastSibling();
                statWindow.Open();
            }
            else
            {
                statWindow.Close();
                targetPanel.SetActive(false);
            }

            return;
        }

        SkillWindowUI skillWindow = targetPanel.GetComponentInChildren<SkillWindowUI>(true);
        if (skillWindow != null)
        {
            bool shouldOpenSkillWindow = !targetPanel.activeSelf || !skillWindow.IsOpen;
            if (shouldOpenSkillWindow)
            {
                targetPanel.SetActive(true);
                targetPanel.transform.SetAsLastSibling();
                skillWindow.Open();
            }
            else
            {
                skillWindow.Close();
                targetPanel.SetActive(false);
            }

            return;
        }

        CharacterStatWindowUI characterStatWindow = targetPanel.GetComponentInChildren<CharacterStatWindowUI>(true);
        if (characterStatWindow != null)
        {
            bool shouldOpenCharacterWindow = !targetPanel.activeSelf || !characterStatWindow.IsOpen;
            if (shouldOpenCharacterWindow)
            {
                targetPanel.SetActive(true);
                targetPanel.transform.SetAsLastSibling();
                characterStatWindow.Open();
            }
            else
            {
                characterStatWindow.Close();
                targetPanel.SetActive(false);
            }

            return;
        }

        bool shouldOpen = !targetPanel.activeSelf;

        targetPanel.SetActive(shouldOpen);
        if (shouldOpen)
        {
            targetPanel.transform.SetAsLastSibling();
        }
    }
}
