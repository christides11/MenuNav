using System;
using System.Threading;

namespace CT.MenuNav
{
    [Serializable]
    public readonly partial struct MenuNavContext
    {
        public MenuNavDirection Direction { get; }
        public bool IsForced { get; }
        public bool IsInstant { get; }
        public int Depth { get; }
        public MenuPage FromPage { get; }
        public MenuPage ToPage { get; }
        public CancellationToken CancellationToken { get; }

        public MenuNavContext(
            MenuNavDirection direction,
            bool isForced = false,
            bool isInstant = false,
            int depth = 0,
            MenuPage fromPage = null,
            MenuPage toPage = null,
            CancellationToken cancellationToken = default)
        {
            Direction = direction;
            IsForced = isForced;
            IsInstant = isInstant;
            Depth = depth;
            FromPage = fromPage;
            ToPage = toPage;
            CancellationToken = cancellationToken;
        }

        public MenuNavContext WithDepth(int depth)
        {
            return new MenuNavContext(Direction, IsForced, IsInstant, depth, FromPage, ToPage, CancellationToken);
        }

        public MenuNavContext AsForced()
        {
            return new MenuNavContext(Direction, true, IsInstant, Depth, FromPage, ToPage, CancellationToken);
        }

        public MenuNavContext AsInstant()
        {
            return new MenuNavContext(Direction, IsForced, true, Depth, FromPage, ToPage, CancellationToken);
        }

        public MenuNavContext WithPages(MenuPage fromPage, MenuPage toPage)
        {
            return new MenuNavContext(Direction, IsForced, IsInstant, Depth, fromPage, toPage, CancellationToken);
        }

        public MenuNavContext WithCancellationToken(CancellationToken cancellationToken)
        {
            return new MenuNavContext(Direction, IsForced, IsInstant, Depth, FromPage, ToPage, cancellationToken);
        }
    }
}
