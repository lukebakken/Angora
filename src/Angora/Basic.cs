﻿using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading.Tasks;

using static Angora.AmqpConstants;

namespace Angora
{
    public class Basic
    {
        readonly BasicMethods methods;
        readonly Func<uint, object, Action<object, ReadableBuffer, Exception>, Task> SetExpectedReplyMethod;
        readonly Action ThrowIfClosed;

        readonly Action<object, ReadableBuffer, Exception> handle_QosOk;
        readonly Action<object, ReadableBuffer, Exception> handle_ConsumeOk;
        readonly Action<object, ReadableBuffer, Exception> handle_CancelOk;
        readonly Action<object, ReadableBuffer, Exception> handle_RecoverOk;

        internal Basic(Socket socket, ushort channelNumber, uint maxContentBodySize, Func<uint, object, Action<object, ReadableBuffer, Exception>, Task> setExpectedReplyMethod, Action throwIfClosed)
        {
            methods = new BasicMethods(socket, channelNumber, maxContentBodySize);
            SetExpectedReplyMethod = setExpectedReplyMethod;
            ThrowIfClosed = throwIfClosed;

            handle_QosOk = Handle_QosOk;
            handle_ConsumeOk = Handle_ConsumeOk;
            handle_CancelOk = Handle_CancelOk;
            handle_RecoverOk = Handle_RecoverOk;
        }

        public async Task Qos(uint prefetchSize, ushort prefetchCount, bool global)
        {
            ThrowIfClosed();

            var qosOk = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await SetExpectedReplyMethod(Method.Basic.QosOk, qosOk, handle_QosOk);

            await methods.Send_Qos(prefetchSize, prefetchCount, global);

            await qosOk.Task;
        }

        void Handle_QosOk(object tcs, ReadableBuffer arguments, Exception exception)
        {
            var qosOk = (TaskCompletionSource<bool>)tcs;

            if (exception != null)
            {
                qosOk.SetException(exception);
            }
            else
            {
                qosOk.SetResult(true);
            }
        }

        public async Task<string> Consume(string queue, string consumerTag, bool autoAck, bool exclusive, Dictionary<string, object> arguments)
        {
            ThrowIfClosed();

            var consumeOk = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            await SetExpectedReplyMethod(Method.Basic.ConsumeOk, consumeOk, handle_ConsumeOk);

            await methods.Send_Consume(queue, consumerTag, autoAck, exclusive, arguments);

            return await consumeOk.Task;
        }

        void Handle_ConsumeOk(object tcs, ReadableBuffer arguments, Exception exception)
        {
            var consumeOk = (TaskCompletionSource<string>)tcs;

            if (exception != null)
            {
                consumeOk.SetException(exception);
            }
            else
            {
                var consumerTag = arguments.ReadShortString();
                consumeOk.SetResult(consumerTag.value);
            }
        }

        public async Task<string> Cancel(string consumerTag)
        {
            ThrowIfClosed();

            var cancelOk = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            await SetExpectedReplyMethod(Method.Basic.CancelOk, cancelOk, handle_CancelOk);

            await methods.Send_Cancel(consumerTag);

            return await cancelOk.Task;
        }

        void Handle_CancelOk(object tcs, ReadableBuffer arguments, Exception exception)
        {
            var cancelOk = (TaskCompletionSource<string>)tcs;

            if (exception != null)
            {
                cancelOk.SetException(exception);
            }
            else
            {
                var consumerTag = arguments.ReadShortString();
                cancelOk.SetResult(consumerTag.value);
            }
        }

        public async Task Recover()
        {
            ThrowIfClosed();

            var recoverOk = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await SetExpectedReplyMethod(Method.Basic.RecoverOk, recoverOk, handle_RecoverOk);

            await methods.Send_Recover();

            await recoverOk.Task;
        }

        void Handle_RecoverOk(object tcs, ReadableBuffer arguments, Exception exception)
        {
            var recoverOk = (TaskCompletionSource<bool>)tcs;

            if (exception != null)
            {
                recoverOk.SetException(exception);
            }
            else
            {
                recoverOk.SetResult(true);
            }
        }

        public async Task Publish(string exchange, string routingKey, bool mandatory, MessageProperties properties, Span<byte> body)
        {
            ThrowIfClosed();

            await methods.Send_Publish(exchange, routingKey, mandatory, properties, body);
        }

        internal Task Handle_Deliver(ReadableBuffer arguments)
        {
            var consumerTag = arguments.ReadShortString();
            arguments = arguments.Slice(consumerTag.position);

            var deliveryTag = arguments.ReadBigEndian<ulong>();
            arguments = arguments.Slice(sizeof(ulong));

            var redelivered = Convert.ToBoolean(arguments.ReadBigEndian<byte>());
            arguments.Slice(sizeof(byte));

            var exchange = arguments.ReadShortString();
            arguments = arguments.Slice(exchange.position);

            var routingKey = arguments.ReadShortString();
            arguments = arguments.Slice(routingKey.position);

            return Task.CompletedTask;
        }

        internal Task Handle_ContentHeader(ReadableBuffer payload)
        {
            var classId = payload.ReadBigEndian<ushort>();
            payload = payload.Slice(sizeof(ushort));

            var weight = payload.ReadBigEndian<ushort>();
            payload = payload.Slice(sizeof(ushort));

            var bodySize = payload.ReadBigEndian<ulong>();
            payload = payload.Slice(sizeof(ulong));

            var properties = payload.ReadBasicProperties();

            return Task.CompletedTask;
        }

        internal Task Handle_ContentBody(ReadableBuffer payload)
        {
            var bytes = payload.ToArray();

            return Task.CompletedTask;
        }
    }
}
