using UnityEngine;
using System.Collections;

public class Cell : MonoBehaviour
{
    public int x;
    public int y;
    public bool isOccupied = false;
    public float clearEffectDuration = 0.55f;
    public float clearEffectFlyDistance = 0.35f;

    private SpriteRenderer spriteRenderer;
    private GridM gridManager;
    private Color defaultColor = Color.white;
    private Color currentColor = Color.white;
    private Color originalColor;
    private bool isHighlighted = false;
    private Material runtimeSpecialMaterial;
    private Material runtimeSandMaterial;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        gridManager = FindFirstObjectByType<GridM>();
        defaultColor = Color.white;
        SetColor(defaultColor);
        ApplyShader();

        if (gridManager == null)
        {
            Debug.LogError("GridM was not found.");
        }
    }

    public void Occupy(Color blockColor)
    {
        isOccupied = true;
        currentColor = blockColor;
        SetColor(currentColor);
        ApplyShader();
    }

    public void Free()
    {
        isOccupied = false;
        currentColor = defaultColor;
        SetColor(defaultColor);
        ApplyShader();
    }

    public void FreeWithSandEffect(Vector2 flyDirection)
    {
        if (isOccupied)
        {
            SpawnSandEffect(flyDirection);
        }

        Free();
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

    void ApplyShader()
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
        properties.SetColor("_BaseColor", currentColor);
        properties.SetFloat("_IsOccupied", isOccupied ? 1f : 0f);
        spriteRenderer.SetPropertyBlock(properties);
    }

    Material GetSpecialMaterial()
    {
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

    void SpawnSandEffect(Vector2 flyDirection)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        GameObject effectObject = new GameObject("Block Sand Clear FX");
        effectObject.transform.position = transform.position;
        effectObject.transform.rotation = transform.rotation;
        effectObject.transform.localScale = transform.lossyScale;

        SpriteRenderer effectRenderer = effectObject.AddComponent<SpriteRenderer>();
        effectRenderer.sprite = spriteRenderer.sprite;
        effectRenderer.color = currentColor;
        effectRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        effectRenderer.sortingOrder = spriteRenderer.sortingOrder + 5;
        effectRenderer.sharedMaterial = GetSandMaterial();

        StartCoroutine(AnimateSandEffect(effectObject, effectRenderer, flyDirection.normalized));
    }

    IEnumerator AnimateSandEffect(GameObject effectObject, SpriteRenderer effectRenderer, Vector2 flyDirection)
    {
        if (flyDirection == Vector2.zero)
        {
            flyDirection = Random.insideUnitCircle.normalized;
        }

        Vector3 startPosition = effectObject.transform.position;
        Vector3 endPosition = startPosition + new Vector3(flyDirection.x, flyDirection.y + 0.35f, 0f) * clearEffectFlyDistance;
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        float elapsedTime = 0f;

        while (elapsedTime < clearEffectDuration)
        {
            float progress = elapsedTime / clearEffectDuration;
            float easedProgress = 1f - Mathf.Pow(1f - progress, 2f);
            effectObject.transform.position = Vector3.Lerp(startPosition, endPosition, easedProgress);
            effectObject.transform.localScale = transform.lossyScale * Mathf.Lerp(1f, 0.86f, easedProgress);

            effectRenderer.GetPropertyBlock(properties);
            properties.SetFloat("_DissolveAmount", progress);
            properties.SetFloat("_ScatterAmount", easedProgress);
            effectRenderer.SetPropertyBlock(properties);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(effectObject);
    }

    Material GetSandMaterial()
    {
        if (runtimeSandMaterial == null)
        {
            Shader shader = Shader.Find("BlockPuzzle/BlockSandDissolveSprite");
            if (shader != null)
            {
                runtimeSandMaterial = new Material(shader);
            }
        }

        return runtimeSandMaterial;
    }

    public void SetHighlight(bool canPlace)
    {
        SetHighlight(canPlace, canPlace ? new Color(0.35f, 1f, 0.45f, 1f) : new Color(1f, 0.25f, 0.2f, 1f));
    }

    public void SetHighlight(bool canPlace, Color previewColor)
    {
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            Color highlightColor = canPlace ? previewColor : new Color(1f, 0.16f, 0.12f, 1f);
            highlightColor.a = canPlace ? 0.82f : 0.92f;
            spriteRenderer.color = highlightColor;

            Material material = GetSpecialMaterial();
            if (material != null)
            {
                spriteRenderer.sharedMaterial = material;
            }

            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            spriteRenderer.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", highlightColor);
            properties.SetFloat("_IsOccupied", 1f);
            spriteRenderer.SetPropertyBlock(properties);

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
            ApplyShader();
            isHighlighted = false;
        }
    }
}
