using System;
using System.ComponentModel.DataAnnotations;

namespace MTC_System.DTOs
{
    public class UserCreateDto
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9_.]+$", ErrorMessage = "Tên đăng nhập chỉ chứa chữ cái, số, dấu gạch dưới và dấu chấm")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [StringLength(255, ErrorMessage = "Họ và tên tối đa 255 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }

        public Guid? OrganizationId { get; set; }
        public Guid? FacilityId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò")]
        public Guid RoleId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UserUpdateDto
    {
        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [StringLength(255, ErrorMessage = "Họ và tên tối đa 255 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }

        public Guid? OrganizationId { get; set; }
        public Guid? FacilityId { get; set; }

        public Guid? RoleId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UserResponseDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool TwoFactorEnabled { get; set; }

        public Guid? OrganizationId { get; set; }
        public string? OrganizationName { get; set; }

        public Guid? FacilityId { get; set; }
        public string? FacilityName { get; set; }

        public Guid? RoleId { get; set; }
        public string? RoleCode { get; set; }
        public string? RoleName { get; set; }

        public DateTimeOffset? LastLoginAt { get; set; }
        public DateTimeOffset? LastActivityAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class UserFilterDto
    {
        public string? Search { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? FacilityId { get; set; }
        public Guid? RoleId { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu mới không được để trống")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không trùng khớp")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class UpdateUserStatusDto
    {
        public bool IsActive { get; set; }
    }
}
