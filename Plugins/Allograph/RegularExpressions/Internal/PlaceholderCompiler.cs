using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpIrcBot.Plugins.Allograph.RegularExpressions.Internal
{
    public static class PlaceholderCompiler
    {
        enum State
        {
            Text = 0,
            AfterDollar = 1,
            DollarBrace = 2,
            DollarNumber = 3
        }

        static readonly Regex CasingRegex = new Regex("^\\$case\\$(?<templateGroup>[^\\$]+)\\$(?<stringToCase>.+)$", RegexOptions.Compiled);
        static readonly Regex LookupRegex = new Regex("^\\$lookup\\$(?<key>.+)$", RegexOptions.Compiled);

        public static List<IPlaceholder> Compile(Regex regex, string replacementString)
        {
            var state = State.Text;
            var sb = new StringBuilder();
            var placeholders = new List<IPlaceholder>();

            foreach (char c in replacementString)
            {
                switch (state)
                {
                    case State.Text:
                        if (c == '$')
                        {
                            if (sb.Length > 0)
                            {
                                placeholders.Add(new ConstantStringPlaceholder(sb.ToString()));
                                sb.Clear();
                            }
                            state = State.AfterDollar;
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                    case State.AfterDollar:
                        if (c == '$')
                        {
                            sb.Append(c);
                            state = State.Text;
                        }
                        else if (c == '{')
                        {
                            state = State.DollarBrace;
                        }
                        else if (c >= '0' && c <= '9')
                        {
                            sb.Append(c);
                            state = State.DollarNumber;
                        }
                        else
                        {
                            throw new ArgumentException($"unexpected character after $ character: {c}", nameof(replacementString));
                        }
                        break;
                    case State.DollarBrace:
                        if (c == '}')
                        {
                            ProcessNamedGroup(sb.ToString(), regex, placeholders);

                            sb.Clear();
                            state = State.Text;
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                    case State.DollarNumber:
                        if (c >= '0' && c <= '9')
                        {
                            sb.Append(c);
                        }
                        else
                        {
                            ProcessNumberGroup(sb.ToString(), regex, placeholders);

                            sb.Clear();
                            if (c == '$')
                            {
                                state = State.AfterDollar;
                            }
                            else
                            {
                                sb.Append(c);
                                state = State.Text;
                            }
                        }
                        break;
                }
            }

            switch (state)
            {
                case State.Text:
                    if (sb.Length > 0)
                    {
                        placeholders.Add(new ConstantStringPlaceholder(sb.ToString()));
                    }
                    break;
                case State.DollarNumber:
                    ProcessNumberGroup(sb.ToString(), regex, placeholders);
                    break;
                case State.DollarBrace:
                    throw new ArgumentException("unterminated ${...} expression", nameof(replacementString));
                case State.AfterDollar:
                    throw new ArgumentException("trailing $ character", nameof(replacementString));
            }

            return Optimize(placeholders);
        }

        static void ProcessNamedGroup(string groupName, Regex regex, IList<IPlaceholder> placeholders)
        {
            Match casingMatch = CasingRegex.Match(groupName);
            if (casingMatch.Success)
            {
                string templateGroupName = casingMatch.Groups["templateGroup"].Value;
                if (regex.GroupNumberFromName(templateGroupName) == -1)
                {
                    throw new ArgumentException($"case match references unknown capturing group named \"{templateGroupName}\"", "replacementString");
                }
                string stringToCase = casingMatch.Groups["stringToCase"].Value;

                placeholders.Add(new CaseStringPlaceholder(stringToCase, templateGroupName));
                return;
            }

            Match lookupMatch = LookupRegex.Match(groupName);
            if (lookupMatch.Success)
            {
                string key = lookupMatch.Groups["key"].Value;
                placeholders.Add(new LookupPlaceholder(key));
                return;
            }

            int groupNumber = regex.GroupNumberFromName(groupName);
            if (groupNumber != -1)
            {
                placeholders.Add(new MatchGroupPlaceholder(groupName));
                return;
            }

            // try parsing as a number
            if (int.TryParse(groupName, NumberStyles.None, CultureInfo.InvariantCulture, out groupNumber))
            {
                if (regex.GroupNameFromNumber(groupNumber).Length > 0)
                {
                    // the group exists
                    placeholders.Add(new MatchGroupPlaceholder(groupNumber));
                    return;
                }
            }

            throw new ArgumentException($"unknown capturing group named \"{groupName}\"", "replacementString");
        }

        static void ProcessNumberGroup(string groupNumberString, Regex regex, IList<IPlaceholder> placeholders)
        {
            int groupNumber = regex.GroupNumberFromName(groupNumberString);
            if (groupNumber != -1)
            {
                placeholders.Add(new MatchGroupPlaceholder(groupNumber));
                return;
            }

            // try parsing instead
            if (int.TryParse(groupNumberString, NumberStyles.None, CultureInfo.InvariantCulture, out groupNumber))
            {
                if (regex.GroupNameFromNumber(groupNumber).Length > 0)
                {
                    // the group exists
                    placeholders.Add(new MatchGroupPlaceholder(groupNumber));
                    return;
                }
            }

            throw new ArgumentException($"unknown capturing group number {groupNumberString}", "replacementString");
        }

        static List<IPlaceholder> Optimize(List<IPlaceholder> placeholders)
        {
            var sb = new StringBuilder();
            var ret = new List<IPlaceholder>();
            foreach (IPlaceholder placeholder in placeholders)
            {
                var constantPlaceholder = placeholder as ConstantStringPlaceholder;
                if (constantPlaceholder != null)
                {
                    sb.Append(constantPlaceholder.ConstantString);
                }
                else
                {
                    if (sb.Length > 0)
                    {
                        ret.Add(new ConstantStringPlaceholder(sb.ToString()));
                        sb.Clear();
                    }
                    ret.Add(placeholder);
                }
            }
            if (sb.Length > 0)
            {
                ret.Add(new ConstantStringPlaceholder(sb.ToString()));
            }
            return ret;
        }
    }
}
