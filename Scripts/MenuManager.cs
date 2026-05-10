using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace CT.MenuNav
{
    public class MenuManager : MonoBehaviour
    {
        public event Action OnAdvancePage, OnBackPage, OnSwapPage;
        
        public MenuPage startingPage;
        [NonSerialized] protected Stack<MenuPage> Breadcrumb = new Stack<MenuPage>();
        [NonSerialized] protected List<MenuPage> assignedPages = new List<MenuPage>();
        
        protected virtual async void Awake()
        {
            foreach (var page in assignedPages)
                _ = page.TryCloseAsync(MenuNavDirection.Back_FORCED);
            
            if(startingPage != null)
                _ = TryForwardPage(startingPage);
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
        
        public virtual async UniTask<bool> TryForwardPage(MenuPage nextPage)
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
            
            nextPage.currentManager = this;
            var openResult = await nextPage.TryOpenAsync(MenuNavDirection.Advance, GetPageCount());
            if (openResult == false)
            {
                nextPage.currentManager = null;
                return false;
            }

            Breadcrumb.Push(nextPage);
            OnAdvancePage?.Invoke();
            return true;
        }

        public virtual async UniTask<bool> TryBackPage()
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
                returnToMenu.currentManager = this;
                var returnResult = await returnToMenu.TryOpenAsync(MenuNavDirection.Back, GetPageCount());
                if (returnResult == false)
                {
                    returnToMenu.currentManager = null;
                    return false;
                }
            }
            
            OnBackPage?.Invoke();
            return true;
        }

        public virtual async UniTask<bool> TrySwapPage(MenuPage swapToPage)
        {
            if (Breadcrumb.Count == 0)
                return await TryForwardPage(swapToPage);

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
            
            swapToPage.currentManager = this;
            var openResult = await swapToPage.TryOpenAsync(MenuNavDirection.Advance, GetPageCount());
            if (openResult == false)
            {
                swapToPage.currentManager = null;
                return false;
            }
            
            Breadcrumb.Push(swapToPage);
            OnSwapPage?.Invoke();
            return true;
        }

        public virtual MenuPage GetCurrentPage()
        {
            if (Breadcrumb.Count == 0)
                return null;
            return Breadcrumb.Peek();
        }

        public virtual int GetPageCount()
        {
            return Breadcrumb.Count;
        }

        public virtual void PrintBreadcrumbs()
        {
            string output = "Menu Breadcrumbs\n";
            var breadcrumbList = Breadcrumb.ToArray().Reverse();

            foreach (var breadcrumb in breadcrumbList)
            {
                if (breadcrumb == null) output += $"Null\n";
                else output += $"{breadcrumb.GetType().Name}\n";
            }
            
            Debug.Log(output);
        }

        public virtual void SetCurrentSelectedGameObject(GameObject selectedGameObject, int playerID = -1)
        {
            
        }

        public virtual GameObject GetCurrentSelectedGameObject(int playerID = -1)
        {
            return null;
        }
    }
}