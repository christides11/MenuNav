using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace CT.MenuNav
{
    public class MenuPageSection : MonoBehaviour
    {
        public MenuViewState SectionState
        {
            get => sectionState;
            protected set
            {
                sectionState = value;
                switch (sectionState)
                {
                    case MenuViewState.Closed:
                        OnClosed?.Invoke();
                        break;
                    case MenuViewState.Opened:
                        OnOpened?.Invoke();
                        break;
                    case MenuViewState.Closing:
                        OnClosing?.Invoke();
                        break;
                    case MenuViewState.Opening:
                        OnOpening?.Invoke();
                        break;
                }
            }
        }
        
        public UnityEvent OnOpening, OnOpened, OnClosing, OnClosed, OnReset;
        [SerializeField] private MenuViewState sectionState = MenuViewState.Closed;
        public MenuPage AssignedPage { get; internal set; }
        
        public virtual async UniTask<bool> TryEnterSection(MenuNavContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (SectionState is MenuViewState.Opening or MenuViewState.Opened)
                return false;

            SectionState = MenuViewState.Opening;

            try
            {
                if (!await OnEnterSectionAsync(context))
                    return false;

                context.CancellationToken.ThrowIfCancellationRequested();
                SectionState = MenuViewState.Opened;
                return true;
            }
            finally
            {
                if (SectionState == MenuViewState.Opening)
                    SectionState = MenuViewState.Closed;
            }
        }

        protected virtual UniTask<bool> OnEnterSectionAsync(MenuNavContext context)
        {
            return UniTask.FromResult(true);
        }

        public virtual async UniTask<bool> TryExitSection(MenuNavContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (SectionState is MenuViewState.Closing or MenuViewState.Closed)
                return false;

            SectionState = MenuViewState.Closing;

            try
            {
                if (!await OnExitSectionAsync(context))
                    return false;

                context.CancellationToken.ThrowIfCancellationRequested();
                SectionState = MenuViewState.Closed;
                return true;
            }
            finally
            {
                if (SectionState == MenuViewState.Closing)
                    SectionState = MenuViewState.Opened;
            }
        }

        protected virtual UniTask<bool> OnExitSectionAsync(MenuNavContext context)
        {
            return UniTask.FromResult(true);
        }

        public virtual void ForceCloseAndReset()
        {
            if (SectionState != MenuViewState.Closed)
            {
                SectionState = MenuViewState.Closing;
                SectionState = MenuViewState.Closed;
            }

            ResetSection();
            AssignedPage = null;
        }
        
        public virtual void ResetSection()
        {
            OnReset?.Invoke();
        }
    }
}
