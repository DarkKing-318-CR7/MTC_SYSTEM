# 📘 TÀI LIỆU BÀN GIAO & HƯỚNG DẪN TÍCH HỢP FRONT-END (MTC SYSTEM)
> **Dự án**: Hệ thống Quản trị & Điều hành Màn hình / Loa Tập trung (MTC Backend)  
> **Nền tảng**: .NET 8 Web API + PostgreSQL + Swagger OpenAPI + JWT + 2FA TOTP  
> **Dành cho**: Đội ngũ Phát triển Front-end (React / Vue / Angular / Mobile)

---

## 🌐 1. THÔNG TIN MÔI TRƯỜNG & KẾT NỐI

* **Base URL**:
  * HTTPS: `https://localhost:7259/api`
  * HTTP: `http://localhost:5259/api`
* **Swagger UI Trực quan**: `https://localhost:7259/swagger`
* **OpenAPI Spec (JSON)**: `https://localhost:7259/swagger/v1/swagger.json`
* **Cấu hình CORS**: Đã kích hoạt chính sách `AllowFrontend` hỗ trợ tất cả các cổng localhost (`3000`, `5173`, `5174`, `4200`...), hỗ trợ Credentials và mọi Header HTTP.
* **Tài khoản Quản trị tối cao (System Admin)**:
  * **Tên đăng nhập**: `admin`
  * **Mật khẩu**: `Admin@123456`

---

## 📦 2. QUY CHUẨN ĐỊNH DẠNG DỮ LIỆU (RESPONSE ENVELOPE)

100% các API của hệ thống đều trả về cấu trúc bọc duy nhất giúp Front-end dễ dàng viết hàm bắt lỗi và hiển thị thông báo:

### 🔹 Định dạng phản hồi chung (`ApiResponse<T>`):
```typescript
interface ApiResponse<T> {
  success: boolean;       // true nếu thành công, false nếu có lỗi
  message: string;        // Thông báo thân thiện để FE hiển thị Toast/Alert
  data: T | null;         // Dữ liệu nghiệp vụ chính
  errors: string[] | null;// Danh sách chi tiết lỗi validation (nếu có)
}
```

### 🔹 Định dạng danh sách có phân trang (`PagedResult<T>`):
Khi gọi các API lấy danh sách người dùng, thiết bị, media... trường `data` sẽ có cấu trúc:
```typescript
interface PagedResult<T> {
  items: T[];             // Mảng dữ liệu của trang hiện tại
  totalCount: number;     // Tổng số bản ghi trong Database (dùng tính pagination)
  pageIndex: number;      // Trang hiện tại (bắt đầu từ 1)
  pageSize: number;       // Số phần tử trên 1 trang
  totalPages: number;     // Tổng số trang
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
```

---

## 🛡️ 3. CƠ CHẾ XÁC THỰC (AUTH FLOW) & TOKEN REFRESHING

1. **Header gửi kèm**: Mọi API cần đăng nhập (có gắn ổ khóa trên Swagger) bắt buộc gửi kèm Header:
   ```http
   Authorization: Bearer <chuỗi_access_token>
   ```
2. **Thời hạn**:
   * `AccessToken`: Sống **15 phút**.
   * `RefreshToken`: Sống **7 ngày**.
3. **Luồng Đăng nhập 2 bước (2FA)**:
   * FE gọi `POST /api/auth/login`.
   * Nếu `response.data.requires2FA === true`: Chuyển người dùng sang màn hình nhập mã OTP 6 số (hoặc mã dự phòng) và gọi tiếp `POST /api/auth/verify-2fa` truyền `userId` + `code`.
4. **Cơ chế Tự động làm mới Token (Silent Refresh Token)**:
   * Khi gọi một API bất kỳ nhận mã HTTP **`401 Unauthorized`**, Axios Interceptor của FE tự động giữ request đó lại, âm thầm gọi API `POST /api/auth/refresh-token` gửi `accessToken` và `refreshToken` cũ lên để lấy cặp token mới rồi thực hiện lại request cũ.

---

## 💻 4. MẪU CODE AXIOS CLIENT CHO FRONT-END (TYPESCRIPT)

FE chỉ cần copy đoạn code này vào file `src/api/client.ts` để sử dụng cho toàn bộ dự án:

