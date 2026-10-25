using System;
using System.Collections.Generic;

namespace MTC_System.DTOs
{
    // DTO nhận dữ liệu khi Thêm cơ sở mới
    public class CreateFacilityDto
    {
        public Guid OrganizationId { get; set; }                 // Thuộc tổ chức nào
        public string FacilityCode { get; set; } = string.Empty; // Mã cơ sở (duy nhất, cấm sửa sau này)
        public string FacilityName { get; set; } = string.Empty; // Tên cơ sở (VD: TP.HCM, Quận 1...)
        public Guid? ParentId { get; set; }                      // null nếu là cơ sở gốc (Root), hoặc Id của cơ sở cha
    }

    // DTO nhận dữ liệu khi Cập nhật cơ sở
    public class UpdateFacilityDto
    {
        public string FacilityName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    //hiển thị dạng danh sách phẳng
    public class FacilityResponseDto
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public string FacilityCode { get; set; } = string.Empty;
        public string FacilityName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public Guid? ParentId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    //Trả về cấu trúc CÂY PHÂN CẤP
    public class FacilityTreeNodeDto
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public string FacilityCode { get; set; } = string.Empty;
        public string FacilityName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public Guid? ParentId { get; set; }
        // Danh sách các cơ sở con cấp dưới
        public List<FacilityTreeNodeDto> Children { get; set; } = new List<FacilityTreeNodeDto>();
    }
}
