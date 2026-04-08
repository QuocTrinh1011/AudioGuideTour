BEGIN TRANSACTION;

UPDATE Categories
SET Name = 'Phố ẩm thực',
    Description = 'Danh mục món ăn và cụm quán ăn.'
WHERE Id = 1;

UPDATE Categories
SET Name = 'Lịch sử địa phương',
    Description = 'Các điểm kể chuyện lịch sử khu vực.'
WHERE Id = 2;

UPDATE Categories
SET Name = 'Văn hóa - đời sống',
    Description = 'Nét sinh hoạt và văn hóa phố Vĩnh Khánh.'
WHERE Id = 3;

UPDATE Categories
SET Name = 'Check-in - trải nghiệm',
    Description = 'Các điểm dừng chân và chụp ảnh.'
WHERE Id = 4;

UPDATE Pois
SET Name = 'Phố ẩm thực Vĩnh Khánh',
    Summary = 'Điểm bắt đầu phố ẩm thực nổi tiếng của Quận 4.',
    Description = 'Khu phố này sáng đèn từ chiều tới đêm khuya, nổi bật với các quán ốc, món nướng và không khí ăn đêm rất sôi động.',
    Address = 'Đường Vĩnh Khánh, Phường 8, Quận 4, TP.HCM',
    TtsScript = 'Bạn đang đứng tại cửa ngõ phố ẩm thực Vĩnh Khánh. Đây là điểm hợp lý để bắt đầu tour và làm quen với không khí ăn đêm của Quận 4.'
WHERE Id = 1;

UPDATE Pois
SET Name = 'Cụm quán ốc Vĩnh Khánh',
    Summary = 'Cụm quán ốc và món ăn đêm đông khách nhất trên tuyến phố.',
    Description = 'Du khách thường dừng lại tại đây để thử ốc, hải sản, món nướng và nhiều phiên bản nước chấm đặc trưng của khu vực.',
    Address = 'Giữa phố Vĩnh Khánh, Quận 4, TP.HCM',
    TtsScript = 'Đây là cụm quán ốc tiêu biểu của Vĩnh Khánh. Nhịp phục vụ nhanh, bàn sát vỉa hè và mùi nướng tạo nên bản sắc rất riêng của khu phố này.'
WHERE Id = 2;

UPDATE Pois
SET Name = 'Trạm xe buýt Khánh Hội',
    Summary = 'Điểm vào tour bằng QR cho visitor đến bằng xe buýt.',
    Description = 'Khách có thể quét QR tại điểm này để nghe giới thiệu ngay mà không cần đợi GPS kích hoạt.',
    Address = 'Khu vực Khánh Hội, Quận 4, TP.HCM',
    TtsScript = 'Trạm xe buýt Khánh Hội là điểm vào nhanh cho tour. Nếu bạn vừa xuống xe, hãy quét QR để nghe tổng quan và bắt đầu hành trình ngay lập tức.'
WHERE Id = 3;

UPDATE Pois
SET Name = 'Trạm xe buýt Vĩnh Hội',
    Summary = 'Điểm dừng chân để vào hoặc kết thúc lộ trình tham quan.',
    Description = 'Tại đây visitor có thể quét QR, nghe tóm tắt và chọn hướng tiếp tục đi bộ vào phố ẩm thực.',
    Address = 'Khu vực Vĩnh Hội, Quận 4, TP.HCM',
    TtsScript = 'Trạm xe buýt Vĩnh Hội phù hợp làm điểm kết tour hoặc trung chuyển. Nội dung QR tại đây giúp visitor nghe nhanh mà không phụ thuộc vào vị trí GPS.'
WHERE Id = 4;

UPDATE Pois
SET Name = 'Trạm xe buýt Xuân Chiếu',
    Summary = 'Điểm QR cho visitor tiếp cận từ hướng Xuân Chiếu - Xóm Chiếu.',
    Description = 'Nội dung được kích hoạt bằng QR để visitor nghe ngay khi vừa đến khu vực bằng xe buýt.',
    Address = 'Khu vực Xuân Chiếu - Xóm Chiếu, Quận 4, TP.HCM',
    TtsScript = 'Đây là điểm vào từ hướng Xuân Chiếu, còn gọi là Xóm Chiếu. QR tại đây giúp visitor vào nội dung nhanh và không cần đợi app bật geofence.'
WHERE Id = 5;

UPDATE Pois
SET Name = 'Nhịp sống khu vực Vĩnh Khánh',
    Summary = 'Điểm kể chuyện về không khí đường phố, sinh hoạt và nhịp sống về đêm.',
    Description = 'Ngoài ẩm thực, khu vực này còn hấp dẫn nhờ sự nhộn nhịp của người bán, khách đi bộ và không gian sinh hoạt sát nhau trên vỉa hè.',
    Address = 'Trục đường Vĩnh Khánh, Quận 4, TP.HCM',
    TtsScript = 'Điểm này giúp visitor hiểu thêm về đời sống phố phường ở Quận 4. Không chỉ có món ăn, Vĩnh Khánh còn là nơi thể hiện nhịp sống và văn hóa giao tiếp rất riêng.'
WHERE Id = 6;

UPDATE Tours
SET Name = 'Đêm Vĩnh Khánh 45 phút',
    Description = 'Lộ trình demo đi bộ từ trạm xe buýt đến phố ẩm thực và các điểm nhịp sống khu Vĩnh Khánh.'
WHERE Id = 1;

UPDATE TourStops SET Note = 'Điểm vào tour bằng QR hoặc tự chọn.' WHERE Id = 1;
UPDATE TourStops SET Note = 'Tổng quan về phố ẩm thực.' WHERE Id = 2;
UPDATE TourStops SET Note = 'Giới thiệu cụm quán ốc và món ăn đêm.' WHERE Id = 3;
UPDATE TourStops SET Note = 'Kể chuyện về không khí và nhịp sống về đêm.' WHERE Id = 4;
UPDATE TourStops SET Note = 'Điểm kết tour và định hướng di chuyển tiếp.' WHERE Id = 5;

UPDATE QRCodes SET Note = 'Điểm dừng xe buýt phường Khánh Hội' WHERE Code = 'BUS-KH-001';
UPDATE QRCodes SET Note = 'Điểm dừng xe buýt phường Vĩnh Hội' WHERE Code = 'BUS-VH-002';
UPDATE QRCodes SET Note = 'Điểm dừng xe buýt phường Xuân Chiếu / Xóm Chiếu' WHERE Code = 'BUS-XC-003';

UPDATE Visitors
SET DisplayName = 'Khách ẩn danh'
WHERE DisplayName IN ('Khach an danh', 'Khách ẩn danh');

UPDATE Visitors
SET DisplayName = 'Khách demo'
WHERE DisplayName IN ('Khach demo', 'Khách demo');

COMMIT;
