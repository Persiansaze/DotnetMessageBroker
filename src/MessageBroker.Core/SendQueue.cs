﻿using MessageBroker.Core.Models;
using MessageBroker.Core.Serialize;
using MessageBroker.Messages;
using MessageBroker.SocketServer.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.Core
{
    /// <summary>
    /// SendQueue is in charge of sending messages to subscribers
    /// it will handle how many messages has been sent based on the concurrency requested
    /// </summary>
    public class SendQueue
    {
        private readonly IClientSession _session;
        private readonly ISerializer _serializer;
        private ConcurrentQueue<Message> _queue;
        private readonly List<Guid> _pendingMessages;
        private int _maxConcurrency;
        private int _currentConcurrency;

        public int CurrentCuncurrency => _currentConcurrency;
        public IReadOnlyList<Guid> PendingMessages => _pendingMessages;
        public IClientSession Session => _session;
        private bool IsQueueFull => _currentConcurrency >= _maxConcurrency;


        public SendQueue(IClientSession session, ISerializer serializer, int maxConcurrency, int currentConcurrency = 0)
        {
            _maxConcurrency = maxConcurrency;
            _currentConcurrency = currentConcurrency;
            _session = session;
            _serializer = serializer;
            _queue = new();
            _pendingMessages = new();
        }

        /// <summary>
        /// Enqueue will send the message to subscriber immediately if the queue isn't full
        /// otherwise the message will be saved in a queue to be sent when the queue isn't full
        /// </summary>
        /// <param name="message">The message that must be sent</param>
        public void Enqueue(Message message)
        {
            if (IsQueueFull)
            {
                _queue.Enqueue(message);
            }
            else
            {
                Send(message);
            }
        }

        /// <summary>
        /// ReleaseOne will remove the message from pending messages array, clearing room in the queue
        /// </summary>
        /// <param name="messageId">The id of message that was acked or nacked</param>
        public void ReleaseOne(Guid messageId)
        {
            if (_pendingMessages.Contains(messageId))
            {
                Interlocked.Decrement(ref _currentConcurrency);
                SendPendingMessagesIfQueueNotFull();
            }
        }

        /// <summary>
        /// This method will send another message if the queue isn't full
        /// This method will be called when ReleaseOne is called
        /// </summary>
        private void SendPendingMessagesIfQueueNotFull()
        {
            if (IsQueueFull)
                return;

            if (_queue.TryDequeue(out var msg))
                Send(msg);
        }

        /// <summary>
        /// Send will immediately send the message to ClientSession
        /// it will also increament the _currentConcurrency and add the message to _pendingMessages list
        /// </summary>
        /// <param name="msg"></param>
        public void Send(Message msg)
        {
            Interlocked.Increment(ref _currentConcurrency);
            _pendingMessages.Add(msg.Id);
            var b = _serializer.Serialize(msg);
            _session.Send(b);
        }


    }
}
