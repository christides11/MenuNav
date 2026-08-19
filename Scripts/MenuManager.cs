using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.EventSystems;

namespace CT.MenuNav
{
    public class MenuManager : MonoBehaviour, IMenuInputOnConfirm, IMenuInputOnBack, IMenuInputOnStart,
        IMenuInputOnNavigate, IMenuInputOnNavigateRaw
    {
        public event Action OnAdvancePage, OnBackPage, OnSwapPage;
        public event Action<MenuModal> OnOpenedModal, OnClosedModal;

        public Action<GameObject, int> SetCurrentSelectedGameObject { get; set; }
        public Func<int, GameObject> GetCurrentSelectedGameObject { get; set; }

        public bool IsNavigating { get; protected set; }

        public MenuPage startingPage;
        [NonSerialized] protected NavigationStack<MenuPage> Breadcrumb = new NavigationStack<MenuPage>();
        [NonSerialized] protected NavigationStack<MenuModal> ModalHistory = new NavigationStack<MenuModal>();
        [NonSerialized] protected List<MenuPage> assignedPages = new List<MenuPage>();

        protected virtual async void Awake()
        {
            try
            {
                foreach (var page in assignedPages)
                    await page.TryCloseAsync(new MenuNavContext(MenuNavDirection.Back, isForced: true,
                        isInstant: true, fromPage: page));

                if (startingPage != null)
                    await TryForwardPageAsync(startingPage);
            }
            catch (OperationCanceledException) when (this.GetCancellationTokenOnDestroy().IsCancellationRequested)
            {
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        protected virtual void OnDestroy()
        {
        }

        public virtual void Teardown()
        {
            while (ModalHistory.Count > 0)
            {
                var modal = ModalHistory.Pop();
                modal?.ForceClose();
            }

            while (Breadcrumb.Count > 0)
            {
                var page = Breadcrumb.Pop();
                page?.ForceClose();
            }
        }

        public UniTask<bool> SetBreadcrumbsAsync(
            List<MenuPage> breadcrumbs,
            bool closeModals = true,
            CancellationToken cancellationToken = default)
        {
            cancellationToken = ResolveCancellationToken(cancellationToken);
            return RunPageNavigationAsync(
                () => SetBreadcrumbsInternalAsync(breadcrumbs, cancellationToken), closeModals, cancellationToken);
        }

        protected virtual async UniTask<bool> SetBreadcrumbsInternalAsync(
            List<MenuPage> breadcrumbs,
            CancellationToken cancellationToken)
        {
            var closeResult = await BackOutAllPagesInternalAsync(0, cancellationToken);
            if (closeResult == false)
                return false;

            foreach (var page in breadcrumbs)
            {
                if (page != null)
                {
                    var fromPage = Breadcrumb.Current;
                    var openResult = await page.TryOpenAsync(new MenuNavContext(
                        MenuNavDirection.Advance, isForced: true, isInstant: true, depth: GetPageCount(),
                        fromPage: fromPage, toPage: page, cancellationToken: cancellationToken));
                    if (openResult == false) return false;
                }

                Breadcrumb.Push(page);
            }

            return true;
        }

        #region Pages

        public UniTask<bool> BackOutAllPagesAsync(
            int pageCountToLeaveOpen = 0,
            bool closeModals = true,
            CancellationToken cancellationToken = default)
        {
            cancellationToken = ResolveCancellationToken(cancellationToken);
            return RunPageNavigationAsync(
                () => BackOutAllPagesInternalAsync(pageCountToLeaveOpen, cancellationToken),
                closeModals, cancellationToken);
        }

        protected virtual async UniTask<bool> BackOutAllPagesInternalAsync(
            int pageCountToLeaveOpen,
            CancellationToken cancellationToken)
        {
            while (Breadcrumb.Count > pageCountToLeaveOpen)
            {
                var transaction = Breadcrumb.BeginTransaction();
                var currentPage = transaction.Pop();
                if (currentPage == null)
                {
                    transaction.Commit();
                    continue;
                }

                var exitResult = await currentPage.TryCloseAsync(new MenuNavContext(
                    MenuNavDirection.Back, isForced: true, isInstant: true, depth: Breadcrumb.Count - 1,
                    fromPage: currentPage, toPage: transaction.Current, cancellationToken: cancellationToken));
                if (exitResult == false)
                    return false;

                transaction.Commit();
            }

            return true;
        }

        public UniTask<bool> TryForwardPageAsync(
            MenuPage nextPage,
            bool closeModals = true,
            CancellationToken cancellationToken = default)
        {
            cancellationToken = ResolveCancellationToken(cancellationToken);
            return RunPageNavigationAsync(
                () => TryForwardPageInternalAsync(nextPage, cancellationToken), closeModals, cancellationToken);
        }

        protected virtual async UniTask<bool> TryForwardPageInternalAsync(
            MenuPage nextPage,
            CancellationToken cancellationToken)
        {
            var transaction = Breadcrumb.BeginTransaction();
            var currentPage = transaction.Current;
            transaction.Push(nextPage);

            if (currentPage != null)
            {
                var exitResult = await currentPage.TryCloseAsync(new MenuNavContext(
                    MenuNavDirection.Advance, depth: Breadcrumb.Count - 1,
                    fromPage: currentPage, toPage: nextPage, cancellationToken: cancellationToken));
                if (exitResult == false)
                    return false;
            }

            if (nextPage == null)
            {
                transaction.Commit();
                OnAdvancePage?.Invoke();
                return true;
            }

            var previousManager = nextPage.currentManager;
            nextPage.currentManager = this;
            bool openResult;
            try
            {
                openResult = await nextPage.TryOpenAsync(new MenuNavContext(
                    MenuNavDirection.Advance, depth: GetPageCount(),
                    fromPage: currentPage, toPage: nextPage, cancellationToken: cancellationToken));
            }
            catch
            {
                nextPage.currentManager = previousManager;
                await TryRestorePageAsync(currentPage, new MenuNavContext(
                    MenuNavDirection.Back, isForced: true,
                    depth: Mathf.Max(0, Breadcrumb.Count - 1),
                    fromPage: nextPage, toPage: currentPage));
                throw;
            }

            if (openResult == false)
            {
                nextPage.currentManager = previousManager;
                await TryRestorePageAsync(currentPage, new MenuNavContext(
                    MenuNavDirection.Back, isForced: true,
                    depth: Mathf.Max(0, Breadcrumb.Count - 1),
                    fromPage: nextPage, toPage: currentPage));
                return false;
            }

            transaction.Commit();
            OnAdvancePage?.Invoke();
            return true;
        }

        public UniTask<bool> TryHandleBackAsync(CancellationToken cancellationToken = default)
        {
            return ModalHistory.Count > 0
                ? TryCloseModalAsync(cancellationToken)
                : TryBackPageAsync(cancellationToken: cancellationToken);
        }

        public UniTask<bool> TryBackPageAsync(
            bool closeModals = true,
            CancellationToken cancellationToken = default)
        {
            cancellationToken = ResolveCancellationToken(cancellationToken);
            return RunPageNavigationAsync(
                () => TryBackPageInternalAsync(cancellationToken), closeModals, cancellationToken);
        }

        protected virtual async UniTask<bool> TryBackPageInternalAsync(CancellationToken cancellationToken)
        {
            if (Breadcrumb.Count == 0)
                return false;

            var transaction = Breadcrumb.BeginTransaction();
            var currentPage = transaction.Pop();
            var returnToMenu = transaction.Current;
            if (currentPage != null)
            {
                var exitResult = await currentPage.TryCloseAsync(new MenuNavContext(
                    MenuNavDirection.Back, depth: Breadcrumb.Count - 1,
                    fromPage: currentPage, toPage: returnToMenu, cancellationToken: cancellationToken));
                if (exitResult == false)
                    return false;
            }

            if (transaction.Count == 0)
            {
                transaction.Commit();
                if (currentPage != null)
                    currentPage.currentManager = null;
                OnBackPage?.Invoke();
                return true;
            }

            if (returnToMenu != null)
            {
                returnToMenu.currentManager = this;
                bool returnResult;
                try
                {
                    returnResult = await returnToMenu.TryOpenAsync(new MenuNavContext(
                        MenuNavDirection.Back, depth: transaction.Count - 1,
                        fromPage: currentPage, toPage: returnToMenu, cancellationToken: cancellationToken));
                }
                catch
                {
                    await TryRestorePageAsync(currentPage, new MenuNavContext(
                        MenuNavDirection.Advance, isForced: true,
                        depth: Breadcrumb.Count - 1,
                        fromPage: returnToMenu, toPage: currentPage));
                    throw;
                }

                if (returnResult == false)
                {
                    await TryRestorePageAsync(currentPage, new MenuNavContext(
                        MenuNavDirection.Advance, isForced: true,
                        depth: Breadcrumb.Count - 1,
                        fromPage: returnToMenu, toPage: currentPage));
                    return false;
                }
            }

            transaction.Commit();
            if (currentPage != null)
                currentPage.currentManager = null;

            OnBackPage?.Invoke();
            return true;
        }

        public virtual UniTask<bool> TrySwapPageAsync(
            MenuPage swapToPage,
            bool closeModals = true,
            CancellationToken cancellationToken = default)
        {
            cancellationToken = ResolveCancellationToken(cancellationToken);
            return RunPageNavigationAsync(
                () => TrySwapPageInternalAsync(swapToPage, cancellationToken), closeModals, cancellationToken);
        }

        protected virtual async UniTask<bool> TrySwapPageInternalAsync(
            MenuPage swapToPage,
            CancellationToken cancellationToken)
        {
            if (Breadcrumb.Count == 0)
                return await TryForwardPageInternalAsync(swapToPage, cancellationToken);

            var transaction = Breadcrumb.BeginTransaction();
            var currentPage = transaction.ReplaceCurrent(swapToPage);
            if (currentPage != null)
            {
                var exitResult = await currentPage.TryCloseAsync(new MenuNavContext(
                    MenuNavDirection.Back, depth: Breadcrumb.Count - 1,
                    fromPage: currentPage, toPage: swapToPage, cancellationToken: cancellationToken));
                if (exitResult == false)
                    return false;
            }

            if (swapToPage == null)
            {
                transaction.Commit();
                if (currentPage != null)
                    currentPage.currentManager = null;
                OnSwapPage?.Invoke();
                return true;
            }

            var previousManager = swapToPage.currentManager;
            swapToPage.currentManager = this;
            bool openResult;
            try
            {
                openResult = await swapToPage.TryOpenAsync(new MenuNavContext(
                    MenuNavDirection.Advance, depth: Breadcrumb.Count - 1,
                    fromPage: currentPage, toPage: swapToPage, cancellationToken: cancellationToken));
            }
            catch
            {
                swapToPage.currentManager = previousManager;
                await TryRestorePageAsync(currentPage, new MenuNavContext(
                    MenuNavDirection.Back, isForced: true,
                    depth: Breadcrumb.Count - 1,
                    fromPage: swapToPage, toPage: currentPage));
                throw;
            }

            if (openResult == false)
            {
                swapToPage.currentManager = previousManager;
                await TryRestorePageAsync(currentPage, new MenuNavContext(
                    MenuNavDirection.Back, isForced: true,
                    depth: Breadcrumb.Count - 1,
                    fromPage: swapToPage, toPage: currentPage));
                return false;
            }

            transaction.Commit();
            if (currentPage != null)
                currentPage.currentManager = null;
            OnSwapPage?.Invoke();
            return true;
        }

        protected virtual async UniTask TryRestorePageAsync(
            MenuPage page,
            MenuNavContext context)
        {
            if (page == null)
                return;

            page.currentManager = this;
            try
            {
                var restored = await page.TryOpenAsync(context);
                if (restored == false)
                    Debug.LogError($"Failed to restore menu page {page.name} after a navigation failure.", page);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, page);
                Debug.LogError($"Failed to restore menu page {page.name} after a navigation failure.", page);
            }
        }

        protected virtual async UniTask<bool> RunNavigationAsync(
            Func<UniTask<bool>> operation,
            CancellationToken cancellationToken)
        {
            if (IsNavigating)
                return false;

            IsNavigating = true;
            DisableViewInput();

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await operation();
            }
            finally
            {
                IsNavigating = false;
                RefreshInputFocus();
            }
        }

        protected virtual UniTask<bool> RunPageNavigationAsync(
            Func<UniTask<bool>> operation,
            bool closeModals,
            CancellationToken cancellationToken)
        {
            return RunNavigationAsync(async () =>
            {
                if (closeModals && ModalHistory.Count > 0)
                {
                    var closeResult = await TryCloseAllModalsInternalAsync(false, cancellationToken);
                    if (closeResult == false)
                        return false;
                }

                return await operation();
            }, cancellationToken);
        }

        #endregion

        #region Modal

        public UniTask<bool> TryOpenModalAsync(
            MenuModal modal,
            CancellationToken cancellationToken = default)
        {
            cancellationToken = ResolveCancellationToken(cancellationToken);
            return TryOpenModalAsync(modal, new MenuNavContext(
                MenuNavDirection.Advance, cancellationToken: cancellationToken));
        }

        public UniTask<bool> TryOpenModalAsync(MenuModal modal, MenuNavContext context)
        {
            context = ResolveContext(context);
            return RunNavigationAsync(
                () => TryOpenModalInternalAsync(modal, context), context.CancellationToken);
        }

        protected virtual async UniTask<bool> TryOpenModalInternalAsync(
            MenuModal modal,
            MenuNavContext context)
        {
            if (modal == null)
                return false;

            var transaction = ModalHistory.BeginTransaction();
            transaction.Push(modal);

            var previousManager = modal.Manager;
            modal.Manager = this;
            bool openResult;
            try
            {
                openResult = await modal.TryOpenAsync(context.WithDepth(ModalHistory.Count));
            }
            catch
            {
                modal.Manager = previousManager;
                throw;
            }

            if (openResult == false)
            {
                modal.Manager = previousManager;
                return false;
            }

            transaction.Commit();
            OnOpenedModal?.Invoke(modal);
            return true;
        }

        public UniTask<bool> TryCloseModalAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken = ResolveCancellationToken(cancellationToken);
            return TryCloseModalAsync(new MenuNavContext(
                MenuNavDirection.Back, cancellationToken: cancellationToken));
        }

