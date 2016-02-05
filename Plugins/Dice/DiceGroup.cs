namespace Dice
{
    public class DiceGroup
    {
        public int DieCount { get; set; }
        public int SideCount { get; set; }

        public DiceGroup(int dieCount, int sideCount)
        {
            DieCount = dieCount;
            SideCount = sideCount;
        }
    }
}