```typescript
import axios from 'axios';

export const apiClient = axios.create({
  baseURL: 'https://localhost:7259/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

// 1. Request Interceptor: Tự động đính kèm Bearer Token
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// 2. Response Interceptor: Tự động xử lý Refresh Token khi Token hết hạn (401)
let isRefreshing = false;
let failedQueue: Array<{ resolve: (token: string) => void; reject: (error: any) => void }> = [];

const processQueue = (error: any, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (token) prom.resolve(token);
    else prom.reject(error);
  });
  failedQueue = [];
};

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            originalRequest.headers.Authorization = `Bearer ${token}`;
            return apiClient(originalRequest);
          })
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const accessToken = localStorage.getItem('accessToken') || '';
      const refreshToken = localStorage.getItem('refreshToken') || '';

      try {
        const res = await axios.post('https://localhost:7259/api/auth/refresh-token', {
          accessToken,
          refreshToken,
        });

        if (res.data.success && res.data.data?.accessToken) {
          const newAccessToken = res.data.data.accessToken;
          const newRefreshToken = res.data.data.refreshToken;

          localStorage.setItem('accessToken', newAccessToken);
          localStorage.setItem('refreshToken', newRefreshToken);

          processQueue(null, newAccessToken);
          originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
          return apiClient(originalRequest);
        }
      } catch (refreshErr) {
        processQueue(refreshErr, null);
        localStorage.clear();
        window.location.href = '/login';
        return Promise.reject(refreshErr);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);
```

---

## 📋 5. DANH MỤC CÁC API BACKEND ĐÃ SẴN SÀNG GHÉP NỐI

### 1. Phân hệ Xác thực & Bảo mật (`/api/auth`)
| Phương thức | Đường dẫn API | Cần đăng nhập | Mô tả & Chức năng |
| :--- | :--- | :---: | :--- |
| `POST` | `/api/auth/login` | ❌ Không | Đăng nhập tài khoản. Trả về Token hoặc yêu cầu 2FA. |
| `POST` | `/api/auth/verify-2fa` | ❌ Không | Xác thực bước 2 qua mã OTP 6 số hoặc mã dự phòng `XXXX-XXXX`. |
| `POST` | `/api/auth/refresh-token` | ❌ Không | Cấp lại AccessToken 15 phút mới khi token cũ hết hạn. |
| `POST` | `/api/auth/logout` | ✅ Có | Thu hồi session hiện tại hoặc toàn bộ session. |
| `GET` | `/api/auth/me` | ✅ Có | Lấy toàn bộ thông tin tài khoản đang đăng nhập kèm Role, Cơ sở. |
| `PUT` | `/api/auth/profile` | ✅ Có | Cập nhật Họ tên, SĐT, Email cá nhân. |
| `PUT` | `/api/auth/change-password` | ✅ Có | Đổi mật khẩu cá nhân (kiểm tra chuẩn 8 ký tự, hoa, thường, số, ký tự đặc biệt). |
| `POST` | `/api/auth/2fa/setup` | ✅ Có | Sinh Secret Key và mã QR dạng Data URL để quét bằng Google Authenticator. |
| `POST` | `/api/auth/2fa/enable` | ✅ Có | Kích hoạt 2FA và nhận danh sách 10 mã dự phòng dùng một lần. |
| `POST` | `/api/auth/2fa/disable` | ✅ Có | Tắt 2FA (yêu cầu mật khẩu và mã xác nhận). |
| `POST` | `/api/auth/2fa/backup-codes/regenerate` | ✅ Có | Sinh lại 10 mã dự phòng mới. |
| `GET` | `/api/auth/2fa/backup-codes/count` | ✅ Có | Lấy số lượng mã dự phòng còn lại (vd: 9/10). |

---

