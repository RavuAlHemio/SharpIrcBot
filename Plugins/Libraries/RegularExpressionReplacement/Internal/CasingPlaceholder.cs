using System.Diagnostics;
using System.Text;

namespace SharpIrcBot.Plugins.Libraries.RegularExpressionReplacement.Internal
{
    public class CaseStringPlaceholder : IPlaceholder
    {
        public string StringToCase { get; }
        public string CaseTemplateGroupName { get; }

        public CaseStringPlaceholder(string stringToCase, string caseTemplateGroupName)
        {
            StringToCase = stringToCase;
            CaseTemplateGroupName = caseTemplateGroupName;
        }

        public string Replace(ReplacementState state)
        {
            if (StringToCase.Length == 0)
            {
                return StringToCase;
            }

            string caseTemplate = state.Match.Groups[CaseTemplateGroupName].Value;
            if (caseTemplate.Length == StringToCase.Length)
            {
                return OneToOneCase(caseTemplate);
            }
            else if (caseTemplate.Length == 0)
            {
                return StringToCase;
            }
            else if (caseTemplate.Length == 1)
            {
                if (char.IsUpper(caseTemplate[0]))
                {
                    return StringToCase.ToUpper();
                }
                else
                {
                    return StringToCase.ToLower();
                }
            }
            else
            {
                return BestGuessCase(caseTemplate);
            }
        }

        protected string OneToOneCase(string caseTemplate)
        {
            Debug.Assert(StringToCase.Length == caseTemplate.Length);

            var ret = new StringBuilder(StringToCase.Length);
            for (int i = 0; i < StringToCase.Length; ++i)
            {
                if (char.IsUpper(caseTemplate[i]))
                {
                    ret.Append(char.ToUpper(StringToCase[i]));
                }
                else if (char.IsLower(caseTemplate[i]))
                {
                    ret.Append(char.ToLower(StringToCase[i]));
                }
                else
                {
                    ret.Append(StringToCase[i]);
                }
            }

            return ret.ToString();
        }

        protected string BestGuessCase(string caseTemplate)
        {
            Debug.Assert(caseTemplate.Length > 1);
            Debug.Assert(StringToCase.Length > 0);

            bool firstUpper = char.IsUpper(caseTemplate[0]);
            bool firstLower = char.IsLower(caseTemplate[0]);
            bool secondUpper = char.IsUpper(caseTemplate[1]);
            bool secondLower = char.IsLower(caseTemplate[1]);

            if (firstUpper && secondUpper)
            {
                // AA
                return StringToCase.ToUpper();
            }
            else if (firstUpper && secondLower)
            {
                // Aa
                return char.ToUpper(StringToCase[0]) + StringToCase.Substring(1).ToLower();
            }
            else if (firstLower && secondUpper)
            {
                // aA
                return char.ToLower(StringToCase[0]) + StringToCase.Substring(1).ToUpper();
            }
            else if (firstLower && secondLower)
            {
                // aa
                return StringToCase.ToLower();
            }
            else
            {
                // 0a, 0A, a0, A0
                return StringToCase;
            }
        }
    }
}
