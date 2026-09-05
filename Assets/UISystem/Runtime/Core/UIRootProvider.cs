using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>
    /// 레이어 캔버스를 제공한다. 없으면 설정의 레이어 이름으로 만들어 붙인다.
    /// 여기서 만든 캔버스가 CanvasScaler 를 하나씩 갖고, 그 아래 뷰들은 서브캔버스로 배율을 물려받는다.
    /// </summary>
    public sealed class UIRootProvider : MonoBehaviour, IUIRootProvider
    {
        [SerializeField] private UILayerSettings _settings;
        [SerializeField] private RectTransform _container;

        private readonly Dictionary<int, RectTransform> _roots = new();

        public UILayerSettings Settings => _settings;

        public RectTransform GetRoot(UILayerId layer)
        {
            if (_roots.TryGetValue(layer.Index, out var cached) && cached != null)
                return cached;

            if (_settings == null || !_settings.TryGet(layer, out var definition))
            {
                Debug.LogError($"[UISystem] 정의되지 않은 레이어: {layer}", this);
                return null;
            }

            if (!definition.CreateCanvas)
            {
                Debug.LogError($"[UISystem] '{definition.Name}' 레이어는 UIRoot 에 캔버스를 두지 않는다. 여기 붙이려 하면 안 된다.", this);
                return null;
            }

            var parent = _container != null ? _container : (RectTransform)transform;
            var root = FindOrCreate(parent, definition);
            _roots[layer.Index] = root;
            return root;
        }

        private RectTransform FindOrCreate(RectTransform parent, in UILayerSettings.LayerDefinition definition)
        {
            var existing = parent.Find(definition.Name) as RectTransform;
            if (existing != null)
            {
                Configure(existing.gameObject, definition);
                return existing;
            }

            var go = new GameObject(definition.Name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);

            Configure(go, definition);
            return rect;
        }

        /// <summary>
        /// 레이어 캔버스는 루트 캔버스다. 전역 sortingOrder 로 레이어끼리 줄을 서고,
        /// CanvasScaler 설정을 여기서 한 번만 잡아 그 아래 전부가 같은 배율을 쓰게 한다.
        /// </summary>
        private void Configure(GameObject go, in UILayerSettings.LayerDefinition definition)
        {
            if (!go.TryGetComponent<Canvas>(out var canvas))
                canvas = go.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = definition.BaseSortingOrder;

            if (!go.TryGetComponent<CanvasScaler>(out var scaler))
                scaler = go.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = _settings.ReferenceResolution;
            scaler.matchWidthOrHeight = _settings.MatchWidthOrHeight;

            if (!go.TryGetComponent<GraphicRaycaster>(out _))
                go.AddComponent<GraphicRaycaster>();
        }
    }
}
