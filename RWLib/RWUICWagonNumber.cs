using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RWLib
{
    // https://en.wikipedia.org/wiki/UIC_wagon_numbers

    public class RWUICWagonNumber
    {
        public enum Format { Plain, Official }

        public char[] Chars { get; set; } = { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '1', '2' };

        public static RWUICWagonNumber FromDigits(string twelveDigits)
        {
            return new RWUICWagonNumber
            {
                Chars = twelveDigits.ToCharArray()
            };
        }

        public override string ToString()
        {
            return this.ToString(Format.Official);
        }

        public string ToString(Format format = Format.Official)
        {
            if (Chars.Length != 12)
                throw new ArgumentException("The input char array must contain exactly 12 characters.", nameof(Chars));

            switch (format)
            {
                case Format.Official:
                    return String.Format("{0}{1} {2}{3} {4}{5}{6}{7} {8}{9}{10}-{11}", Chars[0], Chars[1], Chars[2], Chars[3], Chars[4], Chars[5], Chars[6], Chars[7], Chars[8], Chars[9], Chars[10], Chars[11]);
                case Format.Plain:
                default:
                    return new string(Chars);
            }
        }

        private static string RemoveWhiteSpace(string input)
        {
            string result = input.Replace(" ", string.Empty);
            // Remove tabs
            result = result.Replace("\t", string.Empty);
            // Remove new line characters
            result = result.Replace("\n", string.Empty).Replace("\r", string.Empty);

            return result;
        }

        private static int DigitSum(int number)
        {
            int sum = 0;

            foreach (char c in number.ToString())
            {
                if (!char.IsDigit(c))
                    throw new ArgumentException("The input number must contain only digits.", nameof(number));

                sum += c - '0'; // Convert char to its corresponding integer value
            }

            return sum;
        }

        public static int DetermineCheckDigit(string uicWagonCodeWithoutDigit)
        {
            if (uicWagonCodeWithoutDigit == null)
                throw new ArgumentNullException(nameof(uicWagonCodeWithoutDigit));

            if (uicWagonCodeWithoutDigit.Length != 11)
                throw new ArgumentException("The wagon number must be exactly 11 digits long.");

            int sum = 0;
            for (int i = 0; i < 11; i++)
            {
                int j = 10 - i;
                int digit = uicWagonCodeWithoutDigit[j] - '0';
                if (i % 2 == 0) digit = digit * 2;
                sum += DigitSum(digit);
            }

            int checkDigit = (10 - (sum % 10)) % 10;

            return checkDigit;
        }

        public static RWUICWagonNumber FromDigits(string typeOfVehicle, string countryCode, string vehicleType, string serialNumber)
        {
            typeOfVehicle = RemoveWhiteSpace(typeOfVehicle);
            countryCode = RemoveWhiteSpace(countryCode);
            vehicleType = RemoveWhiteSpace(vehicleType);
            serialNumber = RemoveWhiteSpace(serialNumber);

            if (typeOfVehicle.Length != 2) throw new FormatException("Vehicle type should be 2 digits");
            if (countryCode.Length != 2) throw new FormatException("Vehicle type should be 2 digits");
            if (vehicleType.Length != 4) throw new FormatException("Vehicle type should be 3 digits");
            if (serialNumber.Length != 3) throw new FormatException("Vehicle type should be 3 digits");

            string wagonNumber = typeOfVehicle + countryCode + vehicleType + serialNumber;

            int checkDigit = DetermineCheckDigit(wagonNumber);

            return new RWUICWagonNumber
            {
                Chars = (wagonNumber + checkDigit).ToCharArray()
            };
        }
    }
}
