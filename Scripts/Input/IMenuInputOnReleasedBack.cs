using UnityEngine.EventSystems;

namespace CT.MenuNav
{
    public interface IMenuInputOnReleasedBack
    {
        void OnInputBackReleased(int playerID, BaseEventData eventData);
    }
}