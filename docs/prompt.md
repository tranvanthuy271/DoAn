# PROMPT VIẾT BÁO CÁO ĐỒ ÁN TỐT NGHIỆP
# Đề tài: "Phát triển trò chơi Mutants Arena với hệ thống tiến hoá gene bằng Unity"

---

## VAI TRÒ VÀ NHIỆM VỤ

Bạn là một giảng viên hướng dẫn đồ án tốt nghiệp ngành Công nghệ Thông tin, đồng thời là technical writer chuyên viết báo cáo học thuật chuẩn đại học Việt Nam.

Hãy viết **TOÀN BỘ** một chương (hoặc phần được chỉ định) của báo cáo đồ án tốt nghiệp cho đề tài trên. Báo cáo hoàn chỉnh có độ dài **khoảng 100–120 trang Word**.

---

## THÔNG TIN DỰ ÁN

| Mục | Nội dung |
|-----|----------|
| Tên game | Mutants Arena |
| Engine | Unity (C#) |
| Thể loại | Multiplayer Arena Mutation Battle (PvP + PvE) |
| Server | Dedicated Server (Unity Netcode for GameObjects) |
| Backend API | Node.js / ASP.NET Core REST API |
| Database | MySQL |
| Networking | Unity Netcode, WebSocket, JSON packet |
| AI | NavMesh Agent, State Machine, ScriptableObject |
| Phong cách | Sci-fi mutation futuristic arena |

**Gameplay core:**
- Người chơi chiến đấu trong đấu trường theo thời gian thực
- Thu thập gene từ quái vật / chiến thắng trận đấu
- Tiến hoá nhân vật (mutation) thay đổi chỉ số + ngoại hình
- Nâng cấp skill, trang bị, buff/debuff
- PvP matchmaking, PvE dungeon wave
- Hệ thống chỉ số: ATK, DEF, HP, SPD, CRIT, gene multiplier

---

## YÊU CẦU VĂN PHONG & TRÌNH BÀY

1. **Văn phong học thuật**, chuyên nghiệp, đúng chuẩn báo cáo tốt nghiệp đại học Việt Nam.
2. **Viết cực kỳ chi tiết** — không viết sơ sài, không liệt kê gạch đầu dòng thay cho văn xuôi học thuật.
3. Mỗi đoạn văn phải có **ít nhất 3–5 câu** giải thích rõ ràng, có luận điểm và dẫn chứng kỹ thuật cụ thể.
4. **Các chương liên kết logic**: kết thúc mỗi chương bằng đoạn "Kết luận chương" tóm tắt và dẫn sang chương tiếp theo.
5. Viết như **sinh viên đã thực sự thực hiện dự án 6 tháng**, không phải mô tả lý thuyết chung chung.
6. Khi đề cập đến kỹ thuật: giải thích **tại sao** chọn giải pháp đó, không chỉ mô tả nó là gì.

---

## QUY TẮC VỀ HÌNH VẼ VÀ SƠ ĐỒ

**QUAN TRỌNG:** Không cần render ảnh thật. Tại mỗi vị trí cần hình, hãy viết theo mẫu sau:

```
Hình X.Y: [Tên hình]

Mô tả render:
- Loại hình: [UML / Wireframe / Gameplay screenshot / Flowchart / ERD / Sequence Diagram]
- Bố cục tổng thể: [mô tả layout chính]
- Thành phần chính: [liệt kê các object/node/component]
- Màu sắc: [palette màu chính]
- Style: [flat design / sci-fi / blueprint]
- Chi tiết kỹ thuật: [quan hệ giữa các object, mũi tên, nhãn, chú thích]
```

**Ví dụ chuẩn:**

```
Hình 2.3: Biểu đồ Use Case tổng quát hệ thống Mutants Arena

Mô tả render:
- Loại hình: UML Use Case Diagram
- Bố cục: System boundary hình chữ nhật lớn tiêu đề "Mutants Arena System" ở trung tâm
- Actors: "Người chơi" (bên trái), "Admin" (bên phải), "AI Enemy System" (bên dưới phải)
- Use cases trong boundary (oval): Đăng nhập, Chọn nhân vật, Vào trận PvP, Vào dungeon PvE,
  Tiến hoá gene, Nâng cấp skill, Quản lý trang bị, Xem bảng xếp hạng, Quản lý hệ thống (Admin)
- Màu sắc: nền trắng, boundary xanh navy, oval xanh nhạt, actor đen
- Mũi tên: association thực nét từ Actor đến Use Case; include/extend nét đứt có label
```

---

## CẤU TRÚC TOÀN BÁO CÁO

### PHẦN ĐẦU (Trang bìa → Lời nói đầu)

- Trang bìa: tên trường/khoa, tên đề tài, sinh viên thực hiện, giảng viên hướng dẫn, năm học
- Lời cảm ơn (~0.5 trang)
- Lời cam đoan (~0.5 trang)
- Mục lục (đánh số đến 3 cấp)
- Danh mục ký hiệu và viết tắt (bảng 2 cột: Ký hiệu | Ý nghĩa) — bao gồm: Unity, NavMesh, NPC, AI, PvP, PvE, REST, API, ERD, UML, HP, ATK, DEF, SPD, CRIT, CCU, FPS, JWT, JSON, RPC, UI, UX
- Danh mục hình vẽ (Hình 1.1 đến Hình 3.x)
- Danh mục bảng (Bảng 1.1 đến Bảng 3.x)
- Lời nói đầu (~1 trang — đặt vấn đề, lý do chọn đề tài, đóng góp kỳ vọng, bố cục báo cáo)

---

### CHƯƠNG 1: TỔNG QUAN VỀ GAME VÀ CÔNG NGHỆ (~25 trang)

> **Lưu ý cấu trúc chương:** Theo đúng chuẩn báo cáo học thuật, chương này phải đi theo trình tự:
> **Bối cảnh hiện trạng → Vấn đề tồn tại → Tổng quan công nghệ → Khả năng ứng dụng → Mới đặt vấn đề và lý do chọn đề tài.**
> Không được đưa lý do chọn đề tài lên trước khi đã trình bày đủ bối cảnh.

**1.1. Tổng quan ngành công nghiệp game**

Viết chi tiết 4–5 đoạn văn học thuật về BỐI CẢNH HIỆN TẠI (viết trước, chưa đặt vấn đề):
- Quy mô và tốc độ tăng trưởng thị trường game toàn cầu (số liệu Newzoo/Statista: doanh thu, số người chơi)
- Thị trường game Việt Nam: tốc độ tăng trưởng, số lượng game thủ, doanh thu, xu hướng
- Phân khúc game theo thể loại: MOBA, RPG, Battle Royale, simulation, mutation/evolution
- Xu hướng game multiplayer online: từ LAN → online → cloud gaming
- Xu hướng cụ thể về game có cơ chế tiến hoá nhân vật / thu thập / mutation ngày càng phổ biến

Hình 1.1: Biểu đồ doanh thu thị trường game toàn cầu 2019–2024

Mô tả render:
- Loại hình: Grouped bar chart
- Trục X: năm 2019, 2020, 2021, 2022, 2023, 2024
- Trục Y: doanh thu tỷ USD (0–250)
- 3 nhóm bar: Mobile (xanh lá), PC/Console (xanh dương), Cloud (tím nhạt)
- Label giá trị trên đỉnh mỗi bar
- Tiêu đề: "Doanh thu thị trường game toàn cầu 2019–2024"

**1.2. Tổng quan về game mutation/evolution và thực trạng các game hiện có**

Viết 2–3 đoạn giới thiệu lịch sử và hiện trạng của thể loại game mutation/evolution, sau đó phân tích từng game:

*1.2.1. Lịch sử phát triển thể loại game mutation/evolution*

Viết 2 đoạn: từ Tamagotchi → Pokemon → Spore → các game mobile breeding → TFT synergy → hiện đại. Giai đoạn nào xuất hiện cơ chế gì mới.

*1.2.2. Phân tích chi tiết các game tiêu biểu*

Viết 2–3 đoạn văn MÔ TẢ chi tiết từng game (cơ chế hoạt động, cách tiến hoá, hệ thống multiplayer):
- **Pokemon** — cơ chế tiến hoá level-based, combat turn-based 1v1, collection 151+ loài; đã có từ 1996
- **Spore** — tùy biến DNA tự do từ microbe → creature → civilization, sandbox nhưng multiplayer hạn chế
- **Teamfight Tactics (TFT)** — synergy mutation giữa các champion, round-based PvP 8 người, meta thay đổi theo patch
- **Monster Legends** — breeding 2 monster tạo offspring mới, PvP asynchronous, heavily monetized
- **Diablo Immortal / Path of Exile** — character build evolution qua item, skill tree sâu, co-op/PvP online

Bảng 1.1: Bảng so sánh chi tiết các game mutation/evolution hiện có

| Game | Nam ra | The loai | Co che Evolution | Real-time Combat | Multiplayer | Nen tang | Uu diem noi bat | Han che chinh |
|------|--------|----------|-----------------|-----------------|-------------|----------|----------------|--------------|
| Pokemon | 1996 | RPG Collection | Level-based, 3 giai doan | Khong (turn-based) | Co-op/PvP | Multi | Da dang, thuong hieu manh | Combat cham, thieu real-time |
| Spore | 2008 | Sandbox | DNA tuy bien hoan toan | Khong | Han che | PC | Sang tao cao nhat | Multiplayer yeu |
| TFT | 2019 | Auto Chess | Synergy tier list | Khong (auto-battle) | PvP 8 nguoi | PC/Mobile | Meta sau, can bang tot | Khong co character growth ca nhan |
| Monster Legends | 2013 | Idle/RPG | Breeding offspring | Khong (auto) | PvP async | Mobile | De tiep can | Pay-to-win nang |
| Mutants Arena | 2024 | Arena Battle | Gene real-time mutation | Co - real-time | PvP+PvE | PC | Ket hop real-time + gene evolution | Dang phat trien |

**1.3. Vấn đề tồn tại trong các game mutation/evolution hiện nay**

> **[ĐÂY LÀ PHẦN QUAN TRỌNG — Phân tích vấn đề DẪN ĐẾN lý do chọn đề tài, viết TRƯỚC khi đặt vấn đề]**

Viết ÍT NHẤT 4–5 đoạn văn phân tích kỹ từng vấn đề tồn tại. Mỗi vấn đề cần có phần "Biểu hiện" và "Hệ quả" riêng biệt (theo cấu trúc báo cáo mẫu).

*1.3.1. Hệ thống tiến hoá tách rời khỏi combat thời gian thực*

Biểu hiện:
- Hầu hết game evolution hiện tại (Pokemon, Monster Legends) sử dụng combat turn-based hoặc auto-battle — người chơi không trực tiếp điều khiển trong trận
- Quá trình tiến hoá diễn ra ngoài trận đấu, không ảnh hưởng trực tiếp đến gameplay trong thời gian thực
- Mutation/evolution chỉ là số liệu tĩnh, không tạo cảm giác nhân vật "thực sự biến đổi" trước mắt người chơi

Hệ quả: Trải nghiệm của người chơi bị phân mảnh — gameplay đánh nhau và hệ thống tiến hoá cảm giác như hai trò chơi riêng biệt, giảm sự gắn kết và hứng thú dài hạn.

*1.3.2. Thiếu sự kết hợp giữa multiplayer real-time và gene evolution sâu*

Biểu hiện:
- Game có real-time combat tốt (MOBA, Battle Royale) lại không có hệ thống gene evolution đủ sâu
- Game có hệ thống evolution phong phú (Pokemon, Spore) lại không hỗ trợ real-time multiplayer mượt mà
- Khoảng trống rõ ràng: chưa có game kết hợp được real-time combat multiplayer và deep gene evolution trong cùng một sản phẩm

Hệ quả: Người chơi yêu thích cả hai cơ chế buộc phải chọn một trong hai — thị trường có nhu cầu thực sự nhưng chưa có sản phẩm đáp ứng.

*1.3.3. Mô hình kinh tế và cân bằng gameplay chưa công bằng*

Biểu hiện:
- Pay-to-win phổ biến trong phân khúc mobile: Monster Legends và nhiều game tương tự cho phép mua gene/skill cao cấp bằng tiền thật
- ELO/matchmaking chưa tính đến gene tier của nhân vật, người mới dễ bị ghép với người chơi lâu năm có gene S-tier
- Hệ số nhân từ gene cấp cao không có giới hạn hợp lý khiến meta bị thống trị bởi một vài build duy nhất

Hệ quả: Trải nghiệm không công bằng làm mất người chơi mới, cộng đồng bị phân chia theo tầng lớp kinh tế, vòng đời sản phẩm rút ngắn.

*1.3.4. Thiếu chiều sâu chiến lược trong hệ thống gene và tính cá nhân hoá*

Biểu hiện:
- Nhiều game chỉ có 1–2 loại gene/trang bị với ít sự đa dạng trong cách xây dựng nhân vật
- Cơ chế loot ngẫu nhiên không có hệ thống craft/combine — người chơi không chủ động được build
- Thiếu gene synergy system tạo chiều sâu cho team composition trong chế độ multiplayer

Hệ quả: Gameplay nhanh nhàm chán vì thiếu lý do để người chơi quay lại thử build mới, retention thấp.

Bảng 1.2: Tổng hợp vấn đề tồn tại trong các game mutation/evolution hiện nay

| Van de | Bieu hien cu the | Game mac phai | He qua |
|--------|-----------------|--------------|--------|
| Evolution tach roi combat | Turn-based hoac auto-battle | Pokemon, Monster Legends | Trai nghiem phan manh |
| Thieu real-time + evolution | Phai chon mot trong hai | Tat ca game hien tai | Nhu cau chua duoc dap ung |
| Pay-to-win | Mua gene/skill bang tien that | Monster Legends, nhieu mobile | Mat cong bang, mat nguoi choi |
| Gene depth nong | It loai gene, khong co synergy | Hau het game | Nham chan, thieu chieu sau |
| Matchmaking kem | Khong can gene tier | Nhieu game PvP | Trai nghiem khong cong bang |

**1.4. Tổng quan về Unity Engine**

Sau khi đã trình bày vấn đề tồn tại, phần này giới thiệu công nghệ được lựa chọn để giải quyết những vấn đề trên:

*1.4.1. Kiến trúc Unity*

Viết 3–4 đoạn chi tiết về: Scene/GameObject/Component pattern, MonoBehaviour lifecycle (Awake→Start→Update→FixedUpdate→LateUpdate), Unity Event System, tại sao kiến trúc component-based phù hợp cho hệ thống gene modular.

Hình 1.2: Sơ đồ kiến trúc Unity Engine

Mô tả render:
- Loại hình: Layered diagram dọc
- Từ trên xuống: Application Layer (Scene/GameObject), Component Layer (MonoBehaviour, ScriptableObject), Engine Core (Rendering, Physics, Audio, Animation, Input), Platform Abstraction
- Mũi tên hai chiều giữa các layer
- Highlight Component Layer vì đây là layer quan trọng nhất cho hệ thống gene
- Màu: mỗi layer một màu khác nhau (xanh, xanh nhạt, cam, xám)

*1.4.2. Unity Netcode for GameObjects*

Viết 2–3 đoạn về: NetworkManager, NetworkObject, NetworkBehaviour, NetworkVariable<T>, ServerRpc/ClientRpc — và tại sao phù hợp để đồng bộ trạng thái gene/combat real-time giữa các client.

Hình 1.3: Sơ đồ luồng NetworkVariable synchronization

Mô tả render:
- Loại hình: Sequence diagram
- Participants: Client A (Owner), Server, Client B (Observer)
- Luong: Client A thay doi gia tri NetworkVariable → gui ServerRpc → Server validate → cap nhat NetworkVariable.Value → broadcast ClientRpc → Client B nhan update
- Màu: Client A xanh lá, Server đỏ, Client B xanh dương; mũi tên có nhãn method name

*1.4.3. NavMesh và AI Pathfinding*

Viết 2 đoạn về NavMesh baking, NavMeshAgent parameters (speed, acceleration, stopping distance, obstacle avoidance).

Hình 1.4: NavMesh baked trên dungeon map (vùng walkable xanh, obstacle đỏ, góc nhìn từ trên xuống)

*1.4.4. Animator và Animation State Machine*

Viết 2 đoạn về Animator Controller, Blend Tree cho movement, Avatar Mask cho upper/lower body split.

Hình 1.5: Animator State Machine nhân vật (states: Idle, Run, Attack, Hit, Die — với transition conditions)

*1.4.5. ScriptableObject Pattern*

Viết 2 đoạn: ScriptableObject là data container cho GeneData, SkillData, ItemData, WaveConfig — tách biệt data khỏi logic, không phụ thuộc scene, dễ thêm gene/skill mới mà không cần sửa code.

Hình 1.6: Sơ đồ quan hệ ScriptableObject với MonoBehaviour Manager

Mô tả render:
- Loại hình: UML-style dependency diagram
- ScriptableObject assets (màu vàng): GeneData, SkillData, ItemData, WaveConfig
- MonoBehaviour Managers (màu xanh): GeneManager, SkillController, InventoryManager, DungeonManager
- Quan he: moi Manager references mot List<ScriptableObject> tuong ung (dependency arrow)
- Lợi ích ghi chú bên cạnh: "De them moi", "Khong coupling", "Inspector editable"

**1.5. Tổng quan công nghệ Networking và Backend**

*1.5.1. So sánh Game Engine và lý do chọn Unity*

Bảng 1.3: So sánh Unity vs Unreal Engine vs Godot

| Tieu chi | Unity | Unreal Engine | Godot |
|----------|-------|---------------|-------|
| Ngon ngu lap trinh | C# | C++/Blueprint | GDScript/C# |
| Do hoa | Tot | Rat tot | Kha |
| Ho tro Multiplayer | Tot (Netcode) | Tot | Co ban |
| Tai lieu & cong dong | Rat phong phu | Phong phu | Dang phat trien |
| Chi phi | Mien phi/Pro | Royalty 5% | MIT |
| Phu hop indie/sinh vien | Rat tot | Trung binh | Tot |

Viết 2–3 đoạn giải thích lý do chọn Unity: C# quen thuộc, Netcode tích hợp sẵn, NavMesh AI built-in, Asset Store phong phú, cộng đồng Việt Nam lớn, phù hợp quy mô đề tài.

*1.5.2. So sánh giải pháp Networking*

Bảng 1.4: So sánh các giải pháp Networking cho Unity

| Giai phap | Kien truc | Latency | Chi phi | Phu hop du an |
|----------|-----------|---------|---------|--------------|
| Unity Netcode | Client-Server | Thap | Mien phi | Tot nhat |
| Photon PUN | Relay | Trung binh | Freemium | Tot cho mobile |
| Mirror | Client-Server | Thap | Mien phi | Linh hoat |
| Custom Socket | Tuy chinh | Rat thap | Dev cost cao | Game lon |

*1.5.3. Backend API và Database*

Viết 2–3 đoạn: REST API (Node.js/Express), JWT authentication flow, MySQL cho game data (lý do chọn RDBMS thay NoSQL: transaction đảm bảo tính toàn vẹn gold/gene khi evolve), JSON WebSocket packet format cho combat sync.

**1.6. Khả năng ứng dụng công nghệ vào phát triển game mutation multiplayer**

> **[Mục này kết nối công nghệ với vấn đề đã nêu ở 1.3 — trả lời câu hỏi "Công nghệ này giải quyết vấn đề đã nêu như thế nào?"]**

Viết 3–4 đoạn văn học thuật phân tích từng vấn đề ở mục 1.3 được giải quyết ra sao nhờ Unity và các công nghệ đã trình bày:

- **Van de 1 — Evolution tach roi combat:** Unity Netcode cho phep NetworkVariable<int> sync HP, stat, gene multiplier real-time duoi 50ms — nguoi choi thay nhan vat thay doi stat ngay lap tuc trong tran.
- **Van de 2 — Thieu real-time + evolution:** Unity ket hop CharacterController (combat) + GeneManager (mutation) trong cung mot scene, dong bo qua Dedicated Server — giai quyet khoang trong chua game nao lam duoc.
- **Van de 3 — Pay-to-win:** Server-side validation moi gene evolve request + ELO matchmaking tinh gene tier — dam bao khong the cheat, ghep tran cong bang.
- **Van de 4 — Gene depth nong:** ScriptableObject cho phep thiet ke 5 gene type x 7 tier + synergy system ma khong can sua code — de mo rong va can bang.

Hình 1.7: Sơ đồ mapping Vấn đề → Giải pháp công nghệ

Mô tả render:
- Loại hình: 2-column mapping diagram
- Cột trái (đỏ nhạt): 4 vấn đề tồn tại (hình chữ nhật bo góc)
- Cột phải (xanh nhạt): Giải pháp công nghệ tương ứng
- Mũi tên từ trái sang phải, màu cam, có nhãn kỹ thuật ngắn gọn
- Tiêu đề: "Mapping van de ton tai → Giai phap ky thuat trong Mutants Arena"

**1.7. Đặt vấn đề và lý do chọn đề tài**

> **[Phần này viết SAU KHI đã trình bày đủ bối cảnh (1.1), thực trạng game (1.2), vấn đề tồn tại (1.3), công nghệ (1.4–1.5), và khả năng ứng dụng (1.6). Đây mới là lúc đặt vấn đề và giới thiệu đề tài.]**

Viết 4–5 đoạn văn học thuật đặt vấn đề theo trình tự:

1. **Đoạn 1 — Kết nối từ vấn đề:** "Từ những phân tích trên, có thể thấy rõ khoảng trống lớn trong thị trường game..." — tổng kết vấn đề đã nêu.
2. **Đoạn 2 — Cơ hội và tính khả thi:** Công nghệ Unity + Netcode đã đủ trưởng thành để giải quyết, nhóm tác giả đã nghiên cứu và thấy khả thi trong phạm vi đề tài tốt nghiệp.
3. **Đoạn 3 — Giới thiệu đề tài:** "Xuất phát từ những lý do đó, đề tài 'Phát triển trò chơi Mutants Arena với hệ thống tiến hoá gene bằng Unity' được đề xuất nhằm..."
4. **Đoạn 4 — Mục tiêu cụ thể:** Liệt kê 4–5 mục tiêu đo lường được (FPS >= 60, latency < 100ms, >= 5 gene type, >= 3 che do choi, >= 100 CCU).
5. **Đoạn 5 — Phạm vi và phương pháp:** PC platform, Agile iteration cá nhân, thời gian 6 tháng, phạm vi cụ thể (3 loại nhân vật, tối đa 100 CCU, các module chính).

**Kết luận chương 1**

Viết 2 đoạn: (1) Tóm tắt những gì đã trình bày trong chương — bối cảnh ngành, thực trạng game mutation, vấn đề tồn tại cụ thể, công nghệ được chọn và lý do. (2) Dẫn sang Chương 2: "Trên cơ sở phân tích trên, Chương 2 sẽ tiến hành phân tích chi tiết yêu cầu hệ thống và thiết kế kiến trúc toàn diện cho trò chơi Mutants Arena...\"

---

### CHƯƠNG 2: PHÂN TÍCH VÀ THIẾT KẾ HỆ THỐNG (~35 trang)

**2.1. Phân tích bài toán**

Viết 3–4 đoạn: bài toán thiết kế game multiplayer arena có gene evolution phức tạp, thách thức kỹ thuật đồng bộ real-time và cân bằng gameplay.

**2.2. Yêu cầu hệ thống**

*2.2.1. Yêu cầu chức năng*

Viết chi tiết từng chức năng theo nhóm (mỗi chức năng 2–3 câu):

- **Nhóm Tài khoản:** Đăng ký, Đăng nhập JWT, Quản lý hồ sơ
- **Nhóm Nhân vật & Gene:** Chọn nhân vật, Thu thập gene, Trang bị gene, Tiến hoá mutation
- **Nhóm Skill & Trang bị:** Nâng cấp skill, Quản lý inventory, Buff/debuff
- **Nhóm Combat:** PvP 1v1/2v2 real-time, PvE dungeon wave, Damage system
- **Nhóm Xã hội:** Bạn bè, Party, Chat (global/party/whisper)
- **Nhóm Hệ thống:** Matchmaking ELO, Leaderboard season, Admin dashboard

*2.2.2. Yêu cầu phi chức năng*

Bảng 2.1: Bảng yêu cầu phi chức năng

| Thuộc tính | Yêu cầu cụ thể | Cách đo lường |
|-----------|---------------|---------------|
| Hiệu năng | FPS ≥ 60 (PC GTX 1060) | Unity Profiler |
| Độ trễ mạng | RTT < 100ms cùng khu vực | Ping test |
| Chịu tải | ≥ 100 CCU đồng thời | Stress test |
| Ổn định | Uptime ≥ 99%, auto-reconnect | Monitor log |
| Bảo mật | Không có lỗi OWASP Top 10 | Security audit |
| Mở rộng | Dễ thêm gene/skill qua ScriptableObject | Code review |

*2.2.3. Yêu cầu bảo mật*

Viết 2–3 đoạn: JWT mọi API quan trọng, server-side validation damage/evolve, rate limiting 60 req/phút, bcrypt salt=12, SQL parameterized query.

**2.3. Phân tích tác nhân và Use Case**

*2.3.1. Tác nhân tham gia*

Bảng 2.2: Bảng các tác nhân

| Tên tác nhân | Vai trò | Quyền hạn |
|-------------|---------|-----------|
| Người chơi (Player) | Tham gia trận đấu, quản lý nhân vật | Toàn bộ gameplay |
| Quản trị viên (Admin) | Vận hành, giám sát hệ thống | Quản lý user, reward, ban, log |
| AI Enemy System | Kẻ thù PvE tự động | NavMesh + State Machine |

*2.3.2. Biểu đồ Use Case tổng quát*

Hình 2.1: Use Case tổng quát hệ thống Mutants Arena

Mô tả render:
- Loại hình: UML Use Case Diagram chuẩn
- System boundary: hình chữ nhật tiêu đề "Mutants Arena System"
- Actors: "Người chơi" (trái), "Admin" (phải), "AI Enemy" (dưới phải ngoài boundary)
- Use cases: Đăng ký/Đăng nhập, Chọn nhân vật, Trang bị gene, Tiến hoá gene, Vào trận PvP, Vào dungeon PvE, Nâng cấp skill, Quản lý inventory, Xem leaderboard, Quản lý bạn bè/party, Chat, Quản lý hệ thống (Admin)
- Màu: boundary xanh navy, oval xanh nhạt, actor đen, mũi tên association đen

*2.3.3. Use Case chi tiết và bảng đặc tả*

Viết đầy đủ Hình + Bảng đặc tả cho ÍT NHẤT 10 use case theo mẫu:

Bảng 2.3: Bảng đặc tả Use Case Đăng nhập

| Mục | Nội dung |
|-----|----------|
| Use Case | Đăng nhập |
| Actor | Người chơi |
| Mô tả | Người chơi nhập thông tin để vào hệ thống và nhận JWT token |
| Điều kiện tiên quyết | Đã có tài khoản; server hoạt động |
| Luồng chính | 1. Mở màn hình Login → 2. Nhập username + password → 3. Nhấn Đăng nhập → 4. POST /api/auth/login → 5. Server validate → 6. Nhận JWT → 7. Vào Lobby |
| Luồng phụ | Sai mật khẩu → báo lỗi 401; Server timeout → retry 3 lần |
| Kết quả | Đăng nhập thành công, truy cập Lobby |

*(Thực hiện tương tự cho: Đăng ký, Tiến hoá gene, PvP matchmaking, Vào dungeon PvE, Sử dụng skill, Nâng cấp trang bị, Thêm bạn bè, Admin ban user, Xem leaderboard)*

**2.4. Phân tích gameplay và hệ thống core**

*2.4.1. Gameplay loop chính*

Hình 2.2: Flowchart gameplay loop tổng quát

Mô tả render:
- Loại hình: Flowchart
- Luồng: START → Đăng nhập → Main Lobby → [Nhánh PvP: Matchmaking→Combat→Result→Loot Gene] → [Nhánh PvE: Chọn Dungeon→Wave→Boss→Clear Reward] → [Nhánh Quản lý: Gene Equip/Evolve→Skill Upgrade→Inventory] → Quay về Lobby
- Màu: decision (diamond) vàng, action (rectangle) xanh dương, terminal oval xanh lá

*2.4.2. Hệ thống tiến hoá gene*

Viết 4–5 đoạn chi tiết:
- 5 loại gene: Attack, Defense, Speed, Crit, Special
- 7 tier: F < E < D < C < B < A < S
- Điều kiện tiến hoá: 3 gene cùng loại cùng tier + gold
- Mutation random bonus: 5% cơ hội thêm hiệu ứng đặc biệt khi evolve

Bảng 2.4: Gene tier, điều kiện và stat multiplier

| Tier | Số gene cần | Gold cost | Stat multiplier | Chance bonus effect |
|------|------------|-----------|-----------------|---------------------|
| F→E | 3×F | 100 | +5% | 5% |
| E→D | 3×E | 300 | +10% | 5% |
| D→C | 3×D | 1,000 | +15% | 5% |
| C→B | 3×C | 3,000 | +20% | 5% |
| B→A | 3×B | 10,000 | +30% | 5% |
| A→S | 3×A | 50,000 | +50% | 5% |

Hình 2.3: Gene Evolution Tree diagram

Mô tả render:
- Loại hình: Tree diagram trái → phải
- Nodes: hình tròn cho mỗi tier, màu gradient xám(F)→trắng→vàng→xanh→tím→đỏ→vàng kim(S)
- Cạnh nối: mũi tên với label "3× + Gold"
- Icon stat bonus bên cạnh mỗi node (+5%, +10%...)
- Background: đen sci-fi với particle nhỏ
- Style: futuristic dark UI

*2.4.3. Công thức damage và combat*

Viết 3–4 đoạn giải thích:

Công thức damage:
DMG = BASE_ATK × GeneMult × SkillMult × (1 - DEF/(DEF+200)) × CritMult

Bảng 2.5: Hệ số SkillMult theo loại skill

| Skill Type | SkillMult | Cooldown | MP Cost |
|-----------|-----------|----------|---------|
| Basic Attack | 1.0× | 0s | 0 |
| Heavy Strike | 1.8× | 3s | 20 |
| Skill Shot | 2.5× | 6s | 40 |
| Ultimate | 3.5× | 20s | 80 |

Hình 2.4: Combat State Machine nhân vật

Mô tả render:
- Loại hình: UML State Diagram
- States: Idle, Running, Attacking, Casting, Hit, Stunned, Dead
- Transitions với điều kiện rõ ràng trên mỗi mũi tên
- Màu: states xanh nhạt, Dead state đỏ nhạt, border đậm cho active state

*2.4.4. AI Enemy State Machine*

Hình 2.5: AI Enemy State Machine

Mô tả render:
- Loại hình: UML State Diagram
- States: Spawn, Idle, Patrol, Alert, Chase, Attack, Cooldown, Retreat, Dead
- Transitions: Spawn→Idle→Patrol→Alert(detect range)→Chase→Attack(attack range)→Cooldown→Chase; HP<20%→Retreat; HP=0→Dead
- Màu: Idle/Patrol xanh olive, Alert/Chase cam, Attack đỏ, Dead xám

**2.5. Thiết kế kiến trúc hệ thống**

*2.5.1. Kiến trúc tổng thể*

Hình 2.6: System Architecture 3 tầng

Mô tả render:
- Loại hình: Layered Architecture Diagram
- Tầng 1 — Client: "Unity Client (PC)" với UI Manager, Game Manager, Network Manager, Audio Manager
- Tầng 2 — Server: "Game Server (Unity Dedicated)" (Netcode, Physics, AI, Match Session) và "Backend API Server" (REST, JWT, Business Logic)
- Tầng 3 — Data: "MySQL Database" và "File Storage"
- Mũi tên: Client↔Game Server (WebSocket), Client↔Backend (HTTP/REST), Server↔DB (SQL)
- Màu: Client xanh nhạt, Server xanh đậm, Data xám

*2.5.2. Networking packet flow*

Bảng 2.6: Bảng các loại packet networking

| Packet Type | Direction | Trigger | Payload (JSON fields) |
|------------|-----------|---------|----------------------|
| PlayerMove | C→S | Input change | playerId, pos, rot, timestamp |
| PlayerAttack | C→S | Attack input | playerId, targetId, skillId |
| DamageResult | S→C | Server validation | targetId, damage, isCrit, hpRemain |
| PlayerDead | S→All | HP=0 | deadId, killerId |
| MatchStart | S→All | All ready | matchId, mapId, players[] |
| MatchEnd | S→All | Win condition | winnerId, stats[] |
| GeneEvolve | C→API | Evolve request | charId, geneId, fromTier |
| LoginToken | API→C | Login success | token, expiry, playerId |

*2.5.3. Thiết kế Database (ERD)*

Hình 2.7: ERD hệ thống Mutants Arena

Mô tả render:
- Loại hình: ERD (crow's foot notation)
- Entities: players, characters, genes, player_genes, character_genes, skills, character_skills, items, inventories, matches, match_players, dungeons, dungeon_runs, friendships, chat_messages, leaderboard
- Quan hệ đầy đủ với cardinality (1-*, *-*, 1-1)
- PK gạch chân, FK in nghiêng
- Màu: entity header xanh navy, attribute background trắng

Bảng 2.7: Database Schema — Bảng `players`

| Cột | Kiểu | Constraint | Mô tả |
|-----|------|-----------|-------|
| id | INT | PK, AUTO_INCREMENT | ID người chơi |
| username | VARCHAR(50) | UNIQUE, NOT NULL | Tên đăng nhập |
| email | VARCHAR(100) | UNIQUE, NOT NULL | Email |
| password_hash | VARCHAR(255) | NOT NULL | Bcrypt hash |
| level | INT | DEFAULT 1 | Cấp độ |
| exp | BIGINT | DEFAULT 0 | Điểm kinh nghiệm |
| gold | BIGINT | DEFAULT 1000 | Tiền tệ game |
| rating | INT | DEFAULT 1000 | ELO rating |
| is_banned | TINYINT(1) | DEFAULT 0 | Trạng thái ban |
| created_at | DATETIME | DEFAULT NOW() | Ngày đăng ký |
| last_login | DATETIME | | Lần đăng nhập cuối |

*(Viết tương tự schema đầy đủ cho: characters, genes, player_genes, character_genes, skills, items, matches, match_players, dungeons, dungeon_runs, friendships, leaderboard)*

*2.5.4. Thiết kế REST API*

Bảng 2.8: Danh sách API endpoints

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| POST | /api/auth/register | No | Đăng ký tài khoản |
| POST | /api/auth/login | No | Đăng nhập, nhận JWT |
| GET | /api/player/profile | JWT | Hồ sơ người chơi |
| GET | /api/character/list | JWT | Danh sách nhân vật |
| POST | /api/gene/equip | JWT | Trang bị gene |
| POST | /api/gene/evolve | JWT | Tiến hoá gene |
| POST | /api/match/join | JWT | PvP Matchmaking |
| GET | /api/leaderboard | JWT | Bảng xếp hạng |
| GET | /api/dungeon/list | JWT | Danh sách dungeon |
| POST | /api/dungeon/start | JWT | Bắt đầu dungeon |
| POST | /api/dungeon/complete | JWT | Hoàn thành dungeon |
| GET | /api/friend/list | JWT | Danh sách bạn bè |
| POST | /api/friend/add | JWT | Thêm bạn bè |
| GET | /api/admin/players | JWT+Admin | Quản lý user |
| POST | /api/admin/ban | JWT+Admin | Ban người chơi |

*2.5.5. Unity Client Architecture*

Hình 2.8: Scene Management flowchart

Mô tả render:
- Scenes: BootScene→LoginScene→LobbyScene→CharacterSelectScene→GameScene→ResultScene→LobbyScene
- Additive scenes: UIScene overlay trên GameScene, LoadingScene overlay khi transition
- Màu: Scene chính xanh đậm, Additive xám nhạt, mũi tên transition cam với nhãn điều kiện

Hình 2.9: Class Diagram hệ thống Character và Gene

Mô tả render:
- Classes: CharacterData (ScriptableObject), GeneData (ScriptableObject), CharacterController (MonoBehaviour), GeneManager (MonoBehaviour), DamageCalculator (static), NetworkCharacter (NetworkBehaviour)
- Attributes và methods trong từng class
- Quan hệ: aggregation, dependency, inheritance rõ ràng
- Màu: ScriptableObject vàng, MonoBehaviour xanh, NetworkBehaviour tím, static xám

*2.5.6. Sequence Diagram*

Hình 2.10: Sequence Diagram — Luồng tiến hoá gene

Mô tả render:
- Participants: Player → Unity Client → Backend API → MySQL Database
- Sequence: Nhấn Evolve → Validate local → POST /api/gene/evolve → SELECT gene count → Validate server-side → UPDATE tier + gold → 200 OK {newTier, newStats} → Update UI → Animation evolution
- Nét đứt cho response, activation box vàng

Hình 2.11: Sequence Diagram — Luồng matchmaking PvP

Mô tả render:
- Participants: Player A, Player B, Unity Client A, Client B, Backend API, Game Server
- Sequence: Cả 2 join queue → API tìm match → Notify cả 2 → Cả 2 connect Game Server → Server start match → Initial state sync → Game begin

**Kết luận chương 2**

---

### CHƯƠNG 3: TRIỂN KHAI XÂY DỰNG HỆ THỐNG (~40 trang)

**3.1. Môi trường và công cụ phát triển**

Bảng 3.1: Môi trường và công cụ

| Công cụ | Phiên bản | Mục đích |
|---------|-----------|---------|
| Unity | 2022.3 LTS | Game client và dedicated server |
| Visual Studio 2022 | 17.x | IDE C# |
| Unity Netcode for GameObjects | 1.7.x | Multiplayer networking |
| Node.js / Express | 18.x | Backend REST API |
| MySQL | 8.0 | Database |
| Docker | 24.x | Server deployment |
| Git / GitHub | — | Version control |

**3.2. Xây dựng Backend API và Database**

*3.2.1. Khởi tạo database*

Code snippet — CREATE TABLE players và characters:

```sql
CREATE TABLE players (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    level INT DEFAULT 1,
    exp BIGINT DEFAULT 0,
    gold BIGINT DEFAULT 1000,
    rating INT DEFAULT 1000,
    is_banned TINYINT(1) DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    last_login DATETIME
);
```

*3.2.2. JWT Authentication middleware*

Code snippet — Node.js/Express JWT middleware:

```javascript
const authMiddleware = (req, res, next) => {
    const token = req.headers['authorization']?.split(' ')[1];
    if (!token) return res.status(401).json({ error: 'No token provided' });
    try {
        const decoded = jwt.verify(token, process.env.JWT_SECRET);
        req.playerId = decoded.playerId;
        req.isAdmin = decoded.isAdmin || false;
        next();
    } catch (err) {
        return res.status(401).json({ error: 'Invalid or expired token' });
    }
};
```

*3.2.3. Gene Evolution API — server-side validation*

Trình bày logic validate đủ gene, đủ gold, rồi UPDATE database — đảm bảo không thể cheat client-side.

**3.3. Xây dựng Unity Client**

*3.3.1. SceneLoader với async loading*

```csharp
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;
    public async void LoadSceneAsync(string sceneName, Action onComplete = null)
    {
        LoadingScreen.Show();
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;
        while (op.progress < 0.9f)
        {
            LoadingScreen.SetProgress(op.progress);
            await Task.Yield();
        }
        op.allowSceneActivation = true;
        onComplete?.Invoke();
    }
}
```

*3.3.2. Character Controller*

Viết 3–4 đoạn về Input System, Rigidbody movement, Animation blend tree.

```csharp
public class PlayerCharacterController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody rb;
    private Animator animator;

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = new Vector3(h, 0, v).normalized;
        rb.velocity = new Vector3(dir.x * moveSpeed, rb.velocity.y, dir.z * moveSpeed);
        animator.SetFloat("Speed", dir.magnitude);
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }
}
```

*3.3.3. GeneManager và mutation calculation*

```csharp
[CreateAssetMenu(fileName = "GeneData", menuName = "Game/GeneData")]
public class GeneData : ScriptableObject
{
    public string geneName;
    public GeneType geneType;
    public GeneTier tier;
    public float statMultiplier;
    public Sprite icon;
}

public class GeneManager : MonoBehaviour
{
    [SerializeField] private GeneSlot[] equippedGenes; // 6 slots

    public CharacterStats CalculateMutatedStats(CharacterStats baseStats)
    {
        CharacterStats result = baseStats.Clone();
        foreach (var slot in equippedGenes)
        {
            if (slot.gene == null) continue;
            switch (slot.gene.geneType)
            {
                case GeneType.Attack:
                    result.atk = Mathf.RoundToInt(result.atk * (1f + slot.gene.statMultiplier));
                    break;
                case GeneType.Defense:
                    result.def = Mathf.RoundToInt(result.def * (1f + slot.gene.statMultiplier));
                    break;
                case GeneType.Speed:
                    result.spd *= (1f + slot.gene.statMultiplier);
                    break;
                case GeneType.Crit:
                    result.critRate = Mathf.Min(result.critRate + slot.gene.statMultiplier, 0.75f);
                    break;
            }
        }
        return result;
    }
}
```

Hình 3.1: Giao diện Gene Evolution UI

Mô tả render:
- Background: đen với particle DNA xanh lá
- Trung tâm: nhân vật 3D xoay chậm
- Trái: 6 gene slot hình lục giác, màu theo tier
- Phải: bảng stat so sánh Before/After
- Dưới: nút "TIẾN HOÁ" cam neon, "Cần: 3× Gene [Tier] + Gold", gold hiện tại
- Style: sci-fi dark UI, glow effects

*3.3.4. Skill system*

```csharp
public class SkillController : MonoBehaviour
{
    [SerializeField] private SkillData[] skills;
    private float[] cooldownTimers;

    public void CastSkill(int slotIndex)
    {
        if (cooldownTimers[slotIndex] > 0) return;
        if (currentMP < skills[slotIndex].mpCost) return;
        currentMP -= skills[slotIndex].mpCost;
        cooldownTimers[slotIndex] = skills[slotIndex].cooldown;
        skills[slotIndex].Execute(transform, target, geneManager.CalculatedStats);
        animator.SetTrigger($"Skill{slotIndex + 1}");
    }

    private void Update()
    {
        for (int i = 0; i < cooldownTimers.Length; i++)
            if (cooldownTimers[i] > 0)
                cooldownTimers[i] -= Time.deltaTime;
    }
}
```

*3.3.5. Damage Calculator*

```csharp
public static class DamageCalculator
{
    public static DamageResult Calculate(
        CharacterStats attacker, CharacterStats defender, SkillData skill)
    {
        float baseDmg = attacker.atk * skill.damageMultiplier;
        float defReduction = defender.def / (defender.def + 200f);
        float finalDmg = baseDmg * (1f - defReduction);
        bool isCrit = Random.value < attacker.critRate;
        if (isCrit) finalDmg *= 2f;
        return new DamageResult
        {
            damage = Mathf.RoundToInt(finalDmg),
            isCrit = isCrit
        };
    }
}
```

**3.4. Xây dựng Multiplayer — Unity Netcode**

*3.4.1. NetworkCharacter với HP sync và ServerRpc*

```csharp
public class NetworkCharacter : NetworkBehaviour
{
    public NetworkVariable<int> CurrentHP = new NetworkVariable<int>(
        1000, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [ServerRpc(RequireOwnership = true)]
    public void TakeDamageServerRpc(int damage, ulong attackerId)
    {
        int newHP = Mathf.Max(0, CurrentHP.Value - damage);
        CurrentHP.Value = newHP;
        NotifyDamageClientRpc(damage, attackerId);
        if (newHP <= 0) HandleDeathServerRpc();
    }

    [ClientRpc]
    private void NotifyDamageClientRpc(int damage, ulong attackerId)
    {
        DamagePopupUI.Show(transform.position, damage);
    }
}
```

Bảng 3.2: State Synchronization

| Variable | Owner | Sync Direction | Update Rate |
|---------|-------|----------------|-------------|
| CurrentHP | Server | Server→All | On change |
| NetworkPosition | Server | Server→All | 20Hz |
| AnimationState | Owner | Owner→Others | On change |
| BuffList | Server | Server→All | On change |

**3.5. AI Enemy**

*3.5.1. Enemy State Machine*

```csharp
public class EnemyStateMachine : MonoBehaviour
{
    private NavMeshAgent agent;
    private EnemyState currentState;
    private Transform target;

    [SerializeField] private float detectRange = 10f;
    [SerializeField] private float attackRange = 2f;

    private void Update()
    {
        switch (currentState)
        {
            case EnemyState.Patrol: UpdatePatrol(); break;
            case EnemyState.Chase:  UpdateChase(); break;
            case EnemyState.Attack: UpdateAttack(); break;
        }
        CheckTransitions();
    }

    private void CheckTransitions()
    {
        if (currentHP / (float)maxHP <= 0.2f) { ChangeState(EnemyState.Retreat); return; }
        float dist = Vector3.Distance(transform.position, target.position);
        if (dist <= attackRange) ChangeState(EnemyState.Attack);
        else if (dist <= detectRange) ChangeState(EnemyState.Chase);
        else ChangeState(EnemyState.Patrol);
    }
}
```

*3.5.2. Boss AI với 3 phase*

Trình bày chi tiết Boss phase: Phase 1 (HP > 70%): tấn công bình thường; Phase 2 (70%–40%): thêm AoE skill; Phase 3 (<40%): tăng speed + rage mode.

Hình 3.2: Giao diện Boss combat

Mô tả render:
- Camera: third-person, góc 45°
- Boss: nhân vật lớn 3× player, màu đỏ/đen, health bar đỏ to phía trên "BOSS: Mutant Alpha - Phase 2"
- Player: nhân vật có skill effect glow xanh
- HUD: góc trên trái HP/MP bar; góc dưới 4 skill icon với cooldown overlay; góc dưới phải "Wave 5/5 — BOSS"
- Sàn dungeon: dark stone + ánh sáng đỏ từ boss

**3.6. Dungeon và Wave System**

Trình bày DungeonManager, WaveConfig ScriptableObject, wave progression, reward on clear.

Hình 3.3: Giao diện HUD dungeon wave

Mô tả render:
- Góc trên phải: "WAVE 3/5" màu vàng + countdown "Next wave: 10s"
- Góc trên trái: "Enemies: 4 remaining" với icon skull
- Góc dưới giữa: progress bar wave màu xanh
- Background: game đang diễn ra

**3.7. UI/UX Implementation**

Hình 3.4: Màn hình đăng nhập

Mô tả render:
- Background: đấu trường sci-fi tối, particle DNA xanh neon
- Panel kính mờ (glassmorphism) chứa: Logo "MUTANTS ARENA" glow cyan, 2 input fields, nút "ĐĂNG NHẬP" cam neon, link "Đăng ký"
- Style: futuristic dark với neon border

Hình 3.5: Main Lobby UI

Mô tả render:
- Navigation bar trên: Avatar, tên, level, gold coin
- 3 button lớn: PvP Arena, PvE Dungeon, Training
- Panel phải: Quick Stats (rank, win rate, last match)
- Panel trái: Friends online list
- Background: dark space + nebula effect

Hình 3.6: Inventory và trang bị

Mô tả render:
- Trái: inventory grid 6×5 ô với item icon, rarity border màu
- Phải: character doll với equipment slots (Head, Body, Weapon, Offhand, Accessory×2)
- Dưới character: 6 gene slot hình lục giác
- Stat panel: bảng Base vs Current với mũi tên màu xanh/đỏ

**3.8. Tối ưu hoá hiệu năng**

*3.8.1. Object Pooling*

```csharp
public class ObjectPool<T> where T : MonoBehaviour
{
    private Queue<T> pool = new Queue<T>();
    private T prefab;

    public ObjectPool(T prefab, int initialSize)
    {
        this.prefab = prefab;
        for (int i = 0; i < initialSize; i++)
        {
            T obj = Object.Instantiate(prefab);
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public T Get()
    {
        T obj = pool.Count > 0 ? pool.Dequeue() : Object.Instantiate(prefab);
        obj.gameObject.SetActive(true);
        return obj;
    }

    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }
}
```

*3.8.2. Các kỹ thuật tối ưu khác*

Viết 3–4 đoạn về: LOD Group 3 mức cho enemy mesh, Static batching cho dungeon walls, Async/Await thay coroutine, Texture compression ETC2/DXT.

**3.9. Bảo mật hệ thống**

Viết 3–4 đoạn về: server-side authoritative (mọi tính toán damage/evolve/reward), JWT secret + expiry, SQL parameterized query, rate limiting, input validation.

**3.10. Kiểm thử hệ thống**

*3.10.1. Test case chức năng*

Bảng 3.3: Bảng test case (ít nhất 20 TC)

| TC# | Chức năng | Điều kiện | Input | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|-----|-----------|-----------|-------|-----------------|-----------------|-----------|
| TC01 | Đăng ký | Email hợp lệ, chưa tồn tại | username, email, pass | 201 Created | 201 OK | Pass |
| TC02 | Đăng ký | Email đã tồn tại | email đã có | 409 Conflict | 409 | Pass |
| TC03 | Đăng nhập | Đúng thông tin | username, pass | 200 + JWT | 200 + token | Pass |
| TC04 | Đăng nhập | Sai mật khẩu | wrong pass | 401 Unauthorized | 401 | Pass |
| TC05 | Tiến hoá gene | Đủ điều kiện | 3×F gene | Gene lên tier E | Tier E OK | Pass |
| TC06 | Tiến hoá gene | Thiếu gene | 2×F gene | 400 Bad Request | 400 | Pass |
| TC07 | PvP match | 2 player online | 2 clients join | Match tạo OK | Match OK | Pass |
| TC08 | Damage calc | Skill Heavy Strike | ATK=100, DEF=50 | DMG≈120 | DMG=118 | Pass |
| TC09 | AI Chase | Player cách 8m | player in range | Enemy chase | Chase active | Pass |
| TC10 | Boss Phase 2 | HP xuống <70% | enough damage | Phase 2 trigger | Phase 2 OK | Pass |

*(Thêm TC11–TC20+ cho: skill cooldown, inventory equip, friend add, chat send, dungeon complete, admin ban, leaderboard sort, reconnect, disconnect handling, anti-cheat)*

*3.10.2. Kiểm thử hiệu năng*

Hình 3.7: Biểu đồ FPS theo số lượng enemy

Mô tả render:
- Loại hình: Line chart
- Trục X: số enemy (0, 5, 10, 20, 30, 50)
- Trục Y: FPS (0–120)
- Line màu xanh, smooth, dấu chấm tại data points: 120→115→105→90→72→58
- Đường tham chiếu đỏ tại FPS=60
- Tiêu đề: "FPS theo số lượng enemy (GTX 1060, 1080p)"

Hình 3.8: Biểu đồ CPU/RAM theo CCU

Mô tả render:
- Loại hình: Grouped bar chart
- Trục X: CCU (10, 25, 50, 75, 100)
- Trục Y: % tài nguyên (0–100%)
- 2 bar nhóm: CPU (xanh dương) và RAM (cam)
- Đường tham chiếu tại 80% (warning threshold)

Hình 3.9: Biểu đồ latency theo CCU

Mô tả render:
- Line chart, trục X: CCU, trục Y: latency ms (0–200ms)
- Đường tham chiếu đỏ tại 100ms
- Data: 15ms(10CCU)→45ms(50CCU)→85ms(100CCU)

*3.10.3. Kết quả tổng hợp*

Bảng 3.4: Tổng hợp kết quả kiểm thử

| Nhóm | Tổng TC | Passed | Failed | Tỉ lệ Pass |
|------|---------|--------|--------|-----------|
| Xác thực tài khoản | 6 | 6 | 0 | 100% |
| Gene & Skill system | 8 | 8 | 0 | 100% |
| Multiplayer & Combat | 10 | 9 | 1 | 90% |
| AI Enemy | 6 | 6 | 0 | 100% |
| Hiệu năng | 4 | 4 | 0 | 100% |
| Bảo mật | 5 | 5 | 0 | 100% |
| **Tổng cộng** | **39** | **38** | **1** | **97.4%** |

**Kết luận chương 3**

---

### KẾT LUẬN VÀ HƯỚNG PHÁT TRIỂN (~5 trang)

**Kết quả đạt được**

Bảng X: So sánh mục tiêu và kết quả

| Mục tiêu | Chỉ tiêu | Kết quả | Đánh giá |
|---------|---------|---------|---------|
| FPS ổn định | ≥ 60 FPS | 72–115 FPS | Đạt |
| Latency | < 100ms | Avg 45ms LAN / 85ms WAN | Đạt |
| CCU hỗ trợ | ≥ 100 | 100 CCU OK | Đạt |
| Test case pass | ≥ 95% | 97.4% (38/39) | Đạt |
| Gene system | 7 tier, 5 type | Hoàn chỉnh | Đạt |
| Số màn hình UI | ≥ 8 | 10 màn hình | Vượt |

**Hạn chế**

Viết 3–4 đoạn: chưa có ranked season tự động kết thúc, Boss AI chưa đa dạng, anti-cheat chỉ ở mức cơ bản, art style dùng asset store.

**Hướng phát triển**

- PvP Ranked Season + prestige reward
- Clan / Guild system + Guild War
- Marketplace gene trading giữa người chơi
- Cross-platform Mobile (Android/iOS)
- AI Mutation Learning (Reinforcement Learning)
- Cloud Save với Unity Gaming Services
- Tournament system

---

### TÀI LIỆU THAM KHẢO

Ít nhất 15 tài liệu theo chuẩn IEEE/APA, bao gồm:
- Tài liệu Unity chính thức (Netcode, NavMesh, Animator)
- Bài báo về game architecture và multiplayer
- Sách Game Programming Patterns (R. Nystrom), Game Engine Architecture (J. Gregory)
- Newzoo Global Games Market Report
- RFC 7519 — JWT
- OWASP Top 10
- Tài liệu MySQL, Node.js, Express.js

---

### PHỤ LỤC

**Phụ lục A:** Source code đầy đủ các module: GeneManager.cs, SkillController.cs, DamageCalculator.cs, EnemyStateMachine.cs, NetworkCharacter.cs, MatchmakingManager.cs

**Phụ lục B:** Toàn bộ database schema SQL (CREATE TABLE + INDEX + FOREIGN KEY)

**Phụ lục C:** JSON packet examples (Login request/response, Match state sync, Gene evolve request/response)

**Phụ lục D:** Danh sách Unity Package (tên, phiên bản, mục đích)

| Package | Phiên bản | Mục đích |
|---------|-----------|---------|
| Unity Netcode for GameObjects | 1.7.1 | Multiplayer |
| Unity Transport | 2.1.0 | Transport layer |
| NavMesh Components | 1.0.0 | AI pathfinding |
| TextMeshPro | 3.0.6 | UI text |
| Cinemachine | 2.9.7 | Camera system |
| Universal Render Pipeline | 14.0.9 | Rendering |
| Input System | 1.7.0 | Input handling |
| Addressables | 1.21.17 | Asset management |

---

## HƯỚNG DẪN SỬ DỤNG PROMPT NÀY

Thêm vào cuối prompt câu lệnh cụ thể:

> **"Hãy viết [PHẦN CỤ THỂ] đầy đủ theo cấu trúc và yêu cầu trên. Không bỏ sót mục nào, không viết tóm tắt thay nội dung đầy đủ."**

Ví dụ:
- "Hãy viết CHƯƠNG 1 đầy đủ."
- "Hãy viết mục 2.5 — Thiết kế kiến trúc hệ thống — chi tiết."
- "Hãy viết PHẦN ĐẦU: Lời nói đầu, Lời cảm ơn, Lời cam đoan."
- "Hãy viết mục 3.10 — Kiểm thử hệ thống — với đầy đủ bảng test case."

---

## LƯU Ý BẮT BUỘC

- **KHÔNG** viết ngắn gọn kiểu gạch đầu dòng thay văn xuôi học thuật.
- **KHÔNG** bỏ qua mục nào — mỗi mục ít nhất 2 đoạn văn chi tiết.
- **PHẢI** có Hình minh hoạ với mô tả render chi tiết tại mỗi sơ đồ/biểu đồ.
- **PHẢI** có Bảng biểu đầy đủ dữ liệu theo format học thuật.
- **PHẢI** viết "Kết luận chương" cuối mỗi chương.
- **PHẢI** có code snippet minh hoạ trong Chương 3 (C#, SQL, JavaScript, JSON).
- Mỗi chương phải đủ **8–15 trang Word** (~3.000–6.000 từ).
