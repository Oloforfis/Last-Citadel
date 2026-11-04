using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Skill_1_Warrior : MonoBehaviour
{
    [Header("Параметры навыка")]
    [SerializeField] private float damage = 20f;        // урон за тик
    [SerializeField] private float radius = 3f;         // радиус зоны поражения
    [SerializeField] private float duration = 3f;       // длительность вращения
    [SerializeField] private float tickInterval = 0.5f; // период урона
    [SerializeField] private float cooldown = 6f;       // перезарядка

    [Header("Замедление во время навыка")]
    [Range(0.1f, 1f)]
    [SerializeField] private float speedMultiplierDuringCast = 0.5f;

    [Header("Поиск врагов")]
    [SerializeField] private LayerMask enemyMask;

    [SerializeField] private bool isCasting = false;
    [SerializeField] private float cooldownTimer = 0f;

    private readonly HashSet<Health> hitThisTick = new();

    public bool IsReady => cooldownTimer <= 0f && !isCasting;

    void Update()
    {
        // Тик кулдауна
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

                    if (health.current_count < 0)
                        Destroy(health.gameObject);
                }
            }
        }
    }
}
