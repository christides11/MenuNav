using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace CT.MenuNav
{
    public abstract class MenuView : MonoBehaviour, IMenuInputState
    {
        public abstract MenuViewState ViewState { get; }
        public bool IsInputEnabled { get; protected set; }

        public UnityEvent OnOpening, OnOpened, OnClosing, OnClosed;

        protected abstract MenuViewState StoredViewState { get; set; }

        internal void SetInputEnabled(bool newEnableState)
        {
            if (IsInputEnabled == newEnableState)
                return;

            IsInputEnabled = newEnableState;
            OnInputEnabledChanged(newEnableState);
        }

        protected virtual void OnInputEnabledChanged(bool currentState)
        {
        }

        protected void SetViewState(MenuViewState state)
        {
            if (StoredViewState == state)
                return;

            StoredViewState = state;
            switch (state)
            {
                case MenuViewState.Closed:
                    OnClosed?.Invoke();
                    break;
                case MenuViewState.Closing:
                    OnClosing?.Invoke();
                    break;
                case MenuViewState.Opening:
                    OnOpening?.Invoke();
                    break;
                case MenuViewState.Opened:
                    OnOpened?.Invoke();
                    break;
            }
        }

        public virtual async UniTask<bool> TryOpenAsync(MenuNavContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (ViewState is MenuViewState.Opening or MenuViewState.Opened)
                return false;

            SetInputEnabled(true);
            SetViewState(MenuViewState.Opening);
            SetDepth(context.Depth);
            gameObject.SetActive(true);

            try
            {
                if (!await OnOpenAsync(context))
                    return false;

                context.CancellationToken.ThrowIfCancellationRequested();
                SetViewState(MenuViewState.Opened);
                return true;
            }
            finally
            {
                if (ViewState == MenuViewState.Opening)
                {
                    SetInputEnabled(false);
                    gameObject.SetActive(false);
                    SetViewState(MenuViewState.Closed);
                }
            }
        }

        public virtual async UniTask<bool> TryCloseAsync(MenuNavContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (ViewState is MenuViewState.Closing or MenuViewState.Closed)
                return false;

            var wasInputEnabled = IsInputEnabled;
            SetInputEnabled(false);
            SetViewState(MenuViewState.Closing);

            try
            {
                if (!await PrepareCloseAsync(context) || !await OnCloseAsync(context))
                    return false;

                context.CancellationToken.ThrowIfCancellationRequested();
                gameObject.SetActive(false);
                SetViewState(MenuViewState.Closed);
                return true;
            }
            finally
            {
                if (ViewState == MenuViewState.Closing)
                {
                    SetViewState(MenuViewState.Opened);
                    SetInputEnabled(wasInputEnabled);
                }
            }
        }

        protected virtual void SetDepth(int depth)
        {
        }

        protected virtual UniTask<bool> PrepareCloseAsync(MenuNavContext context)
        {
            return UniTask.FromResult(true);
        }

        protected virtual UniTask<bool> OnOpenAsync(MenuNavContext context)
        {
            return UniTask.FromResult(true);
        }

        protected virtual UniTask<bool> OnCloseAsync(MenuNavContext context)
        {
            return UniTask.FromResult(true);
        }

        public virtual void ForceClose()
        {
            SetInputEnabled(false);
            gameObject.SetActive(false);
            SetViewState(MenuViewState.Closed);
        }
    }
}
