using System;
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
            => ImmutableList.Create(names);

        public static KeyValuePair<string, IArgumentTaker> MakeOption(string optionName, IArgumentTaker argTaker)
            => new KeyValuePair<string, IArgumentTaker>(optionName, argTaker);

        public static ImmutableList<KeyValuePair<string, IArgumentTaker>> MakeOptions(
                params KeyValuePair<string, IArgumentTaker>[] options)
            => ImmutableList.Create(options);

        public static ImmutableList<IArgumentTaker> MakeArguments(params IArgumentTaker[] arguments)
            => ImmutableList.Create(arguments);

        public static ImmutableHashSet<string> MakeTags(params string[] tags)
            => ImmutableHashSet.Create(tags);

        public static WordMatchTaker ToOptionalWordTaker(this IArgumentMatcher matcher)
            => new WordMatchTaker(matcher, required: false);

        public static WordMatchTaker ToRequiredWordTaker(this IArgumentMatcher matcher)
            => new WordMatchTaker(matcher, required: true);

        public static KeyValuePair<string, IArgumentTaker> MakeFlag(string flagString)
            => new KeyValuePair<string, IArgumentTaker>(flagString, NothingTaker.Instance);

        private static Lazy<WordMatchTaker> _nonzeroStringMatcherRequiredWordTaker
            = new Lazy<WordMatchTaker>(() => new WordMatchTaker(NonzeroStringMatcher.Instance, required: true));
        public static WordMatchTaker NonzeroStringMatcherRequiredWordTaker
            => _nonzeroStringMatcherRequiredWordTaker.Value;

        private static Lazy<WordMatchTaker> _nonzeroStringMatcherOptionalWordTaker
            = new Lazy<WordMatchTaker>(() => new WordMatchTaker(NonzeroStringMatcher.Instance, required: false));
        public static WordMatchTaker NonzeroStringMatcherOptionalWordTaker
            => _nonzeroStringMatcherOptionalWordTaker.Value;
    }
}
