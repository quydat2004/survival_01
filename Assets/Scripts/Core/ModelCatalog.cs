using System.Collections.Generic;
using Survival.Items;
using UnityEngine;

namespace Survival.Core
{
    public static class ModelCatalog
    {
        private static readonly Dictionary<string, GameObject> PrefabCache = new Dictionary<string, GameObject>(64);

        public static readonly string[] CharacterNames = { "George", "Leela", "Mike", "Stan" };

        public static readonly string[] BuildingNames =
        {
            "building-a", "building-b", "building-c", "building-d", "building-e",
            "building-f", "building-g", "building-h", "building-i", "building-j",
            "building-k", "building-l", "building-m", "building-n", "building-o",
            "building-p", "building-q", "building-r", "building-s", "building-t"
        };

        public static readonly string[] ContainerNames =
        {
            "shipping-container-a",
            "shipping-container-b",
            "shipping-container-c"
        };

        public static readonly string[] TreeNames =
        {
            "Pine_1", "Pine_2", "Pine_3", "Pine_4", "Pine_5",
            "CommonTree_1", "CommonTree_2", "CommonTree_3", "CommonTree_4", "CommonTree_5",
            "TwistedTree_1", "TwistedTree_2", "TwistedTree_3", "TwistedTree_4",
            "DeadTree_1", "DeadTree_2", "DeadTree_3"
        };

        public static readonly string[] RockNames =
        {
            "Rock_Medium_1", "Rock_Medium_2", "Rock_Medium_3",
            "Pebble_Round_1", "Pebble_Round_2", "Pebble_Round_3",
            "Pebble_Square_1", "Pebble_Square_2", "Pebble_Square_3"
        };

        public static readonly string[] FoliageNames =
        {
            "Bush_Common", "Bush_Common_Flowers", "Fern_1",
            "Grass_Common_Tall", "Grass_Wispy_Tall", "Plant_1_Big", "Plant_7_Big"
        };

        private static readonly string[] BuildingPaletteTextures =
        {
            "Models/Buildings/Textures/colormap",
            "Models/Buildings/Textures/variation-a",
            "Models/Buildings/Textures/variation-b",
            "Models/Buildings/Textures/variation-c"
        };

        public static GameObject LoadModelPrefab(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
                return null;

            if (PrefabCache.TryGetValue(resourcePath, out GameObject cached) && cached != null)
                return cached;

            GameObject loaded = Resources.Load<GameObject>(resourcePath);
            if (loaded != null)
                PrefabCache[resourcePath] = loaded;

            return loaded;
        }

        public static string GetWeaponModelPath(WeaponId id)
        {
            switch (id)
            {
                case WeaponId.P1911: return "Models/Weapons/Pistol_1";
                case WeaponId.UMP45: return "Models/Weapons/SubmachineGun_1";
                case WeaponId.M416: return "Models/Weapons/AssaultRifle_1";
                case WeaponId.AKM: return "Models/Weapons/AssaultRifle2_1";
                case WeaponId.S1897: return "Models/Weapons/Shotgun_1";
                case WeaponId.Kar98k: return "Models/Weapons/SniperRifle_1";
                case WeaponId.AWM: return "Models/Weapons/SniperRifle_5";
                default: return "Models/Weapons/AssaultRifle_1";
            }
        }

        public static float GetTargetWeaponLength(WeaponId id)
        {
            switch (id)
            {
                case WeaponId.P1911: return 0.34f;
                case WeaponId.UMP45: return 0.62f;
                case WeaponId.M416: return 0.84f;
                case WeaponId.AKM: return 0.86f;
                case WeaponId.S1897: return 0.88f;
                case WeaponId.Kar98k: return 1.06f;
                case WeaponId.AWM: return 1.15f;
                default: return 0.82f;
            }
        }

