using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Attack_Warrior : MonoBehaviour
{
    [System.Serializable]
    public class ComboStep
    {
        public enum HitShape { Sphere, Box }

        [Tooltip("Форма зоны поражения: Сфера (вокруг) или Бокс (полукруг впереди)")]
        public HitShape shape = HitShape.Box;

        [Tooltip("Радиус для сферы")]
        public float radius = 2.5f;

        [Tooltip("Размер бокса (X=ширина, Y=высота, Z=глубина). Для полукруга впереди делайте Z больше X")]
        public Vector3 boxSize = new Vector3(3f, 2f, 2.5f);

        [Tooltip("Смещение центра зоны поражения относительно персонажа (например, вперед по Z)")]
        public Vector3 centerOffset = Vector3.forward;

        [Tooltip("Урон за этот шаг комбо")]
        public float damage = 20f;

        [Tooltip("Длительность активной фазы удара (время, пока хитбокс активен)")]
        public float duration = 0.3f;

        [Tooltip("Интервал между тиками урона внутри длительности удара")]
        public float tickInterval = 0.15f;
    }

    [Header("Параметры комбо")]
    [Tooltip("Список шагов комбо. По умолчанию: Полукруг -> Широкий полукруг -> Круг вокруг")]
    [SerializeField] private List<ComboStep> comboSteps = new List<ComboStep>();

    [Tooltip("Время (в секундах), в течение которого можно продолжить комбо. После этого сбросится на 1-й удар.")]
    [SerializeField] private float comboResetWindow = 1.5f;

    [Tooltip("Глобальная перезарядка всего комбо после завершения последнего удара")]
    [SerializeField] private float globalCooldown = 1f;

    [Header("Замедление во время атаки")]
    [Range(0.1f, 1f)]
    [SerializeField] private float speedMultiplierDuringCast = 0.5f;

    [Header("Поиск врагов")]
    [SerializeField] private LayerMask enemyMask;

    [Header("Визуал")]
    [SerializeField] private Color effectColor = new Color(1f, 0.6f, 0f, 0.35f);
    [SerializeField] private Material visualMaterial;

    // Состояния
    private int currentComboIndex = 0;
    private float comboTimer = 0f;
    private float attackLockTimer = 0f;
    private bool isAttacking = false;
    private float cooldownTimer = 0f;

    private GameObject currentVisual;
    private readonly HashSet<Health> hitInCurrentStep = new();

    public bool IsReady => cooldownTimer <= 0f && !isAttacking && attackLockTimer <= 0f;

    private void Start()
    {
        // Инициализация шагов по умолчанию, если список пуст (для удобства)
        if (comboSteps.Count == 0)
        {
            comboSteps.Add(new ComboStep
            {
                shape = ComboStep.HitShape.Box,
                boxSize = new Vector3(2.5f, 2f, 2f),
                centerOffset = new Vector3(0, 0, 1f),
                damage = 15f,
                duration = 0.25f,
                tickInterval = 0.25f
            });
            comboSteps.Add(new ComboStep
            {
                shape = ComboStep.HitShape.Box,
                boxSize = new Vector3(4f, 2f, 3f),
                centerOffset = new Vector3(0, 0, 1.5f),
                damage = 20f,
                duration = 0.3f,
                tickInterval = 0.3f
            });
            comboSteps.Add(new ComboStep
            {
                shape = ComboStep.HitShape.Sphere,
                radius = 3f,
                centerOffset = Vector3.zero,
                damage = 30f,
                duration = 0.4f,
                tickInterval = 0.2f
            });
        }
    }

    void Update()
    {
        // Обновление таймеров
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        if (attackLockTimer > 0f) attackLockTimer -= Time.deltaTime;

        // Логика сброса комбо
        if (currentComboIndex > 0)
        {
            if (comboTimer > 0f)
            {
                comboTimer -= Time.deltaTime;
            }
            else
            {
                ResetCombo();
            }
        }

        // Ввод атаки
        if (Mouse.current.leftButton.wasPressedThisFrame && IsReady && !PlayerActionState.BlockAttack)
        {
            StartComboStep(currentComboIndex);

            // Подготовка к следующему шагу
            currentComboIndex = (currentComboIndex + 1) % comboSteps.Count;
            comboTimer = comboResetWindow;

            // Если это был последний удар в комбо, запускаем глобальный кулдаун
            if (currentComboIndex == 0)
            {
                cooldownTimer = globalCooldown;
            }
        }
    }

    private void StartComboStep(int stepIndex)
    {
        if (stepIndex >= comboSteps.Count) return;

        ComboStep step = comboSteps[stepIndex];
        isAttacking = true;
        attackLockTimer = step.duration; // Небольшая задержка перед возможным следующим нажатием

        // Применяем замедление
        PlayerActionState.BlockAttack = true;
        PlayerActionState.SpeedMultiplier = speedMultiplierDuringCast;

        hitInCurrentStep.Clear();
        StartCoroutine(ExecuteStep(step));
    }

    private IEnumerator ExecuteStep(ComboStep step)
    {
        ShowVisual(step);

        float elapsed = 0f;
        float nextTick = 0f;

        while (elapsed < step.duration)
        {
            nextTick -= Time.deltaTime;
            if (nextTick <= 0f)
            {
                DealDamage(step);
                nextTick += step.tickInterval;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        HideVisual();
        isAttacking = false;
        PlayerActionState.BlockAttack = false;

        // Если комбо прервалось или закончилось полностью, возвращаем скорость
        if (currentComboIndex == 0 || comboTimer <= 0f)
        {
            PlayerActionState.SpeedMultiplier = 1f;
        }
    }

    private void DealDamage(ComboStep step)
    {
        Collider[] hits = null;

        // Выбор типа коллайдера в зависимости от настройки шага
        if (step.shape == ComboStep.HitShape.Sphere)
        {
            hits = Physics.OverlapSphere(transform.position + step.centerOffset, step.radius, enemyMask);
        }
        else if (step.shape == ComboStep.HitShape.Box)
        {
            // OverlapBox принимает полуразмеры (extents), поэтому делим размер на 2
            Vector3 halfExtents = step.boxSize * 0.5f;
            hits = Physics.OverlapBox(transform.position + step.centerOffset, halfExtents, transform.rotation, enemyMask);
        }

        if (hits == null) return;

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Health health = hit.GetComponentInParent<Health>();
                if (health != null)
                {
                    // Проверяем, не били ли мы уже этого врага в рамках ТЕКУЩЕГО шага комбо
                    if (!hitInCurrentStep.Add(health)) continue;

                    health.current_count -= step.damage;
                    Debug.Log($"{gameObject.name} нанес {step.damage} урона врагу {health.gameObject.name} (шаг комбо {currentComboIndex + 1}).");
                }
            }
        }
    }

    private void ResetCombo()
    {
        currentComboIndex = 0;
        PlayerActionState.BlockAttack = false;
        PlayerActionState.SpeedMultiplier = 1f;
        HideVisual();
    }

    // ===== Визуализация =====

    private void ShowVisual(ComboStep step)
    {
        HideVisual(); // Уничтожаем предыдущий визуал

        if (step.shape == ComboStep.HitShape.Sphere)
        {
            currentVisual = new GameObject("ComboVisual_Sphere");
            currentVisual.transform.SetParent(transform, false);

            var meshFilter = currentVisual.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateDiscMesh(); // Используем ваш диск для красоты

            var meshRenderer = currentVisual.AddComponent<MeshRenderer>();
            SetupMaterial(meshRenderer);

            currentVisual.transform.localPosition = step.centerOffset;
            currentVisual.transform.localScale = Vector3.one * step.radius;
        }
        else // Box
        {
            currentVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(currentVisual.GetComponent<Collider>()); // Убираем физический коллайдер с визуала

            currentVisual.transform.SetParent(transform, false);

            var meshRenderer = currentVisual.GetComponent<MeshRenderer>();
            SetupMaterial(meshRenderer);

            currentVisual.transform.localPosition = step.centerOffset;
            currentVisual.transform.localScale = step.boxSize;
            currentVisual.transform.localRotation = Quaternion.identity; // Выравниваем по направлению персонажа
        }

        currentVisual.SetActive(true);
    }

    private void SetupMaterial(MeshRenderer renderer)
    {
        Material mat = visualMaterial != null ? new Material(visualMaterial) : new Material(Shader.Find("Sprites/Default"));
        mat.color = effectColor;
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void HideVisual()
    {
        if (currentVisual != null)
        {
            Destroy(currentVisual);
            currentVisual = null;
        }
    }

    private void OnDisable()
    {
        // Гарантированный сброс состояний при деактивации объекта
        ResetCombo();
        HideVisual();
    }

    // ===== Генерация меша диска (из вашего исходника) =====
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