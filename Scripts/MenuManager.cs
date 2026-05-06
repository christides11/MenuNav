using System.Collections.Generic;
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;

namespace CT.MenuNav
{
    public class MenuManager : MonoBehaviour
    {
        public event Action OnAdvancePage, OnBackPage, OnSwapPage;
        
        public MenuPage startingPage;
        protected Stack<MenuPage> Breadcrumb = new Stack<MenuPage>();
        protected List<MenuPage> assignedPages = new List<MenuPage>();
        
        protected virtual async void Awake()
        {
            foreach (var page in assignedPages)
                _ = page.TryCloseAsync(MenuNavDirection.Back_FORCED);
            
            if(startingPage != null)
                _ = TryAdvancePage(startingPage);
        }

        protected virtual void OnDestroy()
        {
            
        }
        
        public virtual void Teardown()
        {
            while (Breadcrumb.Count > 0)
            {
                var page = Breadcrumb.Pop();
                _ = page?.TryCloseAsync(MenuNavDirection.Back_FORCED);
            }
        }
        
        public async UniTask<bool> TryAdvancePage(MenuPage nextPage)
        {
            if (Breadcrumb.Count > 0)
            {
                var currentPage = Breadcrumb.Peek();
                var exitResult = await currentPage.TryCloseAsync(MenuNavDirection.Advance);
                if (exitResult == false)
                    return false;
            }

            if (nextPage == null)
            {
                Breadcrumb.Push(null);
                OnAdvancePage?.Invoke();
                return true;
            }
            
            var openResult = await nextPage.TryOpenAsync(MenuNavDirection.Advance, GetPageCount());
            if (openResult == false)
                return false;
            
            Breadcrumb.Push(nextPage);
            nextPage.currentManager = this;
            OnAdvancePage?.Invoke();
            return true;
        }

        public async UniTask<bool> TryBackPage()
        {
            if (Breadcrumb.Count == 0)
                return false;

            if (Breadcrumb.TryPeek(out MenuPage currentPage))
            {
                var exitResult = await currentPage.TryCloseAsync(MenuNavDirection.Back);
                if (exitResult == false)
                    return false;
            }
            Breadcrumb.Pop();

            if (Breadcrumb.Count == 0)
            {
                OnBackPage?.Invoke();
                return true;
            }

            var returnToMenu = Breadcrumb.Peek();
            if (returnToMenu != null)
            {
                var returnResult = await returnToMenu.TryOpenAsync(MenuNavDirection.Back, GetPageCount());
                if (returnResult == false)
                    return false;
                returnToMenu.currentManager = this;
            }
            
            OnBackPage?.Invoke();
            return true;
        }

        public async UniTask<bool> TrySwapPage(MenuPage swapToPage)
        {
            if (Breadcrumb.Count == 0)
                return await TryAdvancePage(swapToPage);

            if (Breadcrumb.TryPeek(out MenuPage currentPage))
            {
                var exitResult = await currentPage.TryCloseAsync(MenuNavDirection.Back);
                if (exitResult == false)
                    return false;
            }
            Breadcrumb.Pop();

            if (swapToPage == null)
            {
                Breadcrumb.Push(null);
                OnSwapPage?.Invoke();
                return true;
            }
            
            var openResult = await swapToPage.TryOpenAsync(MenuNavDirection.Advance, GetPageCount());
            if (openResult == false)
                return false;

            swapToPage.currentManager = this;
            Breadcrumb.Push(swapToPage);
            OnSwapPage?.Invoke();
            return true;
        }

        public MenuPage GetCurrentPage()
        {
            if (Breadcrumb.Count == 0)
                return null;
            return Breadcrumb.Peek();
        }

        public int GetPageCount()
        {
            return Breadcrumb.Count;
        }
    }
}