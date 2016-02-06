namespace Dice
{
    public class DiceGroup
    {
        public int DieCount { get; set; }
        public int SideCount { get; set; }
        public long AddValue { get; set; }

        public DiceGroup(int dieCount, int sideCount, long addValue = 0)
        {
            DieCount = dieCount;
            SideCount = sideCount;
            AddValue = addValue;
        }
    }
}
