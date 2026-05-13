using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Button restartButton;
    public Button refreshButton;

    private GridM gridManager;
    private BlockSpawner blockSpawner;
    private ScoreManager scoreManager;
    private bool hasRefreshed = false;

    private void Start()
    {
        gridManager = FindFirstObjectByType<GridM>();
        blockSpawner = FindFirstObjectByType<BlockSpawner>();
        scoreManager = FindFirstObjectByType<ScoreManager>();

        restartButton.onClick.AddListener(RestartGame);
        refreshButton.onClick.AddListener(RefreshBlocks);

        UpdateRefreshButtonState();
    }

    public bool CanRefreshBlocks()
    {
        return !hasRefreshed;
    }

    private void RestartGame()
    {
        if (gridManager != null)
        {
            gridManager.ClearAllCells();
        }

        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }

        hasRefreshed = false;
        UpdateRefreshButtonState();

        if (blockSpawner != null)
        {
            blockSpawner.enabled = true;
            blockSpawner.SpawnBlocks();
        }
    }

    private void RefreshBlocks()
    {
        if (hasRefreshed)
        {
            Debug.Log("Blocks can be refreshed only once per game.");
            return;
        }

        Debug.Log("Refreshing blocks...");
        hasRefreshed = true;
        UpdateRefreshButtonState();

        if (blockSpawner != null)
        {
            blockSpawner.RefreshBlocks();
            AudioManager.Instance?.PlayBlocksRefreshSound();
        }
    }

    private void UpdateRefreshButtonState()
    {
        refreshButton.interactable = !hasRefreshed;
    }
}
