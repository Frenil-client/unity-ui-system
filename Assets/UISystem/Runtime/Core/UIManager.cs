using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UISystem
{
    public sealed class UIManager : IDisposable
    {
        /// <summary>
        /// 앱에 하나뿐인 인스턴스. UIBootstrap 이 첫 씬보다 먼저 채우고 도메인 리로드 때 되돌린다.
        /// 조립에 실패하면 null 이고, 그 사유는 UIBootstrap 이 이미 로그로 남긴 뒤다.
        /// </summary>
        public static UIManager Instance { get; internal set; }

        private readonly IUIRootProvider _rootProvider;
        private readonly IUIPrefabProvider _prefabProvider;
        private readonly SortingOrderAllocator _allocator;
        private readonly UIDim _dim;

        private readonly List<UIBase> _stack = new();
        private readonly HashSet<UIBase> _closing = new();
        private readonly Dictionary<Type, Stack<UIBase>> _pools = new();
        private readonly List<UIBase> _adoptBuffer = new();

        private bool _dimMissingLogged;

        /// <param name="dim">씬에 하나 두는 공유 반투명 판. 없으면 입력 차단 판 없이 동작한다.</param>
        public UIManager(IUIRootProvider rootProvider, IUIPrefabProvider prefabProvider, SortingOrderAllocator allocator, UIDim dim = null)
        {
            _rootProvider = rootProvider ?? throw new ArgumentNullException(nameof(rootProvider));
            _prefabProvider = prefabProvider ?? throw new ArgumentNullException(nameof(prefabProvider));
            _allocator = allocator ?? throw new ArgumentNullException(nameof(allocator));
            _dim = dim;

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public void Dispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            _prefabProvider.ReleaseAll();
        }

        public int OpenCount => _stack.Count;

        public async Awaitable<T> OpenAsync<T>(CancellationToken cancellationToken = default) where T : UIBase
        {
            var view = await AcquireAsync(typeof(T), cancellationToken);
            if (view == null)
                return null;

            if (!AttachToStack(view, SceneManager.GetActiveScene()))
            {
                Discard(view);
                return null;
            }

            var animation = view.PlayOpenAsync(cancellationToken);
            if (animation != null)
                await animation;

            return (T)view;
        }

        /// <summary>
        /// 씬에 미리 배치된 UI 를 스택에 편입시킨다. 프리팹에서 태어나지 않았으므로
        /// 닫혀도 파괴하지 않고 비활성화만 한다.
        /// </summary>
        public void Adopt(UIBase view)
        {
            if (view == null || _stack.Contains(view))
                return;

            var ownerScene = view.gameObject.scene;
            view.MarkSceneOwned();

            if (view is UIScreen && Find<UIScreen>() != null)
            {
                Debug.LogWarning(
                    $"[UISystem] '{ownerScene.name}' 씬의 '{view.GetType().Name}' 을 붙이는데 이미 다른 UIScreen 이 열려 있다. " +
                    "씬마다 하나가 규약이다. 겹친 씬은 Window 를 쓰는 것이 맞다.", view);
            }

            if (!AttachToStack(view, ownerScene))
                Debug.LogError($"[UISystem] 씬에 배치된 '{view.GetType().Name}' 을 스택에 넣지 못했다.", view);
        }

        /// <summary>부모 지정, 정렬 순서 배정, 스택 삽입까지. 열기와 입양이 함께 쓴다.</summary>
        private bool AttachToStack(UIBase view, Scene ownerScene)
        {
            // UIScreen 은 씬에 놓인 채로 둔다. 옮기면 루트 캔버스가 서브캔버스가 되면서
            // 드리븐 RectTransform 이 풀리고 자기 CanvasScaler 가 죽는다.
            if (view is not UIScreen)
            {
                var root = _rootProvider.GetRoot(view.Layer);
                if (root == null)
                    return false;

                view.transform.SetParent(root, false);
                view.transform.SetAsLastSibling();
            }

            var canvases = view.GetComponentsInChildren<Canvas>(true);
            if (canvases.Length == 0)
            {
                Debug.LogError($"[UISystem] '{view.GetType().Name}' 에 Canvas 가 없다.", view);
                return false;
            }

            // 입력을 막는 UI 는 자기 캔버스 바로 아래 한 칸을 Dim 자리로 비워 예약한다.
            var dimSlot = view.UseDim ? 1 : 0;
            if (!_allocator.TryReserve(view.Layer, canvases.Length + dimSlot, out var block))
                return false;

            view.Canvases = canvases;
            view.OrderBlock = block;
            view.OwnerScene = ownerScene;
            ApplySortingOrder(canvases, block.StartOrder + dimSlot);

            _stack.Insert(FindInsertIndex(view), view);

            view.gameObject.SetActive(true);
            RefreshStackState();
            view.NotifyOpened(this);
            return true;
        }

        public async Awaitable CloseAsync(UIBase view, UICloseReason reason = UICloseReason.ClosedByService)
        {
            if (view == null || !_stack.Contains(view))
                return;

            if (!_closing.Add(view))
                return;

            var animation = view.PlayCloseAsync(default);
            if (animation != null)
                await animation;

            _stack.Remove(view);
            _closing.Remove(view);
            _allocator.Release(view.OrderBlock);

            view.NotifyClosing(new UIResult(reason));
            RefreshStackState();

            Release(view);
        }

        public Awaitable CloseAllAsync<T>() where T : UIBase => CloseWhereAsync(v => v is T);

        public Awaitable CloseAllAsync() => CloseWhereAsync(null);

        public bool TryGetTop(out UIBase view)
        {
            if (_stack.Count == 0)
            {
                view = null;
                return false;
            }

            view = _stack[^1];
            return true;
        }

        public T Find<T>() where T : UIBase
        {
            for (var i = _stack.Count - 1; i >= 0; i--)
            {
                if (_stack[i] is T match)
                    return match;
            }

            return null;
        }

        public bool IsOpen<T>() where T : UIBase => Find<T>() != null;

        public bool OnBackPressed()
        {
            for (var i = _stack.Count - 1; i >= 0; i--)
            {
                var view = _stack[i];

                if (!view.Options.BlockClose)
                {
                    view.Close(UICloseReason.Dismissed);
                    return true;
                }

                // 못 닫는 모달은 아래로 전파시키지 않고 여기서 소비한다.
                if (view.UseDim)
                    return true;
            }

            return false;
        }

        public void Reset()
        {
            for (var i = _stack.Count - 1; i >= 0; i--)
            {
                var view = _stack[i];
                if (view == null)
                    continue;

                view.NotifyClosing(new UIResult(UICloseReason.ClosedByService));

                if (!view.IsSceneOwned)
                    UnityEngine.Object.Destroy(view.gameObject);
            }

            foreach (var pool in _pools.Values)
            {
                foreach (var pooled in pool)
                {
                    if (pooled != null)
                        UnityEngine.Object.Destroy(pooled.gameObject);
                }
            }

            _stack.Clear();
            _closing.Clear();
            _pools.Clear();
            _allocator.Reset();
        }

        private async Awaitable<UIBase> AcquireAsync(Type type, CancellationToken cancellationToken)
        {
            if (TryRent(type, out var pooled))
                return pooled;

            var prefab = await _prefabProvider.LoadPrefabAsync(type, cancellationToken);
            if (prefab == null)
            {
                Debug.LogError($"[UISystem] '{type.Name}' 프리팹을 불러오지 못했다.");
                return null;
            }

            var instance = UnityEngine.Object.Instantiate(prefab);
            instance.name = type.Name;
            instance.SetActive(false);

            var view = instance.GetComponent(type) as UIBase;
            if (view == null)
            {
                Debug.LogError($"[UISystem] '{type.Name}' 프리팹에 해당 컴포넌트가 없다.", instance);
                UnityEngine.Object.Destroy(instance);
                return null;
            }

            return view;
        }

        private async Awaitable CloseWhereAsync(Predicate<UIBase> filter)
        {
            // 닫는 도중 스택이 바뀌므로 대상을 먼저 복사해 둔다.
            var targets = new List<UIBase>();
            for (var i = _stack.Count - 1; i >= 0; i--)
            {
                var view = _stack[i];
                if (filter == null || filter(view))
                    targets.Add(view);
            }

            foreach (var view in targets)
                await CloseAsync(view);
        }

        /// <summary>
        /// 위에서부터 내려가며 가림 상태를 다시 계산한다.
        /// HideBelow 아래는 Canvas 를 끄고, 입력을 막는 것 아래는 켜둔 채 덮인 것으로만 통지한다.
        /// Dim 은 반투명이 겹쳐 짙어지지 않도록 보이는 것 중 최상단 하나만 켠다.
        /// </summary>
        private void RefreshStackState()
        {
            var hidden = false;
            var covered = false;
            UIBase dimOwner = null;

            for (var i = _stack.Count - 1; i >= 0; i--)
            {
                var view = _stack[i];
                var visible = !hidden;

                SetCanvasesEnabled(view, visible);
                view.NotifyCovered(covered);

                if (dimOwner == null && visible && view.UseDim)
                    dimOwner = view;

                if (view.HideBelow)
                    hidden = true;

                if (view.UseDim || view.HideBelow)
                    covered = true;
            }

            RefreshDim(dimOwner);
        }

        /// <summary>
        /// 씬이 올라오면 거기 배치된 UI 를 찾아 스택에 넣는다. 씬마다 별도의 컴포넌트를 둘 필요가 없다.
        /// 우리가 만든 인스턴스는 영속 루트에 있으므로 이 스캔에 걸리지 않는다.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _adoptBuffer.Clear();

            foreach (var rootObject in scene.GetRootGameObjects())
                _adoptBuffer.AddRange(rootObject.GetComponentsInChildren<UIBase>(true));

            if (_adoptBuffer.Count == 0)
                return;

            // 디자이너가 잡아둔 순서를 유지한 채 아래에서부터 넣는다.
            _adoptBuffer.Sort(static (a, b) => AuthoredOrder(a).CompareTo(AuthoredOrder(b)));

            foreach (var view in _adoptBuffer)
                Adopt(view);

            _adoptBuffer.Clear();
        }

        private static int AuthoredOrder(UIBase view)
        {
            var canvas = view.GetComponent<Canvas>();
            return canvas != null ? canvas.sortingOrder : 0;
        }

        /// <summary>
        /// 씬이 내려가면 그 씬이 소유하던 뷰를 스택에서 정리한다. 영속 UIManager 의 유일한 누수 방어선이다.
        /// 입양된 뷰는 이 시점에 이미 Unity 가 파괴했으므로 fake-null 인 것도 함께 걷어낸다.
        /// </summary>
        private void OnSceneUnloaded(Scene scene)
        {
            var removed = false;

            for (var i = _stack.Count - 1; i >= 0; i--)
            {
                var view = _stack[i];
                var alive = view != null;

                if (alive && view.OwnerScene != scene)
                    continue;

                _stack.RemoveAt(i);
                _closing.Remove(view);
                _allocator.Release(view.OrderBlock);
                removed = true;

                if (!alive)
                    continue;

                view.NotifyClosing(new UIResult(UICloseReason.ClosedByService));

                // 입양된 뷰는 영속 루트로 옮겨져 있어 Unity 가 대신 지워주지 않는다.
                // 자기 씬이 사라진 이상 주인이 없으므로 여기서 파괴한다.
                if (view.IsSceneOwned)
                    UnityEngine.Object.Destroy(view.gameObject);
                else
                    Release(view);
            }

            if (removed)
                RefreshStackState();
        }

        private void RefreshDim(UIBase owner)
        {
            if (owner == null)
            {
                if (_dim != null)
                    _dim.Detach();

                return;
            }

            // 씬에 UIDim 을 안 두었거나, 두었던 것이 씬과 함께 파괴된 경우.
            // 뒤쪽 입력이 그대로 통과하므로 기능 결함으로 다룬다.
            if (_dim == null)
            {
                if (!_dimMissingLogged)
                {
                    _dimMissingLogged = true;
                    Debug.LogError($"[UISystem] '{owner.GetType().Name}' 은 UseDim 이지만 UIManager 에 UIDim 이 없다. 뒤쪽 입력이 막히지 않는다.");
                }

                return;
            }

            _dimMissingLogged = false;

            var root = _rootProvider.GetRoot(owner.Layer);
            if (root == null)
            {
                _dim.Detach();
                return;
            }

            _dim.Attach(owner, root, owner.OrderBlock.StartOrder);
        }

        private static void SetCanvasesEnabled(UIBase view, bool enabled)
        {
            var canvases = view.Canvases;
            if (canvases == null)
                return;

            foreach (var canvas in canvases)
            {
                if (canvas != null)
                    canvas.enabled = enabled;
            }
        }

        private static void ApplySortingOrder(Canvas[] canvases, int startOrder)
        {
            for (var i = 0; i < canvases.Length; i++)
            {
                canvases[i].overrideSorting = true;
                canvases[i].sortingOrder = startOrder + i;
            }
        }

        private int FindInsertIndex(UIBase view)
        {
            var layer = view.Layer.Index;
            var order = view.OrderBlock.StartOrder;

            for (var i = _stack.Count - 1; i >= 0; i--)
            {
                var other = _stack[i];
                var otherLayer = other.Layer.Index;

                if (otherLayer < layer || (otherLayer == layer && other.OrderBlock.StartOrder < order))
                    return i + 1;
            }

            return 0;
        }

        private bool TryRent(Type type, out UIBase view)
        {
            view = null;
            if (!_pools.TryGetValue(type, out var pool))
                return false;

            while (pool.Count > 0)
            {
                var candidate = pool.Pop();
                if (candidate == null)
                    continue;

                view = candidate;
                return true;
            }

            return false;
        }

        private void Release(UIBase view)
        {
            // 씬이 소유한 것은 우리가 만들지 않았으므로 파괴하지도 풀에 넣지도 않는다.
            if (view.IsSceneOwned)
            {
                view.gameObject.SetActive(false);
                return;
            }

            if (!view.Options.Pooled)
            {
                UnityEngine.Object.Destroy(view.gameObject);
                return;
            }

            view.gameObject.SetActive(false);

            var type = view.GetType();
            if (!_pools.TryGetValue(type, out var pool))
            {
                pool = new Stack<UIBase>();
                _pools[type] = pool;
            }

            pool.Push(view);
        }

        /// <summary>스택에 들어가기 전에 실패한 인스턴스를 정리한다.</summary>
        private static void Discard(UIBase view)
        {
            if (view != null)
                UnityEngine.Object.Destroy(view.gameObject);
        }
    }
}
