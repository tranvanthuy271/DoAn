# KẾT LUẬN VÀ HƯỚNG PHÁT TRIỂN

## 1. Kết luận

Sau quá trình nghiên cứu, phân tích, thiết kế, hiện thực hoá và kiểm thử, đồ án “Phát triển trò chơi Mutants Arena với hệ thống tiến hóa Gene bằng Unity” đã đạt được các kết quả chính sau:

- Về mặt **lý thuyết**: Đồ án đã hệ thống hoá kiến thức nền tảng về thể loại game 2D Action RPG, kiến trúc Client-Server Authoritative, các kỹ thuật AI trong game (Finite State Machine, Behavior Tree, Pathfinding), các công nghệ nền tảng Unity 2022.3 LTS, ASP.NET Core 7, Unity Netcode for GameObjects, SignalR và MySQL 8.0.
- Về mặt **thiết kế**: Đồ án đã đưa ra một kiến trúc ba tầng (Unity Client – Game Server/API Server – MySQL) chuẩn công nghiệp, đặc tả đầy đủ 9 ca sử dụng chính, mô hình hoá cơ sở dữ liệu 14 bảng với các JSON column cho cấu hình động (boss phases, quest progress, item sockets) cho phép balancing không cần recompile.
- Về mặt **hiện thực hoá**: Đồ án đã triển khai thành công 12/14 nhóm chức năng cốt lõi, bao gồm: di chuyển 2D với coyote/buffer/dash i-frames; combat realtime với hitbox/hurtbox tách bạch và hệ tương khắc 6 nguyên tố ×1,5/×0,75; Gene system 5 Tier kèm Fusion Hybrid với 5 công thức mẫu; trang bị 3 slot với Enhancement +0..+20 và Socket Ngũ Hành; Quest 3 loại; AI 3 lớp quái với Boss Phase System cấu hình JSON; NPC dynamic menu + multi-shop + blacksmith; kiến trúc Zone-based đa scene additive với physics isolation; phó bản Wave-based và Party 4 người đồng bộ qua NGO + SignalR.
- Về mặt **kiểm thử – đánh giá**: FPS duy trì ≥ 60 trên cấu hình khuyến nghị, ≥ 45 trên cấu hình tối thiểu; RTT < 200 ms ngay cả với 16 client đồng thời trên WAN xa; API throughput đạt 180–520 RPS cho mọi endpoint chính; server đáp ứng 32 client đồng thời với CPU 62% và RAM 720 MB trên VPS 4 vCPU/8 GB. Khảo sát UX với 12 tình nguyện viên cho điểm trung bình 4,4/5.

Đề tài chứng minh khả năng kết hợp đồng thời nhiều công nghệ phức tạp — Unity Engine, ASP.NET Core, Netcode for GameObjects, SignalR và MySQL — trong một sản phẩm game multiplayer chơi được hoàn chỉnh, có giá trị tham khảo cho các nghiên cứu và sản phẩm game tiếp theo tại Việt Nam.

## 2. Hạn chế

Bên cạnh các kết quả đạt được, đồ án vẫn còn các hạn chế sau:

- Chưa triển khai chế độ **Ranked PvP** với hệ thống Elo/MMR và mùa giải.
- Chưa có **Marketplace** giao dịch vật phẩm trực tiếp giữa người chơi.
- **Admin Web Dashboard** mới ở mức API; chưa có giao diện web hoàn chỉnh cho operations.
- Boss Phase 3 hơi khó với người chơi mới — cần balancing thêm dữ liệu.
- Hiện chỉ chạy trên **Windows desktop**, chưa hỗ trợ cross-platform (mobile/console).
- Anti-cheat mới ở mức Server Authoritative; chưa có lớp behavioral detection.
- Hệ thống chưa hỗ trợ **internationalization (i18n)** đa ngôn ngữ.
- Server đơn instance — chưa có cơ chế **horizontal scaling** với load balancer / message queue.

## 3. Hướng phát triển

Trên cơ sở các hạn chế kể trên, một số hướng phát triển tiếp theo có thể triển khai:

