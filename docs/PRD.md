# PRD - AudioGuideSystem

## 1. Thong tin tai lieu

- Ten san pham: AudioGuideSystem
- Phien ban PRD: v1.0
- Ngay cap nhat: 2026-03-25
- Trang thai: Draft
- Nguon doi chieu: Codebase hien tai gom `AudioGuideAPI`, `AudioGuideAdmin`, `AudioTourApp`

## 2. Tom tat san pham

AudioGuideSystem la nen tang audio tour theo vi tri, duoc thiet ke de ho tro khach tham quan nhan noi dung thuyet minh tu dong khi di chuyen den cac diem quan tam (POI - Point of Interest). He thong hien tai dang tap trung vao use case khu pho am thuc Vinh Khanh, ket hop app mobile Android, web admin va backend API.

Muc tieu cua san pham la giup du khach kham pha dia diem mot cach tu nhien hon thong qua GPS, geofence, audio narration va lo trinh tour co san; dong thoi cho phep don vi van hanh quan ly noi dung, am thanh, ban dich va theo doi du lieu su dung.

## 3. Van de can giai quyet

- Khach tham quan can mot trai nghiem huong dan tu dong, nhanh, de dung, khong phu thuoc huong dan vien.
- Noi dung tai dia diem can duoc kich hoat dung luc, dung noi dung, dung ngon ngu.
- Don vi van hanh can mot cong cu de tao va quan ly POI, audio, tour, ban dich va xem du lieu van hanh.
- He thong can ghi nhan hanh vi di chuyen va luot nghe de toi uu noi dung va quy hoach tuyen tham quan.

## 4. Muc tieu san pham

### Muc tieu kinh doanh

- Tang gia tri trai nghiem tham quan tai dia diem.
- Tao nen tang co the nhan ban cho nhieu khu du lich, pho am thuc hoac bao tang nho.
- Ho tro don vi van hanh do luong muc do quan tam cua tung POI va tour.

### Muc tieu nguoi dung

- Khach mo app, cap quyen vi tri va nhan huong dan audio gan nhu ngay lap tuc.
- Khach nhin thay ban do, danh sach POI gan nhat va cac tour san sang.
- Noi dung phat tu dong khi khach tien vao vung geofence cua POI.

### Muc tieu van hanh

- Quan tri vien co the tao, sua, an/hien POI.
- Quan tri vien co the upload audio, tao ban dich va sap xep tour.
- Van hanh co the xem dashboard, trigger, tracking, luot nghe va heatmap de danh gia hieu qua.

## 5. Pham vi san pham

### Trong pham vi v1

- App Android MAUI cho visitor.
- Backend API .NET 8 + SQL Server.
- Web admin ASP.NET MVC de quan tri noi dung.
- Quan ly POI, geofence, audio, translation, tour.
- Tracking vi tri foreground.
- Auto trigger audio dua tren geofence.
- Dashboard va endpoint analytics co ban.
- Feed du lieu ban do, nearby POI, bootstrap du lieu khoi dong.
- Quan ly visitor an danh bang `userId` va `deviceId`.
- Ho tro QR code lookup theo ma.

### Ngoai pham vi v1

- Dang nhap visitor, loyalty, thanh toan, dat cho.
- Push notification.
- Offline-first va dong bo khi mat mang.
- Background tracking hoan chinh tren Android service.
- iOS app.
- CMS workflow da cap duyet.
- Recommendation thong minh hoac ca nhan hoa nang cao.

## 6. Doi tuong nguoi dung

### 1. Visitor

- Su dung app mobile de theo doi vi tri, xem ban do, nhan audio thuyet minh.
- Khong can dang ky tai khoan trong phien ban hien tai.

### 2. Admin / Operator

- Dang nhap web admin bang session-based auth.
- Quan ly du lieu POI, ban dich, tour, file audio.
- Theo doi KPI van hanh va lich su trigger.

### 3. Product Owner / Dia diem van hanh

- Quan tam toi KPI nhu luot ghe tham, luot nghe, POI hot, thoi gian nghe trung binh.
- Dung dashboard va analytics de quyet dinh noi dung va bo tri tour.

## 7. Gia thuyet san pham

- Visitor san sang cap quyen vi tri khi gia tri nhan duoc la ro rang.
- Auto narration se tang muc do tuong tac so voi viec de visitor tu bam nghe thu cong.
- Van hanh co the tu quan ly noi dung ma khong can thao tac ky thuat phuc tap.
- Khu tham quan co mat do POI vua phai de geofence khong bi chong lan qua muc.

