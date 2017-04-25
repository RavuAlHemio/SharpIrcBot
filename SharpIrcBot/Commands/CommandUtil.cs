using System.Collections.Generic;
using System.Collections.Immutable;

namespace SharpIrcBot.Commands
{
    public static class CommandUtil
    {
        public static ImmutableList<KeyValuePair<string, IArgumentTaker>> NoOptions
                => ImmutableList<KeyValuePair<string, IArgumentTaker>>.Empty;
        public static ImmutableList<IArgumentTaker> NoArguments => ImmutableList<IArgumentTaker>.Empty;

        public static ImmutableList<string> MakeNames(params string[] names)
        {
            return ImmutableList.Create(names);
        }

        public static KeyValuePair<string, IArgumentTaker> MakeOption(string optionName, IArgumentTaker argTaker)
        {
            return new KeyValuePair<string, IArgumentTaker>(optionName, argTaker);
        }

        public static ImmutableList<KeyValuePair<string, IArgumentTaker>> MakeOptions(
                params KeyValuePair<string, IArgumentTaker>[] options)
        {
            return ImmutableList.Create(options);
        }

        public static ImmutableList<IArgumentTaker> MakeArguments(params IArgumentTaker[] arguments)
        {
            return ImmutableList.Create(arguments);
        }

        public static WordMatchTaker ToOptionalWordTaker(this IArgumentMatcher matcher)
        {
            return new WordMatchTaker(matcher, required: false);
        }

        public static WordMatchTaker ToRequiredWordTaker(this IArgumentMatcher matcher)
        {
            return new WordMatchTaker(matcher, required: true);
        }

        public static KeyValuePair<string, IArgumentTaker> MakeFlag(string flagString)
        {
            return new KeyValuePair<string, IArgumentTaker>(flagString, NothingTaker.Instance);
        }

        private static WordMatchTaker _nonzeroStringMatcherRequiredWordTaker = null;
        public static WordMatchTaker NonzeroStringMatcherRequiredWordTaker
        {
            get
            {
                if (_nonzeroStringMatcherRequiredWordTaker == null)
                {
                    _nonzeroStringMatcherRequiredWordTaker = new WordMatchTaker(NonzeroStringMatcher.Instance, true);
                }
                return _nonzeroStringMatcherRequiredWordTaker;
            }
        }
    }
}
