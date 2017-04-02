using System.Collections.Generic;

namespace SharpIrcBot.Plugins.Sed.Parsing
{
    public class TransposeCommand : ITransformCommand
    {
        public Dictionary<int, int> TranspositionDictionary { get; set; }

        public TransposeCommand(Dictionary<int, int> transpositionDictionary)
        {
            TranspositionDictionary = transpositionDictionary;
        }

        public string Transform(string text)
        {
            var usb = new UnicodeStringBuilder(text)
            {
                AllowSkipCharacters = true
            };
            for (int i = 0; i < usb.Length; ++i)
            {
                int newValue;
                if (TranspositionDictionary.TryGetValue(usb[i], out newValue))
                {
                    usb[i] = newValue;
                }
            }
            return usb.ToString();
        }
    }
}
