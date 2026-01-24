using System.Collections.Generic;
using Il2CppTMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using Logger = SimpleLabels.Utils.Logger;

namespace SimpleLabels.UI
{
    public class LabelPrefabManager
    {
        // Pooling system
        private static readonly Queue<GameObject> _prefabPool = new Queue<GameObject>();
        private static Transform _poolContainer;

        // Prefab components
        private static Material _labelMaterial;
        private static GameObject _prefabTemplate;
        
        // Configuration
        private const int MinimumPoolSize = 8; // Always keep at least this many prefabs ready
        private const int BatchSize = 10; // Create this many prefabs when replenishing

        public static void Initialize()
        {
            CreatePoolContainer();
            CreatePrefabTemplate();
            PrewarmPool(MinimumPoolSize * 2); // Start with double the minimum as a buffer
        }

        private static void CreatePoolContainer()
        {
            _poolContainer = new GameObject("LabelPrefabPool").transform;
            _poolContainer.gameObject.SetActive(false);
            Object.DontDestroyOnLoad(_poolContainer.gameObject);
        }

        private static void CreatePrefabTemplate()
        {
            _prefabTemplate = new GameObject("LabelPrefab");
            _prefabTemplate.SetActive(false);
            _prefabTemplate.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            var labelObject = new GameObject("LabelObject");
            labelObject.transform.SetParent(_prefabTemplate.transform);
            labelObject.transform.localPosition = Vector3.zero;
            labelObject.transform.localScale = Vector3.one;

            var paperBackground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paperBackground.name = "PaperBackground";
            paperBackground.transform.SetParent(labelObject.transform);
            paperBackground.transform.localPosition = Vector3.zero;
            paperBackground.transform.localScale = new Vector3(2f, 0.6f, 0.1f);
            
            paperBackground.GetComponent<BoxCollider>().extents = Vector3.zero;
            
            //Object.Destroy(paperBackground.GetComponent<UnityEngine.BoxCollider>());

            _labelMaterial = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
            if (_labelMaterial != null)
                paperBackground.GetComponent<Renderer>().material = _labelMaterial;
            else
                Logger.Error("Couldn't find material to reuse.");

            var textObject = new GameObject("LabelText");
            textObject.transform.SetParent(labelObject.transform);
            textObject.transform.localPosition = new Vector3(0, 0, -0.052f);
            textObject.transform.localScale = new Vector3(1.1f, 1.25f, 1);
            
            var textMesh = textObject.AddComponent<TextMeshPro>();
            textMesh.fontSizeMin = 1.4f;
            textMesh.fontSizeMax = 3;
            textMesh.fontSize = 2;
            textMesh.fontStyle = FontStyles.Bold;
            textMesh.enableAutoSizing = true;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.color = Color.black;
            textMesh.enableWordWrapping = true;
            textMesh.margin = new Vector4(0.02f, 0.02f, 0.02f, 0.02f);
            
            // Set font to the existing game font
            TrySetOpenSansFont(textMesh);
            
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(1.8f, 0.5f);
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);

        }

        private static void TrySetOpenSansFont(TextMeshPro textMesh)
        {
            try
            {
                // Try to find existing TMP materials in the game
                Material[] allMaterials = Resources.FindObjectsOfTypeAll<Material>();
                Material workingTextMaterial = null;

                foreach (var mat in allMaterials)
                {
                    // Look for TMP materials that are likely used
                    if (mat.shader != null &&
                        (mat.shader.name.Contains("TextMeshPro") || mat.shader.name.Contains("TMP")) &&
                        !mat.shader.name.Contains("Sprite"))
                    {
                        // You could test this material to see if it works
                        workingTextMaterial = mat;
                        break;
                    }
                }

                TMP_FontAsset[] fontAssets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                foreach (var fontAsset in fontAssets)
                {
                    if (fontAsset.name.Contains("OpenSans-Bold"))
                    {
                        textMesh.font = fontAsset;

                        if (workingTextMaterial != null)
                        {
                            // Clone the working material
                            Material textMat = new Material(workingTextMaterial);

                            if (textMat.HasProperty("_FaceColor"))
                            {
                                textMat.SetColor("_FaceColor", Color.black);
                            }

                            textMesh.fontSharedMaterial = textMat;
                        }
                        else
                        {
                            // Fallback to original approach
                            Material textMat = new Material(fontAsset.material);
                            Shader unlitShader = Shader.Find("TextMeshPro/Distance Field");
                            if (unlitShader != null) textMat.shader = unlitShader;
                            textMesh.fontSharedMaterial = textMat;
                        }

                        return;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Error setting font: {ex.Message}");
            }
        }

        private static void PrewarmPool(int count)
        {
            for (var i = 0; i < count; i++) 
            {
                ReturnToPool(CreateNewLabelInstance());
            }

        }

        private static GameObject CreateNewLabelInstance()
        {
            var instance = Object.Instantiate(_prefabTemplate);
            instance.name = "LabelInstance";
            return instance;
        }

        public static GameObject GetLabelInstance()
        {
            // Check if we need to create more instances to maintain minimum buffer
            if (_prefabPool.Count <= MinimumPoolSize)
            {
                ReplenishPool();
            }

            // Now we should definitely have items in the pool
            if (_prefabPool.Count > 0)
            {
                var instance = _prefabPool.Dequeue();
                if (instance == null)
                {
                    // If instance was somehow destroyed, create a new one
                    instance = CreateNewLabelInstance();
                }
                instance.SetActive(true);
                return instance;
            }

            // This should never happen, but as a fallback
            Logger.Warning("Pool empty despite replenishment attempt, creating emergency instance");
            return CreateNewLabelInstance();
        }

        private static void ReplenishPool()
        {
            int toCreate = BatchSize;
            
            for (int i = 0; i < toCreate; i++)
            {
                ReturnToPool(CreateNewLabelInstance());
            }
        }

        public static void ReturnToPool(GameObject labelInstance)
        {
            if (labelInstance == null)
                return;
                
            labelInstance.SetActive(false);
            labelInstance.transform.SetParent(_poolContainer);
            _prefabPool.Enqueue(labelInstance);
        }

        public static void Terminate()
        {
            while (_prefabPool.Count > 0)
            {
                var obj = _prefabPool.Dequeue();
                if (obj == null) continue;
                Object.Destroy(obj);
            }

            // Cleanup template
            if (_prefabTemplate != null) Object.Destroy(_prefabTemplate);
            if (_poolContainer != null) Object.Destroy(_poolContainer.gameObject);
            if (_labelMaterial != null) Object.Destroy(_labelMaterial);
        }
    }
}