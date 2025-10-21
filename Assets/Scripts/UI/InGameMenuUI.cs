using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class InGameMenuUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button returnToTitleButton;
    [SerializeField] private Button closeMenuButton;

    [Header("Confirmation Dialog")]
    [SerializeField] private GameObject confirmationDialog;
    [SerializeField] private Button confirmReturnButton;
    [SerializeField] private Button cancelReturnButton;

    public bool IsVisible => canvasGroup.interactable;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        // --- Main Menu Buttons ---
        returnToTitleButton.onClick.AddListener(ShowConfirmationDialog);
        closeMenuButton.onClick.AddListener(Hide);

        // --- Confirmation Dialog Buttons ---
        confirmReturnButton.onClick.AddListener(ReturnToTitle);
        cancelReturnButton.onClick.AddListener(HideConfirmationDialog);

        Hide(); // Start hidden
        HideConfirmationDialog();
    }

    public void Show()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        HideConfirmationDialog(); // Ensure confirmation is also hidden
    }

    private void ShowConfirmationDialog()
    {
        confirmationDialog.SetActive(true);
    }

    private void HideConfirmationDialog()
    {
        confirmationDialog.SetActive(false);
    }

    private void ReturnToTitle()
    {
        // In a real game, you might want to reset or clear some managers here
        SceneManager.LoadScene("TitleScene");
    }
}
