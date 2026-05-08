using UnityEngine.EventSystems;

namespace CT.MenuNav
{
    public interface IMenuInputOnPressedBack
    {
        void OnInputBackPressed(int playerID, BaseEventData eventData);
    }
}