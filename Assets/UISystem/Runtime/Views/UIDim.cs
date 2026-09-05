using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UISystem
{
    /// <summary>
    /// 모달 UI 뒤에 깔리는 반투명 판. 씬에 하나만 두고 서비스가 필요한 자리로 옮겨 쓴다.
    /// 색과 알파는 이 오브젝트의 Graphic 에서 잡고, 입력 차단은 raycastTarget 이 그대로 해준다.
    /// 인스턴스가 하나뿐이라 반투명이 겹쳐 짙어지는 일이 구조적으로 생기지 않는다.
    /// </summary>
    [RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
    public sealed class UIDim : MonoBehaviour, IPointerClickHandler
    {
        private Canvas _canvas;
        private RectTransform _rect;
        private UIBase _owner;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _rect = (RectTransform)transform;
            gameObject.SetActive(false);
        }

        internal void Attach(UIBase owner, Transform parent, int sortingOrder)
        {
            _owner = owner;

            if (_rect.parent != parent)
                _rect.SetParent(parent, false);

            _rect.anchorMin = Vector2.zero;
            _rect.anchorMax = Vector2.one;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;

            _canvas.overrideSorting = true;
            _canvas.sortingOrder = sortingOrder;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
        }

        internal void Detach()
        {
            _owner = null;

            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_owner == null || _owner.Options.BlockClose)
                return;

            _owner.Close(UICloseReason.Dismissed);
        }
    }
}
