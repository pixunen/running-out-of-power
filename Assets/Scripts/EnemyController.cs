using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    [Header("Enemy Stats")]
    public int maxHealth = 2;
    public int currentHealth;
    public Vector2Int gridPosition;
    public EnemyData enemyData;

    [Header("AI")]
    private EnemyAI enemyAI;
    private MoveAction moveAction;
    private AttackAction attackAction;
    private bool isInitialized = false;
    public bool isAttacking = false;

    void Awake()
    {
        currentHealth = maxHealth;
        enemyAI = new EnemyAI(this);
        moveAction = new MoveAction();
        attackAction = new AttackAction();

        // Add health bar component
        if (GetComponent<EnemyHealthBar>() == null)
        {
            gameObject.AddComponent<EnemyHealthBar>();
        }
    }

    void Start()
    {
        // If not initialized by spawn manager, try to initialize with default values
        // This handles legacy hardcoded enemies in the scene
        if (!isInitialized && GridManager.Instance != null)
        {
            // Try to find an unoccupied cell
            int attempts = 0;
            bool foundSpot = false;

            while (attempts < 100 && !foundSpot)
            {
                Vector2Int randomPos = new Vector2Int(
                    Random.Range(0, GridManager.Instance.gridWidth),
                    Random.Range(0, GridManager.Instance.gridHeight)
                );

                GridCell cell = GridManager.Instance.GetCell(randomPos);
                if (cell != null && !cell.isOccupied && cell.isWalkable)
                {
                    gridPosition = randomPos;
                    foundSpot = true;
                }
                attempts++;
            }

            // Move to spawn position
            Vector3 spawnWorldPos = GridManager.Instance.GetWorldPosition(gridPosition);
            transform.position = spawnWorldPos;

            GridCell spawnCell = GridManager.Instance.GetCell(gridPosition);
            if (spawnCell != null)
            {
                spawnCell.SetOccupied(gameObject);
            }

            isInitialized = true;
            Debug.Log($"{gameObject.name} (legacy) spawned at grid position: {gridPosition}");
        }

        // Register with turn manager
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.RegisterEnemy(this);
        }
    }

    public void Initialize(EnemyData data, Vector2Int spawnPosition)
    {
        enemyData = data;
        gridPosition = spawnPosition;

        // Apply stats from EnemyData
        maxHealth = data.maxHealth;
        currentHealth = maxHealth;

        // Update AI ranges if needed (we'll modify EnemyAI to use data later)
        if (enemyAI != null)
        {
            enemyAI.SetRanges(data.moveRange, data.attackRange);
        }

        // Update visual appearance
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = data.enemyColor;
            if (data.enemySprite != null)
            {
                spriteRenderer.sprite = data.enemySprite;
            }
        }

        // Occupy grid cell
        GridCell cell = GridManager.Instance.GetCell(gridPosition);
        if (cell != null)
        {
            cell.SetOccupied(gameObject);
        }

        isInitialized = true;
    }

    public void TakeTurn()
    {
        if (enemyAI != null)
        {
            enemyAI.DecideAction();
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage! Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} was defeated!");

        // Clear occupied cell
        GridCell cell = GridManager.Instance.GetCell(gridPosition);
        if (cell != null)
        {
            cell.ClearOccupied();
        }

        // Unregister from turn manager
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.UnregisterEnemy(this);
        }

        Destroy(gameObject);
    }

    public void ExecuteMove(Vector2Int targetPosition)
    {
        if (moveAction.CanExecute(gameObject, targetPosition))
        {
            moveAction.Execute(gameObject, targetPosition);
        }
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

    void OnDestroy()
    {
        // Cleanup
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.UnregisterEnemy(this);
        }
    }
}
