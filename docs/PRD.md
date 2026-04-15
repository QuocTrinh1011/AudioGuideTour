PRD - AudioGuideSystem

1. Thông tin tài liệu

Tên sản phẩm: AudioGuideSystem
Phiên bản PRD: v1.0
Ngày cập nhật: 2026-03-25
Trạng thái: Draft
Nguồn đối chiếu: Codebase hiện tại gồm AudioGuideAPI, AudioGuideAdmin, AudioTourApp

2. Tóm tắt sản phẩm

AudioGuideSystem là nền tảng audio tour theo vị trí, được thiết kế để hỗ trợ khách tham quan nhận nội dung thuyết minh tự động khi di chuyển đến các điểm quan tâm (POI - Point of Interest).

Hệ thống hiện tại đang tập trung vào use case khu phố ẩm thực Vĩnh Khánh, kết hợp app mobile Android, web admin và backend API.

Mục tiêu của sản phẩm là giúp du khách khám phá địa điểm một cách tự nhiên hơn thông qua GPS, geofence, audio narration và lộ trình tour có sẵn; đồng thời cho phép đơn vị vận hành quản lý nội dung, âm thanh, bản dịch và theo dõi dữ liệu sử dụng.

3. Vấn đề cần giải quyết

Khách tham quan cần một trải nghiệm hướng dẫn tự động, nhanh, dễ dùng, không phụ thuộc hướng dẫn viên.
Nội dung tại địa điểm cần được kích hoạt đúng lúc, đúng nơi, đúng ngôn ngữ.
Đơn vị vận hành cần một công cụ để tạo và quản lý POI, audio, tour, bản dịch và xem dữ liệu vận hành.
Hệ thống cần ghi nhận hành vi di chuyển và lượt nghe để tối ưu nội dung và quy hoạch tuyến tham quan.

4. Mục tiêu sản phẩm

Mục tiêu kinh doanh

Tăng giá trị trải nghiệm tham quan tại địa điểm.
Tạo nền tảng có thể nhân bản cho nhiều khu du lịch, phố ẩm thực hoặc bảo tàng nhỏ.
Hỗ trợ đơn vị vận hành đo lường mức độ quan tâm của từng POI và tour.

Mục tiêu người dùng

Khách mở app, cấp quyền vị trí và nhận hướng dẫn audio gần như ngay lập tức.
Khách nhìn thấy bản đồ, danh sách POI gần nhất và các tour sẵn sàng.
Nội dung phát tự động khi khách tiến vào vùng geofence của POI.
Mục tiêu vận hành
Quản trị viên có thể tạo, sửa, ẩn/hiện POI.
Quản trị viên có thể upload audio, tạo bản dịch và sắp xếp tour.
Vận hành có thể xem dashboard, trigger, tracking, lượt nghe và heatmap để đánh giá hiệu quả.

5. Phạm vi sản phẩm

Trong phạm vi v1
App Android MAUI cho visitor.
Backend API .NET 8 + SQL Server.
Web admin ASP.NET MVC để quản trị nội dung.
Quản lý POI, geofence, audio, translation, tour.
Tracking vị trí foreground.
Auto trigger audio dựa trên geofence.
Dashboard và endpoint analytics cơ bản.
Feed dữ liệu bản đồ, nearby POI, bootstrap dữ liệu khởi động.
Quản lý visitor ẩn danh bằng userId và deviceId.
Hỗ trợ QR code lookup theo mã.
Ngoài phạm vi v1
Đăng nhập visitor, loyalty, thanh toán, đặt chỗ.
Push notification.
Offline-first và đồng bộ khi mất mạng.
Background tracking hoàn chỉnh trên Android service.
iOS app.
CMS workflow đa cấp duyệt.
Recommendation thông minh hoặc cá nhân hóa nâng cao.

6. Đối tượng người dùng

1. Visitor

Sử dụng app mobile để theo dõi vị trí, xem bản đồ, nhận audio thuyết minh.
Không cần đăng ký tài khoản trong phiên bản hiện tại.

2. Admin / Operator

Đăng nhập web admin bằng session-based auth.
Quản lý dữ liệu POI, bản dịch, tour, file audio.
Theo dõi KPI vận hành và lịch sử trigger.

3. Product Owner / Địa điểm vận hành

Quan tâm tới KPI như lượt ghé thăm, lượt nghe, POI hot, thời gian nghe trung bình.
Dùng dashboard và analytics để quyết định nội dung và bố trí tour.

7. Giả thuyết sản phẩm

Visitor sẵn sàng cấp quyền vị trí khi giá trị nhận được là rõ ràng.
Auto narration sẽ tăng mức độ tương tác so với việc để visitor tự bấm nghe thủ công.
Vận hành có thể tự quản lý nội dung mà không cần thao tác kỹ thuật phức tạp.
Khu tham quan có mật độ POI vừa phải để geofence không bị chồng lấn quá mức.

8. Hành trình người dùng chính