1. **PvP Ranked + Clan System**: hệ thống matchmaking dựa Elo/MMR, mùa giải, thưởng theo top; tính năng tạo Clan với kho chung, chiến tranh Clan và bảng xếp hạng Clan.
2. **Marketplace** giữa người chơi với cơ chế listing, đấu giá và phí giao dịch theo thuế động.
3. **Admin Web Dashboard** xây trên Blazor/React, hiển thị realtime: số người chơi online, throughput, alerts, log.
4. **Cross-platform**: port sang mobile (Android/iOS) với điều khiển ảo và tối ưu UI; sau đó là console.
5. **AI Learning Mutation**: áp dụng Reinforcement Learning đơn giản cho boss AI để tự thích nghi pattern theo cách chơi người dùng — sử dụng Unity ML-Agents.
6. **Blockchain skin / NFT**: hệ thống sở hữu trang phục độc nhất bằng smart contract, có thể trade ngoài game (mô hình tuỳ chọn, không bắt buộc người chơi).
7. **Cloud save + cross-device**: lưu progress trên cloud, đăng nhập đa thiết bị.
8. **Horizontal scaling**: triển khai nhiều Game Server instance đứng sau load balancer; hệ thống message queue (RabbitMQ / Redis Pub/Sub) cho cross-server notification; database read-replica.
9. **i18n / l10n**: hỗ trợ đa ngôn ngữ (EN, JP, KR, CN) cho thị trường quốc tế.
10. **Anti-cheat nâng cao**: lớp behavioral detection (statistical anomaly), kết hợp client integrity check.

---

# TÀI LIỆU THAM KHẢO

[1] Newzoo, *Global Games Market Report 2024*, Newzoo BV, 2024.

[2] Unity Technologies, *Unity 2D Game Development Documentation*, Unity Manual 2022.3 LTS. <https://docs.unity3d.com/2022.3/Documentation/Manual/Unity2D.html>

[3] Unity Technologies, *Unity Netcode for GameObjects (NGO) Documentation*, 2024. <https://docs-multiplayer.unity3d.com/netcode/current/about/>

[4] Microsoft, *ASP.NET Core 7 Documentation*, 2023. <https://learn.microsoft.com/aspnet/core/>

[5] Microsoft, *SignalR Documentation*, 2024. <https://learn.microsoft.com/aspnet/core/signalr/>

[6] Oracle, *MySQL 8.0 Reference Manual*, 2024. <https://dev.mysql.com/doc/refman/8.0/en/>

[7] M. Buckland, *Programming Game AI by Example*, Wordware Publishing, 2005.

[8] J. Gregory, *Game Engine Architecture*, 3rd ed., CRC Press, 2018.

[9] R. Nystrom, *Game Programming Patterns*, Genever Benning, 2014. <https://gameprogrammingpatterns.com/>

[10] Team Cherry, *Hollow Knight — Postmortem Talks*, GDC 2018.

[11] Motion Twin, *Dead Cells: How GDC Saved Our Game*, GDC 2019.

[12] M. Thorson, *Celeste — Designing for Better Game Feel*, GDC 2020.

[13] Glenn Fiedler, *Networking for Game Programmers*, gafferongames.com, 2015. <https://gafferongames.com/>

[14] Y. Bernier, *Latency Compensating Methods in Client/Server In-game Protocols*, Valve Corporation, 2001.

[15] Box2D, *Box2D v2.4 Manual*, Erin Catto, 2024. <https://box2d.org/documentation/>

[16] BCrypt.NET, *Password hashing library*, NuGet Package, 2024.

[17] IETF, *RFC 7519 — JSON Web Token (JWT)*, 2015. <https://www.rfc-editor.org/rfc/rfc7519>

[18] Docker Inc., *Docker Compose Documentation*, 2024.

[19] Cinemachine, *Unity Cinemachine Documentation 2.10*, 2024.

[20] Vietnam Game Summit, *Báo cáo thị trường game Việt Nam 2023–2024*, VGS, 2024.

---

# PHỤ LỤC

