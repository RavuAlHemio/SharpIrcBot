using System.Collections.Generic;

namespace SharpIrcBot.Plugins.Trivia
{
    public class QuestionAnswers
    {
        public string Question { get; set; }
        public List<string> Answers { get; set; }
        public string MainAnswer => Answers[0];
    }
}
