using System;

namespace goh_ui
{
    /// <summary>
    /// In-game ally code.
    /// </summary>
    public struct AllyCode : IEquatable<AllyCode>, IComparable<AllyCode>
    {
        #region Constructors

        /// <summary> Build a new <see cref="AllyCode"/> from a numerical value. </summary>
        public AllyCode(uint code, bool skipValidation = false)
        {
            Value = code;
            if (!skipValidation)
                ThrowInvalid(Value);
        }

        /// <summary> Build a new <see cref="AllyCode"/> from a string representation. </summary>
        public AllyCode(string code)
        {
            // Remove dashes, if present
            if (code.Contains("-"))
                code = code.Replace("-", "");
            Value = uint.Parse(code.Trim());
            ThrowInvalid(Value);
        }

        #endregion

        public uint Value { get; private set; }

        #region Overrides of important methods

        /// <summary> Convert to a string in the format used in-game. </summary>
        public override string ToString() => Value.ToString().Insert(6, "-").Insert(3, "-");

        public bool Equals(AllyCode ac) => Value.Equals(ac.Value);
        public override bool Equals(object obj) => (obj is AllyCode ac) ? Equals(ac) : base.Equals(obj);
        public override int GetHashCode() => Value.GetHashCode();

        public int CompareTo(AllyCode ac) => Value.CompareTo(ac.Value);

        public static bool operator ==(AllyCode left, AllyCode right) => left.Value == right.Value;
        public static bool operator !=(AllyCode left, AllyCode right) => left.Value != right.Value;
        public static bool operator <(AllyCode left, AllyCode right) => left.Value < right.Value;
        public static bool operator >(AllyCode left, AllyCode right) => left.Value > right.Value;
        public static bool operator <=(AllyCode left, AllyCode right) => left.Value <= right.Value;
        public static bool operator >=(AllyCode left, AllyCode right) => left.Value >= right.Value;

        #endregion

        #region Validity checks

        /// <summary> Whether or not this ally code is valid. </summary>
        public bool IsValid() => IsValid(Value);
        /// <summary> Throw an exception if the given ally code is invalid. </summary>
        private static void ThrowInvalid(uint ac) { if (!IsValid(ac)) throw new ArgumentOutOfRangeException(); }
        private static bool IsValid(uint ac) => ((ac >= _min) && (ac <= _max));
        private static uint _max = 999_999_999U;
        private static uint _min = 100_000_000U;

        #endregion

        public static AllyCode None { get; } = new AllyCode(0, true);
    }
}
