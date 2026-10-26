# SURVIVAL 01 : PUBG-Style Battle Royale (Unity + Supabase)

Dự án game sinh tồn Battle Royale 3D theo phong cách **PUBG (PlayerUnknown's Battlegrounds)** được xây dựng bằng **Unity 2022.3 LTS** kết hợp hệ thống đăng nhập, lưu trữ tài khoản, bảng xếp hạng, lịch sử đấu và danh sách phòng trực tuyến trên **Supabase**.

---

## 1. Cấu hình Supabase (`SupabaseConfig.cs`)

Thông tin kết nối Supabase đã được cấu hình sẵn tại `Assets/Scripts/Backend/SupabaseConfig.cs`:

- **`NEXT_PUBLIC_SUPABASE_URL`**: `https://ganxpdiwtwtwpvdgodbi.supabase.co`
- **`NEXT_PUBLIC_SUPABASE_PUBLISHABLE_KEY`**: `sb_publishable_bDXQCIvFgdWJ_tr_25t4ag_z8Hnos0J`

### Thiết lập Database trên Supabase Dashboard
1. Mở **Supabase SQL Editor** của project: `https://supabase.com/dashboard/project/ganxpdiwtwtwpvdgodbi/sql`
2. Copy toàn bộ nội dung file [`supabase_schema.sql`](./supabase_schema.sql) ở thư mục gốc dự án và nhấn **Run** để khởi tạo:
   - Bảng `public.profiles`: Lưu tài khoản, tên hiển thị (`display_name`), số trận (`matches_played`), số trận Top 1 (`wins`), số hạ gục (`kills`), `deaths`, `top3_finishes`, `total_damage`.
   - Bảng `public.match_history`: Lưu lịch sử từng trận đấu (`placement`, `kills`, `damage_dealt`, `survived_seconds`, `mode`).
   - Bảng `public.rooms`: Lưu danh sách phòng chờ trực tuyến (`room_code`, `host_name`, `current_players`, `max_players`, `status`).
   - Trigger `on_auth_user_created`: Tự động khởi tạo hồ sơ trong `public.profiles` ngay khi người chơi đăng ký tài khoản qua Supabase Auth.

---

## 2. Cách Chạy Game (Zero-Config)

1. Mở project trong **Unity 2022.3.62f3** (hoặc phiên bản Unity 2022.3 LTS bất kỳ).
2. Mở scene `Assets/Scenes/SampleScene.unity`.
3. Nhấn nút **Play (▶)** trên thanh công cụ Unity!
   - Hệ thống `GameBootstrapper` sử dụng `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]` sẽ tự động khởi tạo toàn bộ hệ thống (`SupabaseManager`, `BattleRoyaleMapGenerator`, `BlueZoneController`, `AirDropManager`, `MatchManager`, `SurvivalUIManager`) mà không cần kéo thả thủ công.
   - Ngoài ra có thể dùng menu trên thanh công cụ: **`Survival 01 -> Setup Current Scene (Auto-Add Game Systems)`**.

---

## 3. Tính Năng Chính Đã Hoàn Thiện

### Hệ thống Tài khoản & Đám mây (Supabase)
- **Đăng ký tài khoản mới (`Sign Up`)**: Đăng ký bằng Email hoặc Tên tài khoản + Mật khẩu + Tên hiển thị, lưu lên Supabase Auth (`/auth/v1/signup`) và bảng `public.profiles`.
- **Đăng nhập (`Sign In`)**: Xác thực qua `/auth/v1/token?grant_type=password` và tải hồ sơ người chơi từ `public.profiles`, tự động ghi nhớ phiên đăng nhập.
- **Chế độ Khách (`Guest Cloud`)**: Tạo UUID định danh duy nhất và đồng bộ trực tiếp lên bảng `public.profiles`.
- **Đổi tên nhân vật & Đồng bộ tự động**: Cập nhật tên hiển thị trực tiếp lên Supabase; tự động cộng `wins`, `kills`, `matches_played`, `top3_finishes`, `total_damage` và ghi lịch sử vào `public.match_history` ngay khi kết thúc mỗi trận đấu.
- **Bảng Xếp Hạng (`Leaderboard`) & Phòng Chờ (`Online Rooms`)**: Truy vấn trực tiếp Top 20 người chơi có nhiều Top 1 & Kills nhất từ Supabase.

### Lối chơi Sinh tồn Battle Royale (PUBG Style)
- **Giai đoạn Nhảy Dù (`Parachute Drop`)**: Bắt đầu trận đấu từ độ cao `78m`, điều khiển hướng bay bằng `WASD` (giữ `Left Shift + W` để lao nhanh xuống khu vực mong muốn).
- **Bản đồ Đảo Sinh Tồn Erangel Mini (`540m x 540m`)**: Bao gồm 6 khu vực tên tuổi:
  - **POCHINKI** (Khu phố trung tâm + Tháp bắn tỉa)
  - **SCHOOL** (Trường học phía Bắc có cầu thang lên sân thượng)
  - **MILITARY BASE** (Căn cứ quân sự phía Đông Nam nhiều trang bị cấp cao)
  - **GEORGOPOL** (Bãi công-ten-nơ phía Tây Bắc)
  - **ROZHOK** (Đồi căn cứ phía Đông Bắc)
  - **PRISON** (Khu biệt giam tường bao phía Tây Nam)
- **Hệ thống Vũ khí & Trang bị đa dạng**:
  - **M416 Tactical (AR)**, **AKM 7.62mm (AR)**, **UMP45 (SMG)**, **S1897 (Shotgun 8 viên đạn chùm)**, **Kar98k (Sniper ống ngắm 4x)**, **AWM Magnum (Sniper Hòm Thính ống ngắm 8x)**, **P1911 (Pistol)**.
  - **Mũ bảo hiểm & Giáp chống đạn (Cấp 1 / 2 / 3)**: Giảm sát thương đầu và thân (cơ chế Headshot x1.9 sát thương).
  - **Vật phẩm hồi phục & ném**: `Medkit (+55 HP)`, `Bandage (+25 HP)`, `Nước tăng lực RedZone (+45 Boost tăng tốc chạy & hồi máu tự động)`, `Lựu đạn nổ Frag Grenade`.
- **Vòng Bo Xanh (`Blue Zone`) & Hòm Thính (`AirDrop`)**:
  - Vòng bo 5 giai đoạn thu hẹp dần kèm sát thương theo giây và hiệu ứng cảnh báo khi ở ngoài bo.
  - Định kỳ thả **Hòm Thính (Supply Drop)** có dù và cột khói đỏ chứa súng ngắm **AWM** và **Mũ/Giáp Cấp 3**.

---

## 4. Phím Điều Khiển Trong Trận

| Phím | Chức năng |
| :--- | :--- |
| **W, A, S, D** | Di chuyển nhân vật / Điều hướng khi nhảy dù |
| **Left Shift** | Chạy nhanh (Sprint) / Lao dù nhanh |
| **Space** | Nhảy vượt chướng ngại vật |
| **C** | Ngồi (Crouch - giảm độ nở tâm bắn) |
| **V** | Chuyển đổi góc nhìn Thứ 3 (TPP) và Thứ 1 (FPP) |
| **Left Alt + Chuột** | Quan sát tự do xung quanh (Free Look) |
| **Chuột Trái** | Bắn súng (Fire) |
| **Chuột Phải (Giữ)** | Ngắm bắn ADS / Bật ống ngắm 4x, 8x |
| **R** | Nạp đạn (Reload) |
| **F** hoặc **E** | Nhặt Vũ khí / Giáp / Mũ / Đạn / Medkit gần nhất |
| **1, 2, 3** hoặc **Con lăn** | Đổi vũ khí giữa các ô súng |
| **4** hoặc **H** | Dùng Medkit / Bandage hồi máu |
| **5** | Uống Nước Tăng Lực (Tăng thanh Boost) |
| **G** | Ném Lựu đạn nổ (Frag Grenade) |
| **M** hoặc **Tab** | Bật / Tắt Bản đồ chiến thuật toàn đảo |
| **Esc** | Menu tạm dừng / Chỉnh độ nhạy chuột / Rời trận |
