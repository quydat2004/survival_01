using Survival.Combat;
using Survival.Items;
using Survival.World;
using UnityEngine;

namespace Survival.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(CombatantHealth))]
    public class BotAIController : MonoBehaviour
    {
        public CombatantHealth Health { get; private set; }
        public WeaponSpec EquippedWeapon { get; private set; }

        private bool isParachuting = true;
        private Vector3 moveTarget;
        private float repathTimer;
        private float scanTargetTimer;
        private float fireCooldown;
        private float strafeTimer;
        private float strafeDir = 1f;
        private int medkitsCarried = 1;
        private float verticalVelocityY;

        private CharacterController controller;
        private CapsuleCollider hitbox;
        private CombatantHealth currentEnemy;
        private CharacterVisualHandles visualHandles;
        private readonly CharacterAnimationDriver animDriver = new CharacterAnimationDriver();
        private float currentMoveSpeed;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            controller.height = 1.9f;
            controller.radius = 0.42f;
            controller.center = new Vector3(0f, 0.95f, 0f);
            controller.stepOffset = 0.45f;
            controller.slopeLimit = 50f;

            hitbox = GetComponent<CapsuleCollider>();
            hitbox.height = 1.9f;
            hitbox.radius = 0.42f;
            hitbox.center = new Vector3(0f, 0.95f, 0f);

            Health = GetComponent<CombatantHealth>();
        }

        public void InitializeBot(string botName, Vector3 dropGroundPos)
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            if (hitbox == null) hitbox = GetComponent<CapsuleCollider>();
            if (Health == null) Health = GetComponent<CombatantHealth>();

            Health.InitializeCombatant(botName, false);
            Health.OnDied -= OnBotDied;
            Health.OnDied += OnBotDied;

            WeaponId[] starterPool = { WeaponId.UMP45, WeaponId.M416, WeaponId.AKM, WeaponId.S1897, WeaponId.P1911 };
            EquippedWeapon = WeaponDatabase.GetSpec(starterPool[Random.Range(0, starterPool.Length)]);

            if (Random.value < 0.65f) Health.EquipArmorVest(Random.Range(1, 3));
            if (Random.value < 0.65f) Health.EquipHelmet(Random.Range(1, 3));

            isParachuting = true;
            verticalVelocityY = 0f;

            controller.enabled = false;
            transform.position = new Vector3(dropGroundPos.x, Random.Range(58f, 82f), dropGroundPos.z);
            moveTarget = dropGroundPos;
            controller.enabled = true;

            visualHandles = CharacterVisualBuilder.BuildBotVisuals(transform, EquippedWeapon.id);
            animDriver.Initialize(visualHandles.legacyAnimation);
            CharacterVisualBuilder.UpdateEquipmentVisuals(ref visualHandles, Health, new WeaponInstance(EquippedWeapon));
        }

        private void Update()
        {
            if (Health == null || !Health.IsAlive)
                return;

            if (isParachuting)
            {
                UpdateParachuteDescent();
                animDriver.UpdateLocomotion(0f, true, true, Time.deltaTime);
                return;
            }

            if (Health.currentHealth < 45f && medkitsCarried > 0 &&
                (currentEnemy == null || Vector3.Distance(transform.position, currentEnemy.transform.position) > 18f))
            {
                medkitsCarried--;
                Health.Heal(50f);
            }

            scanTargetTimer -= Time.deltaTime;
            if (scanTargetTimer <= 0f)
            {
                scanTargetTimer = Random.Range(0.45f, 0.85f);
                AcquireTargetAndLoot();
            }

            UpdateMovementAndZone();
            animDriver.UpdateLocomotion(currentMoveSpeed, !controller.isGrounded, true, Time.deltaTime);
            UpdateCombatEngagement();
        }

        private void UpdateParachuteDescent()
        {
            Vector3 target = new Vector3(moveTarget.x, 0f, moveTarget.z);
            Vector3 horizDir = (target - new Vector3(transform.position.x, 0f, transform.position.z));
            Vector3 horizMotion = horizDir.magnitude > 0.5f ? horizDir.normalized * 12.5f : Vector3.zero;

            Vector3 motion = (horizMotion + Vector3.down * 14.5f) * Time.deltaTime;
            CollisionFlags flags = controller.Move(motion);

            bool touchedBelow = (flags & CollisionFlags.Below) != 0 || controller.isGrounded;
            if (touchedBelow || transform.position.y <= 0.25f)
            {
                isParachuting = false;
                verticalVelocityY = -2f;
                if (visualHandles.parachuteVisual != null)
                    Destroy(visualHandles.parachuteVisual);
            }
        }

        private void AcquireTargetAndLoot()
        {
            Vector3 myPos = transform.position;
            for (int i = LootItem.ActiveLoot.Count - 1; i >= 0; i--)
            {
                LootItem item = LootItem.ActiveLoot[i];
                if (item == null || item.IsConsumed) continue;
                float d = Vector3.Distance(myPos, item.transform.position);
                if (d < 2.6f)
                {
                    if (item.category == LootCategory.Weapon)
                    {
                        EquippedWeapon = WeaponDatabase.GetSpec(item.weaponId);
                        CharacterVisualBuilder.AttachWeaponModel(ref visualHandles, EquippedWeapon.id);
                    }
                    else if (item.category == LootCategory.ArmorVest && Health.CanUpgradeArmorVest(item.gearTier))
                    {
                        Health.EquipArmorVest(item.gearTier);
                        CharacterVisualBuilder.UpdateEquipmentVisuals(ref visualHandles, Health, null);
                    }
                    else if (item.category == LootCategory.Helmet && Health.CanUpgradeHelmet(item.gearTier))
                    {
                        Health.EquipHelmet(item.gearTier);
                        CharacterVisualBuilder.UpdateEquipmentVisuals(ref visualHandles, Health, null);
                    }
                    else if (item.category == LootCategory.Medkit)
                    {
                        medkitsCarried++;
                    }

                    item.ConsumeAndDestroy();
                    break;
                }
            }

            currentEnemy = null;
            float bestDist = 56f;

            for (int i = 0; i < CombatantHealth.ActiveCombatants.Count; i++)
            {
                CombatantHealth candidate = CombatantHealth.ActiveCombatants[i];
                if (candidate == null || candidate == Health || !candidate.IsAlive)
                    continue;

                if (candidate.transform.position.y > 12f)
                    continue;

                float dist = Vector3.Distance(myPos, candidate.transform.position);
                if (dist < bestDist && HasLineOfSight(candidate))
                {
                    bestDist = dist;
                    currentEnemy = candidate;
                }
            }
        }

        private bool HasLineOfSight(CombatantHealth target)
        {
            Vector3 eye = transform.position + Vector3.up * 1.5f + transform.forward * 0.45f;
            Vector3 targetChest = target.transform.position + Vector3.up * 1.25f;
            Vector3 toTarget = targetChest - eye;
            float dist = toTarget.magnitude;
            if (dist <= 0.1f) return true;

            if (Physics.Raycast(eye, toTarget / dist, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(target.transform) || hit.transform.IsChildOf(transform))
                    return true;
                return false;
            }
            return true;
        }

        private void UpdateMovementAndZone()
        {
            repathTimer -= Time.deltaTime;
            BlueZoneController zone = BlueZoneController.Instance;

            bool outsideZone = zone != null && zone.IsOutsideBlueZone(transform.position);
            bool outsideSafe = zone != null && zone.DistanceToSafeZoneBorder(transform.position) > 4f;

            if (outsideZone || (outsideSafe && currentEnemy == null && repathTimer <= 0f))
            {
                repathTimer = Random.Range(3.5f, 6.0f);
                Vector2 rand = Random.insideUnitCircle * Mathf.Max(5f, zone.TargetRadius * 0.65f);
                moveTarget = new Vector3(zone.TargetCenter.x + rand.x, 0f, zone.TargetCenter.z + rand.y);
            }
            else if (repathTimer <= 0f)
            {
                repathTimer = Random.Range(4.0f, 7.5f);
                Vector3 center = zone != null ? zone.TargetCenter : Vector3.zero;
                float rad = zone != null ? Mathf.Min(150f, zone.TargetRadius * 0.85f) : 120f;
                Vector2 rand = Random.insideUnitCircle * rad;
                moveTarget = new Vector3(center.x + rand.x, 0f, center.z + rand.y);
            }

            Vector3 flatPos = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 desiredDir;
            float moveSpeed = outsideZone ? 6.4f : 4.8f;

            if (currentEnemy != null && !outsideZone)
            {
                Vector3 toEnemy = currentEnemy.transform.position - transform.position;
                toEnemy.y = 0f;
                float dist = toEnemy.magnitude;

                strafeTimer -= Time.deltaTime;
                if (strafeTimer <= 0f)
                {
                    strafeTimer = Random.Range(1.2f, 2.4f);
                    strafeDir = Random.value < 0.5f ? -1f : 1f;
                }

                Vector3 rightStrafe = Vector3.Cross(Vector3.up, toEnemy.normalized) * strafeDir;
                if (dist > 24f)
                    desiredDir = (toEnemy.normalized * 0.75f + rightStrafe * 0.65f).normalized;
                else if (dist < 9f)
                    desiredDir = (-toEnemy.normalized * 0.55f + rightStrafe * 0.85f).normalized;
                else
                    desiredDir = rightStrafe;

                moveSpeed = 3.9f;

                if (toEnemy.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toEnemy.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 8f * Time.deltaTime);
                }
            }
            else
            {
                Vector3 toTarget = moveTarget - flatPos;
                toTarget.y = 0f;
                if (toTarget.magnitude < 2.2f)
                {
                    repathTimer = 0f;
                    desiredDir = Vector3.zero;
                }
                else
                {
                    desiredDir = toTarget.normalized;
                    Quaternion targetRot = Quaternion.LookRotation(desiredDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 6f * Time.deltaTime);
                }
            }

            if (controller.isGrounded)
            {
                if (verticalVelocityY < 0f) verticalVelocityY = -2f;
            }
            else
            {
                verticalVelocityY += -21f * Time.deltaTime;
            }

            Vector3 motion = desiredDir * (moveSpeed * Time.deltaTime) + new Vector3(0f, verticalVelocityY * Time.deltaTime, 0f);
            controller.Move(motion);
            currentMoveSpeed = desiredDir.magnitude * moveSpeed;

            Vector3 pos = transform.position;
            if (pos.x < -260f || pos.x > 260f || pos.z < -260f || pos.z > 260f)
            {
                pos.x = Mathf.Clamp(pos.x, -260f, 260f);
                pos.z = Mathf.Clamp(pos.z, -260f, 260f);
                controller.enabled = false;
                transform.position = pos;
                controller.enabled = true;
            }
        }

        private void UpdateCombatEngagement()
        {
            fireCooldown -= Time.deltaTime;
            if (currentEnemy == null || !currentEnemy.IsAlive || fireCooldown > 0f)
                return;

            if (EquippedWeapon == null)
                EquippedWeapon = WeaponDatabase.GetSpec(WeaponId.UMP45);

            fireCooldown = Mathf.Max(0.32f, EquippedWeapon.fireInterval * Random.Range(2.4f, 4.2f));
            animDriver.TriggerShoot();

            Vector3 startPos = visualHandles.gunMuzzleTransform != null
                ? visualHandles.gunMuzzleTransform.position
                : transform.position + Vector3.up * 1.4f + transform.forward * 0.5f;
            Vector3 targetCenter = currentEnemy.transform.position + Vector3.up * 1.15f;
            float dist = Vector3.Distance(startPos, targetCenter);

            float scatterDegrees = Mathf.Lerp(1.6f, 4.8f, Mathf.Clamp01(dist / 55f));
            Vector3 aimDir = (targetCenter - startPos).normalized;

            WeaponBallistics.FireWeapon(
                Health,
                EquippedWeapon,
                startPos,
                aimDir,
                startPos,
                scatterDegrees,
                new Color(1f, 0.52f, 0.22f),
                0.55f,
                0.25f);
        }

        private void OnBotDied(CombatantHealth victim, CombatantHealth killer, string weapon, bool headshot)
        {
            animDriver.TriggerDeath();
            if (controller != null) controller.enabled = false;
            Destroy(gameObject, 1.35f);
        }
    }
}
