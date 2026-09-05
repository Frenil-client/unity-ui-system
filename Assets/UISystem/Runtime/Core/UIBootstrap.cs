using UnityEngine;

namespace UISystem
{
    /// <summary>
    /// 어느 씬에서 Play 를 눌러도 영속 영역이 서도록 만드는 진입점.
    /// 부트스트랩 씬을 강제하지 않으므로 작업하던 씬에서 바로 실행할 수 있다.
    /// </summary>
    public static class UIBootstrap
    {
        private const string SettingsResourcePath = "UIBootstrap";

        private static UIManager _current;

        /// <summary>초기화 순서가 어긋나도 죽지 않도록 접근 시점에 한 번 더 시도한다.</summary>
        public static UIManager Current => _current ??= Create();

        /// <summary>
        /// 도메인 리로드를 끈 상태에서는 static 이 Play 세션 사이에 살아남는다.
        /// 이 훅이 없으면 두 번째 실행부터 파괴된 UIManager 를 붙잡게 된다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _current?.Dispose();
            _current = null;
        }

        /// <summary>첫 씬의 어떤 Awake 보다 먼저 돈다.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            _ = Current;
        }

        private static UIManager Create()
        {
            var settings = Resources.Load<UIBootstrapSettings>(SettingsResourcePath);
            if (settings == null)
            {
                Debug.LogError($"[UISystem] Resources/{SettingsResourcePath} 에셋을 찾지 못했다. UI 가 동작하지 않는다.");
                return null;
            }

            if (!settings.Validate())
                return null;

            var root = Object.Instantiate(settings.RootPrefab);
            root.name = nameof(UIRoot);
            Object.DontDestroyOnLoad(root.gameObject);

            return new UIManager(
                root.RootProvider,
                settings.PrefabProvider,
                new SortingOrderAllocator(settings.Layers),
                root.Dim);
        }
    }
}
