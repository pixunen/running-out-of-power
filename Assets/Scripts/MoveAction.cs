using UnityEngine;

public class MoveAction : ActionBase
{
    public MoveAction() : base("Move", 2, 3)
    {
    }

    public override bool CanExecute(GameObject actor, Vector2Int targetPosition)
    {
        if (!base.CanExecute(actor, targetPosition))
        {
            return false;
        }

        // Check if target cell exists and is walkable
        GridCell targetCell = GridManager.Instance.GetCell(targetPosition);
        if (targetCell == null || !targetCell.isWalkable || targetCell.isOccupied)
        {
            return false;
        }

        // Check if within range
        var actorController = actor.GetComponent<PlayerController>();
        if (actorController == null)
        {
            var enemyController = actor.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                int distance = Mathf.Abs(enemyController.GridPosition.x - targetPosition.x) +
                              Mathf.Abs(enemyController.GridPosition.y - targetPosition.y);
                return distance <= range;
            }
            return false;
        }

        int dist = Mathf.Abs(actorController.GridPosition.x - targetPosition.x) +
                   Mathf.Abs(actorController.GridPosition.y - targetPosition.y);
        return dist <= range && dist > 0;
    }

    public override void Execute(GameObject actor, Vector2Int targetPosition)
    {
        var playerController = actor.GetComponent<PlayerController>();
        var enemyController = actor.GetComponent<EnemyController>();

        Vector2Int currentPos = playerController != null ? playerController.GridPosition : enemyController.GridPosition;

        // Clear current cell
        GridCell currentCell = GridManager.Instance.GetCell(currentPos);
        if (currentCell != null)
        {
            currentCell.ClearOccupied();
        }

        // Move to target
        GridCell targetCell = GridManager.Instance.GetCell(targetPosition);
        if (targetCell != null)
        {
            Vector3 worldPos = GridManager.Instance.GetWorldPosition(targetPosition);
            actor.transform.position = worldPos;

            targetCell.SetOccupied(actor);

            // Consume power (only for player)
            if (playerController != null)
            {
                PowerManager.Instance.ConsumePower(powerCost);

                // Check for power-up collection
                if (targetCell.HasPowerUp())
                {
                    CollectPowerUp(targetCell, actor);
                }
            }

            Debug.Log($"{actor.name} moved to {targetPosition}");
        }
    }

    private void CollectPowerUp(GridCell cell, GameObject actor)
    {
        if (cell.powerUpObject == null) return;

        // Get the power-up controller
        PowerUpController powerUpController = cell.powerUpObject.GetComponent<PowerUpController>();
        if (powerUpController != null)
        {
            // Check if it's a health orb or power-up
            if (powerUpController.IsHealthOrb())
            {
                // Collect health orb
                int healthAmount = powerUpController.GetHealthAmount();

                // Heal the player
                PlayerController player = actor.GetComponent<PlayerController>();
                if (player != null)
                {
                    int oldHealth = player.currentHealth;
                    player.currentHealth = Mathf.Min(player.currentHealth + healthAmount, player.maxHealth);
                    int actualHealing = player.currentHealth - oldHealth;

                    Debug.Log($"Collected health orb! Restored {actualHealing} health. Health: {player.currentHealth}/{player.maxHealth}");
                }
            }
            else
            {
                // Collect power-up
                int powerAmount = powerUpController.Collect();
                PowerManager.Instance.AddBonusPower(powerAmount);

                Debug.Log($"Collected power-up! Gained {powerAmount} bonus power.");
            }

            // Remove from spawn manager's tracking
            if (PowerUpSpawnManager.Instance != null)
            {
                PowerUpSpawnManager.Instance.RemovePowerUp(powerUpController);
            }
        }

        // Clear the cell reference
        cell.ClearPowerUp();
    }

    public override void ShowRange(Vector2Int fromPosition)
    {
        if (GridManager.Instance != null)
        {
            var cells = GridManager.Instance.GetCellsInRange(fromPosition, range, true); // Only walkable
            GridManager.Instance.HighlightCells(cells, false);
        }
    }
}