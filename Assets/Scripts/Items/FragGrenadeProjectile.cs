using System.Collections;
using Survival.Combat;
using Survival.Core;
using UnityEngine;

namespace Survival.Items
{
    public class FragGrenadeProjectile : MonoBehaviour
    {
        public CombatantHealth owner;
        public float fuseSeconds = 2.8f;
        public float blastRadius = 9.5f;
        public float maxBlastDamage = 115f;

        private bool exploded = false;

        public static void ThrowGrenade(Vector3 origin, Vector3 throwVelocity, CombatantHealth thrower)
        {
            GameObject go = MaterialPool.CreatePrimitive(
                PrimitiveType.Sphere,
                thrower != null ? thrower.transform.parent : null,
                "LiveFragGrenade",
                origin,
                new Vector3(0.26f, 0.30f, 0.26f),
                new Color(0.22f, 0.36f, 0.18f),
                true);

            if (thrower != null)
            {
                Collider grenadeCol = go.GetComponent<Collider>();
                Collider[] throwerCols = thrower.GetComponentsInChildren<Collider>();
                if (grenadeCol != null && throwerCols != null)
                {
                    for (int i = 0; i < throwerCols.Length; i++)
                    {
                        if (throwerCols[i] != null)
                            Physics.IgnoreCollision(grenadeCol, throwerCols[i], true);
                    }
                }
            }

            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.6f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.velocity = throwVelocity;

            FragGrenadeProjectile proj = go.AddComponent<FragGrenadeProjectile>();
            proj.owner = thrower;
        }

        private void Start()
        {
            StartCoroutine(FuseRoutine());
        }

        private IEnumerator FuseRoutine()
        {
            yield return new WaitForSeconds(fuseSeconds);
            Explode();
        }

        private void Explode()
        {
            if (exploded) return;
            exploded = true;

            Vector3 pos = transform.position + Vector3.up * 0.25f;

            CombatantHealth[] snapshot = CombatantHealth.ActiveCombatants.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                CombatantHealth target = snapshot[i];
                if (target == null || !target.IsAlive) continue;

                Vector3 targetCenter = target.transform.position + Vector3.up * 0.95f;
                float dist = Vector3.Distance(pos, targetCenter);
                if (dist > blastRadius) continue;

                float coverMultiplier = 1f;
                Vector3 dir = (targetCenter - pos).normalized;
                if (Physics.Raycast(pos, dir, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (!hit.transform.IsChildOf(target.transform) && !hit.transform.IsChildOf(transform))
                    {
                        coverMultiplier = 0.35f;
                    }
                }

                float falloff = Mathf.Clamp01(1f - (dist / blastRadius));
                float dmg = maxBlastDamage * (0.25f + 0.75f * falloff) * coverMultiplier;
                target.TakeDamage(dmg, owner, "Frag Grenade", false, false);
            }

            GameObject blastVisual = MaterialPool.CreatePrimitive(
                PrimitiveType.Sphere,
                null,
                "GrenadeExplosion",
                pos,
                Vector3.one * 0.5f,
                new Color(1f, 0.52f, 0.12f),
                false);
            blastVisual.AddComponent<ExplosionExpandEffect>();

            Destroy(gameObject);
        }
    }

    public class ExplosionExpandEffect : MonoBehaviour
    {
        private float elapsed = 0f;
        private const float Duration = 0.38f;

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Duration);
            float scale = Mathf.Lerp(1.2f, 11.5f, t);
            transform.localScale = Vector3.one * scale;

            if (elapsed >= Duration)
            {
                Destroy(gameObject);
            }
        }
    }
}
