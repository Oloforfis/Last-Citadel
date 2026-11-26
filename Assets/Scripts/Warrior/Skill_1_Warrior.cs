using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class Skill_1_Warrior : MonoBehaviour
{
    [Header("Параметры навыка")]
    [SerializeField] private float damage = 20f;
    [SerializeField] private float radius = 3f;
    [SerializeField] private float duration = 3f;
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField] private float cooldown = 6f;

    [Header("Замедление во время навыка")]
    [Range(0.1f, 1f)]
    [SerializeField] private float speedMultiplierDuringCast = 0.5f;

    [Header("Поиск врагов")]
    [SerializeField] private LayerMask enemyMask;

    [Header("Визуал круга")]
    [SerializeField] private Color effectColor = new Color(1f, 0.6f, 0f, 0.35f);
    [SerializeField] private float yOffset = 0.05f;
    [SerializeField] private Material visualMaterial;

    [SerializeField] private bool isCasting = false;
    [SerializeField] private float cooldownTimer = 0f;

    private readonly HashSet<Health> hitThisTick = new();
    private GameObject circleVisual;

    public bool IsReady => cooldownTimer <= 0f && !isCasting;

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (Keyboard.current.qKey.wasPressedThisFrame
            && IsReady
            && !PlayerActionState.BlockAttack)
        {
            StartCoroutine(SpinAttack());
        }
    }

    private IEnumerator SpinAttack()
    {
        isCasting = true;
        PlayerActionState.BlockAttack = true;
        PlayerActionState.SpeedMultiplier = speedMultiplierDuringCast;

        ShowCircle(true);

        float elapsed = 0f;
        float nextTick = 0f;

        while (elapsed < duration)
        {
            nextTick -= Time.deltaTime;
            if (nextTick <= 0f)
            {
                DealDamageInRadius();
                nextTick += tickInterval;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        ShowCircle(false);

        PlayerActionState.BlockAttack = false;
        isCasting = false;
        PlayerActionState.SpeedMultiplier = 1f;
        cooldownTimer = cooldown;
    }

    private void DealDamageInRadius()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemyMask);

        hitThisTick.Clear();

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Health health = hit.GetComponentInParent<Health>();
                if (health != null)
                {
                    if (!hitThisTick.Add(health)) continue;

                    health.current_count -= damage;

                    Debug.Log(gameObject.name + " нанес " + damage + " урона " + health.gameObject.name + " первым навыком.");
                }
            }
        }
    }

    // ===== Визуальный круг =====

    private void ShowCircle(bool show)
    {
        EnsureVisual();
        circleVisual.SetActive(show);
    }

    private void EnsureVisual()
    {
        if (circleVisual != null) return;

        circleVisual = new GameObject("SpinRadiusVisual");
        circleVisual.transform.SetParent(transform, false);

        var meshFilter = circleVisual.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateDiscMesh();

        var meshRenderer = circleVisual.AddComponent<MeshRenderer>();
        Material mat = visualMaterial != null
            ? new Material(visualMaterial)
            : new Material(Shader.Find("Sprites/Default"));

        mat.color = effectColor;
        meshRenderer.sharedMaterial = mat;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        circleVisual.transform.localPosition = new Vector3(0f, yOffset, 0f);
        circleVisual.transform.localScale = Vector3.one * radius;
        circleVisual.SetActive(false);
    }

    private Mesh CreateDiscMesh()
    {
        const int segments = 48;

        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];
        Color[] colors = new Color[vertices.Length];

        vertices[0] = Vector3.zero;
        colors[0] = Color.white;

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            colors[i + 1] = Color.white;
        }

        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 2;
            triangles[i * 3 + 2] = i + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        return mesh;
    }
}
