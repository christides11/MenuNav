using UnityEngine.EventSystems;

namespace CT.MenuNav
{
    public interface IMenuInputOnReleasedStart
    {
        void OnInputStartReleased(int playerID, BaseEventData eventData);
    }
}