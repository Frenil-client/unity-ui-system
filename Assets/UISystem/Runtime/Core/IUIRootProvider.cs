using UnityEngine;

namespace UISystem
{
    public interface IUIRootProvider
    {
        RectTransform GetRoot(UILayerId layer);
    }
}