Hành trình 1: Visitor bắt đầu tham quan
Mở app và nhập hoặc giữ nguyên API Base URL.
Bấm Bootstrap hoặc Bật / Tắt tracking.
App tải danh sách POI và tour.
App xin quyền vị trí.
Hệ thống bắt đầu gửi vị trí định kỳ lên backend.
Hành trình 2: Visitor đi ngang một POI
App gửi tọa độ hiện tại lên API.
Backend tính khoảng cách tới POI và kiểm tra geofence.
Nếu thỏa trigger mode và không vi phạm cooldown/debounce, hệ thống tạo bản ghi trigger.
App đưa POI vào hàng đợi audio.
App phát audio URL hoặc TTS script.
Hành trình 3: Admin vận hành nội dung
Đăng nhập web admin.
Tạo hoặc chỉnh sửa POI.
Upload file audio.
Tạo translation theo ngôn ngữ.
Tạo tour và sắp xếp các điểm dừng.
Theo dõi dashboard và tab tracking.

9. Yêu cầu chức năng

F1. Quản lý POI
Admin tạo, sửa, xóa, xem chi tiết và lọc danh sách POI.
Mỗi POI có: tên, danh mục, mô tả ngắn, mô tả dài, địa chỉ, vĩ độ, kinh độ.
Tham số geofence: radius, approachRadiusMeters, triggerMode, priority, debounceSeconds, cooldownSeconds.
Trạng thái hiển thị: IsActive.
Hỗ trợ: ImageUrl, MapUrl, AudioMode, AudioUrl, TtsScript, DefaultLanguage, EstimatedDurationSeconds.
F2. Translation management
Admin tạo translation theo POI và ngôn ngữ.
Mỗi translation có: title, summary, description, audioUrl, ttsScript, voiceName, isPublished.
Ưu tiên translation phù hợp ngôn ngữ request.
Mỗi cặp PoiId + Language là duy nhất.
F3. Tour management
Admin tạo, sửa, xóa tour.
Tour gồm: name, description, language, coverImageUrl, estimatedDurationMinutes, isActive.
Gồm nhiều TourStop theo SortOrder.
Chọn danh sách POI cho từng tour.
F4. Audio library
Upload file audio.
Xem danh sách audio.
API cung cấp thư viện audio.
Visitor phát audio từ URL.
F5. Bootstrap và map feed
Mobile gọi bootstrap để lấy POI và tour.
Lấy POI gần nhất theo vị trí.
Hiển thị bản đồ và marker.
F6. Geofence và auto play
Kiểm tra trong radius hoặc approachRadiusMeters.
Trigger mode: enter, nearby, manual, both.
Chống lặp bằng cooldown và debounce.
Lưu lịch sử trigger.
App tự phát audio.
F7. Tracking visitor
Gửi vị trí định kỳ.
Lưu latitude, longitude, accuracy, speed, bearing, trạng thái foreground, thời gian.
Truy vấn theo userId.
F8. Visit history
Lưu lịch sử nghe/tham quan.
Có startTime, endTime, duration.
F9. Analytics và dashboard
Tổng POI, visit, tracking, tour, unique visitors.
Top POI.
Recent trigger.
Heatmap.
F10. QR code lookup
Trả thông tin POI từ QR code.
F11. Admin authentication
Bắt buộc đăng nhập.
Đăng ký admin đầu tiên.
Session timeout 8 giờ.

10. Yêu cầu phi chức năng

Hiệu năng
Tracking và geofence phải nhanh.
Bootstrap hoạt động tốt với dữ liệu vừa.
Độ tin cậy
Không phát audio lặp nhanh.
Dữ liệu có ràng buộc.
Bảo mật
Admin phải đăng nhập.
Password hash.
API public chưa có auth.
Khả năng mở rộng
Kiến trúc tách biệt.
Có thể scale nhiều khu vực.
Khả dụng
Visitor thao tác tối thiểu.
Admin dễ dùng.

11. KPI / Success metrics

Tỷ lệ cấp quyền vị trí.
Tỷ lệ có trigger.
Lượt nghe trung bình.
Thời lượng nghe.
Visitor unique.
Top POI.
Tỷ lệ bị cooldown.

12. Dữ liệu chính

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

13. Giới hạn hiện tại

Android-only
Tracking foreground
Chưa có auth API
Audio chủ yếu Android native
Map dùng Leaflet/OSM
Chưa offline

14. Roadmap đề xuất

Phase 1 - Pilot
Ổn định geofence
Chuẩn hóa dữ liệu
Dashboard cơ bản
Test thực địa
Phase 2 - Production
Background tracking
Logging + bảo mật
QR flow
Cấu hình ngôn ngữ
Phase 3 - Scale
Multi-tenant
Cá nhân hóa
Offline
iOS

15. Câu hỏi mở

Auto hay manual narration?
KPI chính là gì?
Có cần multi-tenant sớm?
Privacy policy?
QR mở app hay web?

16. Tiêu chí chấp nhận

Admin quản lý được dữ liệu
Visitor bootstrap + tracking
Geofence hoạt động
Có dashboard
Dữ liệu đồng bộ SQL Server