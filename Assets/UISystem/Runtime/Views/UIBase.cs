using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>
    /// 스택에 참여하는 모든 UI의 베이스. 자기 콘텐츠와 닫기 결과만 안다.
    /// 스택 관리, 정렬 순서, 프리팹 로딩, 입력 라우팅은 UIManager 가 담당한다.
    /// </summary>
    // GraphicRaycaster 는 강제하지 않는다. UIToast 처럼 입력을 받지 않는 것은 붙이면 낭비다.
    // 상호작용 요소가 있는데 빠진 경우는 무결성 툴이 잡는다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public abstract class UIBase : MonoBehaviour
    {
        [SerializeField] private UIViewOptions _options;

        private AwaitableCompletionSource<UIResult> _closeSource;
        private UIManager _service;
        private bool _closed;
        private UIResult _result;

        public UIViewOptions Options => _options;
        public bool IsOpen { get; private set; }
        public bool IsCovered { get; private set; }

        public abstract UILayerId Layer { get; }
        /// <summary>뒤에 공유 Dim 을 깐다. 어두워지는 동시에 입력도 막히고, 눌러서 닫는 통로가 된다.</summary>
        public abstract bool UseDim { get; }
        public abstract bool HideBelow { get; }

        internal SortingOrderBlock OrderBlock { get; set; }
        internal Canvas[] Canvases { get; set; }

        /// <summary>씬에 배치되어 입양된 인스턴스. 닫아도 파괴하거나 풀에 넣지 않는다.</summary>
        internal bool IsSceneOwned { get; private set; }

        /// <summary>
        /// 이 UI 를 소유한 씬. 입양된 것은 자기가 놓여 있던 씬, 프리팹에서 태어난 것은 연 시점의 활성 씬이다.
        /// 그 씬이 언로드되면 UIManager 가 스택에서 정리한다.
        /// </summary>
        internal Scene OwnerScene { get; set; }

        internal void MarkSceneOwned() => IsSceneOwned = true;

        protected virtual UIViewOptions DefaultOptions => default;

        public Awaitable<UIResult> WaitForCloseAsync()
        {
            // 이미 닫힌 뒤에 기다리면 영원히 대기하게 되므로 완료된 것을 돌려준다.
            if (_closed)
            {
                var completed = new AwaitableCompletionSource<UIResult>();
                completed.SetResult(_result);
                return completed.Awaitable;
            }

            _closeSource ??= new AwaitableCompletionSource<UIResult>();
            return _closeSource.Awaitable;
        }

        public void Close(UICloseReason reason = UICloseReason.Dismissed)
        {
            if (!IsOpen)
                return;

            _ = _service?.CloseAsync(this, reason);
        }

        protected virtual void OnOpened() { }

        protected virtual void OnClosing(UIResult result) { }

        /// <summary>위에 다른 UI가 덮이거나 걷혔을 때. 가려진 동안 갱신을 멈추는 용도.</summary>
        protected virtual void OnCoveredChanged(bool covered) { }

        /// <summary>열기 연출. null 을 반환하면 연출 없이 즉시 진행한다.</summary>
        protected internal virtual Awaitable PlayOpenAsync(CancellationToken cancellationToken) => null;

        protected internal virtual Awaitable PlayCloseAsync(CancellationToken cancellationToken) => null;

        protected virtual void Reset() => _options = DefaultOptions;

        internal void NotifyOpened(UIManager service)
        {
            _service = service;
            _closed = false;
            _closeSource = null;
            IsOpen = true;
            OnOpened();
        }

        internal void NotifyClosing(UIResult result)
        {
            IsOpen = false;
            _closed = true;
            _result = result;

            OnClosing(result);

            _closeSource?.SetResult(result);
            _closeSource = null;
            _service = null;
        }

        internal void NotifyCovered(bool covered)
        {
            if (IsCovered == covered)
                return;

            IsCovered = covered;
            OnCoveredChanged(covered);
        }
    }
}
