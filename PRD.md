PRD - AudioGuideSystem
1. Thông tin tài liệu
Tên sản phẩm: AudioGuideSystem
Phiên bản PRD: v1.0
Ngày cập nhật: 2026-03-25
Trạng thái: Draft
Nguồn đối chiếu: Codebase hiện tại gồm AudioGuideAPI, AudioGuideAdmin, AudioTourApp

## 2. Tom tat san pham

AudioGuideSystem là nền tảng audio tour theo vị trí, được thiết kế để hỗ trợ khách tham quan nhận nội dung thuyết minh tự động khi di chuyển đến các điểm quan tâm (POI - Point of Interest).

Hệ thống hiện tại đang tập trung vào use case khu phố ẩm thực Vĩnh Khánh, kết hợp:

Ứng dụng mobile Android
Web admin
Backend API

Mục tiêu của sản phẩm là giúp du khách khám phá địa điểm một cách tự nhiên hơn thông qua:

GPS
Geofence
Audio narration
Lộ trình tour có sẵn

Đồng thời cho phép đơn vị vận hành:

Quản lý nội dung
Quản lý âm thanh
Quản lý bản dịch
Theo dõi dữ liệu sử dụng

## 3. Van de can giai quyet

Khách tham quan cần một trải nghiệm hướng dẫn tự động, nhanh và dễ dùng, không phụ thuộc hướng dẫn viên.
Nội dung tại địa điểm cần được kích hoạt đúng lúc, đúng nội dung và đúng ngôn ngữ.
Đơn vị vận hành cần một công cụ để tạo và quản lý POI, audio, tour, bản dịch và dữ liệu vận hành.
Hệ thống cần ghi nhận hành vi di chuyển và lượt nghe để tối ưu nội dung và quy hoạch tuyến tham quan.

## 4. Muc tieu san pham

### Mục tiêu kinh doanh

Tăng giá trị trải nghiệm tham quan tại địa điểm.
Tạo nền tảng có thể nhân bản cho nhiều khu du lịch, phố ẩm thực hoặc bảo tàng nhỏ.
Hỗ trợ đơn vị vận hành đo lường mức độ quan tâm của từng POI và tour.

### Mục tiêu người dùng

Khách mở app, cấp quyền vị trí và nhận hướng dẫn audio gần như ngay lập tức.
Khách nhìn thấy bản đồ, danh sách POI gần nhất và các tour có sẵn.
Nội dung phát tự động khi khách tiến vào vùng geofence của POI.

### Mục tiêu vận hành

Quản trị viên có thể tạo, sửa, ẩn/hiện POI.
Quản trị viên có thể upload audio, tạo bản dịch và sắp xếp tour.
Vận hành có thể xem dashboard, trigger, tracking, lượt nghe và heatmap để đánh giá hiệu quả.

## 5. Pham vi san pham

### Trong pham vi v1

App Android MAUI cho visitor
Backend API .NET 8 + SQL Server
Web admin ASP.NET MVC
Quản lý POI, geofence, audio, translation, tour
Tracking vị trí foreground
Auto trigger audio dựa trên geofence
Dashboard và endpoint analytics cơ bản
Feed dữ liệu bản đồ, nearby POI, bootstrap dữ liệu khởi động
Quản lý visitor ẩn danh bằng userId và deviceId
Hỗ trợ QR code lookup theo mã

### Ngoai pham vi v1

Đăng nhập visitor
Loyalty
Thanh toán
Đặt chỗ
Push notification
Offline-first và đồng bộ khi mất mạng
Background tracking hoàn chỉnh Android service
iOS app
CMS workflow đa cấp duyệt
Recommendation thông minh hoặc cá nhân hóa nâng cao

## 6. Doi tuong nguoi dung

### 1. Visitor

Sử dụng app mobile để:
Theo dõi vị trí
Xem bản đồ
Nhận audio thuyết minh
Không cần đăng ký tài khoản trong phiên bản hiện tại.

### 2. Admin / Operator

Đăng nhập web admin bằng session-based authentication
Quản lý dữ liệu:
POI
Bản dịch
Tour
File audio
Theo dõi:
KPI vận hành
Lịch sử trigger

### 3. Product Owner / Dia diem van hanh

Quan tâm đến các KPI như:

Lượt ghé thăm
Lượt nghe
POI phổ biến
Thời gian nghe trung bình

Sử dụng dashboard và analytics để quyết định:

Nội dung
Bố trí tour

## 7. Gia thuyet san pham

Visitor sẵn sàng cấp quyền vị trí khi giá trị nhận được là rõ ràng.
Auto narration sẽ tăng mức độ tương tác so với việc visitor tự bấm nghe.
Đơn vị vận hành có thể tự quản lý nội dung mà không cần thao tác kỹ thuật phức tạp.
Khu tham quan có mật độ POI vừa phải để geofence không bị chồng lấn quá mức.

## 8. Hanh trinh nguoi dung chinh

### Hanh trinh 1: Visitor bat dau tham quan

Mở app và nhập hoặc giữ nguyên API Base URL.
Bấm Bootstrap hoặc Bật / Tắt tracking.
App tải danh sách POI và tour.
App xin quyền vị trí.
Hệ thống bắt đầu gửi vị trí định kỳ lên backend.

### Hanh trinh 2: Visitor di ngang mot POI

