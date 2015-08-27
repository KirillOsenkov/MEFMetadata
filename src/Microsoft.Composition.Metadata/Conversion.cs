namespace Microsoft.Composition.Metadata
{
    public class Conversion
    {
        public static string ByteArrayToHexString(byte[] bytes, int digits = 0)
        {
            if (digits == 0)
            {
                digits = bytes.Length * 2;
            }

            char[] c = new char[digits];
            byte b;
            for (int i = 0; i < digits / 2; i++)
            {
                b = ((byte)(bytes[i] >> 4));
                c[i * 2] = (char)(b > 9 ? b + 87 : b + 0x30);
                b = ((byte)(bytes[i] & 0xF));
                c[i * 2 + 1] = (char)(b > 9 ? b + 87 : b + 0x30);
            }

            return new string(c);
        }

        public static byte[] HexStringToByteArray(string hex)
        {
            byte[] result = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; i++)
            {
                result[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return result;
        }

        public static byte GetHexVal(char hex)
        {
            int val = (int)hex;
            //For uppercase A-F letters:
            //return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            return (byte)(val - (val < 58 ? 48 : 87));
            //Or the two combined, but a bit slower:
            //return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }
    }
}
