using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace CT.MenuNav
{
    public abstract class MenuPageSection : MonoBehaviour
    {
        public MenuPageSectionState SectionState
        {
            get => sectionState;
            protected set
            {
                sectionState = value;
                switch (sectionState)
                {
                    case MenuPageSectionState.Closed:
                        OnClosed?.Invoke();
                        break;
                    case MenuPageSectionState.Opened:
                        OnOpened?.Invoke();
                        break;
                    case MenuPageSectionState.Closing:
                        OnClosing?.Invoke();
                        break;
                    case MenuPageSectionState.Opening:
                        OnOpening?.Invoke();
                        break;
                }
            }
        }
        
        public UnityEvent OnOpening, OnOpened, OnClosing, OnClosed, OnReset;
        [SerializeField] private MenuPageSectionState sectionState = MenuPageSectionState.Closed;

        public virtual UniTask<bool> EnterSection(MenuNavDirection direction)
        {
            SectionState = MenuPageSectionState.Opening;
            SectionState = MenuPageSectionState.Opened;
            return UniTask.FromResult(true);
        }

        public virtual UniTask<bool> TryExitSection(MenuNavDirection direction)
        {
            SectionState = MenuPageSectionState.Closing;
            SectionState = MenuPageSectionState.Closed;
            return UniTask.FromResult(true);
        }

        public virtual void ForceExitSection(MenuNavDirection direction)
        {
            SectionState = MenuPageSectionState.Closed;
        }

        public virtual void ResetSection()
        {
            OnReset?.Invoke();
        }
    }
}