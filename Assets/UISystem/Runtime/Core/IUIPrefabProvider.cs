using System;
using System.Threading;
using UnityEngine;

namespace UISystem
{
    /// <summary>프리팹 공급원. Resources / Addressables / 번들 중 무엇을 쓰든 이 구현만 교체하면 된다.</summary>
    public interface IUIPrefabProvider
    {
        Awaitable<GameObject> LoadPrefabAsync(Type viewType, CancellationToken cancellationToken = default);

        /// <summary>
        /// 붙잡고 있던 로드 핸들을 모두 놓는다. UIManager.Dispose 에서 호출한다.
        /// 직접 참조 표는 할 일이 없지만, Addressables 구현은 여기서 Release 하지 않으면
        /// 프리팹이 영원히 상주하고 번들이 언로드되지 않는다.
        /// </summary>
        void ReleaseAll();
    }
}
