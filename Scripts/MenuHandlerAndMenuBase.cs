using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace CT.MenuNav
{
    public class MenuHandlerAndMenuBase : MenuBase, IMenuHandler
    {
        protected List<IMenu> menus = new();
        protected List<IMenu> history = new();
        protected IMenu NextMenu = null;

        IMenu IMenuHandler.NextMenu
        {
            get => NextMenu;
            set => NextMenu = value;
        }

        public override void Open(MenuDirection direction, IMenuHandler menuHandler)
        {
            base.Open(direction, menuHandler);
            gameObject.SetActive(true);
        }

        public override bool TryClose(MenuDirection direction, bool forceClose = false)
        {
            gameObject.SetActive(false);
            return true;
        }

        public virtual void ResetAndForwardTo(IMenu menuToForwardTo)
        {
            for (int i = history.Count - 1; i >= 0; i--)
            {
                if (history[i] != null) history[i].TryClose(MenuDirection.BACKWARDS, forceClose: true);
            }
            history.Clear();
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

            history.Add(menu);
            return true;
        }

        public virtual bool Back()
        {
            if (history.Count <= 1) return false;
            var currMenu = GetCurrentMenu();
            var result = currMenu == null || currMenu.TryClose(MenuDirection.BACKWARDS);

            if (result == false) return false;

            history.RemoveAt(history.Count - 1);
            GetCurrentMenu()?.Open(MenuDirection.BACKWARDS, this);
            return true;
        }

        public virtual List<IMenu> GetHistory()
        {
            return history;
        }

        public virtual IMenu GetPreviousMenu()
        {
            if (history.Count == 0) return null;
            return history[^1];
        }

        public virtual IMenu GetCurrentMenu()
        {
            if (history.Count == 0) return null;
            return history[^1];
        }

        public virtual bool HasSubmenuOpen()
        {
            return false;
        }
    }
}