        public static GameObject SpawnCharacterModel(
            Transform parent,
            string characterName,
            int skinVariantIndex,
            out Animation legacyAnimation,
            out Transform headTransform,
            out Transform rightHandTransform)
        {
            legacyAnimation = null;
            headTransform = null;
            rightHandTransform = null;

            if (string.IsNullOrEmpty(characterName))
                characterName = "George";

            GameObject prefab = LoadModelPrefab("Models/Characters/" + characterName);
            if (prefab == null)
                return null;

            GameObject holder = new GameObject("Character3D_" + characterName);
            if (parent != null)
                holder.transform.SetParent(parent, false);

            GameObject instance = Object.Instantiate(prefab);
            instance.name = characterName + "_Mesh";
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;

            legacyAnimation = instance.GetComponent<Animation>();

            Transform[] allTransforms = instance.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < allTransforms.Length; i++)
            {
                Transform t = allTransforms[i];
                if (t.name == "Head")
                    headTransform = t;
                else if (t.name == "PalmI.R" || (rightHandTransform == null && t.name == "LowerArm.R"))
                    rightHandTransform = t;
            }

            float rawHeadY = headTransform != null && headTransform.position.y > 0.5f
                ? headTransform.position.y
                : 4.5f;
            float scaleFactor = 1.52f / rawHeadY;

            instance.transform.SetParent(holder.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * scaleFactor;

            string texFile = skinVariantIndex <= 0
                ? characterName + "_Texture"
                : characterName + "_" + Mathf.Clamp(skinVariantIndex, 1, 4) + "_Texture";
            Material charMat = MaterialPool.GetTexturedLit("Models/Characters/Textures/" + texFile, Color.white, 0.18f);

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = charMat;
            }

            StripColliders(instance);
            return holder;
        }

        public static GameObject SpawnWeaponModel(
            WeaponId weaponId,
            Transform parent,
            Vector3 localPosition,
            out Transform muzzleTransform)
        {
            muzzleTransform = null;
            string path = GetWeaponModelPath(weaponId);
            GameObject prefab = LoadModelPrefab(path);
            float targetLen = GetTargetWeaponLength(weaponId);

            GameObject holder = new GameObject("Weapon3D_" + weaponId);
            if (parent != null)
                holder.transform.SetParent(parent, false);
            holder.transform.localPosition = localPosition;
            holder.transform.localRotation = Quaternion.identity;

            if (prefab != null)
            {
                GameObject rawMesh = Object.Instantiate(prefab);
                rawMesh.name = "Mesh";
                rawMesh.transform.position = Vector3.zero;
                rawMesh.transform.rotation = Quaternion.identity;

                Bounds rawBounds = CalculateWorldBounds(rawMesh);
                float rawLength = Mathf.Max(0.1f, rawBounds.size.x, rawBounds.size.y, rawBounds.size.z);
                float scaleFactor = targetLen / rawLength;

                Quaternion alignForward = Quaternion.Euler(0f, 90f, 0f) * Quaternion.Euler(-90f, 0f, 0f);
                rawMesh.transform.SetParent(holder.transform, false);
                rawMesh.transform.localRotation = alignForward;
                rawMesh.transform.localScale = rawMesh.transform.localScale * scaleFactor;

                Vector3 rotatedCenter = alignForward * (rawBounds.center * scaleFactor);
                rawMesh.transform.localPosition = -rotatedCenter;

                ApplyWeaponMaterials(rawMesh, weaponId);
                StripColliders(rawMesh);
            }
            else
            {
                WeaponSpec spec = WeaponDatabase.GetSpec(weaponId);
                MaterialPool.CreatePrimitive(
                    PrimitiveType.Cube,
                    holder.transform,
                    "FallbackGun",
                    Vector3.zero,
                    new Vector3(0.10f, 0.14f, targetLen),
                    spec.bodyColor,
                    false);
            }

            GameObject muzzleGo = new GameObject("GunMuzzle");
            muzzleGo.transform.SetParent(holder.transform, false);
            muzzleGo.transform.localPosition = new Vector3(0f, 0.03f, targetLen * 0.52f);
            muzzleTransform = muzzleGo.transform;

            return holder;
        }