## Phụ lục A. Cấu trúc thư mục mã nguồn

```
DoAn/
├── Client/                     # Unity 2022.3 LTS project
│   ├── Assets/
│   │   ├── Scripts/
│   │   │   ├── Player/         # PlayerController, InputReader, GroundProbe
│   │   │   ├── Combat/         # CombatResolver, Hitbox, Hurtbox, BuffContainer
│   │   │   ├── Gene/           # GeneInventory, GeneUpgradeService, GeneFusionService
│   │   │   ├── Enemy/          # EnemyAI, BossController, PhaseLoader
│   │   │   ├── NPC/            # NpcInteractable, DynamicMenu, ShopUI
│   │   │   ├── Equipment/      # EnhanceService, SocketService
│   │   │   ├── Quest/          # QuestTracker, QuestUI
│   │   │   ├── Map/            # ZoneRoomRegistry, DungeonInstance, WaveController
│   │   │   ├── Net/            # NGO Bootstrap, ConnectionApproval, RpcDispatcher
│   │   │   └── UI/             # HUD, Inventory, Party, Chat
│   │   ├── Prefabs/
│   │   ├── ScriptableObjects/  # SkillDefinition, GeneDefinition, NpcDefinition, ...
│   │   └── Scenes/
│   └── Packages/
├── GameServerApi/              # ASP.NET Core 7
│   ├── Controllers/            # AuthController, CharacterController, ShopController, ...
│   ├── Hubs/                   # PartyHub, ChatHub
│   ├── Services/               # GeneService, EnhanceService, QuestService, ...
│   ├── Data/                   # AppDbContext (EF Core / Dapper)
│   └── Program.cs
├── docker-compose.yml
├── gamedb.sql                  # Schema 14 bảng + view
└── Docs/                       # HUONG_DAN_*.md
```

## Phụ lục B. Trích đoạn schema cơ sở dữ liệu

```sql
-- players: tài khoản
CREATE TABLE players (
  id BIGINT PRIMARY KEY AUTO_INCREMENT,
  username VARCHAR(64) NOT NULL UNIQUE,
  password_hash VARCHAR(255) NOT NULL,        -- BCrypt cost 11
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  last_login DATETIME NULL
) ENGINE=InnoDB;

-- characters: nhân vật
CREATE TABLE characters (
  id BIGINT PRIMARY KEY AUTO_INCREMENT,
  player_id BIGINT NOT NULL,
  name VARCHAR(32) NOT NULL UNIQUE,
  class_element ENUM('Kim','Moc','Thuy','Hoa','Tho','Phong') NOT NULL,
  level INT NOT NULL DEFAULT 1,
  exp BIGINT NOT NULL DEFAULT 0,
  gold BIGINT NOT NULL DEFAULT 0,
  zone_id INT NOT NULL DEFAULT 1,
  pos_x FLOAT NOT NULL DEFAULT 0,
  pos_y FLOAT NOT NULL DEFAULT 0,
  stats_json JSON NULL,
  FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE
) ENGINE=InnoDB;

-- gene_inventory
CREATE TABLE gene_inventory (
  id BIGINT PRIMARY KEY AUTO_INCREMENT,
  character_id BIGINT NOT NULL,
  element ENUM('Kim','Moc','Thuy','Hoa','Tho','Phong') NOT NULL,
  tier TINYINT NOT NULL DEFAULT 1,
  is_equipped BOOLEAN NOT NULL DEFAULT FALSE,
  is_hybrid BOOLEAN NOT NULL DEFAULT FALSE,
  hybrid_pair VARCHAR(16) NULL,    -- e.g. "Kim+Hoa"
  FOREIGN KEY (character_id) REFERENCES characters(id) ON DELETE CASCADE
) ENGINE=InnoDB;

-- bosses
CREATE TABLE bosses (
  id INT PRIMARY KEY AUTO_INCREMENT,
  code VARCHAR(64) NOT NULL UNIQUE,
  display_name VARCHAR(128) NOT NULL,
  max_hp INT NOT NULL,
  base_atk INT NOT NULL,
  base_def INT NOT NULL,
  element ENUM('Kim','Moc','Thuy','Hoa','Tho','Phong') NOT NULL,
  phases_json JSON NOT NULL
) ENGINE=InnoDB;
```