## 8. Hanh trinh nguoi dung chinh

### Hanh trinh 1: Visitor bat dau tham quan

1. Mo app va nhap hoac giu nguyen API Base URL.
2. Bam `Bootstrap` hoac `Bat / Tat tracking`.
3. App tai danh sach POI va tour.
4. App xin quyen vi tri.
5. He thong bat dau gui vi tri dinh ky len backend.

### Hanh trinh 2: Visitor di ngang mot POI

1. App gui toa do hien tai len API.
2. Backend tinh khoang cach toi POI va kiem tra geofence.
3. Neu thoa trigger mode va khong vi pham cooldown/debounce, he thong tao ban ghi trigger.
4. App dua POI vao hang doi audio.
5. App phat audio URL hoac TTS script.

### Hanh trinh 3: Admin van hanh noi dung

1. Dang nhap web admin.
2. Tao hoac chinh sua POI.
3. Upload file audio.
4. Tao translation theo ngon ngu.
5. Tao tour va sap xep cac diem dung.
6. Theo doi dashboard va tab tracking.

## 9. Yeu cau chuc nang

### F1. Quan ly POI

- Admin tao, sua, xoa, xem chi tiet va loc danh sach POI.
- Moi POI co ten, danh muc, mo ta ngan, mo ta dai, dia chi, vi do, kinh do.
- Moi POI co tham so geofence: `radius`, `approachRadiusMeters`, `triggerMode`, `priority`, `debounceSeconds`, `cooldownSeconds`.
- POI co the bat/tat hien thi bang `IsActive`.
- POI ho tro `ImageUrl`, `MapUrl`, `AudioMode`, `AudioUrl`, `TtsScript`, `DefaultLanguage`, `EstimatedDurationSeconds`.

### F2. Translation management

- Admin tao translation theo POI va ngon ngu.
- Moi translation co `title`, `summary`, `description`, `audioUrl`, `ttsScript`, `voiceName`, `isPublished`.
- He thong uu tien translation published phu hop ngon ngu request.
- Moi cap `PoiId + Language` phai la duy nhat.

### F3. Tour management

- Admin tao, sua, xoa tour.
- Moi tour co `name`, `description`, `language`, `coverImageUrl`, `estimatedDurationMinutes`, `isActive`.
- Tour gom nhieu `TourStop`, duoc sap theo `SortOrder`.
- Admin chon danh sach POI cho tung tour.

### F4. Audio library

- Admin upload file audio len thu muc static.
- Admin xem danh sach audio da upload.
- API cung cap thu vien audio qua endpoint.
- Visitor phat audio tu URL neu POI hoac translation co file audio.

### F5. Bootstrap va map feed

- Mobile app co the goi bootstrap de lay toan bo POI active va tour active luc khoi dong.
- App co the lay danh sach POI gan nhat theo vi tri hien tai.
- App hien thi ban do va marker cua vi tri nguoi dung cung POI gan nhat.

### F6. Geofence va auto play

- Backend kiem tra nguoi dung dang o trong `radius` hoac `approachRadiusMeters`.
- He thong ho tro cac che do trigger: `enter`, `nearby`, `manual`, `both`.
- He thong phai chong phat lap bang cooldown va debounce.
- He thong ghi nhan lich su geofence trigger cho moi visitor va POI.
- App tu dong dua audio vao hang doi va phat lan luot.

### F7. Tracking visitor

- App gui ban ghi vi tri dinh ky khi tracking duoc bat.
- Backend luu latitude, longitude, accuracy, speed, bearing, foreground state, recordedAt.
- He thong co the tra lich su tracking theo `userId`.

### F8. Visit history

- Backend nhan va luu lich su nghe/tham quan theo POI va user.
- Visit phai co `startTime`, `endTime`, `duration`; he thong co the tu tinh neu du lieu thieu.

### F9. Analytics va dashboard

- Dashboard admin hien tong POI, tong visit, tong tracking point, tong tour, unique visitors, average listen duration.
- He thong co top POI theo luot nghe.
- He thong co recent trigger de van hanh quan sat.
- API co analytics overview, top POI, average listen duration va heatmap.

### F10. QR code lookup

- API tra ve thong tin POI theo QR code de phuc vu deep-link hoac entry point tai dia diem.

### F11. Admin authentication

- Web admin yeu cau dang nhap truoc khi truy cap cac route khac `Auth`.
- Ho tro dang ky admin dau tien.
- Session timeout mac dinh la 8 gio.

## 10. Yeu cau phi chuc nang

