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

        public int PageCount { get; protected set; }
        
        public UnityEvent OnOpening, OnOpened, OnClosing, OnClosed;
        
        [NonSerialized] protected Stack<MenuPageSection> Breadcrumb = new Stack<MenuPageSection>();
        [NonSerialized] protected HashSet<MenuPageSection> SectionsToReset = new HashSet<MenuPageSection>();
        [SerializeField] protected MenuPageState pageState;
        [NonSerialized] public MenuManager currentManager;
        
        public virtual async UniTask<bool> TryOpenAsync(MenuNavDirection direction, int pageCount)
        {
            PageState = MenuPageState.Opening;
            gameObject.SetActive(true);
            PageCount = pageCount;
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

        public virtual void ResetPage()
        {
            while (Breadcrumb.Count > 0)
            {
                MenuPageSection closing = Breadcrumb.Pop();
                closing.ForceExitSection(MenuNavDirection.Back_FORCED);
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
        public virtual async UniTask<bool> TryAdvanceSection(MenuPageSection nextSection)
        {
            if (Breadcrumb.Count > 0)
            {
                var currentSection = Breadcrumb.Peek();
                if (currentSection != null)
                {
                    var exitResult = await currentSection.TryExitSection(MenuNavDirection.Advance);
                    if (exitResult == false)
                        return false;
                }
            }

            if (nextSection == null)
            {
                Breadcrumb.Push(null);
                return true;
            }
            
            var enterResult = await nextSection.TryEnterSection(MenuNavDirection.Advance);
            if (enterResult == false)
            {
                var oldSection = Breadcrumb.Peek();
                if (oldSection != null)
                {
                    await oldSection.TryEnterSection(MenuNavDirection.Back_FORCED);
                }
                return false;
            }
            Breadcrumb.Push(nextSection);
            
            SectionsToReset.Add(nextSection);
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
                var returnResult = await previousSection.TryEnterSection(MenuNavDirection.Back);
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