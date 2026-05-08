using UnityEngine.EventSystems;

namespace CT.MenuNav
{
    public interface IMenuInputOnPressedConfirm
    {
        void OnInputConfirmPressed(int playerID, BaseEventData eventData);
    }
}