        public UniTask<bool> TryCloseModalAsync(MenuNavContext context)
        {
            context = ResolveContext(context);
            return RunNavigationAsync(
                () => TryCloseModalInternalAsync(context), context.CancellationToken);
        }

        protected virtual async UniTask<bool> TryCloseModalInternalAsync(MenuNavContext context)
        {
            if (ModalHistory.Count == 0)
                return false;

            var transaction = ModalHistory.BeginTransaction();
            var modal = transaction.Pop();
            if (modal != null)
            {
                var closeResult = await modal.TryCloseAsync(
                    context.WithDepth(ModalHistory.Count - 1));
                if (closeResult == false)
                    return false;
            }

            transaction.Commit();
            if (modal != null)
                modal.Manager = null;
            OnClosedModal?.Invoke(modal);
            return true;
        }

        public UniTask<bool> TryCloseAllModalsAsync(
            bool isInstant = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken = ResolveCancellationToken(cancellationToken);
            return RunNavigationAsync(
                () => TryCloseAllModalsInternalAsync(isInstant, cancellationToken), cancellationToken);
        }

        protected virtual async UniTask<bool> TryCloseAllModalsInternalAsync(
            bool isInstant,
            CancellationToken cancellationToken)
        {
            while (ModalHistory.Count > 0)
            {
                var closeResult = await TryCloseModalInternalAsync(new MenuNavContext(
                    MenuNavDirection.Back,
                    isForced: true,
                    isInstant: isInstant,
                    cancellationToken: cancellationToken));
                if (closeResult == false)
                    return false;
            }

            return true;
        }

