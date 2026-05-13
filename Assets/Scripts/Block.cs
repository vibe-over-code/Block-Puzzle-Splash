using UnityEngine;

public enum BlockType
{
    Normal,
    Dynamite
}

public class Block : MonoBehaviour
{
    public Vector2Int[] shape;
    public Color blockColor = Color.green;
    public BlockType blockType = BlockType.Normal;

    [Header("Dynamite")]
    [SerializeField] private int explosionRadius = 1;

    [Header("Block shader visuals")]
    [SerializeField] private Material blockMaterial;

    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;
    private GridM gridManager;
    private Vector3 initialSpawnPosition;
    private Material runtimeSpecialMaterial;

    void Start()
    {
        mainCamera = Camera.main;
        gridManager = FindFirstObjectByType<GridM>();
        initialSpawnPosition = transform.position;

        if (gridManager == null)
        {
            Debug.LogError("Block: GridM was not found in the scene.");
        }

        ApplyVisuals();
    }

    void OnMouseDown()
    {
        Debug.Log("Started dragging block.");
        isDragging = true;
        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        offset = transform.position - mousePosition;
        UpdateHighlight();
    }

    void OnMouseUp()
    {
        Debug.Log("Released block.");
        isDragging = false;
        gridManager.ClearHighlight();
        TryPlaceBlock();
    }

    void Update()
    {
        if (isDragging)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector3(mousePosition.x + offset.x, mousePosition.y + offset.y, 0);
            UpdateHighlight();
        }
    }

    void TryPlaceBlock()
    {
        if (gridManager == null)
        {
            Debug.LogError("GridM was not found.");
            ReturnToSpawnPosition();
            return;
        }

        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int gridPos = gridManager.WorldToGridPosition(mousePosition);

        Vector2Int offsetToOrigin = GetBlockOriginOffset();
        Vector2Int targetOrigin = new Vector2Int(gridPos.x - offsetToOrigin.x, gridPos.y - offsetToOrigin.y);

        if (gridManager.CanPlaceBlock(shape, targetOrigin.x, targetOrigin.y))
        {
            if (blockType == BlockType.Dynamite)
            {
                gridManager.PlaceDynamite(targetOrigin.x, targetOrigin.y, explosionRadius, blockColor);
            }
            else
            {
                gridManager.PlaceBlock(shape, targetOrigin.x, targetOrigin.y, blockColor);
            }

            BlockSpawner spawner = FindFirstObjectByType<BlockSpawner>();
            if (spawner != null)
            {
                spawner.OnBlockPlaced();
            }
            Destroy(gameObject);
        }
        else
        {
            ReturnToSpawnPosition();
        }
    }

    Vector2Int GetBlockOriginOffset()
    {
        if (shape == null || shape.Length == 0)
            return Vector2Int.zero;

        int minX = shape[0].x;
        int minY = shape[0].y;

        foreach (var cell in shape)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.y < minY) minY = cell.y;
        }

        return new Vector2Int(minX, minY);
    }

    void ReturnToSpawnPosition()
    {
        transform.position = initialSpawnPosition;
    }

    void ApplyVisuals()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers)
        {
            r.color = GetDisplayColor();
            ApplyShader(r);
        }
    }

    Color GetDisplayColor()
    {
        return blockColor;
    }

    void ApplyShader(SpriteRenderer spriteRenderer)
    {
        Material material = GetSpecialMaterial();
        if (material != null)
        {
            spriteRenderer.sharedMaterial = material;
        }

        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(properties);
        properties.SetColor("_BaseColor", GetDisplayColor());
        properties.SetFloat("_IsOccupied", 1f);
        spriteRenderer.SetPropertyBlock(properties);
    }

    Material GetSpecialMaterial()
    {
        if (blockMaterial != null)
        {
            return blockMaterial;
        }

        if (runtimeSpecialMaterial == null)
        {
            Shader shader = Shader.Find("BlockPuzzle/BlockBlastBlockSprite");
            if (shader != null)
            {
                runtimeSpecialMaterial = new Material(shader);
            }
        }

        return runtimeSpecialMaterial;
    }

    void UpdateHighlight()
    {
        if (!isDragging)
            return;

        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int gridPos = gridManager.WorldToGridPosition(mousePosition);
        Vector2Int offsetToOrigin = GetBlockOriginOffset();
        Vector2Int targetOrigin = new Vector2Int(gridPos.x - offsetToOrigin.x, gridPos.y - offsetToOrigin.y);

        bool canPlace = gridManager.CanPlaceBlock(shape, targetOrigin.x, targetOrigin.y);
        if (blockType == BlockType.Dynamite)
        {
            gridManager.HighlightArea(targetOrigin.x, targetOrigin.y, explosionRadius, canPlace, blockColor);
        }
        else
        {
            gridManager.HighlightCells(shape, targetOrigin.x, targetOrigin.y, canPlace, blockColor);
        }
    }
}