## Phụ lục C. Ví dụ packet đồng bộ Netcode (rút gọn)

```text
# Move input (client → server, ServerRpc unreliable)
[MoveInputRpc]
{ tick: u32, axis: f32, jumpPressed: bool, dashPressed: bool, dt: f32 }

# Server transform sync (server → all, NetworkTransform, snapshot interpolation)
[ServerPosition]   Vector3   pos
[ServerVelocity]   Vector2   vel
[FacingDir]        i8

# Cast skill (client → server, reliable)
[CastSkillRpc] { tick: u32, slot: u8, aimX: f32, aimY: f32 }

# Damage dealt (server → all, ClientRpc reliable)
[DamageDealtClientRpc]
{ targetId: u64, dmg: f32, isCrit: bool, isElementBonus: bool }
```

## Phụ lục D. Ví dụ REST API

```http
POST /api/auth/login           Content-Type: application/json
{ "username": "demo", "password": "***" }
→ 200 { "token": "eyJhbGciOi..." , "expiresIn": 86400 }

POST /api/gene/upgrade         Authorization: Bearer <jwt>
{ "characterId": 12, "geneId": 33 }
→ 200 { "success": true, "newTier": 3, "consumed": { "fragments": 50, "gold": 1000 } }

POST /api/equipment/enhance    Authorization: Bearer <jwt>
{ "characterId": 12, "equipmentId": 88, "useProtect": true }
→ 200 { "success": true, "newTier": 14, "result": "Upgraded" }
```

## Phụ lục E. Tham chiếu tài liệu thiết kế kèm theo

- `HUONG_DAN_KIEN_TRUC_SERVER_CLIENT.md` — Kiến trúc tổng thể.
- `HUONG_DAN_MULTI_GENE_UNITY.md`, `HUONG_DAN_NANG_CAP_GENE.md`, `HUONG_DAN_FUSION_KIM_PHONG.md`, `HUONG_DAN_HYBRID_UNITY.md` — Gene system.
- `HUONG_DAN_ENEMY_BOSS.md`, `HUONG_DAN_BOSS_ADVANCED.md`, `HUONG_DAN_CONFIG_SKILL_ENEMY*.md` — AI quái & boss.
- `HUONG_DAN_NPC_NETCODE.md`, `HUONG_DAN_NPC_SHOP_UNITY.md`, `HUONG_DAN_NPC_SHOP_BLACKSMITH_MULTI_SHOP.md`, `HUONG_DAN_CONFIG_NPC_DYNAMIC_MENU.md` — NPC & cửa hàng.
- `HUONG_DAN_NANG_CAP_TRANG_BI.md`, `HUONG_DAN_CUONG_HOA_UNITY.md`, `HUONG_DAN_GHEP_DA.md`, `HUONG_DAN_CONFIG_EQUIPMENT_TIER_ANIMATION.md` — Trang bị.
- `HUONG_DAN_MAP_*.md`, `HUONG_DAN_CONFIG_DUNGEON_*.md`, `HUONG_DAN_PHO_BAN_VA_TO_DOI.md`, `HUONG_DAN_UI_PHO_BAN.md`, `HUONG_DAN_WAVE_HUD_UNITY.md` — Bản đồ & phó bản.
- `HUONG_DAN_CONFIG_QUEST_SYSTEM_LANGLA.md` — Quest.
- `HUONG_DAN_FRIEND_SYSTEM.md`, `HUONG_DAN_CONFIG_CHAT_UNITY.md`, `HUONG_DAN_CONFIG_UNITY_BUFF_HUD.md`, `HUONG_DAN_ITEM_BUFF*.md` — Hệ xã hội & Buff.
- `HUONG_DAN_DEPLOY_VPS.md`, `DOCKER_DEPLOY.md` — Triển khai.

---

*Hết báo cáo.*