        public static GameObject SpawnBuildingModel(
            Transform parent,
            string modelName,
            Vector3 localPos,
            float yRotationDegrees,
            float uniformScale,
            int paletteVariant = 0,
            bool addBoxCollider = true)
        {
            GameObject prefab = LoadModelPrefab("Models/Buildings/" + modelName);
            if (prefab == null)
                return null;

            GameObject inst = Object.Instantiate(prefab);
            inst.name = "Building_" + modelName;
            inst.transform.position = Vector3.zero;
            inst.transform.rotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one * uniformScale;

            string texPath = BuildingPaletteTextures[Mathf.Abs(paletteVariant) % BuildingPaletteTextures.Length];
            Material bldMat = MaterialPool.GetTexturedLit(texPath, Color.white, 0.2f);

            Renderer[] rends = inst.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                Material[] mats = rends[i].sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                    mats[m] = bldMat;
                rends[i].sharedMaterials = mats;
            }

            if (addBoxCollider)
            {
                Bounds worldB = CalculateWorldBounds(inst);
                if (worldB.size.sqrMagnitude > 0.001f)
                {
                    BoxCollider box = inst.AddComponent<BoxCollider>();
                    box.center = inst.transform.InverseTransformPoint(worldB.center);
                    Vector3 invScale = new Vector3(
                        1f / Mathf.Max(0.01f, inst.transform.lossyScale.x),
                        1f / Mathf.Max(0.01f, inst.transform.lossyScale.y),
                        1f / Mathf.Max(0.01f, inst.transform.lossyScale.z));
                    box.size = Vector3.Scale(worldB.size, invScale);
                }
            }

            if (parent != null)
                inst.transform.SetParent(parent, false);

