using UnityEngine;

namespace Survival.Player
{
    public class CharacterAnimationDriver
    {
        private Animation anim;
        private string idleClip;
        private string walkClip;
        private string runClip;
        private string jumpClip;
        private string shootClip;
        private string deathClip;
        private string currentLoopClip;
        private float shootAnimTimer;
        private bool deathPlayed;

        public void Initialize(Animation legacyAnimation)
        {
            anim = legacyAnimation;
            currentLoopClip = null;
            shootAnimTimer = 0f;
            deathPlayed = false;

            if (anim == null) return;

            foreach (AnimationState state in anim)
            {
                if (state == null) continue;
                string n = state.name;
                if (n.EndsWith("|Idle"))
                {
                    idleClip = n;
                    state.wrapMode = WrapMode.Loop;
                }
                else if (n.EndsWith("|Walk_Holding"))
                {
                    walkClip = n;
                    state.wrapMode = WrapMode.Loop;
                }
                else if (walkClip == null && n.EndsWith("|Walk"))
                {
                    walkClip = n;
                    state.wrapMode = WrapMode.Loop;
                }
                else if (n.EndsWith("|Run_Holding"))
                {
                    runClip = n;
                    state.wrapMode = WrapMode.Loop;
                }
                else if (runClip == null && n.EndsWith("|Run"))
                {
                    runClip = n;
                    state.wrapMode = WrapMode.Loop;
                }
                else if (n.EndsWith("|Jump"))
                {
                    jumpClip = n;
                    state.wrapMode = WrapMode.Loop;
                }
                else if (n.EndsWith("|Shoot"))
                {
                    shootClip = n;
                    state.wrapMode = WrapMode.Once;
                }
                else if (n.EndsWith("|Death"))
                {
                    deathClip = n;
                    state.wrapMode = WrapMode.ClampForever;
                }
            }

            if (!string.IsNullOrEmpty(idleClip))
            {
                anim.Play(idleClip);
                currentLoopClip = idleClip;
            }
        }

        public void TriggerShoot()
        {
            if (anim == null || deathPlayed || string.IsNullOrEmpty(shootClip))
                return;

            shootAnimTimer = 0.22f;
            anim.CrossFade(shootClip, 0.05f);
        }

        public void TriggerDeath()
        {
            if (anim == null || deathPlayed)
                return;

            deathPlayed = true;
            if (!string.IsNullOrEmpty(deathClip))
            {
                anim.CrossFade(deathClip, 0.1f);
            }
        }

        public void UpdateLocomotion(float planarSpeed, bool isAirborneOrParachuting, bool isAlive, float deltaTime)
        {
            if (anim == null) return;

            if (!isAlive)
            {
                TriggerDeath();
                return;
            }

            if (shootAnimTimer > 0f)
            {
                shootAnimTimer -= deltaTime;
                if (shootAnimTimer > 0f)
                    return;
                currentLoopClip = null;
            }

            string desired = idleClip;
            if (isAirborneOrParachuting && !string.IsNullOrEmpty(jumpClip))
            {
                desired = jumpClip;
            }
            else if (planarSpeed > 5.8f && !string.IsNullOrEmpty(runClip))
            {
                desired = runClip;
            }
            else if (planarSpeed > 0.35f && !string.IsNullOrEmpty(walkClip))
            {
                desired = walkClip;
            }

            if (!string.IsNullOrEmpty(desired) && desired != currentLoopClip)
            {
                currentLoopClip = desired;
                anim.CrossFade(desired, 0.14f);
            }
        }
    }
}
