using Survival.Combat;
using Survival.Core;
using Survival.Items;
using UnityEngine;

namespace Survival.Player
{
    public struct CharacterVisualHandles
    {
        public Transform visualBodyRoot;
        public Transform weaponSocket;
        public Transform gunMuzzleTransform;
        public GameObject currentWeaponModel;
        public WeaponId displayedWeaponId;
        public bool hasDisplayedWeapon;
        public GameObject parachuteVisual;
        public GameObject helmetVisual;
        public GameObject vestVisual;
        public Renderer helmetRenderer;
        public Renderer vestRenderer;
        public Animation legacyAnimation;
    }

    public static class CharacterVisualBuilder
    {
        public static CharacterVisualHandles BuildPlayerVisuals(
            Transform ownerTransform,
            WeaponId initialWeaponId = WeaponId.UMP45,
            string characterName = "George",
            int skinVariant = 0)
        {
            return BuildCharacterVisualsInternal(ownerTransform, characterName, skinVariant, initialWeaponId, true);
        }

        public static CharacterVisualHandles BuildBotVisuals(
            Transform ownerTransform,
            WeaponId initialWeaponId = WeaponId.UMP45)
        {
            string chosenChar = ModelCatalog.CharacterNames[Random.Range(0, ModelCatalog.CharacterNames.Length)];
            int skinVariant = Random.Range(1, 5);
            return BuildCharacterVisualsInternal(ownerTransform, chosenChar, skinVariant, initialWeaponId, false);
        }

        private static CharacterVisualHandles BuildCharacterVisualsInternal(
            Transform ownerTransform,
            string characterName,
            int skinVariant,
            WeaponId initialWeaponId,
            bool isPlayer)
        {
            CharacterVisualHandles handles = new CharacterVisualHandles();

            GameObject root = new GameObject(isPlayer ? "PlayerVisualModel" : "BotModel");
            root.transform.SetParent(ownerTransform, false);
            handles.visualBodyRoot = root.transform;

            GameObject charHolder = ModelCatalog.SpawnCharacterModel(
                handles.visualBodyRoot,
                characterName,
                skinVariant,
                out Animation anim,
                out Transform headBone,
                out Transform handBone);

            handles.legacyAnimation = anim;

            if (charHolder == null)
            {
                MaterialPool.CreatePrimitive(
                    PrimitiveType.Cube,
                    handles.visualBodyRoot,
                    "FallbackTorso",
                    new Vector3(0f, 1.05f, 0f),
                    new Vector3(0.52f, 0.72f, 0.30f),
                    new Color(0.88f, 0.88f, 0.90f));
            }

            Color gearDefault = new Color(0.25f, 0.30f, 0.25f);
            handles.helmetVisual = MaterialPool.CreatePrimitive(
                PrimitiveType.Sphere,
                handles.visualBodyRoot,
                "HelmetVisual",
                new Vector3(0f, 1.72f, 0.02f),
                new Vector3(0.42f, 0.30f, 0.42f),
                gearDefault);
            handles.helmetRenderer = handles.helmetVisual.GetComponent<Renderer>();
            handles.helmetVisual.SetActive(false);

            handles.vestVisual = MaterialPool.CreatePrimitive(
                PrimitiveType.Cube,
                handles.visualBodyRoot,
                "VestVisual",
                new Vector3(0f, 1.02f, 0.02f),
                new Vector3(0.52f, 0.48f, 0.34f),
                gearDefault);
            handles.vestRenderer = handles.vestVisual.GetComponent<Renderer>();
            handles.vestVisual.SetActive(false);

            GameObject socketGo = new GameObject("WeaponSocket");
            socketGo.transform.SetParent(handles.visualBodyRoot, false);
            socketGo.transform.localPosition = new Vector3(0.26f, 1.12f, 0.38f);
            handles.weaponSocket = socketGo.transform;

            AttachWeaponModel(ref handles, initialWeaponId);

            handles.parachuteVisual = new GameObject("ParachuteGlider");
            handles.parachuteVisual.transform.SetParent(ownerTransform, false);
            Color canopyCol = isPlayer ? new Color(0.90f, 0.48f, 0.14f) : new Color(0.25f, 0.62f, 0.35f);
            MaterialPool.CreatePrimitive(PrimitiveType.Cube, handles.parachuteVisual.transform, "Canopy", new Vector3(0f, 3.6f, 0f), new Vector3(3.6f, 0.24f, 1.7f), canopyCol);
            MaterialPool.CreatePrimitive(PrimitiveType.Cylinder, handles.parachuteVisual.transform, "StrapL", new Vector3(-1.1f, 2.4f, 0f), new Vector3(0.04f, 1.2f, 0.04f), Color.black);
            MaterialPool.CreatePrimitive(PrimitiveType.Cylinder, handles.parachuteVisual.transform, "StrapR", new Vector3(1.1f, 2.4f, 0f), new Vector3(0.04f, 1.2f, 0.04f), Color.black);

            return handles;
        }

        public static void AttachWeaponModel(ref CharacterVisualHandles handles, WeaponId weaponId)
        {
            if (handles.weaponSocket == null)
                return;

            if (handles.hasDisplayedWeapon && handles.displayedWeaponId == weaponId && handles.currentWeaponModel != null)
                return;

            if (handles.currentWeaponModel != null)
            {
                MaterialPool.SafeDestroy(handles.currentWeaponModel);
                handles.currentWeaponModel = null;
            }

            handles.currentWeaponModel = ModelCatalog.SpawnWeaponModel(
                weaponId,
                handles.weaponSocket,
                Vector3.zero,
                out Transform muzzle);

            handles.gunMuzzleTransform = muzzle;
            handles.displayedWeaponId = weaponId;
            handles.hasDisplayedWeapon = true;
        }

        public static void UpdateEquipmentVisuals(ref CharacterVisualHandles handles, CombatantHealth health, WeaponInstance activeWeapon)
        {
            if (health != null)
            {
                if (handles.helmetVisual != null)
                {
                    bool hasHelmet = health.helmetLevel > 0 && health.helmetDurability > 0f;
                    handles.helmetVisual.SetActive(hasHelmet);
                    if (hasHelmet && handles.helmetRenderer != null)
                        MaterialPool.ApplyLitColor(handles.helmetRenderer, LootItem.GetTierColor(health.helmetLevel));
                }

                if (handles.vestVisual != null)
                {
                    bool hasVest = health.armorLevel > 0 && health.armorDurability > 0f;
                    handles.vestVisual.SetActive(hasVest);
                    if (hasVest && handles.vestRenderer != null)
                        MaterialPool.ApplyLitColor(handles.vestRenderer, LootItem.GetTierColor(health.armorLevel));
                }
            }

            if (activeWeapon != null && activeWeapon.spec != null)
            {
                AttachWeaponModel(ref handles, activeWeapon.spec.id);
            }
        }
    }
}
