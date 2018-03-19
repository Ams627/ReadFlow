namespace ReadFlow
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;

    internal static class RjisParser
    {
        public static uint GetNlc(string s, int offset)
        {
            uint result = 0;
            for (int i = 0; i < 4; i++)
            {
                result = (result << 8) | s[i + offset];
            }
            return result;
        }


        /// <summary>
        ///     return a 16 bit integer from a string - the integer is the base 36 encoding of the string specified.
        /// </summary>
        /// <param name="s">string from which to extract integer</param>
        /// <param name="offset">offset with in the string to start extracting</param>
        /// <param name="length">number of characters to extract</param>
        /// <returns>ushort value read of the specified characters</returns>
        public static ushort GetBase36(string s, int offset, int length)
        {
            if (s.Length - offset < length)
            {
                throw new Exception(
                    $"Base 36 encoding requested at position {offset} of string {s}: string too short");
            }

            ushort result = 0;
            for (var i = 0; i < length; ++i)
            {
                var c = s[offset + i];
                if (!char.IsLetterOrDigit(c))
                {
                    throw new Exception($"Invalid characer '{c}' in NLC");
                }

                // convert the character to encode into the range zero to 35:
                var encDigit = c < 'A' ? c - '0' : c - 'A' + 10;
                result = (ushort)((result * 36) + encDigit);
            }

            return result;
        }

        public static (UInt64 rjisKey, UInt32 flowid, UInt64 value) GetKeysValue(string line)
        {
            var flowid = (UInt32)RjisParser.GetInt(line, 2, 7);
            var ticketCode = (UInt32)RjisParser.GetBase36(line, 9, 3);
            var price = (UInt32)RjisParser.GetInt(line, 12, 8);
            UInt32 resCode;
            // zero means no rescode:
            if (line[20] == ' ' && line[21] == ' ')
            {
                resCode = 0;
            }
            else
            {
                resCode = 1 + (UInt32)RjisParser.GetBase36(line, 20, 2);
            }

            Debug.Assert(price < 100_000_000);
            Debug.Assert(ticketCode < 36 * 36 * 36);
            Debug.Assert(resCode < 36 * 36);
            Debug.Assert(flowid < 10_000_000);

            UInt64 value = (((UInt64)ticketCode) << 32);
            value |= ((UInt64)resCode << 48);
            value |= price;
            var rjisKey = flowid + (UInt64)ticketCode * 10_000_000;
            return (rjisKey, flowid, value);
        }


        /// <summary>
        /// Reads a specified number of characters from a string and converts them to an integer
        /// </summary>
        /// <param name="s">string to read from</param>
        /// <param name="offset">position in the string to start reading</param>
        /// <param name="length">number of characters to read</param>
        /// <returns>int value of the specified characters</returns>
        public static int GetInt(string s, int offset, int length)
        {
            var result = 0;
            for (var i = 0; i < length; ++i)
            {
                var c = s[offset + i];
                if (!char.IsDigit(c))
                {
                    throw new Exception("Invalid number: " + s.Substring(offset, length) + ". '" + c + "' is not allowed");
                }

                result = (result * 10) + (c - '0');
            }

            return result;
        }

    }
}