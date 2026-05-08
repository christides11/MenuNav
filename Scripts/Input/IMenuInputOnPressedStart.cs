using UnityEngine.EventSystems;

namespace CT.MenuNav
{
    public interface IMenuInputOnPressedStart
    {
        void OnInputStartPressed(int playerID, BaseEventData eventData);
    }
}