        #endregion

        protected virtual CancellationToken ResolveCancellationToken(CancellationToken cancellationToken)
        {
            return cancellationToken.CanBeCanceled
                ? cancellationToken
                : this.GetCancellationTokenOnDestroy();
        }

        protected virtual MenuNavContext ResolveContext(MenuNavContext context)
        {
            return context.CancellationToken.CanBeCanceled
                ? context
                : context.WithCancellationToken(this.GetCancellationTokenOnDestroy());
        }

        public virtual MenuPage GetCurrentPage()
        {
            if (Breadcrumb.Count == 0)
                return null;
            return Breadcrumb.Current;
        }

        public virtual int GetPageCount()
        {
            return Breadcrumb.Count;
        }

        public bool HasOpenModal => ModalHistory.Count > 0;

        public virtual MenuModal GetCurrentModal()
        {
            return ModalHistory.Current;
        }

        public virtual int GetModalCount()
        {
            return ModalHistory.Count;
        }

        public virtual List<MenuModal> GetModalList()
        {
            return ModalHistory.ToList();
        }

        public virtual List<MenuPage> GetBreadcrumbList()
        {
            return Breadcrumb.ToList();
        }

        public virtual void PrintBreadcrumbs()
        {
            string output = "Menu Breadcrumbs\n";
            var breadcrumbList = Breadcrumb.ToList();

            foreach (var breadcrumb in breadcrumbList)
            {
                if (breadcrumb == null) output += $"Null\n";
                else output += $"{breadcrumb.GetType().Name}\n";
            }

            Debug.Log(output);
        }

