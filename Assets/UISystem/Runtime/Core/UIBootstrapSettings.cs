using UnityEngine;

namespace UISystem
{
    /// <summary>
    /// 영속 영역을 조립하는 데 필요한 참조 묶음.
    /// Addressables 가 준비되기 전에 읽혀야 하므로 Resources 에 둔다.
    /// </summary>
    [CreateAssetMenu(fileName = "UIBootstrap", menuName = "UI System/UI Bootstrap Settings")]
    public sealed class UIBootstrapSettings : ScriptableObject
    {
        [SerializeField] private UIRoot _rootPrefab;
        [SerializeField] private UILayerSettings _layers;

        [SerializeField]
        [Tooltip("IUIPrefabProvider 를 구현한 ScriptableObject. 기본은 UIPrefabTable.")]
        private ScriptableObject _prefabProvider;

        public UIRoot RootPrefab => _rootPrefab;
        public UILayerSettings Layers => _layers;

        public IUIPrefabProvider PrefabProvider => _prefabProvider as IUIPrefabProvider;

        public bool Validate()
        {
            if (_rootPrefab == null)
            {
                Debug.LogError("[UISystem] UIBootstrapSettings 에 RootPrefab 이 비어 있다.", this);
                return false;
            }

            if (_layers == null)
            {
                Debug.LogError("[UISystem] UIBootstrapSettings 에 Layers 가 비어 있다.", this);
                return false;
            }

            if (PrefabProvider == null)
            {
                Debug.LogError("[UISystem] UIBootstrapSettings 의 PrefabProvider 가 비었거나 IUIPrefabProvider 를 구현하지 않았다.", this);
                return false;
            }

            return true;
        }
    }
}
