using System;
using UnityEngine;

namespace UISystem
{
    [CreateAssetMenu(fileName = "UILayerSettings", menuName = "UI System/UI Layer Settings")]
    public sealed class UILayerSettings : ScriptableObject
    {
        [Serializable]
        public struct LayerDefinition
        {
            public string Name;

            /// <summary>레이어 캔버스가 받는 전역 sortingOrder. 그 안의 뷰들은 이 값 위에서 상대 정렬된다.</summary>
            public int BaseSortingOrder;

            public int OrderCapacity;

            [Tooltip("UIRoot 아래에 이 레이어의 캔버스를 만든다. Screen 은 씬에 남으므로 끈다.")]
            public bool CreateCanvas;
        }

        [SerializeField]
        private LayerDefinition[] _layers =
        {
            new() { Name = "Screen", BaseSortingOrder = 0, OrderCapacity = 1000, CreateCanvas = false },
            new() { Name = "Window", BaseSortingOrder = 1000, OrderCapacity = 1000, CreateCanvas = true },
            new() { Name = "Popup", BaseSortingOrder = 2000, OrderCapacity = 1000, CreateCanvas = true },
            new() { Name = "Overlay", BaseSortingOrder = 3000, OrderCapacity = 1000, CreateCanvas = true },
            new() { Name = "Toast", BaseSortingOrder = 4000, OrderCapacity = 1000, CreateCanvas = true },
        };

        [Header("레이어 캔버스 공통 스케일 설정")]
        [SerializeField] private Vector2 _referenceResolution = new(1080, 1920);
        [SerializeField, Range(0f, 1f)] private float _matchWidthOrHeight = 0.5f;

        /// <summary>씬에 놓인 UIScreen 의 CanvasScaler 도 이 값과 같아야 배율이 어긋나지 않는다.</summary>
        public Vector2 ReferenceResolution => _referenceResolution;

        public float MatchWidthOrHeight => _matchWidthOrHeight;

        public int Count => _layers?.Length ?? 0;

        public bool TryGet(UILayerId id, out LayerDefinition definition)
        {
            var index = id.Index;
            if (_layers == null || index < 0 || index >= _layers.Length)
            {
                definition = default;
                return false;
            }

            definition = _layers[index];
            return true;
        }
    }
}