### 2. Phân hệ Quản lý Người dùng (`/api/users`)
| Phương thức | Đường dẫn API | Cần đăng nhập | Mô tả & Chức năng |
| :--- | :--- | :---: | :--- |
| `GET` | `/api/users` | ✅ Có | Lấy danh sách Người dùng phân trang, lọc theo tên, email, cơ sở, vai trò. |
| `GET` | `/api/users/{id}` | ✅ Có | Xem chi tiết thông tin một người dùng. |
| `POST` | `/api/users` | ✅ Có | Tạo tài khoản nhân viên mới, gán vào cơ sở và vai trò. |
| `PUT` | `/api/users/{id}` | ✅ Có | Sửa thông tin nhân viên, đổi vai trò hoặc chuyển cơ sở. |
| `PATCH` | `/api/users/{id}/status` | ✅ Có | Khóa hoặc Mở khóa tài khoản nhân viên. |
| `POST` | `/api/users/{id}/reset-password` | ✅ Có | Reset mật khẩu cho nhân viên. |
| `DELETE` | `/api/users/{id}` | ✅ Có | Xóa mềm tài khoản người dùng. |

---

### 3. Phân hệ Vai trò & Ma trận Phân quyền (`/api/roles`)
| Phương thức | Đường dẫn API | Cần đăng nhập | Mô tả & Chức năng |
| :--- | :--- | :---: | :--- |
| `GET` | `/api/roles` | ✅ Có | Danh sách Vai trò (ADMINISTRATOR, CREATOR, APPROVER, IT...) kèm số lượng User. |
| `GET` | `/api/roles/{id}` | ✅ Có | Chi tiết vai trò và danh sách các quyền đã được bật. |
| `POST` | `/api/roles` | ✅ Có | Tạo vai trò tùy biến riêng cho từng Tổ chức. |
| `GET` | `/api/roles/permissions` | ✅ Có | Danh mục 18 quyền chức năng phân theo từng nhóm (để vẽ bảng phân quyền). |
| `POST` | `/api/roles/{id}/permissions` | ✅ Có | Lưu lại cấu hình ma trận phân quyền cho vai trò. |

---

### 4. Phân hệ Tổ chức & Cây cơ sở (`/api/organizations` & `/api/facilities`)
| Phương thức | Đường dẫn API | Cần đăng nhập | Mô tả & Chức năng |
| :--- | :--- | :---: | :--- |
| `GET` | `/api/organizations` | ❌ Tạm thời | Lấy danh sách toàn bộ các Tổ chức thuê bao SaaS. |
| `GET` | `/api/facilities/tree/{organizationId}` | ❌ Tạm thời | **Dựng cây địa bàn đệ quy**: Tỉnh/Thành phố $\rightarrow$ Quận/Huyện $\rightarrow$ Phường/Xã. |
| `GET` | `/api/facilities/{id}/descendant-ids` | ❌ Tạm thời | Lấy toàn bộ ID cơ sở cấp con/cháu để FE lọc danh sách dữ liệu. |
| `POST` | `/api/facilities` | ❌ Tạm thời | Tạo cơ sở mới (tự động kế thừa từ cơ sở cha). |
| `PUT` | `/api/facilities/{id}` | ❌ Tạm thời | Sửa cơ sở (nếu ngưng hoạt động cha, con cháu tự động đóng băng). |
| `DELETE` | `/api/facilities/{id}` | ❌ Tạm thời | Xóa cơ sở (kiểm tra ràng buộc cấm xóa nếu có thiết bị/media). |

---

### 5. Phân hệ Media, Bản tin, Thiết bị & Gói cước (Dev 2, 3, 4, 5)
* **Media Upload (Dev 2)**:
  * `POST /api/ChunkedMediaUpload/initialize`: Khởi tạo phiên tải file 1GB.
  * `POST /api/ChunkedMediaUpload/chunk`: Đẩy từng khối (chunk) file.
  * `POST /api/ChunkedMediaUpload/complete`: Ghép các khối thành file hoàn chỉnh.
* **Bản tin & Lịch phát (Dev 3)**:
  * `GET /api/Broadcasts`: Danh sách bản tin.
  * `POST /api/Broadcasts`: Tạo bản tin thường hoặc khẩn cấp.
* **Thiết bị & Nhóm (Dev 4)**:
  * `GET /api/Devices`: Danh sách màn hình / loa.
  * `GET /api/DeviceGroups`: Danh sách nhóm thiết bị.
  * `GET /api/DeviceVendors`: Danh mục nhà cung cấp phần cứng.
* **Gói cước SaaS (Dev 5)**:
  * `GET /api/Packages`: Danh sách các gói cước bán sẵn.
  * `GET /api/Subscriptions`: Danh sách thuê bao dịch vụ.
