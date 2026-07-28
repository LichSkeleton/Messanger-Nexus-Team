namespace NexusTeam.Client.Infrastructure
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Implementation of message bus for in-app communication.
    /// </summary>
    public class MessageBus : IMessageBus
    {
        private readonly Dictionary<Type, List<Delegate>> subscriptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageBus"/> class.
        /// </summary>
        public MessageBus()
        {
            this.subscriptions = new Dictionary<Type, List<Delegate>>();
        }

        /// <inheritdoc/>
        public IDisposable Subscribe<TMessage>(Action<TMessage> handler)
        {
            var messageType = typeof(TMessage);

            if (!this.subscriptions.ContainsKey(messageType))
            {
                this.subscriptions[messageType] = new List<Delegate>();
            }

            this.subscriptions[messageType].Add(handler);

            return new Subscription<TMessage>(this, handler);
        }

        /// <inheritdoc/>
        public void Publish<TMessage>(TMessage message)
        {
            var messageType = typeof(TMessage);

            if (this.subscriptions.ContainsKey(messageType))
            {
                foreach (var handler in this.subscriptions[messageType])
                {
                    ((Action<TMessage>)handler)(message);
                }
            }
        }

        private void Unsubscribe<TMessage>(Action<TMessage> handler)
        {
            var messageType = typeof(TMessage);

            if (this.subscriptions.ContainsKey(messageType))
            {
                this.subscriptions[messageType].Remove(handler);
            }
        }

        private class Subscription<TMessage> : IDisposable
        {
            private readonly MessageBus messageBus;
            private readonly Action<TMessage> handler;
            private bool disposed;

            public Subscription(MessageBus messageBus, Action<TMessage> handler)
            {
                this.messageBus = messageBus;
                this.handler = handler;
            }

            public void Dispose()
            {
                if (!this.disposed)
                {
                    this.messageBus.Unsubscribe(this.handler);
                    this.disposed = true;
                }
            }
        }
    }
}
