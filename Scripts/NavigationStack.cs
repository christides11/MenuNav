using System;
using System.Collections.Generic;

namespace CT.MenuNav
{
    public sealed class NavigationStack<T> where T : class
    {
        private List<T> items = new();
        private int version;

        public int Count => items.Count;
        public T Current => items.Count > 0 ? items[^1] : null;
        public T Previous => items.Count > 1 ? items[^2] : null;

        public void Push(T item)
        {
            items.Add(item);
            version++;
        }

        public T Pop()
        {
            if (items.Count == 0)
                throw new InvalidOperationException("The navigation stack is empty.");

            var index = items.Count - 1;
            var item = items[index];
            items.RemoveAt(index);
            version++;
            return item;
        }

        public void Clear()
        {
            if (items.Count == 0)
                return;

            items.Clear();
            version++;
        }

        public List<T> ToList()
        {
            return new List<T>(items);
        }

        public Transaction BeginTransaction()
        {
            return new Transaction(this, new List<T>(items), version);
        }

        private void Commit(Transaction transaction)
        {
            if (transaction.SourceVersion != version)
                throw new InvalidOperationException(
                    "The navigation stack changed after this transaction began.");

            items = transaction.TakeItems();
            version++;
        }

        public sealed class Transaction
        {
            private readonly NavigationStack<T> owner;
            private List<T> stagedItems;
            private bool isCompleted;

            internal int SourceVersion { get; }

            internal Transaction(NavigationStack<T> owner, List<T> stagedItems, int sourceVersion)
            {
                this.owner = owner;
                this.stagedItems = stagedItems;
                SourceVersion = sourceVersion;
            }

            public int Count
            {
                get
                {
                    EnsureActive();
                    return stagedItems.Count;
                }
            }

            public T Current
            {
                get
                {
                    EnsureActive();
                    return stagedItems.Count > 0 ? stagedItems[^1] : null;
                }
            }

            public T Previous
            {
                get
                {
                    EnsureActive();
                    return stagedItems.Count > 1 ? stagedItems[^2] : null;
                }
            }

            public void Push(T item)
            {
                EnsureActive();
                stagedItems.Add(item);
            }

            public T Pop()
            {
                EnsureActive();
                if (stagedItems.Count == 0)
                    throw new InvalidOperationException("The navigation transaction is empty.");

                var index = stagedItems.Count - 1;
                var item = stagedItems[index];
                stagedItems.RemoveAt(index);
                return item;
            }

            public T ReplaceCurrent(T item)
            {
                EnsureActive();
                if (stagedItems.Count == 0)
                    throw new InvalidOperationException("The navigation transaction is empty.");

                var index = stagedItems.Count - 1;
                var previous = stagedItems[index];
                stagedItems[index] = item;
                return previous;
            }

            public void Commit()
            {
                EnsureActive();
                owner.Commit(this);
                isCompleted = true;
            }

            internal List<T> TakeItems()
            {
                var result = stagedItems;
                stagedItems = null;
                return result;
            }

            private void EnsureActive()
            {
                if (isCompleted || stagedItems == null)
                    throw new InvalidOperationException("This navigation transaction is already complete.");
            }
        }
    }
}