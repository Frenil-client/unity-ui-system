namespace UISystem
{
    /// <summary>전체 화면 콘텐츠. 인벤토리, 상점 등. 위에 쌓이며 아래를 끈다.</summary>
    public abstract class UIWindow : UIBase
    {
        public sealed override UILayerId Layer => UILayerId.Window;
        public sealed override bool UseDim => false;
        public sealed override bool HideBelow => true;
    }
}
