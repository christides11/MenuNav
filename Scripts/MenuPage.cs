using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CT.MenuNav
{
    public class MenuPage : MonoBehaviour
    {
        public MenuPageState PageState
        {
            get => pageState;
            protected set
            {
                pageState = value;
                switch (pageState)
                {
                    case MenuPageState.Closed:
                        OnClosed?.Invoke();
                        break;
                    case MenuPageState.Opened:
                        OnOpened?.Invoke();
                        break;
                    case MenuPageState.Closing:
                        OnClosing?.Invoke();
                        break;
                    case MenuPageState.Opening:
                        OnOpening?.Invoke();
                        break;
                }
            }
        }

        public UnityEvent OnOpening, OnOpened, OnClosing, OnClosed;

        [SerializeField] protected MenuPageSection defaultSection;

        protected Stack<MenuPageSection> Breadcrumb = new Stack<MenuPageSection>();
        protected List<MenuPageSection> SectionsToReset = new List<MenuPageSection>();
        [SerializeField] protected MenuPageState pageState;

        public int PageCount { get; protected set; }

        public virtual async UniTask<bool> TryOpenAsync(MenuNavDirection direction, int pageCount)
        {
            PageState = MenuPageState.Opening;

            gameObject.SetActive(true);
            PageCount = pageCount;

            await TryAdvanceSection(defaultSection);

            PageState = MenuPageState.Opened;
            return true;
        }

        public virtual async UniTask<bool> TryCloseAsync(MenuNavDirection direction)
        {
            PageState = MenuPageState.Closing;
            ResetSections();
            gameObject.SetActive(false);
            PageState = MenuPageState.Closed;
            return true;
        }

        public virtual void ForceClose()
        {
            ResetSections();
            gameObject.SetActive(false);
            PageState = MenuPageState.Closed;
        }

        public virtual void ResetPage()
        {
            while (Breadcrumb.Count > 0)
            {
                MenuPageSection closing = Breadcrumb.Pop();
                closing.ForceExitSection(MenuNavDirection.Reset);
                closing.ResetSection();
            }
        }

        protected virtual void ResetSections()
        {
            foreach (MenuPageSection section in SectionsToReset)
            {
                section.ResetSection();
            }

            SectionsToReset.Clear();
        }

        // Section Navigation
        public virtual async UniTask<bool> TryAdvanceSection(MenuPageSection section)
        {
            if (section == null)
                return false;

            if (Breadcrumb.Count > 0)
            {
                var exitResult = await Breadcrumb.Peek().TryExitSection(MenuNavDirection.Advance);
                if (exitResult == false)
                    return false;
            }

            Breadcrumb.Push(section);

            var enterResult = await section.EnterSection(MenuNavDirection.Advance);
            if (enterResult == false)
            {
                // TODO: Re enter old section.
                return false;
            }

            if (SectionsToReset.Contains(section))
                return true;

            SectionsToReset.Add(section);
            return true;
        }

        public virtual async UniTask<bool> TryExitCurrentSection()
        {
            if (Breadcrumb.Count == 0)
                return false;

            MenuPageSection currentSection = Breadcrumb.Peek();
            var exitResult = await currentSection.TryExitSection(MenuNavDirection.Back);
            if (exitResult == false)
            {
                return false;
            }

            Breadcrumb.Pop();
            SectionsToReset.Remove(currentSection);

            if (Breadcrumb.Count > 0)
            {
                MenuPageSection previousSection = Breadcrumb.Peek();
                var returnResult = await previousSection.EnterSection(MenuNavDirection.Back);
                return returnResult;
            }

            return true;
        }

        public virtual MenuPageSection GetCurrentSection()
        {
            if (Breadcrumb.Count == 0) return null;
            return Breadcrumb.Peek();
        }
    }
}