        #region Input

        protected virtual void DisableViewInput()
        {
            foreach (var page in Breadcrumb.ToList())
                page?.SetInputEnabled(false);

            foreach (var modal in ModalHistory.ToList())
                modal?.SetInputEnabled(false);
        }

        protected virtual void RefreshInputFocus()
        {
            DisableViewInput();

            if (ModalHistory.Count > 0)
            {
                ModalHistory.Current?.SetInputEnabled(true);
                return;
            }

            Breadcrumb.Current?.SetInputEnabled(true);
        }

        protected virtual object GetCurrentInputTarget()
        {
            object inputTarget = ModalHistory.Count > 0
                ? ModalHistory.Current
                : Breadcrumb.Current;

            return inputTarget is IMenuInputState { IsInputEnabled: true }
                ? inputTarget
                : null;
        }

        public virtual void OnInputBackPressed(MenuInputButtonPhase buttonPhase, MenuInputContext context)
        {
            if (GetCurrentInputTarget() is not IMenuInputOnBack inputTarget)
                return;
            inputTarget.OnInputBackPressed(buttonPhase, context);
        }

        public virtual void OnInputStartPressed(MenuInputButtonPhase buttonPhase, MenuInputContext context)
        {
            if (GetCurrentInputTarget() is not IMenuInputOnStart inputTarget)
                return;
            inputTarget.OnInputStartPressed(buttonPhase, context);
        }

        public virtual void OnInputConfirmPressed(MenuInputButtonPhase buttonPhase, MenuInputContext context)
        {
            if (GetCurrentInputTarget() is not IMenuInputOnConfirm inputTarget)
                return;
            inputTarget.OnInputConfirmPressed(buttonPhase, context);
        }

        public virtual void OnNavigate(Vector2 navInput, MenuInputContext context)
        {
            if (GetCurrentInputTarget() is not IMenuInputOnNavigate inputTarget)
                return;
            inputTarget.OnNavigate(navInput, context);
        }

        public virtual void OnNavigateRaw(Vector2 navInput, MenuInputContext context)
        {
            if (GetCurrentInputTarget() is not IMenuInputOnNavigateRaw inputTarget)
                return;
            inputTarget.OnNavigateRaw(navInput, context);
        }

        #endregion
    }
}