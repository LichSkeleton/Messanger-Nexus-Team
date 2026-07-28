namespace NexusTeam.Client.Infrastructure
{
    using System;

    /// <summary>
    /// Message bus for in-app communication.
    /// </summary>
    public interface IMessageBus
    {
        /// <summary>
        /// Subscribes to messages of a specific type.
        /// </summary>
        /// <typeparam name="TMessage">The type of message to subscribe to.</typeparam>
        /// <param name="handler">The handler to invoke when a message is published.</param>
        /// <returns>A subscription that can be disposed to unsubscribe.</returns>
        IDisposable Subscribe<TMessage>(Action<TMessage> handler);

        /// <summary>
        /// Publishes a message to all subscribers.
        /// </summary>
        /// <typeparam name="TMessage">The type of message to publish.</typeparam>
        /// <param name="message">The message to publish.</param>
        void Publish<TMessage>(TMessage message);
    }
}
