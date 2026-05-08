using UnityEngine;
using UnityEngine.EventSystems;

namespace CT.MenuNav
{
    public interface IMenuInputOnNavigate
    {
        void OnNavigate(Vector2 navInput, int playerID, BaseEventData eventData);
    }
}