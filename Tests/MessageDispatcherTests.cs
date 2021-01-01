﻿using MessageBroker.Core;
using MessageBroker.Core.BufferPool;
using MessageBroker.Core.MessageRefStore;
using MessageBroker.Core.Models;
using MessageBroker.Core.Serialize;
using MessageBroker.Messages;
using MessageBroker.SocketServer.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class MessageDispatcherTests
    {
        [Fact]
        public void TestDispatchMessage()
        {
            var bufferPool = new DefaultBufferPool();
            var messageRefStore = new DefaultMessageRefStore();
            var sessionId = Guid.NewGuid();
            var session = new Mock<IClientSession>(); 
            var sessionResolver = new Mock<ISessionResolver>();
            var serializer = new DefaultSerializer(bufferPool);

            session.Setup(s => s.SessionId).Returns(sessionId);
            sessionResolver.Setup(sr => sr.Resolve(It.IsAny<Guid>())).Returns(session.Object);

            var dispatcher = new MessageDispatcher(sessionResolver.Object, serializer, messageRefStore);

            dispatcher.AddSendQueue(sessionId, 1);

            var originalMessage = new Message { Id = Guid.NewGuid(), Route = "TEST", Data = Encoding.UTF8.GetBytes("TEST") };

            var originalMessageSendData = serializer.ToSendPayload(originalMessage);

            var message = serializer.ToMessage(originalMessageSendData.Data.Span);

            dispatcher.Dispatch(message, new Guid[] { session.Object.SessionId });

            // make sure the send queue is created and it's the same as session
            var sendQueue = dispatcher.GetSendQueue(sessionId);
            Assert.Equal(session.Object, sendQueue.Session);

            // make sure the resolve method of session resolver was called
            sessionResolver.Verify(sr => sr.Resolve(It.IsAny<Guid>()));

            // make sure the send method of session was called
            session.Verify(s => s.SendAsync(It.IsAny<Memory<byte>>()));
        }

    }
}
