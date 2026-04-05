using System.Collections.Generic;

namespace CT.MenuNav
{
    public interface IMenuHandler
    {
        public IMenu NextMenu { get; protected set; }
        public bool Forward(IMenu menu, bool autoClose = true);
        public bool Back();
        public List<IMenu> GetHistory();
        public IMenu GetCurrentMenu();
        public IMenu GetPreviousMenu();
        public bool HasSubmenuOpen();
    }
}