using Survival.Items;
using UnityEngine;

namespace Survival.Combat
{
    public static class WeaponBallistics
    {
        private static readonly RaycastHit[] HitBuffer = new RaycastHit[24];

        public static void FireWeapon(
            CombatantHealth shooter,
            WeaponSpec spec,
            Vector3 aimOrigin,
            Vector3 aimDirection,
            Vector3 muzzleOrigin,
            float spreadDegrees,
            Color tracerColor,
            float damageMultiplier = 1f,
            float maxBotHeadshotChance = 1f)
        {
            if (shooter == null || spec == null) return;

            int pellets = Mathf.Max(1, spec.pelletsPerShot);
            Quaternion baseLook = aimDirection.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(aimDirection.normalized)
                : shooter.transform.rotation;

            for (int i = 0; i < pellets; i++)
            {
                Vector2 randCone = Random.insideUnitCircle * Mathf.Max(0f, spreadDegrees);
                Quaternion spreadRot = Quaternion.Euler(randCone.y, randCone.x, 0f);
                Vector3 shotDir = (baseLook * spreadRot) * Vector3.forward;

                float range = Mathf.Max(10f, spec.maxRange);
                Vector3 endPoint = aimOrigin + shotDir * range;
                bool hitFlesh = false;

                int hitCount = Physics.RaycastNonAlloc(
                    aimOrigin,
                    shotDir,
                    HitBuffer,
                    range,
                    ~0,
                    QueryTriggerInteraction.Ignore);

                float closestDist = float.MaxValue;
                RaycastHit bestHit = default;
                bool foundHit = false;

                for (int h = 0; h < hitCount; h++)
                {
                    RaycastHit candidate = HitBuffer[h];
                    if (candidate.collider == null)
                        continue;

                    if (candidate.transform.IsChildOf(shooter.transform))
                        continue;

                    if (candidate.distance < closestDist)
                    {
                        closestDist = candidate.distance;
                        bestHit = candidate;
                        foundHit = true;
                    }
                }

                if (foundHit)
                {
                    endPoint = bestHit.point;
                    CombatantHealth targetHealth = bestHit.collider.GetComponentInParent<CombatantHealth>();
                    if (targetHealth != null && targetHealth != shooter && targetHealth.IsAlive)
                    {
                        hitFlesh = true;
                        bool hitHeadRegion = (bestHit.point.y - targetHealth.transform.position.y) >= 1.42f;
                        bool isHeadshot = hitHeadRegion && (maxBotHeadshotChance >= 1f || Random.value <= maxBotHeadshotChance);
                        float rawDamage = Mathf.Max(1f, spec.damagePerPellet * damageMultiplier);
                        targetHealth.TakeDamage(rawDamage, shooter, spec.displayName, isHeadshot, false);
                    }
                }

                BulletTracerEffect.SpawnTracer(muzzleOrigin, endPoint, tracerColor, hitFlesh);
            }
        }
    }
}
