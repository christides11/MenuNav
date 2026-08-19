using UnityEngine.EventSystems;

namespace CT.MenuNav
{
    public readonly partial struct MenuInputContext
    {
        public int PlayerId { get; }
        public BaseEventData EventData { get; }

        public MenuInputContext(
            int playerId,
            BaseEventData eventData = null)
        {
            PlayerId = playerId;
            EventData = eventData;
        }
    }
}