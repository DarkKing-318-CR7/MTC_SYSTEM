using System;

namespace MTC_System.Common
{
    /// BaseEntity dùng chung cho các Entity toàn hệ thống và bảng con.
    /// Kế thừa class này để tự động có Khóa chính (Id), Thời gian tạo/sửa và Xóa mềm (IsDeleted).

    public abstract class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }

        /// Phục vụ XÓA MỀM (Soft Delete) theo yêu cầu SRS:
        /// Khi bấm xóa, chỉ set IsDeleted = true. Dữ liệu vẫn được giữ trong DB để đối soát.
  
        public bool IsDeleted { get; set; } = false;
    }
}
