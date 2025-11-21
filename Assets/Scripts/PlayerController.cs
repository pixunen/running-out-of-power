using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Player Stats")]
    public int maxHealth = 3;
    public int currentHealth;
    public Vector2Int gridPosition;

    [Header("Actions")]
    private MoveAction moveAction;
    private AttackAction attackAction;
    private SpecialAction specialAction;
    private ActionBase currentAction;

    [Header("Input")]
    private PlayerInput playerInput;

    [Header("State")]
    private bool isSelectingTarget = false;
    private Vector2Int hoveredCell;
    private bool isAttacking = false;

    void Awake()
    {
        currentHealth = maxHealth;

        // Initialize actions
        moveAction = new MoveAction();
        attackAction = new AttackAction();
        specialAction = new SpecialAction();

        // Setup input
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            playerInput = gameObject.AddComponent<PlayerInput>();
        }
    }

    void Start()
    {
        // Spawn at random grid position
        if (GridManager.Instance != null)
        {
            // Pick random position on grid
            gridPosition = new Vector2Int(
                Random.Range(0, GridManager.Instance.gridWidth),
                Random.Range(0, GridManager.Instance.gridHeight)
            );

            // Move to that position
            Vector3 spawnWorldPos = GridManager.Instance.GetWorldPosition(gridPosition);
            transform.position = spawnWorldPos;

            Debug.Log($"Player spawned at world {transform.position}, grid position: {gridPosition}");

            GridCell cell = GridManager.Instance.GetCell(gridPosition);
            if (cell != null)
            {
                cell.SetOccupied(gameObject);
            }
            else
            {
                Debug.LogError($"Player grid cell is null at {gridPosition}!");
            }
        }

        // Setup input callbacks
        SetupInputCallbacks();
    }

    void SetupInputCallbacks()
    {
        // Disable the PlayerInput component to avoid conflicts
        // We're handling input manually with Keyboard.current and Mouse.current
        PlayerInput pInput = GetComponent<PlayerInput>();
        if (pInput != null)
        {
            pInput.enabled = false;
        }
    }

    void Update()
    {
        // Don't allow any input during game over
        if (TurnManager.Instance.currentTurnState == TurnState.GameOver)
        {
            return;
        }

        if (isAttacking || !TurnManager.Instance.IsPlayerTurn())
        {
            return;
        }

        // Handle mouse input for cell selection using new Input System
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleMouseClick();
        }

        // Keyboard shortcuts for actions using new Input System
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.mKey.wasPressedThisFrame)
            {
                SelectMoveAction();
            }
            if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
            {
                SelectAttackAction();
            }
            if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
            {
                SelectSpecialAction();
            }
            if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.cKey.wasPressedThisFrame)
            {
                CancelAction();
            }
            if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame)
            {
                EndTurn();
            }
        }
    }


    public void SelectMoveAction()
    {
        if (!TurnManager.Instance.IsPlayerTurn()) return;

        if (PowerManager.Instance.HasEnoughPower(moveAction.powerCost))
        {
            currentAction = moveAction;
            isSelectingTarget = true;
            moveAction.ShowRange(gridPosition);
            Debug.Log("Move action selected. Click a cell to move.");
        }
        else
        {
            Debug.LogWarning("Not enough power for Move action!");
        }
    }

    public void SelectAttackAction()
    {
        if (!TurnManager.Instance.IsPlayerTurn()) return;

        if (PowerManager.Instance.HasEnoughPower(attackAction.powerCost))
        {
            currentAction = attackAction;
            isSelectingTarget = true;
            attackAction.ShowRange(gridPosition);
            Debug.Log("Attack action selected. Click a cell to attack.");
        }
        else
        {
            Debug.LogWarning("Not enough power for Attack action!");
        }
    }

    public void SelectSpecialAction()
    {
        if (!TurnManager.Instance.IsPlayerTurn()) return;

        if (PowerManager.Instance.HasEnoughPower(specialAction.powerCost))
        {
            currentAction = specialAction;
            isSelectingTarget = true;
            specialAction.ShowRange(gridPosition);
            Debug.Log("Special action selected. Click a cell to use special ability.");
        }
        else
        {
            Debug.LogWarning("Not enough power for Special action!");
        }
    }

    public void CancelAction()
    {
        isSelectingTarget = false;
        currentAction = null;
        GridManager.Instance.ClearHighlights();
        Debug.Log("Action cancelled.");
    }

    public void PerformAttack(Vector2Int targetPosition)
    {
        StartCoroutine(Attack(targetPosition));
    }

    private IEnumerator Attack(Vector2Int targetPosition)
    {
        if (attackAction.CanExecute(gameObject, targetPosition))
        {
            isAttacking = true;

            Vector3 originalPosition = transform.position;
            Vector3 targetWorldPosition = GridManager.Instance.GetWorldPosition(targetPosition);
            // Move halfway to the target
            Vector3 attackPosition = Vector3.Lerp(originalPosition, targetWorldPosition, 0.5f);

            float animTime = 0.1f;

            // Move towards target
            float elapsedTime = 0f;
            while (elapsedTime < animTime)
            {
                transform.position = Vector3.Lerp(originalPosition, attackPosition, elapsedTime / animTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.position = attackPosition;

            attackAction.Execute(gameObject, targetPosition);

            // Wait a moment
            yield return new WaitForSeconds(0.1f);

            // Move back
            elapsedTime = 0f;
            while (elapsedTime < animTime)
            {
                transform.position = Vector3.Lerp(attackPosition, originalPosition, elapsedTime / animTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.position = originalPosition;

            isAttacking = false;
        }
    }

    void HandleMouseClick()
    {
        if (!isSelectingTarget || currentAction == null) return;

        // Raycast to get clicked cell - use RaycastAll to get all hits
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        RaycastHit2D[] hits = Physics2D.RaycastAll(ray.origin, ray.direction, 100f);

        Debug.Log($"Mouse click at screen: {mousePos}, hit {hits.Length} objects");

        // Find the first GridCell in the hits (ignore player/enemy colliders)
        GridCell clickedCell = null;
        foreach (var hit in hits)
        {
            clickedCell = hit.collider.GetComponent<GridCell>();
            if (clickedCell != null)
            {
                Debug.Log($"Found GridCell at {clickedCell.gridPosition}");
                break;
            }
        }

        if (clickedCell != null)
        {
            Vector2Int targetPos = clickedCell.gridPosition;

            if (currentAction.CanExecute(gameObject, targetPos))
            {
                if (currentAction is AttackAction)
                {
                    PerformAttack(targetPos);
                }
                else
                {
                    currentAction.Execute(gameObject, targetPos);
                }
                CancelAction();
            }
            else
            {
                Debug.LogWarning($"Cannot execute {currentAction.actionName} at {targetPos}!");
            }
        }
        else
        {
            Debug.LogWarning("No GridCell found at click position!");
        }
    }

    public void EndTurn()
    {
        if (!TurnManager.Instance.IsPlayerTurn()) return;

        CancelAction();
        TurnManager.Instance.EndPlayerTurn();
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"Player took {damage} damage! Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Player died! Game Over!");
        TurnManager.Instance.GameOver();
        // Could add game over UI here
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        // Cleanup
    }
}