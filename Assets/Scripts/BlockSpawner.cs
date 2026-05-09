using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    public GameObject[] blockPrefabs;
    public Transform spawnAreaStart;
    public int blocksCount = 3;
    public float spacing = 120f;
    public bool spawnSpecialBlocks = true;

    private int currentBlocksCount;
    private bool isRespawning = false;
    private GridM gridManager;

    private void Start()
    {
        gridManager = FindFirstObjectByType<GridM>();
        SpawnBlocks();
    }

    public void SpawnBlocks()
    {
        if (isRespawning) return;
        isRespawning = true;

        ClearBlocks();
        currentBlocksCount = blocksCount;

        for (int i = 0; i < blocksCount; i++)
        {
            SpawnRandomBlockAtPosition(i);
        }

        isRespawning = false;
        CheckGameOver();
    }

    void SpawnRandomBlockAtPosition(int index)
    {
        if (blockPrefabs == null || blockPrefabs.Length == 0)
        {
            Debug.LogError("No block prefabs available for spawning.");
            return;
        }

        int randomIndex = Random.Range(0, blockPrefabs.Length);
        GameObject blockToSpawn = blockPrefabs[randomIndex];

        Vector3 startPos = spawnAreaStart != null ? spawnAreaStart.position : new Vector3(5.5f, 3f, 0);
        Vector3 spawnPos = new Vector3(startPos.x, startPos.y - (index * spacing), startPos.z);

        GameObject spawnedBlock = Instantiate(blockToSpawn, spawnPos, Quaternion.identity);
        Block blockComponent = spawnedBlock.GetComponent<Block>();
        if (blockComponent != null)
        {
            blockComponent.SetBlockType(RollBlockType());
        }
    }

    BlockType RollBlockType()
    {
        if (!spawnSpecialBlocks)
            return BlockType.Normal;

        int randomType = Random.Range(0, 3);
        switch (randomType)
        {
            case 1:
                return BlockType.Dynamite;
            case 2:
                return BlockType.Freeze;
            default:
                return BlockType.Normal;
        }
    }

    void ClearBlocks()
    {
        Block[] blocks = FindObjectsByType<Block>(FindObjectsSortMode.None);
        foreach (Block block in blocks)
        {
            Destroy(block.gameObject);
        }
    }

    public void OnBlockPlaced()
    {
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
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridM>();
        }

        bool canPlace = gridManager.CanPlaceAnyBlock(blockPrefabs);

        if (!canPlace)
        {
            Debug.Log("GAME OVER! No available moves.");
            OnGameOver();
        }
    }

    private void OnGameOver()
    {
        Block[] blocks = FindObjectsByType<Block>(FindObjectsSortMode.None);
        foreach (Block block in blocks)
        {
            block.enabled = false;
        }

        Debug.Log("=========================================");
        Debug.Log("              GAME OVER!                 ");
        Debug.Log("   Press Restart to continue             ");
        Debug.Log("=========================================");
    }
}
