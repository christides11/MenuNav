using System.Collections.Generic;

namespace CT.MenuNav
{
    public interface IMenuHandler
    {
        public bool Forward(IMenu menu, bool autoClose = true);
        public bool Back();
        public List<IMenu> GetHistory();
        public IMenu GetCurrentMenu();
        public bool HasSubmenuOpen();
    }
}