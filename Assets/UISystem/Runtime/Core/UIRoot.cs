using UnityEngine;

namespace UISystem
{
    /// <summary>
    /// 영속 UI 루트. 프리팹 하나로 만들어 DontDestroyOnLoad 로 올린다.
    /// 씬이 몇 개 떠 있든 이것과 UIManager 는 앱에 하나뿐이다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIRoot : MonoBehaviour
    {
        [SerializeField] private UIRootProvider _rootProvider;
        [SerializeField] private UIDim _dim;

        public UIRootProvider RootProvider => _rootProvider;
        public UIDim Dim => _dim;
    }
}
