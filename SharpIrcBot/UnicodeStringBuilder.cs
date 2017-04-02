using System;
using System.Collections.Generic;
using System.Text;

namespace SharpIrcBot
{
    public class UnicodeStringBuilder
    {
        protected List<int> Characters { get; set; }

        public UnicodeStringBuilder()
        {
            Characters = new List<int>();
        }

        public UnicodeStringBuilder(string baseString)
        {
            Characters = new List<int>(baseString.Length);
            Append(baseString);
        }

        public UnicodeStringBuilder(int initialCapacity)
        {
            Characters = new List<int>(initialCapacity);
        }

        public int Length => Characters.Count;

        public virtual void Append(string str)
        {
            int i = 0;
            while (i < str.Length)
            {
                if (char.IsHighSurrogate(str[i]) && i < str.Length - 1 && char.IsLowSurrogate(str[i+1]))
                {
                    Characters.Add(char.ConvertToUtf32(str, i));
                    i += 2;
                }
                else
                {
                    Characters.Add(str[i]);
                    ++i;
                }
            }
        }

        public virtual void AppendLine()
        {
            Append(Environment.NewLine);
        }

        public virtual void AppendLine(string line)
        {
            Append(line);
            AppendLine();
        }

        public virtual void AppendFormat(string format, params object[] args)
        {
            Append(string.Format(format, args));
        }

        public virtual void AppendFormat(IFormatProvider provider, string format, params object[] args)
        {
            Append(string.Format(provider, format, args));
        }

        public int this[int i]
        {
            get
            {
                return Characters[i];
            }
            set
            {
                Characters[i] = value;
            }
        }

        public string CharAtAsString(int i)
        {
            return char.ConvertFromUtf32(Characters[i]);
        }

        public void SetCharAtToString(int i, string s)
        {
            if (
                (char.IsHighSurrogate(s[0]) && s.Length != 2) ||
                (!char.IsHighSurrogate(s[0]) && s.Length != 1)
            )
            {
                throw new ArgumentException(
                    "The string must be either one character (Basic Multilingual Plane) or two characters " +
                    "(Supplemental Multilingual Planes) long",
                    nameof(s)
                );
            }

            Characters[i] = char.ConvertToUtf32(s, 0);
        }

        public override string ToString()
        {
            var builder = new StringBuilder(Characters.Count * 2);
            foreach (int c in Characters)
            {
                builder.Append(char.ConvertFromUtf32(c));
            }
            return builder.ToString();
        }

        public List<int> GetCharacterListCopy()
        {
            return new List<int>(Characters);
        }
    }
}
