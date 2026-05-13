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
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;

        gridState[x, y] = true;
        if (cells[x, y] != null)
        {
            cells[x, y].Occupy(blockColor);
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

    public void FreeCellWithSandEffect(int x, int y, Vector2 flyDirection)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;

        gridState[x, y] = false;
        if (cells[x, y] != null)
        {
            cells[x, y].FreeWithSandEffect(flyDirection);
        }
    }

    public void FreeCellWithExplosionEffect(int x, int y, Vector2 flyDirection)
    {
        FreeCellWithExplosionEffect(x, y, flyDirection, GridToWorldPosition(x, y));
    }

    public void FreeCellWithExplosionEffect(int x, int y, Vector2 flyDirection, Vector3 blastCenter)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;

        gridState[x, y] = false;
        if (cells[x, y] != null)
        {
            cells[x, y].FreeWithExplosionEffect(flyDirection, blastCenter);
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
        for (int i = 0; i < shape.Length; i++)
        {
            int cellX = originX + shape[i].x;
            int cellY = originY + shape[i].y;
            OccupyCellWithColor(cellX, cellY, blockColor);
        }

        AddScoreForBlock(shape);
        CheckLines();
        AudioManager.Instance?.PlayBlockPlaceSound();
    }

    public void PlaceDynamite(int x, int y, int explosionRadius, Color blockColor)
    {
        OccupyCellWithColor(x, y, blockColor);
        AddScoreForBlock(new Vector2Int[] { Vector2Int.zero });
        ExplodeCellsAround(x, y, Mathf.Max(0, explosionRadius));
        AudioManager.Instance?.PlayBlockPlaceSound();
    }

    void ExplodeCellsAround(int centerX, int centerY, int explosionRadius)
    {
        int clearedCells = 0;
        Vector3 blastCenter = GridToWorldPosition(centerX, centerY);

        for (int x = centerX - explosionRadius; x <= centerX + explosionRadius; x++)
        {
            for (int y = centerY - explosionRadius; y <= centerY + explosionRadius; y++)
            {
                if (x < 0 || x >= width || y < 0 || y >= height)
                    continue;

                Vector2 flyDirection = new Vector2(x - centerX, y - centerY);
                if (flyDirection == Vector2.zero)
                {
                    flyDirection = Vector2.up;
                }

                FreeCellWithExplosionEffect(x, y, flyDirection.normalized, blastCenter);
                clearedCells++;
            }
        }

        Debug.Log($"Dynamite exploded at ({centerX},{centerY}). Cleared {clearedCells} cells.");
    }

    private List<int> rowsToClear = new List<int>();
    private List<int> columnsToClear = new List<int>();

    void CheckLines()
    {
        rowsToClear.Clear();
        columnsToClear.Clear();

        for (int y = 0; y < height; y++)
        {
            if (IsRowFull(y))
            {
                rowsToClear.Add(y);
            }
        }

        for (int x = 0; x < width; x++)
        {
            if (IsColumnFull(x))
            {
                columnsToClear.Add(x);
            }
        }

        int clearedLines = rowsToClear.Count + columnsToClear.Count;
        if (clearedLines == 0)
            return;

        bool[,] cellsToClear = new bool[width, height];
        foreach (int row in rowsToClear)
        {
            for (int x = 0; x < width; x++)
            {
                cellsToClear[x, row] = true;
            }
        }

        foreach (int column in columnsToClear)
        {
            for (int y = 0; y < height; y++)
            {
                cellsToClear[column, y] = true;
            }
        }

        ClearCollectedCells(cellsToClear);

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

    void ClearCollectedCells(bool[,] cellsToClear)
    {
        int clearedCells = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!cellsToClear[x, y])
                    continue;
                clearedCells++;

                Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
                Vector2 fromCenter = new Vector2(x - center.x, y - center.y);
                if (fromCenter == Vector2.zero)
                {
                    fromCenter = Vector2.up;
                }

                FreeCellWithSandEffect(x, y, fromCenter.normalized);
            }
        }
        
        // Play sound for cleared lines
        int clearedLines = rowsToClear.Count + columnsToClear.Count;
        if (clearedCells > 0)
        {
            AudioManager.Instance?.PlayLineClearSound(clearedLines);
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

    void ClearRow(int y)
    {
        for (int x = 0; x < width; x++)
        {
            float sidePush = x < width * 0.5f ? -0.35f : 0.35f;
            FreeCellWithSandEffect(x, y, new Vector2(sidePush, 1f));
        }
        Debug.Log($"Cleared row {y}.");
    }

    void ClearColumn(int x)
    {
        for (int y = 0; y < height; y++)
        {
            float sidePush = y < height * 0.5f ? -0.25f : 0.25f;
            FreeCellWithSandEffect(x, y, new Vector2(sidePush, 1f));
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
        HighlightCells(shape, originX, originY, canPlace, canPlace ? new Color(0.35f, 1f, 0.45f, 1f) : new Color(1f, 0.25f, 0.2f, 1f));
    }

    public void HighlightCells(Vector2Int[] shape, int originX, int originY, bool canPlace, Color previewColor)
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
                    cells[cellX, cellY].SetHighlight(canPlace, previewColor);
                }
            }
        }
    }

    public void HighlightArea(int centerX, int centerY, int radius, bool canPlace, Color previewColor)
    {
        ClearHighlight();

        for (int x = centerX - radius; x <= centerX + radius; x++)
        {
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    if (cells[x, y] != null)
                    {
                        cells[x, y].SetHighlight(canPlace, previewColor);
                    }
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
