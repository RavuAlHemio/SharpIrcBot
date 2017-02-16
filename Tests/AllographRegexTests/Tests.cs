using System;
using System.Collections.Generic;
using Xunit;
using Allograph.RegularExpressions;

namespace AllographRegexTests
{
    public class Tests
    {
        void TestReplacement(string expected, string regex, string replacement, string subject,
            IDictionary<string, string> lookups = null)
        {
            Assert.Equal(expected, new ReplacerRegex(regex, replacement).Replace(subject, lookups));
        }

        [Fact]
        public void SimpleReplacement() 
        {
            TestReplacement("aaqqccqqddqqeeqq", "b", "q", "aabbccbbddbbeebb");
        }

        [Fact]
        public void SimpleLongerReplacement() 
        {
            TestReplacement("aapqpqccpqpqddpqpqeepqpq", "b", "pq", "aabbccbbddbbeebb");
        }

        [Fact]
        public void DollarSignReplacement() 
        {
            TestReplacement("aap$qp$qccp$qp$qddp$qp$qeep$qp$q", "b", "p$$q", "aabbccbbddbbeebb");
        }

        [Fact]
        public void NumericalGroupReferenceReplacement() 
        {
            TestReplacement("pbqprqrpkqf", "a(.)c", "p$1q", "abcarcrakcf");
            TestReplacement("bprprkpf", "a(.)c", "$1p", "abcarcrakcf");
            TestReplacement("pbprrpkf", "a(.)c", "p$1", "abcarcrakcf");
            TestReplacement("pbbqprrqrpkkqf", "a(.)c", "p$1$1q", "abcarcrakcf");
        }

        [Fact]
        public void TextualGroupReferenceReplacement()
        {
            TestReplacement("pbqprqrpkqf", "a(?<abc>.)c", "p${abc}q", "abcarcrakcf");
            TestReplacement("bprprkpf", "a(?<abc>.)c", "${abc}p", "abcarcrakcf");
            TestReplacement("pbprrpkf", "a(?<abc>.)c", "p${abc}", "abcarcrakcf");
            TestReplacement("pbbqprrqrpkkqf", "a(?<abc>.)c", "p${abc}${abc}q", "abcarcrakcf");
        }

        [Fact]
        public void MixedReferenceReplacement()
        {
            TestReplacement("pbbqprrqrpkkqf", "a(?<abc>.)c", "p$1${abc}q", "abcarcrakcf");
            TestReplacement("pbbbqprrrqrpkkkqf", "a(?<abc>.)c", "p$1${abc}${1}q", "abcarcrakcf");
        }

        [Fact]
        public void CaseReplacement()
        {
            TestReplacement("prqprqrprqf", "a(.)c", "p${$case$1$r}q", "abcarcrakcf");
            TestReplacement("pMqpmqrpmqf", "(?i)a(.)c", "p${$case$1$m}q", "aBcArCrAkCf");

            TestReplacement("what is dis", "(?i)(th)(is)", "${$case$1$d}$2", "what is this");
            TestReplacement("WHAT IS DIS", "(?i)(th)(is)", "${$case$1$d}$2", "WHAT IS THIS");
            TestReplacement("What Is Dis", "(?i)(th)(is)", "${$case$1$d}$2", "What Is This");
            TestReplacement("wHAT iS dIS", "(?i)(th)(is)", "${$case$1$d}$2", "wHAT iS tHIS");

            TestReplacement("what is dis", "(?i)(?<th>th)(?<is>is)", "${$case$th$d}${is}", "what is this");
            TestReplacement("WHAT IS DIS", "(?i)(?<th>th)(?<is>is)", "${$case$th$d}${is}", "WHAT IS THIS");
            TestReplacement("What Is Dis", "(?i)(?<th>th)(?<is>is)", "${$case$th$d}${is}", "What Is This");
            TestReplacement("wHAT iS dIS", "(?i)(?<th>th)(?<is>is)", "${$case$th$d}${is}", "wHAT iS tHIS");

            TestReplacement("kiwifruit", "(?i)(?<kiw>kiw)(?<i>i)", "${kiw}${$case$i$ifruit}", "kiwi");
            TestReplacement("KIWIFRUIT", "(?i)(?<kiw>kiw)(?<i>i)", "${kiw}${$case$i$ifruit}", "KIWI");
            TestReplacement("KiWifruit", "(?i)(?<kiw>kiw)(?<i>i)", "${kiw}${$case$i$ifruit}", "KiWi");
            TestReplacement("kIwIFRUIT", "(?i)(?<kiw>kiw)(?<i>i)", "${kiw}${$case$i$ifruit}", "kIwI");
            TestReplacement("KIWifruit", "(?i)(?<kiw>kiw)(?<i>i)", "${kiw}${$case$i$ifruit}", "KIWi");
            TestReplacement("kiwIFRUIT", "(?i)(?<kiw>kiw)(?<i>i)", "${kiw}${$case$i$ifruit}", "kiwI");
        }

        [Fact]
        public void LookupReplacement()
        {
            var lookups = new Dictionary<string, string>
            {
                ["username"] = "WHAT"
            };
            TestReplacement("apWHATqpWHATqc", "b", "p${$lookup$username}q", "abbc", lookups);
        }

        [Fact]
        public void TrailingDollarSignThrows()
        {
            Assert.Throws<ArgumentException>("replacementString", () => new ReplacerRegex("abc", "pq$"));
        }

        [Fact]
        public void UnclosedDollarBraceThrows()
        {
            Assert.Throws<ArgumentException>("replacementString", () => new ReplacerRegex("abc", "p${q"));
        }

        [Fact]
        public void MatchGroupIndexOutOfRangeThrows()
        {
            Assert.Throws<ArgumentException>("replacementString", () => new ReplacerRegex("abc", "p$1q"));
            Assert.Throws<ArgumentException>("replacementString", () => new ReplacerRegex("abc", "p${abc}q"));
            Assert.Throws<ArgumentException>("replacementString", () => new ReplacerRegex("a(b)c", "p$1q$2"));
            Assert.Throws<ArgumentException>("replacementString", () => new ReplacerRegex("a(<abc>b)c", "p${abc}q${def}"));
        }
    }
}
