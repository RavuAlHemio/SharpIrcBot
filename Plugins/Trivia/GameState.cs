using System.Collections.Generic;
using System.Threading;

namespace SharpIrcBot.Plugins.Trivia
{
    public class GameState
    {
        public List<QuestionAnswers> Questions { get; set; }
        public int CurrentQuestionIndex { get; set; }
        public QuestionAnswers CurrentQuestion => Questions[CurrentQuestionIndex];
        public int HintsAlreadyShown { get; set; }
        public Timer Timer { get; set; }
        public object Lock { get; set; }
    }
}
