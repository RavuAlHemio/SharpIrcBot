using System.Diagnostics;

namespace SharpIrcBot.Plugins.Calc.AST
{
    public static class CalcASTUtils
    {
        public static string ToOperatorString(this Operation op)
        {
            switch (op)
            {
                case Operation.Add:
                    return "+";
                case Operation.Divide:
                    return "/";
                case Operation.Multiply:
                    return "*";
                case Operation.Power:
                    return "**";
                case Operation.Remainder:
                    return "%";
                case Operation.Negate:
                case Operation.Subtract:
                    return "-";
                default:
                    Debug.Fail($"unhandled operation {op}");
                    return "???";
            }
        }
    }
}
