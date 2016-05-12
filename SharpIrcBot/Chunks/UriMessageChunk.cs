using System;
using JetBrains.Annotations;

namespace SharpIrcBot.Chunks
{
    /// <summary>
    /// A message chunk representing a URI.
    /// </summary>
    public class UriMessageChunk : IMessageChunk
    {
        [NotNull]
        public string OriginalText { get; }
        [NotNull]
        public Uri Uri { get; }

        public UriMessageChunk([NotNull] string originalText, [NotNull] Uri uri)
        {
            OriginalText = originalText;
            Uri = uri;
        }

        public override string ToString()
        {
            return OriginalText;
        }
    }
}
