using System;
using Survival.Combat;
using Survival.Core;
using UnityEngine;

namespace Survival.World
{
    public class BlueZoneController : MonoBehaviour
    {
        private static BlueZoneController instance;
        public static BlueZoneController Instance
        {
            get
            {
                if (instance == null)
                    instance = FindFirstObjectByType<BlueZoneController>();
                return instance;
            }
            private set => instance = value;
        }

        [Serializable]
        public struct ZonePhase
        {
            public string label;
            public float targetRadius;
            public float waitDuration;
            public float shrinkDuration;
            public float damagePerSecond;
        }

        public readonly ZonePhase[] Phases =
        {
            new ZonePhase { label = "Vòng Bo 1", targetRadius = 175f, waitDuration = 35f, shrinkDuration = 30f, damagePerSecond = 2f },
            new ZonePhase { label = "Vòng Bo 2", targetRadius = 110f, waitDuration = 28f, shrinkDuration = 25f, damagePerSecond = 4f },
            new ZonePhase { label = "Vòng Bo 3", targetRadius = 60f,  waitDuration = 22f, shrinkDuration = 22f, damagePerSecond = 7f },
            new ZonePhase { label = "Vòng Bo 4", targetRadius = 26f,  waitDuration = 16f, shrinkDuration = 18f, damagePerSecond = 12f },
            new ZonePhase { label = "Vòng Bo Cuối", targetRadius = 4f, waitDuration = 12f, shrinkDuration = 15f, damagePerSecond = 18f }
        };

        public Vector3 CurrentCenter { get; private set; } = Vector3.zero;
        public float CurrentRadius { get; private set; } = 265f;

        public Vector3 TargetCenter { get; private set; } = Vector3.zero;
        public float TargetRadius { get; private set; } = 175f;

        public int CurrentPhaseIndex { get; private set; } = 0;
        public bool IsShrinking { get; private set; } = false;
        public bool IsZoneActive { get; private set; } = false;
        public float PhaseTimer { get; private set; } = 35f;
        public float CurrentDamagePerSecond { get; private set; } = 2f;

        private Vector3 shrinkStartCenter;
        private float shrinkStartRadius;
        private float damageTickTimer;
        private LineRenderer blueRingLine;
        private LineRenderer safeRingLine;
        private Transform zonePillarRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public void StartZoneCycle()
        {
            CurrentCenter = Vector3.zero;
            CurrentRadius = 265f;
            CurrentPhaseIndex = 0;
            IsShrinking = false;
            IsZoneActive = true;
            damageTickTimer = 0f;

            SetupPhase(0);
            CreateVisualRings();
            SetRingsVisible(true);
            UpdateVisualRings();
        }

        public void StopZone()
        {
            IsZoneActive = false;
            IsShrinking = false;
            SetRingsVisible(false);
        }

        private void SetupPhase(int index)
        {
            if (index >= Phases.Length)
            {
                IsShrinking = false;
                PhaseTimer = 0f;
                return;
            }

            CurrentPhaseIndex = index;
            ZonePhase phase = Phases[index];
            IsShrinking = false;
            PhaseTimer = phase.waitDuration;
            CurrentDamagePerSecond = phase.damagePerSecond;

            shrinkStartCenter = CurrentCenter;
            shrinkStartRadius = CurrentRadius;
            TargetRadius = phase.targetRadius;

            float maxOffset = Mathf.Max(0f, (CurrentRadius - TargetRadius) * 0.58f);
            Vector2 randOffset = UnityEngine.Random.insideUnitCircle * maxOffset;
            TargetCenter = new Vector3(
                Mathf.Clamp(CurrentCenter.x + randOffset.x, -170f, 170f),
                0f,
                Mathf.Clamp(CurrentCenter.z + randOffset.y, -170f, 170f)
            );

            GameEvents.RaiseZonePhaseChanged(CurrentPhaseIndex, false);
        }

        private void Update()
        {
            if (!IsZoneActive) return;

            if (CurrentPhaseIndex < Phases.Length)
            {
                ZonePhase phase = Phases[CurrentPhaseIndex];
                PhaseTimer -= Time.deltaTime;

                if (!IsShrinking)
                {
                    if (PhaseTimer <= 0f)
                    {
                        IsShrinking = true;
                        PhaseTimer = phase.shrinkDuration;
                        shrinkStartCenter = CurrentCenter;
                        shrinkStartRadius = CurrentRadius;
                        GameEvents.RaiseZonePhaseChanged(CurrentPhaseIndex, true);
                    }
                }
                else
                {
                    float progress = 1f - Mathf.Clamp01(PhaseTimer / Mathf.Max(1f, phase.shrinkDuration));
                    CurrentCenter = Vector3.Lerp(shrinkStartCenter, TargetCenter, progress);
                    CurrentRadius = Mathf.Lerp(shrinkStartRadius, TargetRadius, progress);

                    if (PhaseTimer <= 0f)
                    {
                        CurrentCenter = TargetCenter;
                        CurrentRadius = TargetRadius;
                        SetupPhase(CurrentPhaseIndex + 1);
                    }
                }
            }

            UpdateVisualRings();

            damageTickTimer += Time.deltaTime;
            if (damageTickTimer >= 1.0f)
            {
                damageTickTimer = 0f;
                ApplyZoneDamageTick();
            }
        }

