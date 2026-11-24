using System.Text.RegularExpressions;

namespace MTC_System.Common
{
    public static class PasswordValidator
    {
        /// <summary>
        /// Kiểm tra độ phức tạp của mật khẩu theo chuẩn SRS (Tối thiểu 8 ký tự, chữ hoa, thường, số, ký tự đặc biệt)
        /// </summary>
        public static string? Validate(string? password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            {
                return "Mật khẩu phải có độ dài tối thiểu 8 ký tự.";
            }

            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                return "Mật khẩu phải chứa ít nhất 1 chữ cái in hoa (A-Z).";
            }

            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                return "Mật khẩu phải chứa ít nhất 1 chữ cái in thường (a-z).";
            }

            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                return "Mật khẩu phải chứa ít nhất 1 chữ số (0-9).";
            }

            if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?"":{}|<>\-_=+]"))
            {
                return "Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt (!@#$%^&*...).";
            }

            return null;
        }
    }
}
