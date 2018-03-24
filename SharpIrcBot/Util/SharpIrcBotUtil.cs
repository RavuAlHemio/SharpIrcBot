using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Chunks;
using SharpIrcBot.Config;

namespace SharpIrcBot.Util
{
    public static class SharpIrcBotUtil
    {
        [NotNull]
        public static BotConfig LoadConfig([CanBeNull] string configPath = null)
        {
            if (configPath == null)
            {
                configPath = Path.Combine(AppDirectory, "Config.json");
            }
            return new BotConfig(JObject.Parse(File.ReadAllText(configPath, Encoding.UTF8)));
        }

        [NotNull]
        public static string AppDirectory => AppContext.BaseDirectory;

        /// <summary>
        /// Collects adjacent text chunks in the given list into one text chunk.
        /// </summary>
        /// <param name="chunks">The list of chunks to simplify.</param>
        /// <returns>The simplified list of chunks.</returns>
        [NotNull, ItemNotNull]
        public static List<IMessageChunk> SimplifyAdjacentTextChunks([NotNull, ItemNotNull] IEnumerable<IMessageChunk> chunks)
        {
            var textCollector = new StringBuilder();
            var ret = new List<IMessageChunk>();

            foreach (IMessageChunk chunk in chunks)
            {
                var textChunk = chunk as TextMessageChunk;
                if (textChunk != null)
                {
                    textCollector.Append(textChunk.Text);
                }
                else
                {
                    // not a text message chunk

                    if (textCollector.Length > 0)
                    {
                        // add our collected text chunk
                        ret.Add(new TextMessageChunk(textCollector.ToString()));
                        textCollector.Clear();
                    }

                    // add this chunk
                    ret.Add(chunk);
                }
            }

            // last text chunk?
            if (textCollector.Length > 0)
            {
                ret.Add(new TextMessageChunk(textCollector.ToString()));
            }

            return ret;
        }

        public static T SyncWait<T>(this Task<T> task)
        {
            task.Wait();
            if (task.IsFaulted)
            {
                throw task.Exception;
            }
            if (task.IsCanceled)
            {
                throw new OperationCanceledException();
            }
            return task.Result;
        }
    }
}
