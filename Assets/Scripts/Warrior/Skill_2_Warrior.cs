using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Skill_2_Warrior : MonoBehaviour
{
    [Header("Параметры волны")]
    [SerializeField] private float damage = 80f;
    [SerializeField] private float width = 2f;       // ширина волны
    [SerializeField] private float height = 2f;      // высота волны
    [SerializeField] private float speed = 12f;      // скорость полёта
    [SerializeField] private float flightTime = 2f;  // время полёта
    [SerializeField] private float chargeTime = 1f;  // время зарядки
    [SerializeField] private float cooldown = 8f;    // перезарядка

    [Header("Поиск врагов")]
    [SerializeField] private LayerMask enemyMask;

    [Header("Визуал")]
    [SerializeField] private GameObject waveVisualPrefab;

    [SerializeField] private bool isCharging = false;
    [SerializeField] private bool waveActive = false;
    [SerializeField] private float cooldownTimer = 0f;
    [SerializeField] private GameObject waveVisual;

    public bool IsReady => cooldownTimer <= 0f && !isCharging && !waveActive;

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (Keyboard.current.eKey.wasPressedThisFrame
            && IsReady
            && !PlayerActionState.BlockAttack)
        {
            StartCoroutine(Charge());
        }
    }

    private IEnumerator Charge()
    {
        isCharging = true;
        PlayerActionState.BlockAttack = true; // блокируем движение и другие навыки/атаку
        PlayerActionState.BlockMovement = true;
        // Поворот к мыши НЕ блокируется — игрок задаёт направление во время зарядки

        yield return new WaitForSeconds(chargeTime);

        isCharging = false;
        PlayerActionState.BlockAttack = false;
        PlayerActionState.BlockMovement = false;
        cooldownTimer = cooldown; // кулдаун идёт с момента выпуска волны

        Vector3 dir = transform.forward;
        dir.y = 0f;
        dir.Normalize();

        StartCoroutine(WaveFlight(dir));
    }

    private IEnumerator WaveFlight(Vector3 dir)
    {
        waveActive = true;
        HashSet<Health> alreadyHit = new(); // каждый враг — урон только один раз

        Vector3 origin = transform.position;
        float maxDistance = speed * flightTime;
        float traveled = 0f;

        if (waveVisualPrefab != null)
            waveVisual = Instantiate(waveVisualPrefab, origin, Quaternion.LookRotation(dir));

        while (traveled < maxDistance)
        {
            float step = speed * Time.deltaTime;
            traveled += step;

            Vector3 wavePos = origin + dir * traveled;
            Vector3 prevWavePos = origin + dir * (traveled - step);

            if (waveVisual != null)
                waveVisual.transform.position = wavePos;

            float sweepDepth = step * 0.5f + 0.25f; // половина глубины бокса (за и перед центром)
            Vector3 boxCenter = prevWavePos + dir * (step * 0.5f);
            Vector3 halfExtents = new Vector3(width * 0.5f, height * 0.5f, sweepDepth);
            Quaternion boxRotation = Quaternion.LookRotation(dir);

            Collider[] hits = Physics.OverlapBox(boxCenter, halfExtents, boxRotation, enemyMask);


            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                {
                    Health health = hit.GetComponentInParent<Health>();
                    if (health == null || alreadyHit.Contains(health)) continue;

                    alreadyHit.Add(health);

                    Debug.Log(gameObject.name + " нанес " + damage + " урона " + health.gameObject.name + " вторым навыком.");

                    health.current_count -= damage;
                }
            }

            yield return null;
        }

        if (waveVisual != null) Destroy(waveVisual);
        waveActive = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 halfExtents = new Vector3(width * 0.5f, height * 0.5f, 0.5f);
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
    }
}
