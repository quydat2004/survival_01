using System.Collections.Generic;
using UnityEngine;

namespace Survival.Core
{
    public static class MaterialPool
    {
        private static readonly Dictionary<int, Material> LitCache = new Dictionary<int, Material>(64);
        private static readonly Dictionary<int, Material> UnlitCache = new Dictionary<int, Material>(32);
        private static readonly Dictionary<string, Material> TexturedMatCache = new Dictionary<string, Material>(64);
        private static readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>(64);

        private static Shader cachedLitShader;
        private static Shader cachedUnlitShader;

        private static Shader GetLitShader()
        {
            if (cachedLitShader == null)
            {
                cachedLitShader = Shader.Find("Standard");
                if (cachedLitShader == null)
                    cachedLitShader = Shader.Find("Universal Render Pipeline/Lit");
                if (cachedLitShader == null)
                    cachedLitShader = Shader.Find("Sprites/Default");
            }
            return cachedLitShader;
        }

        private static Shader GetUnlitShader()
        {
            if (cachedUnlitShader == null)
            {
                cachedUnlitShader = Shader.Find("Sprites/Default");
                if (cachedUnlitShader == null)
                    cachedUnlitShader = GetLitShader();
            }
            return cachedUnlitShader;
        }

        public static Texture2D LoadTexture(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
                return null;

            if (TextureCache.TryGetValue(resourcePath, out Texture2D cached) && cached != null)
                return cached;

            Texture2D loaded = Resources.Load<Texture2D>(resourcePath);
            if (loaded != null)
            {
                loaded.filterMode = FilterMode.Trilinear;
                loaded.anisoLevel = 9;
                loaded.mipMapBias = -0.55f;
                TextureCache[resourcePath] = loaded;
            }

            return loaded;
        }

        public static Material GetTexturedLit(string textureResourcePath, Color tint, float glossiness = 0.25f, bool alphaCutout = false)
        {
            string key = textureResourcePath + "|" + ColorUtility.ToHtmlStringRGBA(tint) + "|" + (alphaCutout ? "1" : "0");
            if (TexturedMatCache.TryGetValue(key, out Material cachedMat) && cachedMat != null)
                return cachedMat;

            Texture2D tex = LoadTexture(textureResourcePath);
            if (tex == null)
                return GetLit(tint, glossiness);

            Material mat = new Material(GetLitShader())
            {
                color = tint,
                mainTexture = tex
            };

            if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", glossiness);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0.06f);

            if (alphaCutout && mat.HasProperty("_Mode"))
            {
                mat.SetFloat("_Mode", 1f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                mat.SetFloat("_Cutoff", 0.35f);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            }

            TexturedMatCache[key] = mat;
            return mat;
        }

        public static Material GetLit(Color color, float glossiness = 0.35f)
        {
            int key = ComputeColorKey(color, glossiness);
            if (LitCache.TryGetValue(key, out Material mat) && mat != null)
                return mat;

            mat = new Material(GetLitShader())
            {
                color = color
            };
            if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", glossiness);

            LitCache[key] = mat;
            return mat;
        }

        public static Material GetUnlit(Color color)
        {
            int key = ComputeColorKey(color, 0f);
            if (UnlitCache.TryGetValue(key, out Material mat) && mat != null)
                return mat;

            mat = new Material(GetUnlitShader())
            {
                color = color
            };
            UnlitCache[key] = mat;
            return mat;
        }

        public static void ApplyLitColor(Renderer renderer, Color color, float glossiness = 0.35f)
        {
            if (renderer == null) return;
            renderer.sharedMaterial = GetLit(color, glossiness);
        }

        public static GameObject CreatePrimitive(
            PrimitiveType type,
            Transform parent,
            string name,
            Vector3 localPos,
            Vector3 localScale,
            Color color,
            bool keepCollider = false)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            if (!string.IsNullOrEmpty(name))
                part.name = name;

            if (parent != null)
                part.transform.SetParent(parent, false);

            part.transform.localPosition = localPos;
            part.transform.localScale = localScale;

            if (!keepCollider)
            {
                Collider col = part.GetComponent<Collider>();
                if (col != null)
                {
                    SafeDestroy(col);
                }
            }

            ApplyLitColor(part.GetComponent<Renderer>(), color);
            return part;
        }

        public static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
                Object.Destroy(obj);
            else
                Object.DestroyImmediate(obj);
        }

        private static int ComputeColorKey(Color c, float gloss)
        {
            Color32 c32 = c;
            int g = Mathf.RoundToInt(Mathf.Clamp01(gloss) * 100f);
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + c32.r;
                hash = hash * 31 + c32.g;
                hash = hash * 31 + c32.b;
                hash = hash * 31 + c32.a;
                hash = hash * 31 + g;
                return hash;
            }
        }
    }
}
