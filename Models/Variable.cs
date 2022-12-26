using ZENBitPackToolbox.Managers;

namespace ZENBitPackToolbox.Models
{
    public class Variable
    {
        public Variable(string? name, int index, int min, int max, int currentValue)
        {
            Index = index;
            Update(name, index, min, max, currentValue);
        }

        public string? Name { get; private set; }

        public int Index { get; private set; }

        public int TotalBits { get; private set; }

        public int Min { get; private set; }
        public int Max { get; private set; }

        public int CurrentValue { get; private set; }

        public int StartBit { get; private set; }
        public int EndBit
        {
            get
            {
                if (IsSplit)
                {
                    return 32 - StartBit + TotalBits;
                }
                return StartBit + TotalBits;
            }
        }

        public int Spvar { get; private set; }

        public bool IsSplit => TotalBits >= 32 - StartBit;
        public bool IsSigned => Min < 0 || Max < 0;

        public bool Update(string? name, int index, int min, int max, int currentValue)
        {
            bool recalculate = false;
            Name = name;
            Index = index;
            if (min != Min || max != Max)
            {
                recalculate = true;
            }

            Min = min;
            Max = max;

            if (recalculate)
            {
                CalculateUsedBits();
            }

            recalculate = recalculate || CurrentValue != currentValue;
            CurrentValue = Math.Clamp(currentValue, min, max);

            return recalculate;
        }

        public void SetSpvarInfo(int index, int startBit)
        {
            StartBit = startBit;
            Spvar = index;
        }

        public void Load(StateManager manager, bool showRealValue)
        {
            var spvar_current_bit = StartBit;
            var spvar_bits = TotalBits;
            var spvar_current_slot = Spvar;
            var spvar_current_value = manager.GetExpectedSpvarValue(Spvar);

            if (spvar_bits >= 32 - spvar_current_bit) // Check if we are dealing with a split SPVAR value, essentially if the current position means we're using more than 32 bits in the SPVAR, we need to retrieve the missing bits from the next SPVAR and put them back to our current value, we use the same space saving trick here as in the save function
            {
                spvar_current_value = (spvar_current_value & MakeFullMask(32 - spvar_current_bit)) | ((manager.GetExpectedSpvarValue(spvar_current_slot + 1) & MakeFullMask(spvar_bits - (32 - spvar_current_bit))) << (32 - spvar_current_bit));
            }
            spvar_current_value &= MakeFullMask(spvar_bits); // Extract all bits included for this value and discard any other bits

            if (IsSigned) // Check if the value can be negative and handle it accordingly
            {
                spvar_current_value = Unpack(spvar_current_value, spvar_bits); // Restore the signed, possibly negative value
            }

            if (showRealValue == false && (spvar_current_value < Min || spvar_current_value > Max))
            {
                CurrentValue = Min;
                return;
            }
            CurrentValue = spvar_current_value;
        }

        public async Task SaveAsync(StateManager manager)
        {
            var spvar_current_bit = StartBit;
            var spvar_bits = TotalBits;
            var val = CurrentValue;
            if (IsSigned)
            {
                val = Pack(val, spvar_bits);
            }

            val &= MakeFullMask(spvar_bits);
            var spvar_current_slot = Spvar;
            var spvar_current_value = manager.GetExpectedSpvarValue(Spvar);

            if (spvar_bits >= 32 - spvar_current_bit)
            {
                spvar_current_value |= (val << spvar_current_bit); // Add what we can to the current value where there is bits available to use
                await manager.SetSpvarExpectedValueAsync(spvar_current_slot, spvar_current_value); // Save the current SPVAR before advancing to the next one
                spvar_current_slot++; // Move to the next slot
                spvar_bits -= (32 - spvar_current_bit); // Update the required bits according to our needs for the next slot, if we don't do this here, we'll screw up the saved value by moving it too far out of range
                val >>= (32 - spvar_current_bit); // Move the remaining bits down, discarding the bits we've already saved
                spvar_current_bit = 0; // Reset the current bit counter since we're starting with a new SPVAR
                spvar_current_value = 0; // Reset our value so we start clean, we aren't currently using any bits anyways
            }

            spvar_current_value |= val << spvar_current_bit; // Merge the current SPVAR value with our currently value where there is space to keep our value
            spvar_current_bit += spvar_bits; // Move up the counter of next available bit to where we are currently saving data at
            if (spvar_current_bit != 0)
            {
                spvar_current_value = 0;
            }
            await manager.SetSpvarExpectedValueAsync(spvar_current_slot, spvar_current_value);
        }

        public void UpdateIndex(int index) => Index = index;

        private void CalculateUsedBits() => TotalBits = Math.Max(CalculateBitsRequired(Min), CalculateBitsRequired(Max)) + (IsSigned ? 1 : 0);

        private int CalculateBitsRequired(int value)
        {
            value = Math.Abs(value); // We need it to be positive for this to work
            int i = 0;
            while (value > 0)
            {
                i++;
                value >>= 1; // Move the bits down 1 position
            }
            return i;
        }

        private int MakeFullMask(int bits = 32)
        {
            if (bits == 32) return -1;
            return 0x7FFFFFFF >> (31 - bits);
        }
        private int MakeSign(int bits) => 1 << Math.Clamp(bits - 1, 0, 31);
        private int MakeSignMask(int bits) => MakeFullMask(bits);

        private int Pack(int value, int bits)
        {
            if (value < 0)
            {
                return (Math.Abs(value) & MakeSignMask(bits)) | MakeSign(bits);
            }

            return value & MakeSignMask(bits);
        }

        private int Unpack(int val, int bits)
        {
            if ((val & MakeSign(bits)) != 0)
            {
                return 0 - (val & MakeSignMask(bits));
            }
            return val & MakeSignMask(bits);
        }
    }
}
