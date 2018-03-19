namespace ReadFlow
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    ///     Specialist date class for storing dates found in the RJIS feed. If we use a DateTime we will store eight bytes for
    ///     each date. With
    ///     this class, we will store only two. We store the number of days since 2016 Jan 01. If a date is before that, we
    ///     store zero. If a date
    ///     is after 2194-Dec-31 we store 0xFFFF (65535 decimal). 0xFFFF will be encoded as 2999-Dec-31 at 23:59:59 in Tlv -
    ///     the exporter will
    ///     contain a special processor for this type.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1407:Arithmetic expressions must declare precedence", Justification = "Textbook formula - too risky to modify parenthesis")]
    internal class RjisDate
    {
        private static readonly int[] MonthLengths = { -1, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>
        /// Initializes a new instance of the <see cref="RjisDate" /> class. Sets the date using the supplied year, month and day.
        /// </summary>
        /// <param name="y">Year</param>
        /// <param name="m">Month</param>
        /// <param name="d">Day</param>
        public RjisDate(int y, int m, int d)
        {
            var serial32 = GetSerial(y, m, d);
            if ((serial32 & 0xFFFF0000) > 0)
            {
                Serial = 0xFFFF;
            }
            else
            {
                Serial = (ushort)(serial32 & 0xFFFF);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RjisDate" /> class. Sets the date using the number of days since 1st January 2016.
        /// </summary>
        /// <param name="u">Days since 1st January 2016.</param>
        public RjisDate(ushort u)
        {
            Serial = u;
        }

        /// <summary>
        /// Gets the maximum possible value of an RjisDate.
        /// </summary>
        public static RjisDate Max => new RjisDate(0xFFFF);

        /// <summary>
        /// Gets a string representation of this RjisDate in the format ddMMyyyy. (d = day, M = month, y = year)
        /// </summary>
        public string ToDdmmyyyy
        {
            get
            {
                var (y, m, d) = GetYMD(Serial);
                return $"{d:D2}{m:D2}{y:D4}";
            }
        }

        /// <summary>
        /// Gets the value of this RjisDate
        /// </summary>
        public ushort Serial { get; }

        public static bool operator >(RjisDate lhs, RjisDate rhs)
        {
            return lhs.Serial > rhs.Serial;
        }

        public static bool operator <(RjisDate lhs, RjisDate rhs)
        {
            return lhs.Serial < rhs.Serial;
        }

        public static bool operator <=(RjisDate lhs, RjisDate rhs)
        {
            return lhs.Serial <= rhs.Serial;
        }

        public static bool operator >=(RjisDate lhs, RjisDate rhs)
        {
            return lhs.Serial >= rhs.Serial;
        }

        public static bool operator ==(RjisDate lhs, RjisDate rhs)
        {
            return lhs.Serial == rhs.Serial;
        }

        public static bool operator !=(RjisDate lhs, RjisDate rhs)
        {
            return lhs.Serial != rhs.Serial;
        }

        /// <summary>
        /// Create a RjisDate using a value read from a RJIS datafeed. Expects date to be in the format ddMMyyyy. (d = day, M = month, y = year)
        /// </summary>
        /// <param name="line">Line to parse</param>
        /// <param name="offset">First character to read</param>
        /// <returns>RjisDate value read from a RJIS datafeed</returns>
        public static RjisDate Parse(string line, int offset)
        {
            if (line.Length - offset < 8)
            {
                throw new Exception("Cannot parse date string as the string is too short.");
            }

            for (var i = 0; i < 8; i++)
            {
                var c = line[i + offset];
                if (!char.IsDigit(c))
                {
                    throw new Exception($"Invalid character in date: '{c}'");
                }
            }

            var d = (10 * (line[offset] - '0')) + (line[offset + 1] - '0');
            var m = (10 * (line[offset + 2] - '0')) + (line[offset + 3] - '0');
            var y = (1000 * (line[offset + 4] - '0')) + (100 * (line[offset + 5] - '0')) + (10 * (line[offset + 6] - '0')) + (line[offset + 7] - '0');

            if (m == 0 || m > 12)
            {
                throw new ArgumentOutOfRangeException(message: $"Month {m} is out of range - must be 1-12.", paramName: nameof(m));
            }

            if (y < 1970 || y > 2999)
            {
                throw new ArgumentOutOfRangeException($"Year {y} is out of range - must be 1970-2999.");
            }

            var maxDays = MonthLengths[m];
            if (m == 2)
            {
                var isLeapYear = y % 4 == 0 && (y % 100 != 0 || y % 400 == 0);
                if (isLeapYear)
                {
                    maxDays = 29;
                }
            }

            if (d == 0 || d > maxDays)
            {
                throw new ArgumentOutOfRangeException($"Day {d} is out of range - must be in the range 1-{maxDays}.");
            }

            // maximum date is 2016-Jan-1 + 0xFFFF days - this is some date in 2195, so we will say that all dates past this are set to
            // 0xFFFF. Our Tlv encoder will encode 0xFFFF as 2999-Dec-31 at 23:59:59
            return y > 2194 ? Max : new RjisDate(y, m, d);
        }

        /// <summary>
        /// Returns a ValueTuple of the int values of the year, month and day if this RjisDate
        /// </summary>
        /// <returns>ValueTuple of int values of year, month and day</returns>
        public (int y, int m, int d) GetYmd()
        {
            var (y, m, d) = GetYMD(Serial);
            return (y, m, d);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Serial.GetHashCode();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        private static uint GetSerial(int y, int m, int d)
        {
            var result = 367 * y - 7 * (y + (m + 9) / 12) / 4 - 3 * ((y + (m - 9) / 7) / 100 + 1) / 4 + 275 * m / 9 +
                         d - 736360;
            return (uint)result;
        }

        [SuppressMessage(
            "StyleCop.CSharp.DocumentationRules",
            "SA1008:Not coping with value tuples",
            Justification = "AMS need a newer version of stylecop")]
        private static (int y, int m, int d) GetYMD(int serial)
        {
            int y, m, d;
            if (serial == 0xFFFF)
            {
                y = 2999;
                m = 12;
                d = 31;
            }
            else
            {
                var j = serial + 719469 + 16801;
                y = (4 * j - 1) / 146097;
                j = (4 * j - 1) % 146097;
                d = j / 4;
                j = (4 * d + 3) / 1461;
                d = (4 * d + 3) % 1461 / 4 + 1;
                m = (5 * d - 3) / 153;
                d = (5 * d - 3) % 153 / 5 + 1;
                y = 100 * y + j;
                if (m < 10)
                {
                    m = m + 3;
                }
                else
                {
                    m = m - 9;
                    y = y + 1;
                }
            }

            return (y, m, d);
        }
    }
}