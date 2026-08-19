using UnityEngine;

namespace CT.MenuNav
{
    public interface IMenuInputOnNavigate
    {
        void OnNavigate(Vector2 navInput, MenuInputContext context);
    }
}