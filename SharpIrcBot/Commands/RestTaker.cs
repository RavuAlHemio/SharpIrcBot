namespace SharpIrcBot.Commands
{
    public class RestTaker : IArgumentTaker
    {
        private static RestTaker _instance = null;

        public static RestTaker Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RestTaker();
                }
                return _instance;
            }
        }

        protected RestTaker()
        {
        }

        public string Take(string input, out object value)
        {
            value = input;
            return "";
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
            return 109429641;
        }
    }
}
