﻿using System;
using System.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

using static RabbitMQClient.AmqpConstants;

namespace RabbitMQClient
{
    public class Exchange
    {
        readonly ushort channelNumber;
        readonly Socket socket;
        readonly SemaphoreSlim semaphore;
        readonly Action<ushort> SetExpectedMethodId;

        TaskCompletionSource<bool> declareOk;
        TaskCompletionSource<bool> deleteOk;
        TaskCompletionSource<bool> bindOk;
        TaskCompletionSource<bool> unbindOk;

        internal Exchange(ushort channelNumber, Socket socket, SemaphoreSlim semaphore, Action<ushort> setExpectedMethodId)
        {
            this.channelNumber = channelNumber;
            this.socket = socket;
            this.semaphore = semaphore;
            SetExpectedMethodId = setExpectedMethodId;
        }

        internal void ParseExchangeMethod(ushort methodId, ReadableBuffer arguments)
        {
            switch (methodId)
            {
                case Command.Exchange.DeclareOk:
                    Handle_DeclareOk();
                    break;
                case Command.Exchange.DeleteOk:
                    Handle_DeleteOk();
                    break;
                case Command.Exchange.BindOk:
                    Handle_BindOk();
                    break;
                case Command.Exchange.UnbindOk:
                    Handle_UnbindOk();
                    break;
            }
        }

        void Handle_DeclareOk()
        {
            declareOk.SetResult(true);
        }

        void Handle_DeleteOk()
        {
            deleteOk.SetResult(true);
        }

        void Handle_BindOk()
        {
            bindOk.SetResult(true);
        }

        void Handle_UnbindOk()
        {
            unbindOk.SetResult(true);
        }

        public async Task Declare(string exchangeName, string type, bool passive, bool durable, bool autoDelete, bool @internal, Dictionary<string, object> arguments)
        {
            await semaphore.WaitAsync();

            declareOk = new TaskCompletionSource<bool>();
            SetExpectedMethodId(Command.Exchange.DeclareOk);

            var buffer = await socket.GetWriteBuffer();

            try
            {
                var payloadSizeHeader = buffer.WriteFrameHeader(FrameType.Method, channelNumber);

                buffer.WriteBigEndian(Command.Exchange.ClassId);
                buffer.WriteBigEndian(Command.Exchange.Declare);
                buffer.WriteBigEndian(Reserved);
                buffer.WriteBigEndian(Reserved);
                buffer.WriteShortString(exchangeName);
                buffer.WriteShortString(type);
                buffer.WriteBits(passive, durable, autoDelete, @internal);
                buffer.WriteTable(arguments);

                payloadSizeHeader.WriteBigEndian((uint)buffer.BytesWritten - FrameHeaderSize);

                buffer.WriteBigEndian(FrameEnd);

                await buffer.FlushAsync();

                await declareOk.Task;
            }
            finally
            {
                socket.ReleaseWriteBuffer();
            }
        }


        public async Task Delete(string exchange, bool onlyIfUnused)
        {
            await semaphore.WaitAsync();

            deleteOk = new TaskCompletionSource<bool>();
            SetExpectedMethodId(Command.Exchange.DeleteOk);

            var buffer = await socket.GetWriteBuffer();

            try
            {
                var payloadSizeHeader = buffer.WriteFrameHeader(FrameType.Method, channelNumber);

                buffer.WriteBigEndian(Command.Exchange.ClassId);
                buffer.WriteBigEndian(Command.Exchange.Delete);
                buffer.WriteBigEndian(Reserved);
                buffer.WriteBigEndian(Reserved);
                buffer.WriteShortString(exchange);
                buffer.WriteBits(onlyIfUnused);

                payloadSizeHeader.WriteBigEndian((uint)buffer.BytesWritten - FrameHeaderSize);

                buffer.WriteBigEndian(FrameEnd);

                await buffer.FlushAsync();

                await deleteOk.Task;
            }
            finally
            {
                socket.ReleaseWriteBuffer();
            }
        }


        public async Task Bind(string source, string destination, string routingKey, Dictionary<string, object> arguments)
        {
            await semaphore.WaitAsync();

            bindOk = new TaskCompletionSource<bool>();
            SetExpectedMethodId(Command.Exchange.BindOk);

            var buffer = await socket.GetWriteBuffer();

            try
            {
                var payloadSizeHeader = buffer.WriteFrameHeader(FrameType.Method, channelNumber);

                buffer.WriteBigEndian(Command.Exchange.ClassId);
                buffer.WriteBigEndian(Command.Exchange.Bind);
                buffer.WriteBigEndian(Reserved);
                buffer.WriteBigEndian(Reserved);
                buffer.WriteShortString(source);
                buffer.WriteShortString(destination);
                buffer.WriteShortString(routingKey);
                buffer.WriteBits();
                buffer.WriteTable(arguments);

                payloadSizeHeader.WriteBigEndian((uint)buffer.BytesWritten - FrameHeaderSize);

                buffer.WriteBigEndian(FrameEnd);

                await buffer.FlushAsync();

                await bindOk.Task;
            }
            finally
            {
                socket.ReleaseWriteBuffer();
            }
        }


        public async Task Unbind(string source, string destination, string routingKey, Dictionary<string, object> arguments)
        {
            await semaphore.WaitAsync();

            unbindOk = new TaskCompletionSource<bool>();
            SetExpectedMethodId(Command.Exchange.UnbindOk);

            var buffer = await socket.GetWriteBuffer();

            try
            {
                var payloadSizeHeader = buffer.WriteFrameHeader(FrameType.Method, channelNumber);

                buffer.WriteBigEndian(Command.Exchange.ClassId);
                buffer.WriteBigEndian(Command.Exchange.Unbind);
                buffer.WriteBigEndian(Reserved);
                buffer.WriteBigEndian(Reserved);
                buffer.WriteShortString(source);
                buffer.WriteShortString(destination);
                buffer.WriteShortString(routingKey);
                buffer.WriteBits();
                buffer.WriteTable(arguments);

                payloadSizeHeader.WriteBigEndian((uint)buffer.BytesWritten - FrameHeaderSize);

                buffer.WriteBigEndian(FrameEnd);

                await buffer.FlushAsync();

                await unbindOk.Task;
            }
            finally
            {
                socket.ReleaseWriteBuffer();
            }
        }
    }
}
