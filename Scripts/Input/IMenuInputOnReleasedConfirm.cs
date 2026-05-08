using UnityEngine.EventSystems;

namespace CT.MenuNav
{
    public interface IMenuInputOnReleasedConfirm
    {
        void OnInputConfirmReleased(int playerID, BaseEventData eventData);
    }
}