using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UISystem.Samples.Editor
{
    /// <summary>
    /// 샘플 씬과 거기 필요한 에셋을 한 번에 만든다.
    /// 씬 파일을 손으로 쓰면 깨지기 쉬워서 에디터에서 생성한다.
    /// </summary>
    public static class SampleSceneBuilder
    {
        private const string GeneratedFolder = "Assets/Samples/Generated";
        private const string SceneFolder = "Assets/Samples/Scene";
        private const string ResourcesFolder = "Assets/Resources";
        private const string LobbyScene = "SampleLobby";
        private const string BattleScene = "SampleBattle";

        [MenuItem("UI System/Samples/샘플 씬과 에셋 생성")]
        public static void Build()
        {
            EnsureFolder(GeneratedFolder);
            EnsureFolder(SceneFolder);
            EnsureFolder(ResourcesFolder);

            var layers = CreateOrLoad<UILayerSettings>($"{GeneratedFolder}/SampleUILayerSettings.asset");
            var table = CreateOrLoad<UIPrefabTable>($"{GeneratedFolder}/SampleUIPrefabTable.asset");

            var popupPrefab = BuildPopupPrefab();
            RegisterInTable(table, typeof(SampleConfirmPopup), popupPrefab);

            var rootPrefab = BuildUIRootPrefab(layers);

            var bootstrap = CreateOrLoad<UIBootstrapSettings>($"{ResourcesFolder}/UIBootstrap.asset");
            SetReference(bootstrap, "_rootPrefab", rootPrefab);
            SetReference(bootstrap, "_layers", layers);
            SetReference(bootstrap, "_prefabProvider", table);

            BuildScene(LobbyScene, typeof(SampleLobbyScreen), BattleScene, new Color(0.16f, 0.28f, 0.20f));
            BuildScene(BattleScene, typeof(SampleBattleScreen), LobbyScene, new Color(0.30f, 0.18f, 0.18f));

            RegisterBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UISystem] 샘플 생성 완료. {LobbyScene} 씬을 열고 실행해 보세요.");
        }

        private static GameObject BuildUIRootPrefab(UILayerSettings layers)
        {
            // UIRoot 자체에는 Canvas 가 없다. 레이어 캔버스는 UIRootProvider 가 실행 시 만든다.
            var rootGo = new GameObject("UIRoot", typeof(RectTransform), typeof(UIRoot));

            var provider = rootGo.AddComponent<UIRootProvider>();
            SetReference(provider, "_settings", layers);
            SetReference(provider, "_container", rootGo.GetComponent<RectTransform>());

            // Dim 은 서브캔버스로 레이어 캔버스 사이를 옮겨 다닌다. CanvasScaler 는 붙이지 않는다.
            var dimGo = new GameObject("Dim", typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
            dimGo.transform.SetParent(rootGo.transform, false);
            Stretch((RectTransform)dimGo.transform);

            var dimImage = dimGo.GetComponent<Image>();
            dimImage.color = new Color(0.10f, 0.14f, 0.19f, 0.65f);
            dimImage.raycastTarget = true;

            dimGo.AddComponent<UIDim>();

            SetReference(rootGo.GetComponent<UIRoot>(), "_rootProvider", provider);
            SetReference(rootGo.GetComponent<UIRoot>(), "_dim", dimGo.GetComponent<UIDim>());

            var path = $"{GeneratedFolder}/UIRoot.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(rootGo, path);
            Object.DestroyImmediate(rootGo);
            return prefab;
        }

        private static GameObject BuildPopupPrefab()
        {
            // PopupCanvas 아래 서브캔버스로 들어간다. CanvasScaler 를 붙이면 안 된다.
            var go = new GameObject("SampleConfirmPopup", typeof(SampleConfirmPopup), typeof(GraphicRaycaster));
            go.GetComponent<Canvas>().overrideSorting = true;
            Stretch((RectTransform)go.transform);

            var panelGo = new GameObject("Panel", typeof(Image));
            panelGo.transform.SetParent(go.transform, false);

            var rect = (RectTransform)panelGo.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600, 320);
            rect.anchoredPosition = Vector2.zero;

            panelGo.GetComponent<Image>().color = new Color(0.92f, 0.92f, 0.94f);

            var path = $"{GeneratedFolder}/SampleConfirmPopup.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void BuildScene(string sceneName, Type screenType, string otherScene, Color background)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Screen 은 씬에 남는 루트 캔버스다. 자기 CanvasScaler 가 필요하고,
            // 그 값이 UILayerSettings 와 같아야 실행 결과가 씬 뷰와 일치한다.
            var screenGo = new GameObject(screenType.Name, screenType, typeof(GraphicRaycaster));
            AddScaler(screenGo);

            var bgGo = new GameObject("Background", typeof(Image));
            bgGo.transform.SetParent(screenGo.transform, false);
            Stretch((RectTransform)bgGo.transform);
            bgGo.GetComponent<Image>().color = background;

            var harnessGo = new GameObject("TestHarness", typeof(SampleUITestHarness));
            SetString(harnessGo.GetComponent<SampleUITestHarness>(), "_otherScene", otherScene);

            EditorSceneManager.SaveScene(scene, $"{SceneFolder}/{sceneName}.unity");
        }

        private static void RegisterInTable(UIPrefabTable table, Type viewType, GameObject prefab)
        {
            var so = new SerializedObject(table);
            var entries = so.FindProperty("_entries");
            entries.arraySize = 1;

            var entry = entries.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("TypeName").stringValue = viewType.FullName;
            entry.FindPropertyRelative("Prefab").objectReferenceValue = prefab;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RegisterBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{SceneFolder}/{LobbyScene}.unity", true),
                new EditorBuildSettingsScene($"{SceneFolder}/{BattleScene}.unity", true),
            };
        }

        /// <summary>모든 뷰가 루트 캔버스라 참조 해상도가 서로 같아야 한다.</summary>
        private static void AddScaler(GameObject go)
        {
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = System.IO.Path.GetDirectoryName(path)!.Replace('\\', '/');
            var leaf = System.IO.Path.GetFileName(path);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void SetReference(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
