namespace UISystem
{
    /// <summary>화면 일부를 덮는 모달. 확인창, 결과창 등. 아래는 켜둔 채 가려진 것으로 통지한다.</summary>
    public abstract class UIPopup : UIBase
    {
        public sealed override UILayerId Layer => UILayerId.Popup;
        public sealed override bool UseDim => true;
        public sealed override bool HideBelow => false;

        protected override UIViewOptions DefaultOptions => default;
    }
}
