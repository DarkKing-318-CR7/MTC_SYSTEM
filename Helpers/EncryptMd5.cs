using System.Security.Cryptography;
using System.Text;

namespace Helpers
{
    public class EncryptMd5
    {
        public static string MD5Function(string inputStr)
        {
            MD5 md5 = MD5.Create();
            byte[] myInput = Encoding.UTF8.GetBytes(inputStr);
            byte[] myOutput = md5.ComputeHash(myInput);
            string outStr = BitConverter.ToString(myOutput).Replace("-", string.Empty);
            return outStr;
        }
    }
}