### Hieu nang

- Tracking request va geofence check phai dap ung nhanh de khong tre trai nghiem audio.
- Bootstrap phai hoat dong tot voi tap POI va tour muc do nho den trung binh.

### Do tin cay

- He thong phai tranh phat audio lap lai qua nhanh cho cung mot POI.
- Du lieu POI, tour, translation phai luu SQL Server va co rang buoc unique can thiet.

### Bao mat

- Admin area phai yeu cau dang nhap.
- Mat khau admin can duoc hash truoc khi luu.
- API public hien chua co auth; day la gioi han can danh gia khi mo rong production.

### Kha nang mo rong

- Kien truc 3 thanh phan tach biet cho phep scale doc lap mobile, admin va API.
- Model hien tai phu hop pilot va co the mo rong them nhieu khu vuc, POI, ngon ngu.

### Kha dung

- Visitor can thao tac toi thieu: bootstrap, bat tracking, xem map, nghe audio.
- Admin can co giao dien CRUD don gian, ro rang, khong can dao tao nhieu.

## 11. KPI / Success metrics

- Ty le visitor cap quyen vi tri sau khi mo app.
- Ty le session co it nhat 1 geofence trigger thanh cong.
- So luot nghe trung binh tren moi visitor.
- Thoi luong nghe trung binh moi POI.
- So visitor unique moi ngay/tuan.
- Top POI theo luot trigger va luot nghe.
- Ty le trigger bi bo qua do cooldown/debounce.

## 12. Du lieu chinh

- Visitor: dinh danh an danh, device, ngon ngu, preference.
- POI: noi dung co ban, toa do, geofence, audio, media, muc uu tien.
- POI Translation: noi dung da ngon ngu hoa.
- Tour: goi hanh trinh va danh sach diem dung.
- Tour Stop: lien ket tour - POI theo thu tu.
- User Tracking: diem GPS theo thoi gian.
- Geofence Trigger: lich su kich hoat audio.
- Visit History: luot nghe/tham quan.
- QR Code: ma lien ket vao POI.
- Admin User: tai khoan quan tri noi bo.

## 13. Gioi han hien tai trong codebase

- Mobile app hien la Android-only va tracking chu yeu foreground.
- Chua co workflow hoan chinh de visitor bat/tat tu dong autoplay hay background tracking tu UI.
- Chua co auth, rate limit hoac phan quyen chi tiet cho API public.
- Audio playback native hien wire ro cho Android, cac nen tang khac dung TTS fallback.
- Ban do trong app va admin dang dua tren Leaflet/OpenStreetMap nhung khong co che do offline.
- PRD nay mo ta san pham theo hien trang code va huong mo rong, khong mac dinh rang tat ca muc tieu da duoc implement day du.

## 14. Roadmap de xuat

### Phase 1 - Pilot readiness

- On dinh geofence va autoplay.
- Chuan hoa bo du lieu POI, translation, audio.
- Hoan thien dashboard KPI co ban.
- Test thuc dia tai khu vuc Vinh Khanh.

### Phase 2 - Production readiness

- Them background tracking/service cho Android.
- Them logging, rate limit, auth cho API va hardening bao mat.
- Hoan thien luong QR code vao noi dung chi tiet.
- Bo sung cau hinh ngon ngu va tuy chon autoplay trong mobile UI.

### Phase 3 - Scale & productization

- Ho tro nhieu khu vuc / tenant.
- Ca nhan hoa tour va de xuat diem den.
- Ho tro offline cache.
- Xem xet phien ban iOS.

## 15. Cac cau hoi mo can chot

- San pham se uu tien auto narration hoan toan hay cho phep visitor chon manual mode tren UI?
- KPI chinh cua pilot la so luot trigger, thoi gian nghe hay chuyen doi den cac diem kinh doanh?
- Co can tach du lieu theo nhieu khu vuc / nhieu doi van hanh ngay tu giai doan tiep theo khong?
- Muc do can thiet cua privacy consent va retention policy cho du lieu tracking la gi?
- QR code se mo app, mo web hay deep-link vao man hinh POI cu the?

## 16. Tieu chi chap nhan tong quan

- Admin dang nhap duoc va quan ly POI, translation, tour, audio.
- Visitor co the bootstrap du lieu va bat tracking tren Android.
- He thong co the xac dinh POI gan nhat, kiem tra geofence va phat audio tu dong.
- Dashboard co so lieu tong quan va lich su trigger.
- Toan bo thanh phan cung doc/ghi du lieu nhat quan tren SQL Server.

