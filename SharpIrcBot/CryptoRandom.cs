using System;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;

namespace SharpIrcBot
{
    public class CryptoRandom : Random, IDisposable
    {
        private bool _disposed = false;
        protected RandomNumberGenerator RNG { get; set; }

        public CryptoRandom()
            : base()
        {
            RNG = RandomNumberGenerator.Create();
        }

        public override int Next()
        {
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() < int.MaxValue);

            return (int)Next(0, int.MaxValue);
        }

        public override int Next(int maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue), maxValue, $"{nameof(maxValue)} must be 0 or greater");
            }
            Contract.EndContractBlock();
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures((maxValue == 0) || (Contract.Result<int>() < maxValue));

            return (int)Next(0, maxValue);
        }

        public override int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentException($"{nameof(minValue)} must be less than or equal to {nameof(maxValue)}");
            }
            Contract.EndContractBlock();
            Contract.Ensures(Contract.Result<int>() >= minValue);
            Contract.Ensures((minValue == maxValue) || (Contract.Result<int>() < maxValue));

            return (int)Next((long)minValue, (long)maxValue);
        }

        public long Next(long minValue, long maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentException($"{nameof(minValue)} must be less than or equal to {nameof(maxValue)}");
            }
            Contract.EndContractBlock();
            Contract.Ensures(Contract.Result<int>() >= minValue);
            Contract.Ensures((minValue == maxValue) || (Contract.Result<int>() < maxValue));

            long range = maxValue - minValue;
            if (range == 0) { return minValue; }

            double sample = NextDouble() * range;
            return minValue + (long)sample;
        }

        public override double NextDouble()
        {
            Contract.Ensures(Contract.Result<double>() >= 0.0);
            Contract.Ensures(Contract.Result<double>() < 1.0);

            ulong dividend = NextUInt64();
            double divisor = ulong.MaxValue + 1.0;

            return (dividend / divisor);
        }

        public override void NextBytes(byte[] buffer)
        {
            if (buffer == null) { throw new ArgumentNullException(nameof(buffer)); }
            Contract.EndContractBlock();

            ulong currentRandom = 0;
            for (int i = 0; i < buffer.Length; ++i)
            {
                if (i % 8 == 0)
                {
                    currentRandom = NextUInt64();
                }

                buffer[i] = (byte)((currentRandom >> ((i % 8) * 8)) & 0xFF);
            }
        }

        public virtual ulong NextUInt64()
        {
            var buf = new byte[8];
            RNG.GetBytes(buf);

            return (
                ((ulong)buf[0] << 56) |
                ((ulong)buf[1] << 48) |
                ((ulong)buf[2] << 40) |
                ((ulong)buf[3] << 32) |
                ((ulong)buf[4] << 24) |
                ((ulong)buf[5] << 16) |
                ((ulong)buf[6] <<  8) |
                ((ulong)buf[7] <<  0)
            );
        }

        public void Dispose()
        {
            Dispose(true);
            //GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // release managed resources
                RNG.Dispose();
            }

            // release unmanaged resources

            _disposed = true;
        }
    }
}
