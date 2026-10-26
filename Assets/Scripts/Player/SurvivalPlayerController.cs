using Survival.Combat;
using Survival.Core;
using Survival.Items;
using UnityEngine;

namespace Survival.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(CombatantHealth))]
    public class SurvivalPlayerController : MonoBehaviour
    {
        public static SurvivalPlayerController LocalInstance { get; private set; }

        [Header("Movement Settings")]
        public float walkSpeed = 5.6f;
        public float sprintSpeed = 8.4f;
        public float crouchSpeed = 3.4f;
        public float jumpHeight = 1.45f;
        public float gravity = -21f;
        public float mouseSensitivity = 2.2f;

        [Header("State")]
        public bool isParachuting = true;
        public bool isFirstPerson = false;
        public bool isCrouching = false;
        public bool isAiming = false;
        public bool isMapOpen = false;
        public bool isPaused = false;

        [Header("Modular Subsystems")]
        public PlayerInventory inventory = new PlayerInventory();
        public readonly PlayerCameraController cameraController = new PlayerCameraController();

        public WeaponInstance[] weaponSlots => inventory.weaponSlots;
        public int activeSlotIndex => inventory.activeSlotIndex;
        public int reserveAmmo => inventory.reserveAmmo;
        public int medkitCount => inventory.medkitCount;
        public int bandageCount => inventory.bandageCount;
        public int energyDrinkCount => inventory.energyDrinkCount;
        public int grenadeCount => inventory.grenadeCount;
        public bool isReloading => inventory.isReloading;
        public float ReloadProgress01 => inventory.reloadProgress01;
        public WeaponInstance ActiveWeapon => inventory.ActiveWeapon;

        public CombatantHealth Health { get; private set; }
        public LootItem NearestLoot { get; private set; }

        public float LastHitMarkerTime { get; private set; } = -10f;
        public bool LastHitWasHeadshot { get; private set; }
        public bool LastHitWasKill { get; private set; }
        public int LastHitDamage { get; private set; }
        public string LastActionNotification { get; private set; } = string.Empty;
        public float LastNotificationTime { get; private set; } = -10f;

        private CharacterController controller;
        private CapsuleCollider hitbox;
        private CharacterVisualHandles visualHandles;
        private readonly CharacterAnimationDriver animDriver = new CharacterAnimationDriver();
        private Vector3 verticalVelocity;
        private float currentPlanarSpeed;
        private float fireCooldown;

        private void Awake()
        {
            LocalInstance = this;
            controller = GetComponent<CharacterController>();
            hitbox = GetComponent<CapsuleCollider>();
            if (hitbox != null)
            {
                hitbox.height = 1.9f;
                hitbox.radius = 0.42f;
                hitbox.center = new Vector3(0f, 0.95f, 0f);
            }
            Health = GetComponent<CombatantHealth>();
        }

        private void OnEnable()
        {
            GameEvents.OnLocalEquipmentChanged += RefreshVisualEquipment;
            GameEvents.OnPlayerNotification += HandleGlobalNotification;
        }

        private void OnDisable()
        {
            GameEvents.OnLocalEquipmentChanged -= RefreshVisualEquipment;
            GameEvents.OnPlayerNotification -= HandleGlobalNotification;
        }

        public void SetupSpawn(Vector3 dropPosition, string playerName)
        {
            LocalInstance = this;
            if (controller == null) controller = GetComponent<CharacterController>();
            if (Health == null) Health = GetComponent<CombatantHealth>();

            Health.InitializeCombatant(playerName, true);
            Health.OnDealtDamage -= HandleDealtDamage;
            Health.OnDealtDamage += HandleDealtDamage;
            Health.OnDied -= HandlePlayerDied;
            Health.OnDied += HandlePlayerDied;

            inventory.InitializeDefaultLoadout();

            isParachuting = true;
            isCrouching = false;
            isAiming = false;
            isMapOpen = false;
            isPaused = false;
            verticalVelocity = Vector3.zero;
            fireCooldown = 0f;

            controller.enabled = false;
            transform.position = new Vector3(dropPosition.x, 78f, dropPosition.z);
            float initialYaw = Mathf.Atan2(-dropPosition.x, -dropPosition.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, initialYaw, 0f);
            controller.enabled = true;

            cameraController.Initialize(initialYaw);

            if (visualHandles.visualBodyRoot != null)
                MaterialPool.SafeDestroy(visualHandles.visualBodyRoot.gameObject);
            if (visualHandles.parachuteVisual != null)
                MaterialPool.SafeDestroy(visualHandles.parachuteVisual);

            WeaponId startWep = ActiveWeapon != null && ActiveWeapon.spec != null ? ActiveWeapon.spec.id : WeaponId.UMP45;
            string selectedCharacter = PlayerPrefs.GetString("SB_CHAR_MODEL", "George");
            int selectedSkin = PlayerPrefs.GetInt("SB_CHAR_SKIN", 0);
            visualHandles = CharacterVisualBuilder.BuildPlayerVisuals(transform, startWep, selectedCharacter, selectedSkin);
            animDriver.Initialize(visualHandles.legacyAnimation);
            RefreshVisualEquipment();

            LockCursor(true);
            Notify("Đang nhảy dù! Dùng WASD để lái dù hướng về khu nhà nhặt vũ khí!");
        }

        private void OnDestroy()
        {
            if (LocalInstance == this)
                LocalInstance = null;
        }

        private void Update()
        {
            if (Health == null || !Health.IsAlive)
            {
                cameraController.UpdateSpectatorView(transform);
                return;
            }

            HandleUtilityHotkeys();

            if (isPaused)
                return;

            if (isParachuting)
            {
                UpdateParachuteMovement();
                animDriver.UpdateLocomotion(0f, true, true, Time.deltaTime);
                cameraController.UpdateActiveLook(
                    transform,
                    mouseSensitivity,
                    false,
                    false,
                    true,
                    isFirstPerson,
                    ActiveWeapon,
                    isMapOpen || isPaused);
                ScanNearbyLoot();
                return;
            }

            cameraController.UpdateActiveLook(
                transform,
                mouseSensitivity,
                isAiming,
                isCrouching,
                false,
                isFirstPerson,
                ActiveWeapon,
                isMapOpen || isPaused);

            UpdateGroundMovement();
            animDriver.UpdateLocomotion(currentPlanarSpeed, !controller.isGrounded, true, Time.deltaTime);
            ScanNearbyLoot();
            HandleCombatInput();
        }

        private void HandleUtilityHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isMapOpen)
                {
                    isMapOpen = false;
                }
                else
                {
                    isPaused = !isPaused;
                    if (isPaused) isAiming = false;
                    LockCursor(!isPaused);
                }
            }

            if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Tab))
            {
                isMapOpen = !isMapOpen;
                if (isMapOpen) isAiming = false;
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                isFirstPerson = !isFirstPerson;
                if (visualHandles.visualBodyRoot != null)
                    visualHandles.visualBodyRoot.gameObject.SetActive(!isFirstPerson);
                Notify(isFirstPerson ? "Chế độ góc nhìn thứ nhất (FPP)" : "Chế độ góc nhìn thứ ba (TPP)");
            }
        }

        private void UpdateParachuteMovement()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            Vector3 horizMove = (forward * Mathf.Max(-0.3f, v + 0.55f) + right * h).normalized * 16.5f;

            bool fastDive = Input.GetKey(KeyCode.LeftShift) || v > 0.5f;
            float fallSpeed = fastDive ? -19.5f : -12.5f;

            Vector3 motion = new Vector3(horizMove.x, fallSpeed, horizMove.z) * Time.deltaTime;
            CollisionFlags flags = controller.Move(motion);

            bool touchedBelow = (flags & CollisionFlags.Below) != 0 || controller.isGrounded;
            if (touchedBelow || transform.position.y <= 0.15f)
            {
                isParachuting = false;
                if (visualHandles.parachuteVisual != null)
                    visualHandles.parachuteVisual.SetActive(false);

                if (transform.position.y < 0.05f)
                {
                    Vector3 landedPos = transform.position;
                    landedPos.y = 0.05f;
                    controller.enabled = false;
                    transform.position = landedPos;
                    controller.enabled = true;
                }

                verticalVelocity = new Vector3(0f, -2f, 0f);
                Notify("Đã tiếp đất an toàn! Nhấn [F] để nhặt súng & trang bị.");
            }
        }

        private void UpdateGroundMovement()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                SetCrouchState(!isCrouching);
            }

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 moveDir = (transform.forward * v + transform.right * h);
            if (moveDir.sqrMagnitude > 1f)
                moveDir.Normalize();

            bool sprinting = Input.GetKey(KeyCode.LeftShift) && v > 0.1f && !isAiming && !isCrouching;
            float baseSpeed = isCrouching ? crouchSpeed : (sprinting ? sprintSpeed : walkSpeed);
            if (isAiming) baseSpeed *= 0.68f;
            float finalSpeed = baseSpeed * Health.GetSpeedBoostMultiplier();

            if (controller.isGrounded)
            {
                if (verticalVelocity.y < 0f)
                    verticalVelocity.y = -2f;

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (isCrouching)
                        SetCrouchState(false);

                    verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }

            verticalVelocity.y += gravity * Time.deltaTime;

            currentPlanarSpeed = moveDir.magnitude * finalSpeed;
            Vector3 velocity = moveDir * finalSpeed + new Vector3(0f, verticalVelocity.y, 0f);
            controller.Move(velocity * Time.deltaTime);

            Vector3 pos = transform.position;
            bool clamped = false;
            if (pos.x < -280f || pos.x > 280f) { pos.x = Mathf.Clamp(pos.x, -280f, 280f); clamped = true; }
            if (pos.z < -280f || pos.z > 280f) { pos.z = Mathf.Clamp(pos.z, -280f, 280f); clamped = true; }
            if (pos.y < -2f) { pos.y = 0.2f; verticalVelocity.y = 0f; clamped = true; }

            if (clamped)
            {
                controller.enabled = false;
                transform.position = pos;
                controller.enabled = true;
            }
        }

        private void SetCrouchState(bool crouch)
        {
            isCrouching = crouch;
            controller.height = isCrouching ? 1.25f : 1.9f;
            controller.center = new Vector3(0f, controller.height * 0.5f, 0f);
            if (visualHandles.visualBodyRoot != null)
            {
                visualHandles.visualBodyRoot.localScale = new Vector3(1f, isCrouching ? 0.68f : 1f, 1f);
            }
        }

        private void HandleCombatInput()
        {
            fireCooldown -= Time.deltaTime;

            isAiming = !isMapOpen && !isReloading && Input.GetMouseButton(1);

            if (Input.GetKeyDown(KeyCode.Alpha1) && inventory.TrySwitchSlot(0, out string msg1)) Notify(msg1);
            if (Input.GetKeyDown(KeyCode.Alpha2) && inventory.TrySwitchSlot(1, out string msg2)) Notify(msg2);
            if (Input.GetKeyDown(KeyCode.Alpha3) && inventory.TrySwitchSlot(2, out string msg3)) Notify(msg3);

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.02f)
            {
                int dir = scroll > 0f ? -1 : 1;
                if (inventory.TryCycleSlot(dir, out string cycleMsg))
                    Notify(cycleMsg);
            }

            if (isReloading)
            {
                inventory.TickReload(Time.deltaTime);
                return;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                if (!inventory.StartReload(out string reloadMsg) && !string.IsNullOrEmpty(reloadMsg))
                    Notify(reloadMsg);
            }

            if ((Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.E)) && NearestLoot != null)
            {
                if (inventory.TryPickupLoot(NearestLoot, Health, transform.parent, out string pickupMsg))
                {
                    NearestLoot = null;
                    Notify(pickupMsg);
                }
                else if (!string.IsNullOrEmpty(pickupMsg))
                {
                    Notify(pickupMsg);
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.H))
            {
                UseHealingItem();
            }

            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                UseEnergyDrink();
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                ThrowFragGrenade();
            }

            if (!isMapOpen && ActiveWeapon != null)
            {
                bool triggerPressed = ActiveWeapon.spec.isAutomatic
                    ? Input.GetMouseButton(0)
                    : Input.GetMouseButtonDown(0);

                if (triggerPressed && fireCooldown <= 0f)
                {
                    if (ActiveWeapon.currentAmmo > 0)
                    {
                        FireCurrentWeapon();
                    }
                    else
                    {
                        if (!inventory.StartReload(out string noAmmoMsg) && !string.IsNullOrEmpty(noAmmoMsg))
                            Notify(noAmmoMsg);
                    }
                }
            }
        }

        private void FireCurrentWeapon()
        {
            WeaponInstance weapon = ActiveWeapon;
            if (weapon == null) return;

            weapon.currentAmmo--;
            fireCooldown = weapon.spec.fireInterval;
            animDriver.TriggerShoot();

            cameraController.ApplyRecoil(isAiming ? 0.45f : 0.75f, Random.Range(-0.18f, 0.18f));

            float spread = isAiming ? weapon.spec.adsSpreadDegrees : weapon.spec.hipSpreadDegrees;
            if (isCrouching) spread *= 0.65f;

            Vector3 camOrigin = cameraController.GetAimOrigin(transform);
            Vector3 camForward = cameraController.GetAimForward(transform);
            Vector3 muzzleOrigin = visualHandles.gunMuzzleTransform != null
                ? visualHandles.gunMuzzleTransform.position
                : transform.position + Vector3.up * 1.4f + transform.forward * 0.7f;

            WeaponBallistics.FireWeapon(
                Health,
                weapon.spec,
                camOrigin,
                camForward,
                muzzleOrigin,
                spread,
                new Color(1f, 0.88f, 0.32f),
                1f,
                1f);

            if (weapon.currentAmmo <= 0 && inventory.reserveAmmo > 0)
            {
                inventory.StartReload(out _);
            }
        }

        private void ScanNearbyLoot()
        {
            NearestLoot = null;
            float bestDist = 3.4f;
            Vector3 myPos = transform.position;

            for (int i = LootItem.ActiveLoot.Count - 1; i >= 0; i--)
            {
                LootItem item = LootItem.ActiveLoot[i];
                if (item == null) continue;

                float dist = Vector3.Distance(myPos, item.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    NearestLoot = item;
                }
            }
        }

        private void UseHealingItem()
        {
            if (Health.currentHealth >= Health.maxHealth)
            {
                Notify("Máu đã đầy 100 HP!");
                return;
            }

            if (inventory.medkitCount > 0)
            {
                inventory.medkitCount--;
                Health.Heal(55f);
                Notify("Đã dùng Medkit (+55 HP)!");
            }
            else if (inventory.bandageCount > 0)
            {
                inventory.bandageCount--;
                Health.Heal(25f);
                Notify("Đã quấn Bandage (+25 HP)!");
            }
            else
            {
                Notify("Không còn Medkit hay Bandage!");
            }
        }

        private void UseEnergyDrink()
        {
            if (inventory.energyDrinkCount <= 0)
            {
                Notify("Không còn Nước tăng lực!");
                return;
            }

            inventory.energyDrinkCount--;
            Health.AddEnergyBoost(45f);
            Notify("Đã uống Nước tăng lực (+45 Boost, tăng tốc chạy & hồi máu)!");
        }

        private void ThrowFragGrenade()
        {
            if (inventory.grenadeCount <= 0)
            {
                Notify("Đã hết Lựu đạn Frag Grenade!");
                return;
            }

            inventory.grenadeCount--;
            Vector3 origin = transform.position + Vector3.up * 1.55f + transform.forward * 0.75f;
            Vector3 aimDir = cameraController.GetAimForward(transform);
            Vector3 velocity = aimDir * 17.5f + Vector3.up * 4.2f;
            FragGrenadeProjectile.ThrowGrenade(origin, velocity, Health);
            Notify("Đã ném Lựu đạn Frag! (Nổ sau 2.8s)");
        }

        private void HandleDealtDamage(int dmg, bool headshot, bool killed)
        {
            LastHitMarkerTime = Time.time;
            LastHitDamage = dmg;
            LastHitWasHeadshot = headshot;
            LastHitWasKill = killed;
        }

        private void HandlePlayerDied(CombatantHealth victim, CombatantHealth killer, string weapon, bool headshot)
        {
            animDriver.TriggerDeath();
            LockCursor(false);
        }

        private void RefreshVisualEquipment()
        {
            CharacterVisualBuilder.UpdateEquipmentVisuals(ref visualHandles, Health, ActiveWeapon);
        }

        private void HandleGlobalNotification(string message)
        {
            LastActionNotification = message;
            LastNotificationTime = Time.time;
        }

        public void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        public void Notify(string message)
        {
            LastActionNotification = message;
            LastNotificationTime = Time.time;
        }
    }
}
