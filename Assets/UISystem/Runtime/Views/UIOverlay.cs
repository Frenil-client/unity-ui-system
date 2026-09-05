namespace UISystem
{
    /// <summary>입력을 막는 시스템 층. 로딩 인디케이터, 튜토리얼 마스크 등. 뒤로가기로 닫히지 않는다.</summary>
    public abstract class UIOverlay : UIBase
    {
        public sealed override UILayerId Layer => UILayerId.Overlay;
        public sealed override bool UseDim => true;
        public sealed override bool HideBelow => false;

        // 로딩이나 튜토리얼 마스크는 흘려보내 닫을 수 없다.
        protected override UIViewOptions DefaultOptions => new() { BlockClose = true };
    }
}
