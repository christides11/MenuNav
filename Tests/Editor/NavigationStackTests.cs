using System;
using NUnit.Framework;

namespace CT.MenuNav.Tests
{
    public class NavigationStackTests
    {
        [Test]
        public void Push_TracksCurrentPreviousAndBottomToTopOrder()
        {
            var history = new NavigationStack<string>();

            history.Push("Main");
            history.Push("Options");
            history.Push("Audio");

            Assert.That(history.Current, Is.EqualTo("Audio"));
            Assert.That(history.Previous, Is.EqualTo("Options"));
            Assert.That(history.ToList(), Is.EqualTo(new[] { "Main", "Options", "Audio" }));
        }

        [Test]
        public void UncommittedTransaction_DoesNotChangeHistory()
        {
            var history = new NavigationStack<string>();
            history.Push("Main");

            var transaction = history.BeginTransaction();
            transaction.Push("Options");

            Assert.That(transaction.Current, Is.EqualTo("Options"));
            Assert.That(history.Current, Is.EqualTo("Main"));
            Assert.That(history.Count, Is.EqualTo(1));
        }

        [Test]
        public void Commit_AppliesAllStagedChangesAtomically()
        {
            var history = new NavigationStack<string>();
            history.Push("Main");
            history.Push("Options");

            var transaction = history.BeginTransaction();
            Assert.That(transaction.Pop(), Is.EqualTo("Options"));
            transaction.Push("Credits");
            transaction.Commit();

            Assert.That(history.ToList(), Is.EqualTo(new[] { "Main", "Credits" }));
        }

        [Test]
        public void ReplaceCurrent_LeavesEarlierHistoryUntouched()
        {
            var history = new NavigationStack<string>();
            history.Push("Main");
            history.Push("Options");

            var transaction = history.BeginTransaction();
            Assert.That(transaction.ReplaceCurrent("Credits"), Is.EqualTo("Options"));
            transaction.Commit();

            Assert.That(history.ToList(), Is.EqualTo(new[] { "Main", "Credits" }));
        }

        [Test]
        public void NullEntry_IsRepresentedWithoutLosingHistoryDepth()
        {
            var history = new NavigationStack<string>();
            history.Push("Main");
            history.Push(null);

            Assert.That(history.Count, Is.EqualTo(2));
            Assert.That(history.Current, Is.Null);
            Assert.That(history.Previous, Is.EqualTo("Main"));
        }

        [Test]
        public void Commit_RejectsTransactionWhenHistoryChangedExternally()
        {
            var history = new NavigationStack<string>();
            history.Push("Main");
            var transaction = history.BeginTransaction();
            transaction.Push("Options");

            history.Push("Credits");

            Assert.Throws<InvalidOperationException>(() => transaction.Commit());
            Assert.That(history.ToList(), Is.EqualTo(new[] { "Main", "Credits" }));
        }
    }
}