        public bool IsOutsideBlueZone(Vector3 worldPos)
        {
            Vector2 p = new Vector2(worldPos.x, worldPos.z);
            Vector2 c = new Vector2(CurrentCenter.x, CurrentCenter.z);
            return Vector2.Distance(p, c) > CurrentRadius;
        }

        public float DistanceToSafeZoneBorder(Vector3 worldPos)
        {
            Vector2 p = new Vector2(worldPos.x, worldPos.z);
            Vector2 c = new Vector2(TargetCenter.x, TargetCenter.z);
            return Mathf.Max(0f, Vector2.Distance(p, c) - TargetRadius);
        }

        public string GetStatusBanner()
        {
            if (!IsZoneActive) return "VÒNG BO CHƯA KÍCH HOẠT";
            if (CurrentPhaseIndex >= Phases.Length) return "VÒNG BO CUỐI ĐÃ ĐÓNG HOÀN TOÀN!";

            string phaseName = Phases[CurrentPhaseIndex].label;
            int secs = Mathf.CeilToInt(Mathf.Max(0f, PhaseTimer));
            if (IsShrinking)
                return phaseName + " ĐANG THU HẸP! (" + secs + "s) - Sát thương: " + CurrentDamagePerSecond + " HP/s";
            return phaseName + " thu hẹp sau " + secs + "s";
        }

        private void ApplyZoneDamageTick()
        {
            CombatantHealth[] list = CombatantHealth.ActiveCombatants.ToArray();
            for (int i = 0; i < list.Length; i++)
            {
                CombatantHealth c = list[i];
                if (c == null || !c.IsAlive) continue;
                if (IsOutsideBlueZone(c.transform.position))
                {
                    c.TakeDamage(CurrentDamagePerSecond, null, "Vòng Bo Xanh (Blue Zone)", false, true);
                }
            }
        }

        private void SetRingsVisible(bool visible)
        {
            if (blueRingLine != null) blueRingLine.gameObject.SetActive(visible);
            if (safeRingLine != null) safeRingLine.gameObject.SetActive(visible);
            if (zonePillarRoot != null) zonePillarRoot.gameObject.SetActive(visible);
        }

        private void CreateVisualRings()
        {
            if (blueRingLine == null)
            {
                GameObject blueGo = new GameObject("BlueZoneRing");
                blueGo.transform.SetParent(transform, false);
                blueRingLine = SetupCircleRenderer(blueGo, new Color(0.12f, 0.55f, 1.0f, 0.95f), 0.85f);
            }

            if (safeRingLine == null)
            {
                GameObject safeGo = new GameObject("SafeZoneRing");
                safeGo.transform.SetParent(transform, false);
                safeRingLine = SetupCircleRenderer(safeGo, new Color(0.95f, 0.98f, 1.0f, 0.75f), 0.35f);
            }

            if (zonePillarRoot == null)
            {
                GameObject pillars = new GameObject("BlueZonePillars");
                pillars.transform.SetParent(transform, false);
                zonePillarRoot = pillars.transform;

                Material pillarMat = MaterialPool.GetUnlit(new Color(0.12f, 0.55f, 1.0f, 0.35f));

                for (int i = 0; i < 24; i++)
                {
                    GameObject post = MaterialPool.CreatePrimitive(
                        PrimitiveType.Cube,
                        zonePillarRoot,
                        "ZoneBeacon_" + i,
                        Vector3.zero,
                        new Vector3(0.6f, 45f, 0.6f),
                        new Color(0.12f, 0.55f, 1.0f, 0.35f));
                    post.GetComponent<Renderer>().sharedMaterial = pillarMat;
                }
            }
        }

        private static LineRenderer SetupCircleRenderer(GameObject go, Color color, float width)
        {
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.loop = true;
            lr.useWorldSpace = true;
            lr.positionCount = 64;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.sharedMaterial = MaterialPool.GetUnlit(color);
            return lr;
        }

        private void UpdateVisualRings()
        {
            if (blueRingLine != null)
                DrawCircle(blueRingLine, CurrentCenter, CurrentRadius, 1.2f);

            if (safeRingLine != null)
                DrawCircle(safeRingLine, TargetCenter, TargetRadius, 0.45f);

            if (zonePillarRoot != null)
            {
                int count = zonePillarRoot.childCount;
                for (int i = 0; i < count; i++)
                {
                    float angle = (i / (float)count) * Mathf.PI * 2f;
                    Vector3 pos = CurrentCenter + new Vector3(Mathf.Cos(angle) * CurrentRadius, 22.5f, Mathf.Sin(angle) * CurrentRadius);
                    zonePillarRoot.GetChild(i).position = pos;
                }
            }
        }

        private static void DrawCircle(LineRenderer lr, Vector3 center, float radius, float yHeight)
        {
            int segments = lr.positionCount;
            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 pt = new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    yHeight,
                    center.z + Mathf.Sin(angle) * radius
                );
                lr.SetPosition(i, pt);
            }
        }
    }
}
