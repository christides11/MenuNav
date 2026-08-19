using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace CT.MenuNav
{
    public class MenuPage : MenuView
    {
        public MenuViewState PageState
        {
            get => pageState;
            protected set => SetViewState(value);
        }

        public override MenuViewState ViewState => pageState;
        protected override MenuViewState StoredViewState
        {
            get => pageState;
            set => pageState = value;
        }

        public int PageCount { get; protected set; }
        public bool IsNavigatingSections { get; protected set; }

        [NonSerialized] protected NavigationStack<MenuPageSection> Breadcrumb = new();
        [SerializeField] protected MenuViewState pageState;
        [NonSerialized] public MenuManager currentManager;

        protected override void SetDepth(int depth)
        {
            PageCount = depth;
        }

        protected override async UniTask<bool> PrepareCloseAsync(MenuNavContext context)
        {
            var sectionCloseContext = new MenuNavContext(
                MenuNavDirection.Back, isForced: true, isInstant: context.IsInstant,
                depth: Breadcrumb.Count, fromPage: context.FromPage, toPage: context.ToPage,
                cancellationToken: context.CancellationToken);
            return await RunSectionNavigationAsync(
                () => CloseSectionHistoryAsync(sectionCloseContext),
                context.CancellationToken);
        }

        public override void ForceClose()
        {
            ForceResetSectionHistory();
            base.ForceClose();
        }

        public virtual void ResetPage()
        {
            ForceResetSectionHistory();
        }

        #region Sections
        protected virtual async UniTask<bool> CloseSectionHistoryAsync(MenuNavContext context)
        {
            while (Breadcrumb.Count > 0)
            {
                var transaction = Breadcrumb.BeginTransaction();
                var section = transaction.Pop();
                if (section != null && section.SectionState != MenuViewState.Closed)
                {
                    var closeResult = await section.TryExitSection(
                        context.WithDepth(Breadcrumb.Count - 1));
                    if (closeResult == false)
                        return false;
                }

                transaction.Commit();
                if (section != null)
                {
                    section.ResetSection();
                    section.AssignedPage = null;
                }
            }

            return true;
        }

        protected virtual void ForceResetSectionHistory()
        {
            var resetSections = new HashSet<MenuPageSection>();
            while (Breadcrumb.Count > 0)
            {
                var section = Breadcrumb.Pop();
                if (section != null && resetSections.Add(section))
                    section.ForceCloseAndReset();
            }
        }

        // Section Navigation
        public virtual UniTask<bool> TryAdvanceSectionAsync(
            MenuPageSection nextSection,
            CancellationToken cancellationToken = default)
        {
            cancellationToken = ResolveSectionCancellationToken(cancellationToken);
            return RunSectionNavigationAsync(
                () => TryAdvanceSectionInternalAsync(nextSection, cancellationToken),
                cancellationToken);
        }

        protected virtual async UniTask<bool> TryAdvanceSectionInternalAsync(
            MenuPageSection nextSection,
            CancellationToken cancellationToken)
        {
            var transaction = Breadcrumb.BeginTransaction();
            var currentSection = transaction.Current;
            transaction.Push(nextSection);

            if (currentSection != null)
            {
                var exitResult = await currentSection.TryExitSection(new MenuNavContext(
                    MenuNavDirection.Advance, depth: Breadcrumb.Count - 1,
                    cancellationToken: cancellationToken));
                if (exitResult == false)
                    return false;
            }

            if (nextSection == null)
            {
                transaction.Commit();
                return true;
            }

            var previousPage = nextSection.AssignedPage;
            nextSection.AssignedPage = this;
            bool enterResult;
            try
            {
                enterResult = await nextSection.TryEnterSection(new MenuNavContext(
                    MenuNavDirection.Advance, depth: Breadcrumb.Count,
                    cancellationToken: cancellationToken));
            }
            catch
            {
                nextSection.AssignedPage = previousPage;
                await TryRestoreSectionAsync(currentSection, new MenuNavContext(
                    MenuNavDirection.Back, isForced: true, depth: Breadcrumb.Count - 1));
                throw;
            }
            if (enterResult == false)
            {
                nextSection.AssignedPage = previousPage;
                await TryRestoreSectionAsync(currentSection, new MenuNavContext(
                    MenuNavDirection.Back, isForced: true, depth: Breadcrumb.Count - 1));

                return false;
            }

            transaction.Commit();
            return true;
        }

        public virtual UniTask<bool> TryBackSectionAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken = ResolveSectionCancellationToken(cancellationToken);
            return RunSectionNavigationAsync(
                () => TryBackSectionInternalAsync(cancellationToken),
                cancellationToken);
        }

        protected virtual async UniTask<bool> TryBackSectionInternalAsync(
            CancellationToken cancellationToken)
        {
            if (Breadcrumb.Count == 0)
                return false;

            var transaction = Breadcrumb.BeginTransaction();
            var currentSection = transaction.Pop();
            if (currentSection != null)
            {
                var exitResult = await currentSection.TryExitSection(new MenuNavContext(
                    MenuNavDirection.Back, depth: Breadcrumb.Count - 1,
                    cancellationToken: cancellationToken));
                if (exitResult == false)
                {
                    return false;
                }
            }

            if (transaction.Count > 0)
            {
                var previousSection = transaction.Current;
                if (previousSection != null)
                {
                    previousSection.AssignedPage = this;
                    bool returnResult;
                    try
                    {
                        returnResult = await previousSection.TryEnterSection(new MenuNavContext(
                            MenuNavDirection.Back, depth: transaction.Count - 1,
                            cancellationToken: cancellationToken));
                    }
                    catch
                    {
                        await TryRestoreSectionAsync(currentSection, new MenuNavContext(
                            MenuNavDirection.Advance, isForced: true, depth: Breadcrumb.Count - 1));
                        throw;
                    }
                    if (returnResult == false)
                    {
                        await TryRestoreSectionAsync(currentSection, new MenuNavContext(
                            MenuNavDirection.Advance, isForced: true, depth: Breadcrumb.Count - 1));
                        return false;
                    }
                }
            }

            transaction.Commit();
            if (currentSection != null)
                currentSection.AssignedPage = null;

            return true;
        }

        protected virtual async UniTask<bool> RunSectionNavigationAsync(
            Func<UniTask<bool>> operation,
            CancellationToken cancellationToken)
        {
            if (IsNavigatingSections)
                return false;

            cancellationToken.ThrowIfCancellationRequested();
            IsNavigatingSections = true;

            try
            {
                return await operation();
            }
            finally
            {
                IsNavigatingSections = false;
            }
        }

        protected virtual CancellationToken ResolveSectionCancellationToken(
            CancellationToken cancellationToken)
        {
            return cancellationToken.CanBeCanceled
                ? cancellationToken
                : this.GetCancellationTokenOnDestroy();
        }

        private async UniTask TryRestoreSectionAsync(
            MenuPageSection section,
            MenuNavContext context)
        {
            if (section == null)
                return;

            section.AssignedPage = this;
            try
            {
                var restored = await section.TryEnterSection(context);
                if (restored == false)
                    Debug.LogError($"Failed to restore menu section {section.name} after a navigation failure.", section);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, section);
                Debug.LogError($"Failed to restore menu section {section.name} after a navigation failure.", section);
            }
        }

        public virtual MenuPageSection GetCurrentSection()
        {
            if (Breadcrumb.Count == 0) return null;
            return Breadcrumb.Current;
        }
        #endregion
    }
}
