using Survival.Combat;
using Survival.Items;
using UnityEngine;

namespace Survival.Player
{
    public class PlayerCameraController
    {
        private static readonly RaycastHit[] SphereHitBuffer = new RaycastHit[12];

        private Camera targetCamera;
        private float yaw;
        private float pitch = 10f;
        private float freeLookYawOffset;
        private int spectatorIndex = 0;

        public float Yaw => yaw;
        public float Pitch => pitch;
        public CombatantHealth CurrentSpectatedTarget { get; private set; }

        public void Initialize(float initialYaw)
        {
            yaw = initialYaw;
            pitch = 10f;
            freeLookYawOffset = 0f;
            targetCamera = Camera.main;
            CurrentSpectatedTarget = null;
        }

        public void ApplyRecoil(float verticalKick, float horizontalKick = 0f)
        {
            pitch = Mathf.Clamp(pitch - verticalKick, -55f, 68f);
            yaw += horizontalKick;
        }

        public Vector3 GetAimOrigin(Transform fallbackTransform)
        {
            EnsureCamera();
            if (targetCamera != null)
                return targetCamera.transform.position;
            return fallbackTransform.position + Vector3.up * 1.6f;
        }

        public Vector3 GetAimForward(Transform fallbackTransform)
        {
            EnsureCamera();
            if (targetCamera != null)
                return targetCamera.transform.forward;
            return fallbackTransform.forward;
        }

        public void UpdateActiveLook(
            Transform playerTransform,
            float mouseSensitivity,
            bool isAiming,
            bool isCrouching,
            bool isParachuting,
            bool isFirstPerson,
            WeaponInstance activeWeapon,
            bool lockLookInput)
        {
            EnsureCamera();
            if (targetCamera == null || playerTransform == null)
                return;

            if (!lockLookInput)
            {
                float sens = isAiming ? mouseSensitivity * 0.55f : mouseSensitivity;
                float mouseX = Input.GetAxis("Mouse X") * sens;
                float mouseY = Input.GetAxis("Mouse Y") * sens;

                if (Input.GetKey(KeyCode.LeftAlt))
                {
                    freeLookYawOffset = Mathf.Clamp(freeLookYawOffset + mouseX, -110f, 110f);
                }
                else
                {
                    freeLookYawOffset = Mathf.Lerp(freeLookYawOffset, 0f, 14f * Time.deltaTime);
                    yaw += mouseX;
                    playerTransform.rotation = Quaternion.Euler(0f, yaw, 0f);
                }

                pitch = Mathf.Clamp(pitch - mouseY, -55f, 68f);
            }

            Quaternion camRot = Quaternion.Euler(pitch, yaw + freeLookYawOffset, 0f);
            float eyeHeight = isCrouching ? 1.15f : 1.68f;
            Vector3 pivot = playerTransform.position + new Vector3(0f, eyeHeight, 0f);

            bool useScopeView = isAiming && activeWeapon != null && activeWeapon.spec.hasSniperScope;
            if (isFirstPerson || useScopeView)
            {
                targetCamera.transform.position = pivot + playerTransform.forward * 0.18f;
                targetCamera.transform.rotation = camRot;
            }
            else
            {
                float camDist = isParachuting ? 6.8f : (isAiming ? 1.95f : 3.65f);
                Vector3 shoulderOffset = camRot * new Vector3(0.58f, 0.22f, -camDist);
                Vector3 desiredPos = ResolveWallCollision(playerTransform, pivot, shoulderOffset);

                targetCamera.transform.position = desiredPos;
                targetCamera.transform.rotation = camRot;
            }

            float targetFov = 62f;
            if (isAiming && activeWeapon != null)
            {
                targetFov = activeWeapon.spec.adsFov;
            }
            targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, targetFov, 14f * Time.deltaTime);
        }

        public void UpdateSpectatorView(Transform deadPlayerTransform)
        {
            EnsureCamera();
            if (targetCamera == null) return;

            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                spectatorIndex++;
                CurrentSpectatedTarget = null;
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                spectatorIndex--;
                CurrentSpectatedTarget = null;
            }

            if (CurrentSpectatedTarget == null || !CurrentSpectatedTarget.IsAlive)
            {
                CurrentSpectatedTarget = FindLivingSpectatorTarget(spectatorIndex);
            }

            Transform focusTransform = CurrentSpectatedTarget != null
                ? CurrentSpectatedTarget.transform
                : deadPlayerTransform;

            if (focusTransform == null) return;

            Vector3 lookTarget = focusTransform.position + Vector3.up * 1.3f;
            Vector3 desiredCamPos = focusTransform.position - focusTransform.forward * 5.2f + Vector3.up * 3.2f;
            targetCamera.transform.position = Vector3.Lerp(targetCamera.transform.position, desiredCamPos, 5f * Time.deltaTime);
            targetCamera.transform.LookAt(lookTarget);
            targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, 60f, 8f * Time.deltaTime);
        }

        private static CombatantHealth FindLivingSpectatorTarget(int indexSeed)
        {
            int count = CombatantHealth.ActiveCombatants.Count;
            if (count == 0) return null;

            int normalized = Mathf.Abs(indexSeed);
            for (int i = 0; i < count; i++)
            {
                CombatantHealth candidate = CombatantHealth.ActiveCombatants[(normalized + i) % count];
                if (candidate != null && candidate.IsAlive)
                    return candidate;
            }
            return null;
        }

        private static Vector3 ResolveWallCollision(Transform playerRoot, Vector3 pivot, Vector3 offset)
        {
            float dist = offset.magnitude;
            if (dist <= 0.01f)
                return pivot;

            Vector3 dir = offset / dist;
            int hitCount = Physics.SphereCastNonAlloc(pivot, 0.2f, dir, SphereHitBuffer, dist, ~0, QueryTriggerInteraction.Ignore);

            float closestDist = float.MaxValue;
            bool foundWall = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit h = SphereHitBuffer[i];
                if (h.collider == null || h.collider.isTrigger)
                    continue;
                if (h.transform.IsChildOf(playerRoot))
                    continue;

                if (h.distance < closestDist)
                {
                    closestDist = h.distance;
                    foundWall = true;
                }
            }

            if (foundWall)
            {
                return pivot + dir * Mathf.Max(0.55f, closestDist - 0.2f);
            }

            return pivot + offset;
        }

        private void EnsureCamera()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
        }
    }
}
