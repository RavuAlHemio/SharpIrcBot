using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SharpIrcBot.Chunks;

namespace SharpIrcBot.Events
{
    public class MessageChunkingEventArgs : EventArgs
    {
        [NotNull, ItemNotNull]
        public List<IMessageChunk> Chunks { get; set; }

        public MessageChunkingEventArgs([NotNull, ItemNotNull] List<IMessageChunk> chunks)
            : base()
        {
            Chunks = chunks;
        }
    }
}
