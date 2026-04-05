using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CT.MenuNav
{
    public class MenuHandlerBase : MonoBehaviour, IMenuHandler
    {
        protected List<IMenu> Menus = new();
        protected List<IMenu> History = new();
        protected IMenu NextMenu = null;

        IMenu IMenuHandler.NextMenu
        {
            get => NextMenu;
            set => NextMenu = value;
        }
        
        public virtual void ResetAndForwardTo(IMenu menuToForwardTo)
        {
            foreach (var m in Menus)
            {
                m.TryClose(MenuDirection.BACKWARDS);
            }

            History.Clear();
            if (menuToForwardTo != null) Forward(menuToForwardTo, false);
        }
        
        public virtual bool Forward(IMenu menu, bool autoClose = true)
        {
            NextMenu = menu;
            EventSystem.current?.SetSelectedGameObject(null);
            if (autoClose) GetCurrentMenu()?.TryClose(MenuDirection.FORWARDS, false);
            if (menu != null)
            {
                menu.MenuHandler = this;
                menu.Open(MenuDirection.FORWARDS, this);
            }

            History.Add(menu);
            return true;
        }

        public virtual bool Back()
        {
            if (History.Count <= 1) return false;
            var currMenu = GetCurrentMenu();
            var result = currMenu == null || currMenu.TryClose(MenuDirection.BACKWARDS);

            if (result == false) return false;

            History.RemoveAt(History.Count - 1);
            GetCurrentMenu()?.Open(MenuDirection.BACKWARDS, this);
            return true;
        }

        public virtual List<IMenu> GetHistory()
        {
            return History;
        }

        public virtual IMenu GetPreviousMenu()
        {
            if (History.Count == 0) return null;
            return History[^1];
        }

        public virtual IMenu GetCurrentMenu()
        {
            if (History.Count == 0) return null;
            return History[^1];
        }

        public virtual bool HasSubmenuOpen()
        {
            return false;
        }
    }
}