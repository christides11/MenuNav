using Cysharp.Threading.Tasks;

namespace CT.MenuNav
{
    public abstract class BackableMenuPage : MenuPage, IMenuInputOnBack
    {
        public virtual void OnInputBackPressed(
            MenuInputButtonPhase phase,
            MenuInputContext context)
        {
            if (phase == MenuInputButtonPhase.Pressed)
                currentManager.TryHandleBackAsync().Forget();
        }
    }
}