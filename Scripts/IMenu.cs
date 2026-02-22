namespace CT.MenuNav
{
    public interface IMenu
    {
        public IMenuHandler MenuHandler { get; set; }
        public void Open(MenuDirection direction, IMenuHandler menuHandler);
        public bool TryClose(MenuDirection direction, bool forceClose = false);
    }
}