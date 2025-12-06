namespace DarkArmsProto.Core
{
    /// <summary>
    /// Interface for components that want to receive collision events (like Unity's OnCollision).
    /// Implement this on your components to automatically receive collision notifications.
    /// </summary>
    public interface ICollisionHandler
    {
        /// <summary>
        /// Called automatically when this object collides with another
        /// </summary>
        void OnCollision(GameObject other);
    }

    /// <summary>
    /// Interface for components that want to receive trigger events (non-physical collisions)
    /// </summary>
    public interface ITriggerHandler
    {
        /// <summary>
        /// Called automatically when this object enters a trigger zone
        /// </summary>
        void OnTriggerEnter(GameObject other);

        /// <summary>
        /// Called automatically when this object stays in a trigger zone
        /// </summary>
        void OnTriggerStay(GameObject other);

        /// <summary>
        /// Called automatically when this object exits a trigger zone
        /// </summary>
        void OnTriggerExit(GameObject other);
    }
}
