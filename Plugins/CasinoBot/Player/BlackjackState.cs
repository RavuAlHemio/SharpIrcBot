using System.Collections.Generic;
using SharpIrcBot.Plugins.CasinoBot.Cards;

namespace SharpIrcBot.Plugins.CasinoBot.Player
{
    public class BlackjackState
    {
        public BlackjackStage Stage { get; set; }
        public List<Hand> MyHands { get; set; }
        public Card DealersUpcard { get; set; }
        public BasicStrategy BasicStrat { get; set; }
        public int ActionsTaken { get; set; }
        public int Bet { get; set; }
        public int Stack { get; set; }
        public bool Conservation { get; set; }
        public bool CanSurrender => (ActionsTaken == 0);

        public BlackjackState()
        {
            Stage = BlackjackStage.None;
            MyHands = new List<Hand>(2);
            BasicStrat = BasicStrategy.StandOnSoft17;
            ActionsTaken = 0;
            Bet = -1;
            Stack = -1;
            Conservation = false;
        }

        public bool CanDoubleDownOnHand(int handIndex)
        {
            // same logic as with splitting
            return CanSplitHand(handIndex);
        }

        public bool CanSplitHand(int handIndex)
        {
            if (Stack < Bet)
            {
                // money does not permit
                return false;
            }

            if (Conservation)
            {
                // wasting money
                return false;
            }

            // if splitting more than once is allowed:
            return (MyHands[handIndex].Round == 0);

            // if not:
            //return (ActionsTaken == 0);
        }

        public CourseOfAction? ApplyBasicStrategy(int handIndex)
        {
            return BasicStrat.ApplyStrategy(this, handIndex);
        }
    }
}
