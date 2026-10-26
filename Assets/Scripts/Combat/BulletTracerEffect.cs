using Survival.Core;
using UnityEngine;

namespace Survival.Combat
{
    public class BulletTracerEffect : MonoBehaviour
    {
        private float timer;
        private const float Lifetime = 0.08f;

        public static void SpawnTracer(Vector3 startPos, Vector3 endPos, Color color, bool hitFlesh = false)
        {
            if (!Application.isPlaying)
                return;

            GameObject go = new GameObject("BulletTracer");
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, startPos);
            lr.SetPosition(1, endPos);
            lr.startWidth = 0.042f;
            lr.endWidth = 0.012f;
            lr.useWorldSpace = true;
            lr.sharedMaterial = MaterialPool.GetUnlit(color);
            go.AddComponent<BulletTracerEffect>();

            SpawnMuzzleFlash(startPos, (endPos - startPos).normalized);
            SpawnImpactBurst(endPos, hitFlesh);
        }

        private static void SpawnMuzzleFlash(Vector3 muzzlePos, Vector3 shotDir)
        {
            GameObject flash = MaterialPool.CreatePrimitive(
                PrimitiveType.Sphere,
                null,
                "MuzzleFlashVFX",
                muzzlePos + shotDir * 0.08f,
                new Vector3(0.18f, 0.18f, 0.36f),
                new Color(1f, 0.86f, 0.32f),
                false);
            if (shotDir.sqrMagnitude > 0.001f)
                flash.transform.rotation = Quaternion.LookRotation(shotDir);

            Light flashLight = flash.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.color = new Color(1f, 0.78f, 0.28f);
            flashLight.range = 4.5f;
            flashLight.intensity = 2.4f;
            Destroy(flash, 0.055f);

            // Ejected brass shell casing
            GameObject shell = MaterialPool.CreatePrimitive(
                PrimitiveType.Cylinder,
                null,
                "BrassShell",
                muzzlePos - shotDir * 0.15f + Vector3.up * 0.04f,
                new Vector3(0.025f, 0.045f, 0.025f),
                new Color(0.92f, 0.75f, 0.22f),
                false);
            shell.AddComponent<EjectedShellMotion>();
        }

        private static void SpawnImpactBurst(Vector3 hitPos, bool hitFlesh)
        {
            Color mainColor = hitFlesh ? new Color(0.88f, 0.12f, 0.12f) : new Color(1f, 0.82f, 0.30f);
            float coreScale = hitFlesh ? 0.30f : 0.18f;

            GameObject core = MaterialPool.CreatePrimitive(
                PrimitiveType.Sphere,
                null,
                "ImpactCore",
                hitPos,
                Vector3.one * coreScale,
                mainColor,
                false);
            Destroy(core, 0.14f);

            int count = hitFlesh ? 5 : 3;
            for (int i = 0; i < count; i++)
            {
                GameObject droplet = MaterialPool.CreatePrimitive(
                    PrimitiveType.Cube,
                    null,
                    "ImpactParticle",
                    hitPos,
                    Vector3.one * (hitFlesh ? 0.075f : 0.055f),
                    mainColor,
                    false);
                ImpactParticleMotion motion = droplet.AddComponent<ImpactParticleMotion>();
                motion.velocity = Random.onUnitSphere * Random.Range(2.2f, 5.2f) + Vector3.up * 2.0f;
            }
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= Lifetime)
            {
                Destroy(gameObject);
            }
        }
    }

    public class EjectedShellMotion : MonoBehaviour
    {
        private Vector3 velocity;
        private float elapsed;

        private void Start()
        {
            velocity = new Vector3(Random.Range(-1.8f, 1.8f), Random.Range(2.2f, 3.4f), Random.Range(-1.8f, 1.8f));
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            velocity.y -= 14f * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            transform.Rotate(540f * Time.deltaTime, 380f * Time.deltaTime, 0f);
            if (elapsed >= 0.45f)
                Destroy(gameObject);
        }
    }

    public class ImpactParticleMotion : MonoBehaviour
    {
        public Vector3 velocity;
        private float elapsed;

        private void Update()
        {
            elapsed += Time.deltaTime;
            velocity.y -= 16f * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 6f * Time.deltaTime);
            if (elapsed >= 0.24f)
                Destroy(gameObject);
        }
    }
}
