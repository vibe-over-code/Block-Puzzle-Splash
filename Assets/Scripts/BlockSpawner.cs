using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class BlockSpawner : MonoBehaviour
{
    public GameObject[] blockPrefabs;
    public Transform spawnAreaStart;
    public int blocksCount = 3;
    public float spacing = 120f;
    public float spawnPadding = 0.25f;
    public int maxVerticalBlocks = 9;

    [Header("Game over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private float gameOverDelay = 2f;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Special blocks")]
    [Range(0f, 1f)]
    [SerializeField] private float dynamiteSpawnChance = 0.12f;

    private int currentBlocksCount;
    private bool isRespawning = false;
    private bool gameOverTriggered = false;
    private GridM gridManager;

    private void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        gridManager = FindFirstObjectByType<GridM>();
        SpawnBlocks();
    }

    public void SpawnBlocks()
    {
        if (isRespawning || gameOverTriggered) return;
        isRespawning = true;

        ClearBlocks();
        currentBlocksCount = blocksCount;

        List<GameObject> spawnedBlocks = new List<GameObject>();
        for (int i = 0; i < blocksCount; i++)
        {
            GameObject spawnedBlock = SpawnRandomBlockAtPosition();
            if (spawnedBlock != null)
            {
                spawnedBlocks.Add(spawnedBlock);
            }
        }

        ArrangeSpawnedBlocks(spawnedBlocks);

        isRespawning = false;
        CheckGameOver();
    }

    GameObject SpawnRandomBlockAtPosition()
    {
        if (blockPrefabs == null || blockPrefabs.Length == 0)
        {
            Debug.LogError("No block prefabs available for spawning.");
            return null;
        }

        GameObject blockToSpawn = PickRandomSpawnPrefab();
        if (blockToSpawn == null)
        {
            Debug.LogError("No valid block prefab was found.");
            return null;
        }

        Vector3 startPos = spawnAreaStart != null ? spawnAreaStart.position : new Vector3(5.5f, 3f, 0);
        return Instantiate(blockToSpawn, startPos, Quaternion.identity);
    }

    void ArrangeSpawnedBlocks(List<GameObject> spawnedBlocks)
    {
        if (spawnedBlocks == null || spawnedBlocks.Count == 0)
            return;

        Vector3 startPos = spawnAreaStart != null ? spawnAreaStart.position : new Vector3(5.5f, 3f, 0);
        float nextTopY = startPos.y;
        float columnCenterX = startPos.x;
        float columnHeight = 0f;
        float columnWidth = 0f;

        for (int i = 0; i < spawnedBlocks.Count; i++)
        {
            GameObject spawnedBlock = spawnedBlocks[i];
            if (spawnedBlock == null)
                continue;

            spawnedBlock.transform.position = new Vector3(columnCenterX, nextTopY, startPos.z);
            Bounds bounds = GetRendererBounds(spawnedBlock);
            float blockHeight = bounds.size.y;

            if (columnHeight > 0f && columnHeight + spawnPadding + blockHeight > maxVerticalBlocks)
            {
                columnCenterX += columnWidth * 0.5f + spawnPadding + bounds.size.x * 0.5f;
                nextTopY = startPos.y;
                columnHeight = 0f;
                columnWidth = 0f;
            }

            spawnedBlock.transform.position = new Vector3(columnCenterX, nextTopY, startPos.z);
            bounds = GetRendererBounds(spawnedBlock);
            Vector3 correction = new Vector3(columnCenterX - bounds.center.x, nextTopY - bounds.max.y, 0f);
            spawnedBlock.transform.position += correction;

            bounds = GetRendererBounds(spawnedBlock);
            nextTopY = bounds.min.y - spawnPadding;
            columnHeight += blockHeight + (columnHeight > 0f ? spawnPadding : 0f);
            columnWidth = Mathf.Max(columnWidth, bounds.size.x);
        }
    }

    Bounds GetRendererBounds(GameObject block)
    {
        Renderer[] renderers = block.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(block.transform.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    GameObject PickRandomSpawnPrefab()
    {
        List<GameObject> normalBlocks = new List<GameObject>();
        List<GameObject> dynamiteBlocks = new List<GameObject>();

        foreach (GameObject blockPrefab in blockPrefabs)
        {
            if (blockPrefab == null)
                continue;

            Block block = blockPrefab.GetComponent<Block>();
            if (block != null && block.blockType == BlockType.Dynamite)
            {
                dynamiteBlocks.Add(blockPrefab);
            }
            else
            {
                normalBlocks.Add(blockPrefab);
            }
        }

        bool shouldSpawnDynamite = dynamiteBlocks.Count > 0 && Random.value < dynamiteSpawnChance;
        List<GameObject> spawnPool = shouldSpawnDynamite || normalBlocks.Count == 0 ? dynamiteBlocks : normalBlocks;

        if (spawnPool.Count == 0)
            return null;

        int randomIndex = Random.Range(0, spawnPool.Count);
        return spawnPool[randomIndex];
    }

    void ClearBlocks()
    {
        Block[] blocks = FindObjectsByType<Block>(FindObjectsSortMode.None);
        foreach (Block block in blocks)
        {
            block.gameObject.SetActive(false);
            Destroy(block.gameObject);
        }
    }

    public void OnBlockPlaced()
    {
        if (gameOverTriggered) return;

        currentBlocksCount--;
        Debug.Log($"Block placed. Blocks left: {currentBlocksCount}");

        if (currentBlocksCount <= 0)
        {
            ScoreManager.Instance?.AddScore(100);
            Debug.Log("All blocks were used. Bonus +100 points.");
            SpawnBlocks();
        }

        CheckGameOver();
    }

    public void RefreshBlocks()
    {
        ClearBlocks();
        SpawnBlocks();
    }

    public void CheckGameOver()
    {
        if (gameOverTriggered)
            return;

        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridM>();
        }

        if (gridManager == null)
            return;

        Block[] spawnedBlocks = FindObjectsByType<Block>(FindObjectsSortMode.None);
        bool canPlace = gridManager.CanPlaceAnyBlock(spawnedBlocks);
        bool canRefresh = false;

        UIManager uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager != null)
        {
            canRefresh = uiManager.CanRefreshBlocks();
        }

        if (!canPlace && !canRefresh)
        {
            Debug.Log("GAME OVER! No available moves and refresh is unavailable.");
            OnGameOver();
        }
    }

    private void OnGameOver()
    {
        gameOverTriggered = true;

        Block[] blocks = FindObjectsByType<Block>(FindObjectsSortMode.None);
        foreach (Block block in blocks)
        {
            block.enabled = false;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        StartCoroutine(ReturnToMainMenuAfterDelay());
    }

    private IEnumerator ReturnToMainMenuAfterDelay()
    {
        yield return new WaitForSeconds(gameOverDelay);
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
