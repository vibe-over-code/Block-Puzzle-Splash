using UnityEngine;
using System.Collections.Generic;

public class GridM : MonoBehaviour
{
    public int width = 8;
    public int height = 8;
    public float cellSize = 0.1f;
    public GameObject cellPrefab;
    public GameObject blockPrefab;

    private bool[,] gridState;
    private Cell[,] cells;

    void Start()
    {
        gridState = new bool[width, height];
        cells = new Cell[width, height];
        CreateGrid();
    }

    void CreateGrid()
    {
        if (cellPrefab == null)
        {
            Debug.LogError("Cell prefab is not assigned in GameManager.");
            return;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject newCellGO = Instantiate(cellPrefab);
                newCellGO.name = $"Cell ({x},{y})";
                Cell cellComponent = newCellGO.GetComponent<Cell>();
                cellComponent.x = x;
                cellComponent.y = y;
                cells[x, y] = cellComponent;
                float posX = (x - width / 2f) * cellSize;
                float posY = (y - height / 2f) * cellSize;
                newCellGO.transform.position = new Vector3(posX, posY, 0);
            }
        }
    }

    public bool IsCellFree(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return false;
        return !gridState[x, y];
    }

    public void OccupyCellWithColor(int x, int y, Color blockColor)
    {
        OccupyCellWithColor(x, y, blockColor, BlockType.Normal, 0);
    }

    public void OccupyCellWithColor(int x, int y, Color blockColor, BlockType blockType, int freezeTurnsLeft = 0)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;

        gridState[x, y] = true;
        if (cells[x, y] != null)
        {
            cells[x, y].Occupy(blockColor, blockType, freezeTurnsLeft);
        }
    }

    public void OccupyCell(int x, int y)
    {
        OccupyCellWithColor(x, y, Color.gray);
    }

    public void FreeCell(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;

        gridState[x, y] = false;
        if (cells[x, y] != null)
        {
            cells[x, y].Free();
        }
    }

    public void PrintGridState()
    {
        string grid = "";
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                grid += gridState[x, y] ? "1 " : "0 ";
            }
            grid += "\n";
        }
        Debug.Log(grid);
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        float startX = -(width * cellSize) / 2f;
        float startY = -(height * cellSize) / 2f;
        int x = Mathf.RoundToInt((worldPosition.x - startX) / cellSize);
        int y = Mathf.RoundToInt((worldPosition.y - startY) / cellSize);
        return new Vector2Int(x, y);
    }

    public Vector3 GridToWorldPosition(int x, int y)
    {
        float startX = -(width * cellSize) / 2f;
        float startY = -(height * cellSize) / 2f;
        float worldX = startX + x * cellSize + cellSize / 2f;
        float worldY = startY + y * cellSize + cellSize / 2f;
        return new Vector3(worldX, worldY, 0);
    }

    public bool CanPlaceBlock(Vector2Int[] shape, int originX, int originY)
    {
        for (int i = 0; i < shape.Length; i++)
        {
            int cellX = originX + shape[i].x;
            int cellY = originY + shape[i].y;
            if (cellX < 0 || cellX >= width || cellY < 0 || cellY >= height)
                return false;
            if (gridState[cellX, cellY])
                return false;
        }
        return true;
    }

    public void PlaceBlock(Vector2Int[] shape, int originX, int originY, Color blockColor)
    {
        PlaceBlock(shape, originX, originY, blockColor, BlockType.Normal);
    }

    public void PlaceBlock(Vector2Int[] shape, int originX, int originY, Color blockColor, BlockType blockType)
    {
        List<Vector2Int> placedCells = new List<Vector2Int>();
        for (int i = 0; i < shape.Length; i++)
        {
            int cellX = originX + shape[i].x;
            int cellY = originY + shape[i].y;
            placedCells.Add(new Vector2Int(cellX, cellY));
            OccupyCellWithColor(cellX, cellY, blockColor, blockType, blockType == BlockType.Freeze ? 3 : 0);
        }

        AddScoreForBlock(shape);

        if (blockType == BlockType.Dynamite)
        {
            ExplodeAround(placedCells);
            return;
        }

        TickFreezeBlocks(blockType == BlockType.Freeze ? placedCells : null);
        CheckLines();
    }

    void ExplodeAround(List<Vector2Int> placedCells)
    {
        foreach (Vector2Int placedCell in placedCells)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int targetX = placedCell.x + dx;
                    int targetY = placedCell.y + dy;
                    if (targetX >= 0 && targetX < width && targetY >= 0 && targetY < height)
                    {
                        FreeCell(targetX, targetY);
                    }
                }
            }
        }

        Debug.Log("Dynamite exploded. Lines are not scored after the explosion.");
    }

    void TickFreezeBlocks(List<Vector2Int> newlyFrozenCells)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (newlyFrozenCells != null && newlyFrozenCells.Contains(new Vector2Int(x, y)))
                    continue;

                if (cells[x, y] != null)
                {
                    cells[x, y].TickFreeze();
                }
            }
        }
    }

    void CheckLines()
    {
        int clearedLines = 0;

        for (int y = 0; y < height; y++)
        {
            if (IsRowClearable(y))
            {
                ClearRow(y);
                clearedLines++;
            }
        }

        for (int x = 0; x < width; x++)
        {
            if (IsColumnClearable(x))
            {
                ClearColumn(x);
                clearedLines++;
            }
        }

        if (clearedLines > 0)
        {
            int linePoints = clearedLines * 50;
            if (clearedLines >= 2) linePoints += 30;
            if (clearedLines >= 3) linePoints += 50;
            if (clearedLines >= 4) linePoints += 80;

            ScoreManager.Instance?.AddScore(linePoints);
            Debug.Log($"Cleared {clearedLines} lines. +{linePoints} points.");
        }
    }

    bool IsRowFull(int y)
    {
        for (int x = 0; x < width; x++)
        {
            if (!gridState[x, y])
                return false;
        }
        return true;
    }

    bool IsColumnFull(int x)
    {
        for (int y = 0; y < height; y++)
        {
            if (!gridState[x, y])
                return false;
        }
        return true;
    }

    bool IsRowClearable(int y)
    {
        if (!IsRowFull(y))
            return false;

        for (int x = 0; x < width; x++)
        {
            if (cells[x, y] != null && cells[x, y].IsFrozen())
                return false;
        }

        return true;
    }

    bool IsColumnClearable(int x)
    {
        if (!IsColumnFull(x))
            return false;

        for (int y = 0; y < height; y++)
        {
            if (cells[x, y] != null && cells[x, y].IsFrozen())
                return false;
        }

        return true;
    }

    void ClearRow(int y)
    {
        for (int x = 0; x < width; x++)
        {
            FreeCell(x, y);
        }
        Debug.Log($"Cleared row {y}.");
    }

    void ClearColumn(int x)
    {
        for (int y = 0; y < height; y++)
        {
            FreeCell(x, y);
        }
        Debug.Log($"Cleared column {x}.");
    }

    private int GetBlockScore(int blockSize)
    {
        switch (blockSize)
        {
            case 2: return 10;
            case 3: return 20;
            case 4: return 30;
            default: return 10;
        }
    }

    private void AddScoreForBlock(Vector2Int[] shape)
    {
        int blockSize = shape.Length;
        int points = GetBlockScore(blockSize);
        ScoreManager.Instance?.AddScore(points);
    }

    public void HighlightCells(Vector2Int[] shape, int originX, int originY, bool canPlace)
    {
        ClearHighlight();

        for (int i = 0; i < shape.Length; i++)
        {
            int cellX = originX + shape[i].x;
            int cellY = originY + shape[i].y;

            if (cellX >= 0 && cellX < width && cellY >= 0 && cellY < height)
            {
                if (cells[cellX, cellY] != null)
                {
                    cells[cellX, cellY].SetHighlight(canPlace);
                }
            }
        }
    }

    public void ClearHighlight()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (cells[x, y] != null)
                {
                    cells[x, y].ClearHighlight();
                }
            }
        }
    }

    public void ClearAllCells()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (gridState[x, y])
                {
                    FreeCell(x, y);
                }
            }
        }
        Debug.Log("All cells were cleared.");
    }

    public bool CanPlaceAnyBlock(GameObject[] blocks)
    {
        foreach (GameObject blockPrefab in blocks)
        {
            Block blockComponent = blockPrefab.GetComponent<Block>();
            if (blockComponent == null) continue;

            Vector2Int[] shape = blockComponent.shape;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (CanPlaceBlock(shape, x, y))
                    {
                        Debug.Log($"Can place block {blockPrefab.name} at ({x},{y}).");
                        return true;
                    }
                }
            }
        }
        Debug.Log("No available moves. GAME OVER.");
        return false;
    }
}
