using UnityEngine;
using UnityEngine.EventSystems;

namespace CT.MenuNav
{
    public interface IMenuInputOnNavigateRaw
    {
        void OnNavigateRaw(Vector2 navInput, int playerID, BaseEventData eventData);
    }
}