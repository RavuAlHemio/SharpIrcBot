namespace SharpIrcBot.Commands
{
    public class NothingTaker : IArgumentTaker
    {
        private static NothingTaker _instance = null;
        public static NothingTaker Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new NothingTaker();
                }
                return _instance;
            }
        }

        protected NothingTaker()
        {
        }

        public string Take(string input, out object value)
        {
            value = null;
            return input;
        }

        public override bool Equals(object other)
        {
            if (other == null)
            {
                return false;
            }

            if (this.GetType() != other.GetType())
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return 1991039992;
        }
    }
}
