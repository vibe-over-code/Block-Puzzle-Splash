using UnityEngine;

public enum BlockType
{
    Normal,
    Dynamite,
    Freeze
}

public class Block : MonoBehaviour
{
    public Vector2Int[] shape;
    public Color blockColor = Color.green;
    public BlockType blockType = BlockType.Normal;

    [Header("Special block visuals")]
    [SerializeField] private Material specialBlockMaterial;
    [SerializeField] private Color dynamiteTint = new Color(1f, 0.45f, 0.18f, 1f);
    [SerializeField] private Color freezeTint = new Color(0.35f, 0.85f, 1f, 1f);

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
            gridManager.PlaceBlock(shape, targetOrigin.x, targetOrigin.y, blockColor, blockType);

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

    public void SetBlockType(BlockType newBlockType)
    {
        blockType = newBlockType;
        ApplyVisuals();
    }

    void ApplyVisuals()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers)
        {
            r.color = GetDisplayColor();
            ApplyShaderType(r, blockType);
        }
    }

    Color GetDisplayColor()
    {
        switch (blockType)
        {
            case BlockType.Dynamite:
                return dynamiteTint;
            case BlockType.Freeze:
                return freezeTint;
            default:
                return blockColor;
        }
    }

    void ApplyShaderType(SpriteRenderer spriteRenderer, BlockType type)
    {
        Material material = GetSpecialMaterial();
        if (material != null)
        {
            spriteRenderer.sharedMaterial = material;
        }

        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(properties);
        properties.SetFloat("_BlockType", (float)type);
        properties.SetColor("_BaseColor", GetDisplayColor());
        spriteRenderer.SetPropertyBlock(properties);
    }

    Material GetSpecialMaterial()
    {
        if (specialBlockMaterial != null)
        {
            return specialBlockMaterial;
        }

        if (runtimeSpecialMaterial == null)
        {
            Shader shader = Shader.Find("BlockPuzzle/SpecialBlockSprite");
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
        gridManager.HighlightCells(shape, targetOrigin.x, targetOrigin.y, canPlace);
    }
}
