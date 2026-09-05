using UnityEngine;

namespace UISystem
{
    /// <summary>
    /// 영속 영역을 조립하는 것만 한다. 조립이 끝나면 역할이 끝나므로 상시 접근점이 아니다.
    /// 게임 코드는 UIManager.Instance 만 본다.
    /// 부트스트랩 씬을 강제하지 않으므로 작업하던 씬에서 바로 실행할 수 있다.
    /// </summary>
    internal static class UIBootstrap
    {
        private const string SettingsResourcePath = "UIBootstrap";

        /// <summary>
        /// 도메인 리로드를 끈 상태에서는 static 이 Play 세션 사이에 살아남는다.
        /// 이 훅이 없으면 두 번째 실행부터 파괴된 UIRoot 를 붙잡은 UIManager 를 쓰게 된다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            UIManager.Instance?.Dispose();
            UIManager.Instance = null;
        }

        /// <summary>첫 씬의 어떤 Awake 보다 먼저 돈다. UIManager 를 세우는 유일한 지점이다.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() => UIManager.Instance = Create();

        /// <summary>조립에 실패하면 사유를 로그로 남기고 null 을 돌려준다.</summary>
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
