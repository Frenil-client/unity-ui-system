namespace UISystem
{
    /// <summary>
    /// 씬의 주 화면. 로비, 전투 HUD 등. 씬마다 하나 놓아두고 UIManager 가 추적만 한다.
    /// 다른 타입과 달리 UIRoot 로 옮기지 않으므로 루트 캔버스인 채로 남고 자기 CanvasScaler 를 쓴다.
    /// 그래서 씬에서 저작한 모습이 실행 결과와 같다. 대신 그 스케일 설정이 UILayerSettings 와 같아야 한다.
    /// </summary>
    public abstract class UIScreen : UIBase
    {
        public sealed override UILayerId Layer => UILayerId.Screen;
        public sealed override bool UseDim => false;
        public sealed override bool HideBelow => false;

        // 스택 바닥이므로 흘려보내 닫히면 안 된다. 종료 확인은 앱이 처리한다.
        protected override UIViewOptions DefaultOptions => new() { BlockClose = true };
    }
}
