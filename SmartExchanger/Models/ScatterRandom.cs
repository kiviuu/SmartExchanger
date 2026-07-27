namespace SmartExchanger.Models
{
    // Rnadom xorshift generator
    internal struct ScatterRandom
    {
        private uint _state;

        public ScatterRandom(int seed)
        {
            this._state = unchecked((uint)seed) + 0x9E3779B9u;
            if (this._state == 0)
            {
                _state = 0xA341316Cu;
            }
        }

        public uint NextUInt()
        {
            uint value = this._state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;

            this._state = value;
            return value;
        }

        public float NextSingle()
        {
            return (NextUInt() >> 8) * (1f / 16_777_216f);
        }

        public float Range(float min, float max)
        {
            return min + (max - min) * NextSingle();
        }
    }
}
