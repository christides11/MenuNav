using UnityEngine;

namespace CT.MenuNav
{
    public class MenuModal : MenuView
    {
        public MenuViewState ModalState
        {
            get => modalState;
            protected set => SetViewState(value);
        }

        public override MenuViewState ViewState => modalState;
        protected override MenuViewState StoredViewState
        {
            get => modalState;
            set => modalState = value;
        }

        public MenuManager Manager { get; internal set; }
        public int ModalDepth { get; protected set; }

        [SerializeField] protected MenuViewState modalState;

        protected override void SetDepth(int depth)
        {
            ModalDepth = depth;
        }

        public override void ForceClose()
        {
            base.ForceClose();
            Manager = null;
        }
    }
}
