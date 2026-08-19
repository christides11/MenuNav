using UnityEngine;

namespace CT.MenuNav
{
    public interface IMenuInputOnNavigateRaw
    {
        void OnNavigateRaw(Vector2 navInput, MenuInputContext context);
    }
}