            inst.transform.localPosition = localPos;
            inst.transform.localRotation = Quaternion.Euler(0f, yRotationDegrees, 0f);
            return inst;
        }

        public static GameObject SpawnNatureModel(
            Transform parent,
            string modelName,
            Vector3 localPos,
            float yRotationDegrees,
            float scaleMultiplier,
            bool isTree,
            bool isRock,
            bool addCollider)
        {
            GameObject prefab = LoadModelPrefab("Models/Nature/" + modelName);
            if (prefab == null)
                return null;

            GameObject wrapper = new GameObject("Nature_" + modelName);
            if (parent != null)
                wrapper.transform.SetParent(parent, false);
            wrapper.transform.localPosition = localPos;
            wrapper.transform.localRotation = Quaternion.Euler(0f, yRotationDegrees, 0f);

            GameObject meshObj = Object.Instantiate(prefab);
            meshObj.name = "Mesh";
            meshObj.transform.position = Vector3.zero;
            meshObj.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            meshObj.transform.localScale = meshObj.transform.localScale * scaleMultiplier;

            Bounds b = CalculateWorldBounds(meshObj);
            float groundOffset = -b.min.y;
            meshObj.transform.SetParent(wrapper.transform, false);
            meshObj.transform.localPosition = new Vector3(0f, groundOffset, 0f);

            ApplyNatureMaterials(meshObj, modelName);

            if (addCollider)
            {
                if (isTree)
                {
                    CapsuleCollider trunkCol = wrapper.AddComponent<CapsuleCollider>();
                    float h = Mathf.Max(3.8f, b.size.y * scaleMultiplier * 0.95f);
                    trunkCol.height = h;
                    trunkCol.radius = Mathf.Clamp(b.size.x * scaleMultiplier * 0.16f, 0.45f, 1.15f);
                    trunkCol.center = new Vector3(0f, h * 0.45f, 0f);
                }
                else if (isRock)
                {
                    SphereCollider rockCol = wrapper.AddComponent<SphereCollider>();
                    float r = Mathf.Max(0.75f, Mathf.Min(b.size.x, b.size.z) * scaleMultiplier * 0.48f);
                    rockCol.radius = r;
                    rockCol.center = new Vector3(0f, r * 0.82f, 0f);
                }
            }

            return wrapper;
        }

        private static void ApplyWeaponMaterials(GameObject weaponRoot, WeaponId id)
        {
            WeaponSpec spec = WeaponDatabase.GetSpec(id);
            Renderer[] rends = weaponRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                Material[] mats = rends[i].sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    string matName = mats[m] != null ? mats[m].name.ToLowerInvariant() : string.Empty;
                    Color col = spec.bodyColor;

                    if (matName.Contains("wood"))
                        col = new Color(0.45f, 0.26f, 0.13f);
                    else if (matName.Contains("green"))
                        col = new Color(0.22f, 0.34f, 0.20f);
                    else if (matName.Contains("glass"))
                        col = new Color(0.20f, 0.65f, 0.90f);
                    else if (matName.Contains("metal") || matName.Contains("grey"))
                        col = new Color(0.34f, 0.37f, 0.40f);
                    else if (matName.Contains("black"))
                        col = new Color(0.14f, 0.15f, 0.17f);

                    mats[m] = MaterialPool.GetLit(col, 0.45f);
                }
                rends[i].sharedMaterials = mats;
            }
        }

        private static void ApplyNatureMaterials(GameObject natureRoot, string modelName)
        {
            Renderer[] rends = natureRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                Material[] mats = rends[i].sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    string matName = mats[m] != null ? mats[m].name.ToLowerInvariant() : string.Empty;
                    if (matName.Contains("bark_dead"))
                    {
                        mats[m] = MaterialPool.GetTexturedLit("Models/Nature/Textures/Bark_DeadTree", new Color(0.55f, 0.46f, 0.38f), 0.1f);
                    }
                    else if (matName.Contains("bark_twisted"))
                    {
                        mats[m] = MaterialPool.GetTexturedLit("Models/Nature/Textures/Bark_TwistedTree", new Color(0.50f, 0.38f, 0.28f), 0.1f);
                    }
                    else if (matName.Contains("bark"))
                    {
                        mats[m] = MaterialPool.GetTexturedLit("Models/Nature/Textures/Bark_NormalTree", new Color(0.52f, 0.38f, 0.25f), 0.1f);
                    }
                    else if (matName.Contains("pine"))
                    {
                        mats[m] = MaterialPool.GetTexturedLit("Models/Nature/Textures/Leaf_Pine_C", new Color(0.24f, 0.48f, 0.24f), 0.1f, true);
                    }
                    else if (matName.Contains("twisted"))
                    {
                        mats[m] = MaterialPool.GetTexturedLit("Models/Nature/Textures/Leaves_TwistedTree_C", new Color(0.32f, 0.54f, 0.24f), 0.1f, true);
                    }
                    else if (matName.Contains("normaltree"))
                    {
                        mats[m] = MaterialPool.GetTexturedLit("Models/Nature/Textures/Leaves_NormalTree_C", new Color(0.28f, 0.52f, 0.24f), 0.1f, true);
                    }
                    else if (matName.Contains("pathrock"))
                    {
                        mats[m] = MaterialPool.GetTexturedLit("Models/Nature/Textures/PathRocks_Diffuse", new Color(0.68f, 0.68f, 0.68f), 0.15f);
                    }
                    else if (matName.Contains("rock"))
                    {
                        mats[m] = MaterialPool.GetTexturedLit("Models/Nature/Textures/Rocks_Diffuse", new Color(0.65f, 0.66f, 0.68f), 0.15f);
                    }
                    else if (matName.Contains("flower"))
                    {
                        mats[m] = MaterialPool.GetTexturedLit("Models/Nature/Textures/Flowers", Color.white, 0.1f, true);
                    }
                    else if (matName.Contains("grass"))
                    {
                        mats[m] = MaterialPool.GetTexturedLit("Models/Nature/Textures/Grass", new Color(0.36f, 0.58f, 0.26f), 0.1f, true);
                    }
                    else
                    {
                        mats[m] = MaterialPool.GetTexturedLit("Models/Nature/Textures/Leaves", new Color(0.30f, 0.54f, 0.24f), 0.1f, true);
                    }
                }
                rends[i].sharedMaterials = mats;
            }
        }

        public static Bounds CalculateWorldBounds(GameObject root)
        {
            Renderer[] rends = root.GetComponentsInChildren<Renderer>(true);
            if (rends == null || rends.Length == 0)
                return new Bounds(root.transform.position, Vector3.one);

            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
            {
                b.Encapsulate(rends[i].bounds);
            }
            return b;
        }

        private static void StripColliders(GameObject root)
        {
            Collider[] cols = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null)
                    MaterialPool.SafeDestroy(cols[i]);
            }
        }
    }
}
