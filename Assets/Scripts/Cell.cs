using UnityEngine;

public class Cell : MonoBehaviour
{
    public int x;
    public int y;
    public bool isOccupied = false;
    public BlockType blockType = BlockType.Normal;
    public int freezeTurnsLeft = 0;

    private SpriteRenderer spriteRenderer;
    private GridM gridManager;
    private Color defaultColor = Color.white;
    private Color currentColor = Color.white;
    private Color originalColor;
    private bool isHighlighted = false;
    private Material runtimeSpecialMaterial;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        gridManager = FindFirstObjectByType<GridM>();
        defaultColor = Color.white;
        SetColor(defaultColor);
        ApplyShaderType(BlockType.Normal);

        if (gridManager == null)
        {
            Debug.LogError("GridM was not found.");
        }
    }

    public void Occupy(Color blockColor)
    {
        Occupy(blockColor, BlockType.Normal, 0);
    }

    public void Occupy(Color blockColor, BlockType newBlockType, int newFreezeTurnsLeft = 0)
    {
        isOccupied = true;
        blockType = newBlockType;
        freezeTurnsLeft = newBlockType == BlockType.Freeze ? newFreezeTurnsLeft : 0;
        currentColor = blockColor;
        SetColor(currentColor);
        ApplyShaderType(blockType);
    }

    public void Free()
    {
        isOccupied = false;
        blockType = BlockType.Normal;
        freezeTurnsLeft = 0;
        currentColor = defaultColor;
        SetColor(defaultColor);
        ApplyShaderType(BlockType.Normal);
    }

    public void SetOccupied(bool occupied)
    {
        if (occupied)
        {
            Occupy(Color.gray);
        }
        else
        {
            Free();
        }
    }

    void SetColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    public bool IsFrozen()
    {
        return isOccupied && blockType == BlockType.Freeze && freezeTurnsLeft > 0;
    }

    public void TickFreeze()
    {
        if (!IsFrozen())
            return;

        freezeTurnsLeft--;
        if (freezeTurnsLeft <= 0)
        {
            blockType = BlockType.Normal;
            ApplyShaderType(BlockType.Normal);
        }
        else
        {
            ApplyShaderType(BlockType.Freeze);
        }
    }

    void ApplyShaderType(BlockType type)
    {
        if (spriteRenderer == null)
            return;

        Material material = GetSpecialMaterial();
        if (material != null)
        {
            spriteRenderer.sharedMaterial = material;
        }

        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(properties);
        properties.SetFloat("_BlockType", (float)type);
        properties.SetColor("_BaseColor", currentColor);
        properties.SetFloat("_FreezeTurnsLeft", freezeTurnsLeft);
        spriteRenderer.SetPropertyBlock(properties);
    }

    Material GetSpecialMaterial()
    {
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

    public void SetHighlight(bool canPlace)
    {
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            if (canPlace)
                spriteRenderer.color = new Color(0.5f, 1f, 0.5f, 0.7f);
            else
                spriteRenderer.color = new Color(1f, 0.5f, 0.5f, 0.7f);
            isHighlighted = true;
        }
    }

    public void ClearHighlight()
    {
        if (isHighlighted && spriteRenderer != null)
        {
            if (isOccupied)
                spriteRenderer.color = currentColor;
            else
                spriteRenderer.color = defaultColor;
            ApplyShaderType(blockType);
            isHighlighted = false;
        }
    }
}
