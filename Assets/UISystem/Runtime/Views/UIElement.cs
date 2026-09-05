using UnityEngine;

namespace UISystem
{
    /// <summary>
    /// 스택에 참여하지 않는 부품. 썸네일, 슬롯, 게이지처럼 UIBase 안에 얹혀 사는 것들.
    /// 자기 수명을 스스로 관리하지 않고 부모 UI를 따라간다.
    /// </summary>
    public abstract class UIElement : MonoBehaviour
    {
    }
}
