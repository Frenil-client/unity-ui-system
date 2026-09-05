using System;
using UnityEngine;

namespace UISystem
{
    /// <summary>UILayerSettings 의 레이어 정의를 가리키는 인덱스.</summary>
    [Serializable]
    public struct UILayerId : IEquatable<UILayerId>
    {
        [SerializeField] private int _index;

        public UILayerId(int index) => _index = index;

        public int Index => _index;

        /// <summary>씬이 소유한다. UIRoot 에 캔버스를 두지 않고 씬에 놓인 채로 추적만 한다.</summary>
        public static readonly UILayerId Screen = new(0);

        public static readonly UILayerId Window = new(1);
        public static readonly UILayerId Popup = new(2);
        public static readonly UILayerId Overlay = new(3);
        public static readonly UILayerId Toast = new(4);

        public bool Equals(UILayerId other) => _index == other._index;
        public override bool Equals(object obj) => obj is UILayerId other && Equals(other);
        public override int GetHashCode() => _index;
        public override string ToString() => $"UILayer({_index})";

        public static bool operator ==(UILayerId left, UILayerId right) => left._index == right._index;
        public static bool operator !=(UILayerId left, UILayerId right) => left._index != right._index;
    }

    /// <summary>
    /// 같은 역할 안에서도 인스턴스마다 갈리는 것만 담는다.
    /// 레이어, Dim 사용 여부, 아래 가림 여부는 타입이 고정하므로 여기 없다.
    /// </summary>
    [Serializable]
    public struct UIViewOptions
    {
        [Tooltip("버튼을 눌러야만 닫히게 한다. Dim 클릭과 뒤로가기를 모두 막는다. 강제 확인창에 켠다.")]
        public bool BlockClose;

        public bool Pooled;
    }

    public enum UICloseReason
    {
        Confirmed,
        Cancelled,

        /// <summary>Dim 클릭, 뒤로가기 등 사용자가 흘려보낸 경우.</summary>
        Dismissed,

        /// <summary>CloseAll, 씬 전환 등 UI 외부 요인.</summary>
        ClosedByService,
    }

    public readonly struct UIResult
    {
        public readonly UICloseReason Reason;

        public UIResult(UICloseReason reason) => Reason = reason;

        public bool IsConfirmed => Reason == UICloseReason.Confirmed;

        public static UIResult Confirmed => new(UICloseReason.Confirmed);
        public static UIResult Cancelled => new(UICloseReason.Cancelled);
        public static UIResult Dismissed => new(UICloseReason.Dismissed);
    }
}