App gửi tọa độ hiện tại lên API.
Backend tính khoảng cách tới POI và kiểm tra geofence.
Nếu thỏa trigger mode và không vi phạm cooldown/debounce:
Hệ thống tạo bản ghi trigger.
App đưa POI vào hàng đợi audio.
App phát:
Audio URL
hoặc
TTS script

### Hanh trinh 3: Admin van hanh noi dung

Đăng nhập web admin
Tạo hoặc chỉnh sửa POI
Upload file audio
Tạo translation theo ngôn ngữ
Tạo tour và sắp xếp điểm dừng
Theo dõi dashboard và tab tracking

## 9. Yeu cau chuc nang

### F1. Quan ly POI

Admin có thể:

Tạo
Sửa
Xóa
Xem chi tiết
Lọc danh sách POI

Mỗi POI có:

Tên
Danh mục
Mô tả ngắn
Mô tả dài
Địa chỉ
Vĩ độ
Kinh độ

Tham số geofence:

radius
approachRadiusMeters
triggerMode
priority
debounceSeconds
cooldownSeconds

POI có:

IsActive
ImageUrl
MapUrl
AudioMode
AudioUrl
TtsScript
DefaultLanguage
EstimatedDurationSeconds

### F2. Translation management

Admin có thể tạo translation theo POI và ngôn ngữ.

Mỗi translation gồm:

title
summary
description
audioUrl
ttsScript
voiceName
isPublished

Ràng buộc:

PoiId + Language phải duy nhất.

### F3. Tour management

Admin có thể:

Tạo
Sửa
Xóa tour

Tour gồm:

name
description
language
coverImageUrl
estimatedDurationMinutes
isActive

Tour chứa nhiều TourStop được sắp xếp theo SortOrder.

### F4. Audio library

Admin có thể:

Upload file audio
Xem danh sách audio đã upload

API cung cấp thư viện audio qua endpoint.

Visitor phát audio từ URL nếu POI hoặc translation có file audio.

### F5. Bootstrap va map feed
Mobile app có thể:

Gọi bootstrap để lấy toàn bộ POI active và tour active khi khởi động.
Lấy danh sách POI gần nhất theo vị trí hiện tại.

App hiển thị:

Bản đồ
Marker vị trí người dùng
POI gần nhất

### F6. Geofence va auto play

Backend kiểm tra người dùng nằm trong:

radius
hoặc approachRadiusMeters

Trigger mode:

enter
nearby
manual
both

Hệ thống chống phát lặp bằng:

cooldown
debounce

### F7. Tracking visitor

App gửi vị trí định kỳ:

latitude
longitude
accuracy
speed
bearing
foreground state
recordedAt

Backend lưu tracking theo userId.
### F8. Visit history
Backend lưu lịch sử nghe/tham quan:

startTime
endTime
duration

Nếu thiếu dữ liệu hệ thống tự tính duration.

### F9. Analytics va dashboard
Dashboard hiển thị:

Tổng POI
Tổng visit
Tổng tracking point
Tổng tour
Unique visitors
Average listen duration

### F10. QR code lookup

API trả thông tin POI theo QR code.

### F11. Admin authentication

Web admin yêu cầu đăng nhập.

Hỗ trợ:

Đăng ký admin đầu tiên
Session timeout: 8 giờ

## 10. Yeu cau phi chuc nang

### Hieu nang

Tracking và geofence check phải phản hồi nhanh để không trễ audio.

### Do tin cay

Hệ thống phải tránh phát audio lặp lại quá nhanh.

### Bao mat

Admin phải đăng nhập
Mật khẩu phải hash
API public hiện chưa có auth

### Kha nang mo rong
Kiến trúc 3 thành phần:

Mobile
Admin
API

### Kha dung

Visitor thao tác tối thiểu:

bootstrap
bật tracking
xem map
nghe audio

## 11. KPI / Success metrics

Tỷ lệ visitor cấp quyền vị trí
Tỷ lệ session có geofence trigger
Số lượt nghe trung bình mỗi visitor
Thời lượng nghe trung bình mỗi POI
Visitor unique mỗi ngày / tuần
Top POI theo lượt trigger
Tỷ lệ trigger bị bỏ qua do cooldown

## 12. Du lieu chinh

Visitor
POI
POI Translation
Tour
Tour Stop
User Tracking
Geofence Trigger
Visit History
QR Code
Admin User

## 13. Gioi han hien tai trong codebase

Mobile app hiện Android-only
Tracking chủ yếu foreground
Chưa có auth API
Audio playback tối ưu Android
Map dùng Leaflet/OpenStreetMap
Chưa có chế độ offline

## 14. Roadmap de xuat

### Phase 1 - Pilot readiness

Ổn định geofence
Chuẩn hóa dữ liệu POI
Dashboard KPI
Test thực địa Vĩnh Khánh
### Phase 2 - Production readiness

Background tracking Android
Logging + rate limit
QR code flow
Tùy chọn autoplay

### Phase 3 - Scale & productization

Multi-tenant
Recommendation
Offline cache
iOS app

## 15. Cac cau hoi mo can chot

Có nên auto narration hoàn toàn không?
KPI chính của pilot là gì?
Có cần multi-area data không?
Privacy và retention policy?
QR code mở app hay web?

## 16. Tieu chi chap nhan tong quan

Admin đăng nhập và quản lý POI
Visitor bootstrap dữ liệu
Geofence trigger audio
Dashboard có số liệu
Tất cả hệ thống dùng SQL Server chung

