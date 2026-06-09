using UnityEngine;

public class ScoreSystem : MonoBehaviour
{
    private float _totalScore = 0f;
    private Vector3 _originalScale;

    private void Start()
    {
        _originalScale = transform.localScale;
    }

    public void ResetScore()
    {
        _totalScore = 0f;
        transform.localScale = _originalScale;
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.white;
        }
        Debug.Log("Score Reset Visually!");
    }

    public void AddScore(float amount)
    {
        _totalScore += amount;
        Debug.Log($"AddScore called! Total: {_totalScore}");

        // 1. Visually bounce/scale the object
        transform.localScale = _originalScale * 1.4f;
        LeanScaleBack();

        // 2. Change to a random glowing color
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(Random.value, Random.value, Random.value);
        }

        // 3. Spawn a mini "point firework" sphere shooting up
        GameObject miniPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        miniPoint.transform.position = transform.position + Vector3.up * 1.5f;
        miniPoint.transform.localScale = Vector3.one * 0.3f;
        
        var miniRenderer = miniPoint.GetComponent<Renderer>();
        if (miniRenderer != null)
        {
            miniRenderer.material.color = Color.yellow;
        }

        var rb = miniPoint.AddComponent<Rigidbody>();
        rb.AddForce(Vector3.up * 8f + Random.insideUnitSphere * 2f, ForceMode.Impulse);
        Destroy(miniPoint, 1.2f);
    }

    private void LeanScaleBack()
    {
        // Smooth scale down helper
        var scaleTimer = 0f;
        var startScale = transform.localScale;
        
        // Simple coroutine-like simulation or direct update is fine, but we can do it in update or simple invoke
        // For self-contained simplicity, we can spawn a small animation component
        var anim = gameObject.GetComponent<ScoreAnimator>();
        if (anim == null) anim = gameObject.AddComponent<ScoreAnimator>();
        anim.targetScale = _originalScale;
    }

    public void SetPlayerName(string newName)
    {
        Debug.Log($"Player Name set visually to: {newName}");
    }
}

// Helper class for smooth visual bounce animation
public class ScoreAnimator : MonoBehaviour
{
    public Vector3 targetScale;
    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 8f);
        if (Vector3.Distance(transform.localScale, targetScale) < 0.01f)
        {
            transform.localScale = targetScale;
            Destroy(this);
        }
    }
}