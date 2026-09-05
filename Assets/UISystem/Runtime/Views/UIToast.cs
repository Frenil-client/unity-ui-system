namespace UISystem
{
    /// <summary>
    /// 최상단 일시 알림. 입력을 통과시키고 스스로 사라진다.
    /// 프리팹에 GraphicRaycaster 를 붙이지 않는다. 레이캐스트 대상에서 빠져야 한다.
    /// </summary>
    public abstract class UIToast : UIBase
    {
        public sealed override UILayerId Layer => UILayerId.Toast;
        public sealed override bool UseDim => false;
        public sealed override bool HideBelow => false;

        protected override UIViewOptions DefaultOptions => new() { Pooled = true, BlockClose = true };
    }
}
