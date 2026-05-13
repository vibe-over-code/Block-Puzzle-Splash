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
    private Material runtimeExplosionMaterial;

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

    public void FreeWithExplosionEffect(Vector2 flyDirection)
    {
        FreeWithExplosionEffect(flyDirection, transform.position);
    }

    public void FreeWithExplosionEffect(Vector2 flyDirection, Vector3 blastCenter)
    {
        if (isOccupied)
        {
            SpawnExplosionEffect(flyDirection, blastCenter);
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

    void SpawnExplosionEffect(Vector2 flyDirection, Vector3 blastCenter)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        GameObject effectObject = new GameObject("Block Explosion Clear FX");
        effectObject.transform.position = transform.position;
        effectObject.transform.rotation = transform.rotation;
        effectObject.transform.localScale = transform.lossyScale;

        SpriteRenderer effectRenderer = effectObject.AddComponent<SpriteRenderer>();
        effectRenderer.sprite = spriteRenderer.sprite;
        effectRenderer.color = currentColor;
        effectRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        effectRenderer.sortingOrder = spriteRenderer.sortingOrder + 8;
        effectRenderer.sharedMaterial = GetExplosionMaterial();

        StartCoroutine(AnimateExplosionEffect(effectObject, effectRenderer, flyDirection.normalized, blastCenter));
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

    IEnumerator AnimateExplosionEffect(GameObject effectObject, SpriteRenderer effectRenderer, Vector2 flyDirection, Vector3 blastCenter)
    {
        if (flyDirection == Vector2.zero)
        {
            flyDirection = Random.insideUnitCircle.normalized;
        }

        Vector3 startPosition = effectObject.transform.position;
        Vector3 fromCenter = startPosition - blastCenter;
        Vector2 radialDirection = fromCenter.sqrMagnitude > 0.0001f
            ? new Vector2(fromCenter.x, fromCenter.y).normalized
            : flyDirection.normalized;
        Vector2 tangentDirection = new Vector2(-radialDirection.y, radialDirection.x);
        float swirlSign = Mathf.Sign(Vector2.Dot(tangentDirection, flyDirection));
        if (Mathf.Approximately(swirlSign, 0f))
        {
            swirlSign = Random.value < 0.5f ? -1f : 1f;
        }

        float distanceFromCenter = Mathf.Max(fromCenter.magnitude, 0.01f);
        float radialDistance = clearEffectFlyDistance * 1.28f + distanceFromCenter * 0.18f;
        Vector3 endPosition = startPosition + new Vector3(radialDirection.x, radialDirection.y, 0f) * radialDistance;
        float rotationDirection = swirlSign * Random.Range(10f, 26f);
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        float elapsedTime = 0f;

        while (elapsedTime < clearEffectDuration)
        {
            float progress = elapsedTime / clearEffectDuration;
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            float ringPush = Mathf.Sin(progress * Mathf.PI) * clearEffectFlyDistance * 0.12f;
            Vector3 swirlOffset = new Vector3(tangentDirection.x, tangentDirection.y, 0f) * ringPush * swirlSign;
            effectObject.transform.position = Vector3.Lerp(startPosition, endPosition, easedProgress) + swirlOffset;
            effectObject.transform.rotation = Quaternion.Euler(0f, 0f, rotationDirection * easedProgress);
            effectObject.transform.localScale = transform.lossyScale * Mathf.Lerp(1.04f, 0.76f, easedProgress);

            effectRenderer.GetPropertyBlock(properties);
            properties.SetFloat("_Progress", progress);
            properties.SetFloat("_ScatterAmount", easedProgress);
            properties.SetVector("_BlastDirection", new Vector4(radialDirection.x, radialDirection.y, 0f, 0f));
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

    Material GetExplosionMaterial()
    {
        if (runtimeExplosionMaterial == null)
        {
            Shader shader = Shader.Find("BlockPuzzle/BlockExplosionDissolveSprite");
            if (shader != null)
            {
                runtimeExplosionMaterial = new Material(shader);
            }
        }

        return runtimeExplosionMaterial;
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
