using System;
using System.Collections.Generic;
using UnityEngine;

namespace UISystem
{
    /// <summary>한 UI가 점유하는 연속된 sortingOrder 구간.</summary>
    public readonly struct SortingOrderBlock
    {
        public readonly UILayerId Layer;
        public readonly int StartOrder;
        public readonly int Count;

        public SortingOrderBlock(UILayerId layer, int startOrder, int count)
        {
            Layer = layer;
            StartOrder = startOrder;
            Count = count;
        }

        public int EndOrder => StartOrder + Count;
        public bool IsValid => Count > 0;
    }

    /// <summary>
    /// 레이어마다 커서를 하나 두고 구간을 앞에서부터 예약한다.
    /// 반납된 구간은 커서 바로 아래에 닿는 순간 연쇄적으로 회수되며, 그 전까지는 구멍으로 남는다.
    /// </summary>
    public sealed class SortingOrderAllocator
    {
        private sealed class LayerState
        {
            public int Cursor;
            public readonly Dictionary<int, int> ReleasedByEnd = new();
        }

        private readonly UILayerSettings _settings;
        private readonly Dictionary<int, LayerState> _states = new();

        public SortingOrderAllocator(UILayerSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            _settings = settings;
        }

        public bool TryReserve(UILayerId layer, int canvasCount, out SortingOrderBlock block)
        {
            block = default;

            if (canvasCount <= 0)
                return false;

            if (!_settings.TryGet(layer, out var definition))
            {
                Debug.LogError($"[UISystem] 정의되지 않은 레이어: {layer}");
                return false;
            }

            var state = GetOrCreateState(layer, definition);
            if (state.Cursor + canvasCount > definition.BaseSortingOrder + definition.OrderCapacity)
            {
                Debug.LogError($"[UISystem] '{definition.Name}' 레이어의 정렬 순서 용량({definition.OrderCapacity})을 초과했다.");
                return false;
            }

            block = new SortingOrderBlock(layer, state.Cursor, canvasCount);
            state.Cursor += canvasCount;
            return true;
        }

        public void Release(in SortingOrderBlock block)
        {
            if (!block.IsValid)
                return;

            if (!_states.TryGetValue(block.Layer.Index, out var state))
                return;

            state.ReleasedByEnd[block.EndOrder] = block.StartOrder;

            while (state.ReleasedByEnd.TryGetValue(state.Cursor, out var start))
            {
                state.ReleasedByEnd.Remove(state.Cursor);
                state.Cursor = start;
            }
        }

        public void Reset() => _states.Clear();

        private LayerState GetOrCreateState(UILayerId layer, in UILayerSettings.LayerDefinition definition)
        {
            if (_states.TryGetValue(layer.Index, out var state))
                return state;

            state = new LayerState { Cursor = definition.BaseSortingOrder };
            _states[layer.Index] = state;
            return state;
        }
    }
}
