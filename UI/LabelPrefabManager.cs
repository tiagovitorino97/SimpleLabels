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

        public static void Initialize()
        {
            CreatePoolContainer();
            CreatePrefabTemplate();
            PrewarmPool(10);
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

            var textRect = textObject.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(1.8f, 0.5f);
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);

            Logger.Msg("Label prefab initialized successfully");
        }


        private static void PrewarmPool(int count)
        {
            // Calculate initial pool size based on typical usage
            var storageRackCount = count / 4; // Assume 1/4 of count will be storage racks
            var totalNeeded = storageRackCount * 4 + (count - storageRackCount); // 4 labels per rack + 1 for others

            for (var i = 0; i < totalNeeded; i++) ReturnToPool(CreateNewLabelInstance());

            Logger.Msg($"Prewarmed pool with {totalNeeded} label instances");
        }

        private static GameObject CreateNewLabelInstance()
        {
            var instance = Object.Instantiate(_prefabTemplate);
            instance.name = "LabelInstance";
            return instance;
        }

        public static GameObject GetLabelInstance()
        {
            if (_prefabPool.Count > 0)
            {
                var instance = _prefabPool.Dequeue();
                if (instance == null)
                    // If instance was destroyed, create new one
                    instance = CreateNewLabelInstance();
                instance.SetActive(true);
                return instance;
            }

            // If pool is empty, expand it
            var newInstance = CreateNewLabelInstance();
            // Create some extra instances to reduce future pool depletion
            for (var i = 0; i < 25; i++) // Create 25 extra instances
                ReturnToPool(CreateNewLabelInstance());
            return newInstance;
        }

        public static void ReturnToPool(GameObject labelInstance)
        {
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

            //Cleanup template
            if (_prefabTemplate != null) Object.Destroy(_prefabTemplate);
            if (_poolContainer != null) Object.Destroy(_poolContainer.gameObject);
            if (_labelMaterial != null) Object.Destroy(_labelMaterial);
        }
    }
}