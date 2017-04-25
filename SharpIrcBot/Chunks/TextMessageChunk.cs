using JetBrains.Annotations;

namespace SharpIrcBot.Chunks
{
    /// <summary>
    /// A text-only message chunk.
    /// </summary>
    public class TextMessageChunk : IMessageChunk
    {
        [NotNull]
        public string Text { get; }

        public TextMessageChunk([NotNull] string text)
        {
            Text = text;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
