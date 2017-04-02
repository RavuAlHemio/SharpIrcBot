using System;
using System.Collections.Generic;
using System.Text;

namespace SharpIrcBot
{
    public class UnicodeStringBuilder
    {
        protected List<int> Characters { get; set; }

        protected bool ActualAllowSkipCharacters { get; set; }

        public bool AllowSkipCharacters
        {
            get
            {
                return ActualAllowSkipCharacters;
            }
            set
            {
                if (!value)
                {
                    // verify
                    int indexOfSkip = Characters.IndexOf(-1);
                    if (indexOfSkip != -1)
                    {
                        throw new ArgumentException(
                            $"character list already contains skip at position {indexOfSkip}",
                            nameof(value)
                        );
                    }
                }

                ActualAllowSkipCharacters = value;
            }
        }

        public UnicodeStringBuilder()
        {
            Characters = new List<int>();
        }

        public UnicodeStringBuilder(string baseString)
        {
            if (baseString == null)
            {
                throw new ArgumentNullException(nameof(baseString));
            }

            Characters = new List<int>(baseString.Length);
            Append(baseString);
        }

        public UnicodeStringBuilder(int initialCapacity)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }

            Characters = new List<int>(initialCapacity);
        }

        public int Length => Characters.Count;

        public virtual void Append(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

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
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

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

        public virtual void RemoveAt(int index)
        {
            Characters.RemoveAt(index);
        }

        public virtual void RemoveRange(int index, int count)
        {
            Characters.RemoveRange(index, count);
        }

        public int this[int i]
        {
            get
            {
                if (i < 0 || i >= Characters.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(i));
                }

                return Characters[i];
            }
            set
            {
                if (i < 0 || i >= Characters.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(i));
                }
                if (value < -1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                if (!ActualAllowSkipCharacters && value == -1)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        $"the value -1 is only allowed if {nameof(AllowSkipCharacters)} is true"
                    );
                }

                Characters[i] = value;
            }
        }

        public string CharAtAsString(int i)
        {
            if (Characters[i] == -1)
            {
                return "";
            }
            return char.ConvertFromUtf32(Characters[i]);
        }

        public void SetCharAtToString(int i, string s)
        {
            if (s.Length == 0)
            {
                if (!ActualAllowSkipCharacters)
                {
                    throw new ArgumentException(
                        $"An empty string is only allowed if {nameof(AllowSkipCharacters)} is true",
                        nameof(s)
                    );
                }
                Characters[i] = -1;
                return;
            }

            if (
                (char.IsHighSurrogate(s[0]) && s.Length != 2) ||
                (!char.IsHighSurrogate(s[0]) && s.Length != 1)
            )
            {
                throw new ArgumentException(
                    $"The string must be either zero characters (skip character; iff {nameof(AllowSkipCharacters)} is " +
                    "true), one character (Basic Multilingual Plane) or two characters (Supplemental Multilingual " +
                    "Planes) long",
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
                if (c == -1)
                {
                    // skip
                    continue;
                }

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
