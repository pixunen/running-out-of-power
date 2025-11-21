using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionButtonManager : MonoBehaviour
{
    [Header("Action Buttons")]
    public Button moveButton;
    public Button attackButton;
    public Button specialButton;
    public Button cancelButton;
    public Button endTurnButton;

    [Header("Button Colors")]
    public Color normalColor = new Color(1f, 1f, 1f, 1f);
    public Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    private PlayerController playerController;
    private MoveAction moveAction;
    private AttackAction attackAction;
    private SpecialAction specialAction;

    void Start()
    {
        // Find the player controller
        playerController = GameObject.FindFirstObjectByType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("ActionButtonManager: PlayerController not found!");
            return;
        }

        // Initialize actions to get their power costs
        moveAction = new MoveAction();
        attackAction = new AttackAction();
        specialAction = new SpecialAction();

        // Set up button click listeners
        SetupButtonListeners();

        // Set button text with power costs
        SetButtonText();
    }

    void SetupButtonListeners()
    {
        if (moveButton != null)
            moveButton.onClick.AddListener(() => playerController.SelectMoveAction());

        if (attackButton != null)
            attackButton.onClick.AddListener(() => playerController.SelectAttackAction());

        if (specialButton != null)
            specialButton.onClick.AddListener(() => playerController.SelectSpecialAction());

        if (cancelButton != null)
            cancelButton.onClick.AddListener(() => playerController.CancelAction());

        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(() => playerController.EndTurn());
    }

    void SetButtonText()
    {
        SetButtonTextHelper(moveButton, $"(M)ove ({moveAction.powerCost})");
        SetButtonTextHelper(attackButton, $"(A)ttack ({attackAction.powerCost})");
        SetButtonTextHelper(specialButton, $"(S)pecial ({specialAction.powerCost})");
        SetButtonTextHelper(cancelButton, "(C)ancel");
        SetButtonTextHelper(endTurnButton, "(E)nd Turn");
    }

    void SetButtonTextHelper(Button button, string text)
    {
        if (button != null)
        {
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = text;
            }
        }
    }

    void Update()
    {
        if (playerController == null || PowerManager.Instance == null || TurnManager.Instance == null)
            return;

        // Check if it's player's turn
        bool isPlayerTurn = TurnManager.Instance.IsPlayerTurn();
        bool isGameOver = TurnManager.Instance.currentTurnState == TurnState.GameOver;

        // Update each action button based on power availability and turn state
        UpdateActionButton(moveButton, moveAction.powerCost, isPlayerTurn && !isGameOver);
        UpdateActionButton(attackButton, attackAction.powerCost, isPlayerTurn && !isGameOver);
        UpdateActionButton(specialButton, specialAction.powerCost, isPlayerTurn && !isGameOver);

        // Cancel and End Turn buttons are always available during player turn
        UpdateUtilityButton(cancelButton, isPlayerTurn && !isGameOver);
        UpdateUtilityButton(endTurnButton, isPlayerTurn && !isGameOver);
    }

    void UpdateActionButton(Button button, int powerCost, bool isPlayerTurn)
    {
        if (button == null) return;

        bool hasEnoughPower = PowerManager.Instance.HasEnoughPower(powerCost);
        bool shouldEnable = isPlayerTurn && hasEnoughPower;

        button.interactable = shouldEnable;

        // Update visual appearance
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = shouldEnable ? normalColor : disabledColor;
        }

        // Update text color
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.color = shouldEnable ? Color.black : new Color(0.7f, 0.7f, 0.7f, 1f);
        }
    }

    void UpdateUtilityButton(Button button, bool isPlayerTurn)
    {
        if (button == null) return;

        button.interactable = isPlayerTurn;

        // Update visual appearance
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = isPlayerTurn ? normalColor : disabledColor;
        }

        // Update text color
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.color = isPlayerTurn ? Color.black : new Color(0.7f, 0.7f, 0.7f, 1f);
        }
    }

    void OnDestroy()
    {
        // Clean up button listeners
        if (moveButton != null)
            moveButton.onClick.RemoveAllListeners();
        if (attackButton != null)
            attackButton.onClick.RemoveAllListeners();
        if (specialButton != null)
            specialButton.onClick.RemoveAllListeners();
        if (cancelButton != null)
            cancelButton.onClick.RemoveAllListeners();
        if (endTurnButton != null)
            endTurnButton.onClick.RemoveAllListeners();
    }
}
