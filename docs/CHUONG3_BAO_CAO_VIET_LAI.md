CHƯƠNG 3.CÀI ĐẶT VÀ HIỆN THỰC HÓA HỆ THỐNG

Chương 3 trình bày quá trình cài đặt và hiện thực hóa hệ thống trong project DoAn, tập trung vào hai nhóm chức năng chính: hệ thống Gene tiến hóa và kiến trúc nhiều người chơi thời gian thực. Dữ liệu trong chương này được đối chiếu trực tiếp từ mã nguồn Unity tại thư mục Client/Assets/Scripts, backend GameServerApi, các controller REST API, SignalR Hub và cơ sở dữ liệu MySQL trong tệp gamedb.sql.

Project DoAn được hiện thực bằng Unity 2D, Unity Netcode for GameObjects, SignalR, ASP.NET Core .NET 9, Entity Framework Core, Pomelo MySQL và JWT Bearer Authentication. Các nội dung trong chương được trình bày theo góc nhìn công nghệ và cơ chế vận hành: đồng bộ bằng NetworkVariable, truyền lệnh bằng ServerRpc/ClientRpc, realtime xã hội bằng SignalR Hub, lưu dữ liệu bằng MySQL/JSON column, xác thực bằng JWT Bearer và bảo vệ request nội bộ bằng Zone API Key.

Các công thức, endpoint và payload trong chương được rút ra từ triển khai thực tế. Với các đoạn công thức tổng hợp, báo cáo chỉ diễn giải lại chuỗi xử lý đã có trong runtime như buff tấn công, kiểm tra kháng nguyên tố, xử lý trạng thái suy yếu, phòng thủ dungeon và phản đòn; các dữ liệu chỉ mới được lưu hoặc trả API như HybridAtkBonusPct, HybridImmuneElements và tang_dame_* không được ghi như công thức runtime nếu chưa có code trực tiếp áp dụng.

3.1Hiện thực hóa phía máy khách (Client-Side - Unity)

3.1.1Hệ thống khởi tạo phiên chơi và kết nối multiplayer

Phía máy khách của DoAn được xây dựng bằng Unity. Sau khi người chơi đăng nhập thành công, client nhận JWT token, userId và dữ liệu nhân vật từ backend. Các dữ liệu có giá trị lâu dài như nhân vật, Gene, kỹ năng, inventory, trang bị và tiến trình phó bản không được hard-code trong prefab, mà được tải qua REST API từ GameServerApi. Khi bước vào gameplay realtime, Unity client kết nối tới zone server bằng Unity Netcode for GameObjects.

Cơ chế xác thực phiên chơi: Sau khi đăng nhập qua API /api/auth/login, client lưu token JWT và đính kèm token vào các request HTTP bằng header Authorization: Bearer <JWT_TOKEN>. Đối với SignalR, token được truyền vào kết nối hub theo cơ chế access token để backend xác định đúng user đang chat hoặc tham gia tổ đội.

Kết nối gameplay realtime bằng Unity Netcode: Quá trình vào game được tổ chức theo mô hình connection approval của Unity Netcode. Khi client gửi thông tin đăng nhập vào zone server, server kiểm tra phiên, xác định map/zone, tải dữ liệu nhân vật từ API rồi spawn NetworkObject đại diện cho người chơi.

Tách riêng gameplay realtime và social realtime: Unity Netcode được dùng cho di chuyển, máu, kỹ năng, quái, boss, dungeon và các đối tượng trong map. SignalR được dùng cho chat và tổ đội thông qua hai hub /chathub và /partyhub. Thiết kế này giúp phần mô phỏng gameplay không bị phụ thuộc vào các chức năng xã hội.

Cấu hình server nội bộ bằng Zone API Key: Các request server-to-server từ Unity zone server về backend không dùng JWT người chơi, mà dùng header X-Zone-Api-Key. Cơ chế này dựa trên một authentication scheme riêng của ASP.NET Core để phân biệt request nội bộ với request người chơi.

Điểm quan trọng trong kiến trúc client là Unity client chỉ giữ vai trò hiển thị, nhập lệnh và nhận phản hồi. Những thao tác ảnh hưởng đến dữ liệu lâu dài như nâng Gene, nâng kỹ năng, nhận thưởng phó bản, thêm vật phẩm hoặc cập nhật vị trí đều đi qua server hoặc backend để hạn chế gian lận.

3.1.2Hệ thống điều khiển, di chuyển và đồng bộ nhân vật

Hệ thống điều khiển nhân vật được hiện thực bằng mô hình NetworkBehaviour của Unity Netcode kết hợp Rigidbody2D. Do game là 2D realtime multiplayer, hệ thống cần đảm bảo người chơi điều khiển nhân vật mượt ở máy local nhưng vẫn đồng bộ được vị trí cho các client khác.

Owner xử lý input cục bộ: Chỉ client sở hữu NetworkObject mới được đọc input và điều khiển Rigidbody2D. Các thao tác như di chuyển ngang, nhảy, rơi qua platform một chiều và cập nhật animation được thực hiện trong FixedUpdate để giảm độ trễ cảm nhận của người chơi.

Gửi trạng thái di chuyển lên server: Mỗi frame vật lý, owner gửi horizontalInput, trạng thái nhảy/rơi, vị trí client, vận tốc trục Y và trạng thái chạm đất lên server thông qua MoveServerRpc. Server ghi lại vị trí vào transform và cập nhật các NetworkVariable cần thiết để các client khác nhìn thấy.

Đồng bộ hướng quay và animation: Hướng mặt của nhân vật được lưu trong NetworkVariable<float> networkScaleX. Animation được phát qua UpdateAnimationClientRpc, trong khi owner vẫn tự cập nhật animation cục bộ để tránh cảm giác phản hồi chậm.

Nội suy cho nhân vật không sở hữu: Các client không sở hữu nhân vật không mô phỏng lại input. Chúng lấy syncPosition từ NetworkVariable và dùng Rigidbody2D.MovePosition kết hợp Vector2.Lerp để kéo nhân vật về vị trí mới. Cơ chế này giảm hiện tượng giật khi gói tin mạng đến không đều.

Để tránh xung đột giữa hai cơ chế đồng bộ, project không phụ thuộc vào NetworkTransform mặc định cho nhân vật chính, mà sử dụng luồng đồng bộ tùy chỉnh bằng ServerRpc và NetworkVariable vị trí.

Dưới đây là đoạn mã cốt lõi xử lý việc owner gửi vị trí lên server thông qua ServerRpc của Unity Netcode. Đoạn mã này được rút từ runtime điều khiển nhân vật mạng trong project:

```csharp
[ServerRpc]
private void MoveServerRpc(float horizontalInput, bool up, bool down,
    Vector2 clientPosition, float clientVelocityY, bool clientIsGrounded)
{
    if (controller == null || controller.stats == null)
        return;

    transform.position = new Vector3(clientPosition.x, clientPosition.y, 0f);

    if (horizontalInput > 0.01f)
        networkScaleX.Value = 1f;
    else if (horizontalInput < -0.01f)
        networkScaleX.Value = -1f;

    float velocityX = horizontalInput * controller.stats.moveSpeed;
    UpdateAnimationClientRpc(velocityX, clientVelocityY, clientIsGrounded, controller.godMode);
}
```

3.1.3Hệ thống đồng bộ dữ liệu nhân vật và trạng thái chiến đấu

Trong môi trường nhiều người chơi, các chỉ số nhân vật không thể chỉ tồn tại trên client cục bộ. Hệ thống áp dụng cơ chế NetworkVariable của Unity Netcode để đồng bộ dữ liệu runtime quan trọng giữa server và tất cả client trong cùng zone.

Đồng bộ thông tin nhân vật: Các thông tin như playerId, hệ nguyên tố, giới tính, tên nhân vật, level, HP, MP, attack, defense, moveSpeed, Gene Tier và partyId được lưu bằng NetworkVariable. Khi nhân vật được spawn, server đọc dữ liệu phiên chơi và ghi vào các biến mạng này để tất cả client nhận cùng một trạng thái.

Cập nhật chỉ số sau nâng cấp: Khi người chơi nâng Gene, thay đổi trang bị hoặc cập nhật dữ liệu nhân vật, client gọi UpdatePlayerDataServerRpc. Server sau đó cập nhật lại NetworkVariable để các client khác nhận được trạng thái mới mà không cần tự gọi lại API.

Đồng bộ tổ đội vào gameplay: networkPartyId được cập nhật thông qua SyncPartyIdServerRpc. Giá trị này giúp server biết những người chơi nào đang cùng party, phục vụ các cơ chế như đi phó bản tổ đội, lọc hiệu ứng hỗ trợ hoặc xử lý tương tác đồng đội.

Hệ thống máu server-authoritative: HP được quản lý bằng NetworkVariable<int> và chỉ server có quyền ghi giá trị cuối cùng. Client không tự ý trừ máu trực tiếp mà gửi yêu cầu qua ServerRpc hoặc để server gọi luồng TakeDamage. Khi HP thay đổi, hệ thống phát event cho UI, animation và hiệu ứng.

Hệ thống EXP và lưu tiến trình: Khi enemy chết, Unity server xác định người chơi hạ gục rồi gọi API /api/player/{playerId}/gain-exp. Backend cập nhật level, experience và dữ liệu nhân vật trong MySQL, bảo đảm tiến trình không phụ thuộc vào client.

3.1.4Hệ thống tương tác và chiến đấu giữa người chơi

Hệ thống chiến đấu trong project bao gồm PvE, boss fight và PvP cơ bản. Luồng chiến đấu được tổ chức bằng va chạm 2D, skill runtime, NetworkVariable HP, ServerRpc và ClientRpc. Phần xử lý sát thương quan trọng được đặt trên server để hạn chế việc client tự sửa damage hoặc máu.

Đánh thường bằng vùng va chạm 2D: Hệ thống tạo vùng đánh tại AttackPoint, sau đó dùng truy vấn vật lý 2D để quét mục tiêu trong attackRange. Nếu mục tiêu là quái, damage được gửi vào luồng máu network của enemy. Nếu mục tiêu là người chơi khác, damage đi qua luồng máu network của player.

Tính sát thương theo chỉ số và buff: Trước khi gây sát thương, hệ thống đọc chỉ số attack của nhân vật và kiểm tra buff tấn công đang active. Nếu người chơi đang có AttackBuff, sát thương được nhân theo phần trăm buff trước khi gửi lên hệ thống máu.

Quản lý máu quái trên server: HP của quái được lưu bằng NetworkVariable, nhận sát thương qua ServerRpc, phát ClientRpc khi bị đánh hoặc chết, xử lý rơi vật phẩm và gọi API cộng EXP cho người chơi hạ gục.

Boss fight có callback tính sát thương: Luồng máu của boss nhận damage kèm elementType, sau đó đi qua bước kiểm tra né tránh, kháng nguyên tố và trạng thái đặc biệt trước khi trừ HP. Sau khi trừ máu, boss có thể xử lý phản damage hoặc chuyển pha.

PvP giữa người chơi: Khi vùng đánh phát hiện collider thuộc player khác, damage vẫn đi qua server trước khi cập nhật HP. Việc trừ máu theo hướng server-authoritative bảo đảm các client trong zone cùng nhìn thấy kết quả giống nhau.

Chi tiết luồng sát thương trong gameplay được tổ chức theo hướng: Client phát lệnh đánh hoặc dùng kỹ năng, server xác định mục tiêu hợp lệ, tính sát thương, cập nhật NetworkVariable HP và phát ClientRpc để hiển thị hiệu ứng.

3.1.5 Hệ thống Gene chính, Gene phụ, Hybrid Fusion, Gene Tối Thượng và cơ chế nâng cấp

Hệ Gene là phần gameplay trọng tâm của project DoAn. Mỗi nhân vật có Gene chính, có thể mở Gene phụ, nâng tier và dung hợp Hybrid khi đạt điều kiện. Toàn bộ cấu hình chi phí, tỉ lệ, vật phẩm, bonus stat và kỹ năng mở khóa được lấy từ backend, không hard-code trong Unity.
Giao diện nâng Gene chính: UI Gene hiển thị tier hiện tại, kinh nghiệm Gene, item yêu cầu, tỉ lệ thành công, bonus stat và kỹ năng mở khóa. Khi người chơi xác nhận nâng cấp, UI không gọi API trực tiếp mà gửi lệnh vào luồng ServerRpc để zone server kiểm tra phiên và gọi backend.

Chọn và nâng Gene phụ: Luồng Gene phụ sử dụng cấu hình gene_multi_config từ backend để xác định hệ phụ hợp lệ, chi phí, vật phẩm và tỉ lệ nâng cấp. Hệ phụ có chi phí riêng và khi nâng thành công sẽ cộng một phần bonus stat vào nhân vật.

Dung hợp Hybrid: UI Hybrid hiển thị điều kiện fuse, item yêu cầu, vàng, tên Hybrid, prefab path, hệ bị khắc và hệ được miễn/giảm khắc. Khi fuse thành công, backend cập nhật IsHybrid, HybridElementA, HybridElementB, HybridBonusTargets, HybridImmuneElements, HybridAtkBonusPct, HybridId và HybridPrefabPath trong info_char.

----- [BẮT ĐẦU PHẦN THÊM MỚI] -----
Kích hoạt Gene Tối Thượng (Ultimate Gene): Kích hoạt sau khi dung hợp Hybrid thành công. Khi nhân vật đạt đủ 1,000,000 EXP Tối Thượng thông qua diệt quái/boss hoặc sử dụng vật phẩm hỗ trợ, trạng thái Gene Tối Thượng (`is_ultimate = true`) được kích hoạt. Thuộc tính HP, MP, ATK, DEF được nhân x1.5 tại `StatCalculator`, đồng thời client tự động hiển thị Aura hào quang tương ứng ra sau lưng nhân vật (tra cứu qua `UltimateAuraDatabase` dựa theo hệ nguyên tố: `aura1` cho Hỏa-Thổ, `aura2` cho Thủy-Mộc, `aura3` cho Kim-Phong) và hiển thị biểu tượng Tối Thượng ✦ trên HUD.
----- [KẾT THÚC PHẦN THÊM MỚI] -----


Luồng nâng Gene qua server: Client không tự sửa dữ liệu Gene. UI gửi lệnh nâng cấp bằng ServerRpc, zone server lấy JWT của client từ session runtime, gọi API /api/gene/upgrade, sau đó trả kết quả về đúng client bằng targeted ClientRpc. Sau khi nhận kết quả, client cập nhật dữ liệu cục bộ và gửi yêu cầu đồng bộ chỉ số mới bằng ServerRpc để các NetworkVariable trong zone được cập nhật.

Dưới đây là đoạn mã rút gọn thể hiện luồng nâng Gene thông qua ServerRpc, REST API và targeted ClientRpc. Đoạn mã này được rút từ command service chạy trên Unity zone server:

```csharp
[ServerRpc(RequireOwnership = false)]
public void UpgradeGeneServerRpc(string requestJson, ServerRpcParams rpcParams = default)
{
    if (!IsServer) return;
    ulong cid = rpcParams.Receive.SenderClientId;
    string jwt = ResolveClientJwt(cid);

    StartCoroutine(DoPost(
        $"{ApiBase}/gene/upgrade", requestJson, jwt,
        json => GeneUpgradeResultClientRpc(json, Target(cid)),
        err  => GeneUpgradeResultClientRpc(ErrorJson(err), Target(cid))
    ));
}
```

3.1.5.1Cơ chế khắc chế Gene và tính sát thương nguyên tố

Bên cạnh việc tăng chỉ số, Gene còn lưu thông tin hệ của người chơi để phục vụ UI, chọn Gene phụ, Hybrid Fusion và các hàm hỗ trợ khắc chế. Dữ liệu hệ của người chơi nằm trong info_char gồm element_type, secondary_element, is_hybrid, hybrid_bonus_targets, hybrid_immune_elements và hybrid_atk_bonus_pct. Dữ liệu hệ của quái/boss có trong bảng enemy gồm element_type, khang_hoa, khang_thuy, khang_tho, khang_moc, khang_kim, khang_phong, tang_dame_* và counter_rate; tuy nhiên cần tách rõ dữ liệu CSDL/API với dữ liệu đã được runtime combat sử dụng. Qua rà soát hai DTO spawn quái chính là `EnemySkillsEntry` (map thường) và `DungeonWaveEnemySpawnDto` (phó bản wave), cả hai chỉ truyền các trường cơ bản gồm `element_type`, `base_damage`, `base_defense` và `base_hp` vào component quái khi spawn; không có trường `tang_dame_*`, `counter_rate`, hay `khang_*` nào được ánh xạ từ DB vào runtime động. Các giá trị kháng nguyên tố (`khangHoa`, `khangThuy`, v.v.) và tỉ lệ phản đòn (`counterRate`) trên `MobPatrolAI` được đặt thủ công trong prefab qua Unity Inspector, không được load từ bảng enemy khi spawn. Điều này có nghĩa là cùng một loại quái có thể có giá trị kháng khác nhau tùy theo cách cấu hình prefab, chứ không nhất thiết phản ánh đúng cột `khang_*` trong CSDL. Vì vậy báo cáo không gộp `tang_dame_*` và `counter_rate` vào công thức sát thương cuối nếu không có đoạn runtime trực tiếp áp dụng từ dữ liệu DB.

Kiến trúc tính sát thương tập trung qua `DamageCalculator`: Để thống nhất các công thức rải rác vốn tồn tại trong từng component, dự án đã hiện thực lớp tiện ích tĩnh `DamageCalculator` tại `Assets/Scripts/Utilities/DamageCalculator.cs`. Lớp này không cần instance, chứa toàn bộ logic công thức của các nhánh combat, và được gọi lại từ từng component: `MobPatrolAI` gọi `CalcEnemyReceivedDamage()`, `BossController` gọi `CalcBossReceivedDamage()`, `NetworkPlayerHealth` gọi `CalcPlayerReceivedElementDamage()`, `DungeonEnemyRuntimeStats` gọi `CalcDungeonEnemyReceivedDamage()`, và cả `PlayerCombat` lẫn `FireballDamage` đều gọi `CalcPlayerAttackDamage()` cho đánh thường và projectile. Mỗi method của `DamageCalculator` chỉ nhận tham số dữ liệu thuần túy, không phụ thuộc vào bất kỳ Singleton hay MonoBehaviour nào, đảm bảo công thức nhất quán giữa tất cả các điểm gọi.

Quan hệ hệ và cặp Hybrid: `ElementHelper.GetCounteredElement()` định nghĩa vòng khắc chế Ngũ Hành ở mức helper: Kim khắc Mộc, Mộc khắc Thủy, Thủy khắc Hỏa, Hỏa khắc Thổ và Thổ khắc Kim; hệ Phong không nằm trong vòng này và trả về null. Phần chọn Gene phụ/Hybrid không dùng toàn bộ vòng này mà dùng cặp cố định trong `ElementHelper.GetFixedSecondary()` và `GeneController.PartnerMap`: Hỏa ↔ Thổ, Thủy ↔ Mộc, Kim ↔ Phong. Đây là logic đang được backend kiểm tra khi gọi API Hybrid Fusion.

Tăng sát thương theo buff và Hybrid Gene bonus: Đòn đánh thường (`PlayerCombat`) và projectile (`FireballDamage`) đều tính sát thương qua `DamageCalculator.CalcPlayerAttackDamage(baseDamage, attackBonusPct, attackerData, targetElementType)`. Hàm này xử lý hai lớp tăng dần: trước tiên áp AttackBuff (nếu có), sau đó kiểm tra Hybrid Gene bonus — nếu người tấn công là Hybrid và hệ mục tiêu nằm trong `hybrid_bonus_targets`, nhân thêm `(1 + hybrid_atk_bonus_pct / 100)`. Đây là lần đầu tiên `hybrid_atk_bonus_pct` được áp dụng trực tiếp vào runtime combat; trước đó trường này chỉ được lưu CSDL và trả qua API mà không ảnh hưởng đến sát thương thực tế. `ActiveBuffManager.GetBonusPct("AttackBuff")` trả về dạng thập phân, ví dụ value = 15 thì trả 0.15.

```text
Sát thương đánh thường (sau buff + Hybrid bonus):
    Bước 1: damage = Round(baseDamage x (1 + attackBonusPct))
    Bước 2: nếu là Hybrid và hệ enemy ∈ hybrid_bonus_targets:
                damage = Round(damage x (1 + hybrid_atk_bonus_pct / 100))
```

Đoạn code tương ứng (qua DamageCalculator):

```csharp
float attackBonusPct = ActiveBuffManager.Instance != null
    ? ActiveBuffManager.Instance.GetBonusPct("AttackBuff") : 0f;
int finalDamage = DamageCalculator.CalcPlayerAttackDamage(
    stats.baseDamage, attackBonusPct, myPlayerData, targetElement);
```

Riêng `FireballDamage` nhận `attackBonusPercent` qua `SetAttackBonus(int bonusPercent)`, sau đó khi va chạm cũng gọi `DamageCalculator.CalcPlayerAttackDamage()` để đảm bảo đường đi projectile nhất quán với đánh thường. `attackBonusPercent` được thiết lập trong `PlayerSkillManager.SpawnProjectile()` ngay sau `SetDamage()`, đọc từ `ActiveBuffManager` của owner:

```csharp
if (ActiveBuffManager.Instance != null)
{
    int atkBonusPct = Mathf.RoundToInt(ActiveBuffManager.Instance.GetBonusPct("AttackBuff") * 100f);
    if (atkBonusPct > 0) fireballDmg.SetAttackBonus(atkBonusPct);
}
```

Kháng nguyên tố của mục tiêu: Công thức kháng nguyên tố thật sự xuất hiện trong hai nhánh runtime. Với quái dùng `MobPatrolAI.TakeDamageWithElement()`, hệ nguyên tố truyền vào là số 1=Hỏa, 2=Thủy, 3=Thổ, 4=Mộc, 5=Kim, 6=Phong. Runtime lấy kháng từ các field trên component `MobPatrolAI`, không tự đọc trực tiếp từ bảng enemy trong hàm này.

```text
Sát thương sau kháng = Max(1, Round(Sát thương gốc x (1 - Chỉ số kháng / 100)))
```

Nếu mục tiêu đang bị `isWeakened`, `MobPatrolAI` tiếp tục nhân sát thương sau kháng thêm 1.3 lần:

```text
Sát thương quái nhận = Round(Max(1, Round(rawDamage x (1 - resist / 100))) x 1.3)
```

Với boss dùng `NetworkBossHealth` và `BossController.HandleBeforeTakeDamage()`, boss có thể né trước; nếu không né, `BossController` lấy kháng từ `BossData` theo elementType dạng chuỗi `Hoa`, `Thuy`, `Tho`, `Moc`, `Kim`, `Phong` rồi tính:

```text
Sát thương boss nhận = Max(1, Round(rawDamage x (1 - resist / 100)))
```

Trong luồng network enemy phổ biến qua `NetworkEnemyHealth.TakeDamageInternal()`, hàm chỉ nhận damage số, không nhận element. Nếu enemy thuộc dungeon có `DungeonEnemyRuntimeStats`, damage được giảm theo phòng thủ:

```text
Sát thương sau giáp dungeon = Max(1, rawDamage - Defense)
```

Khắc hệ nguyên tố tác động lên người chơi: Khi nhân vật người chơi nhận sát thương từ nguồn có gắn hệ nguyên tố (ví dụ quái `MobPatrolAI` gọi `nph.TakeDamageWithElement(counterDmg, elementType)` khi phản đòn), `NetworkPlayerHealth.TakeDamageWithElementInternal()` kiểm tra xem `attackerElement` có phải hệ khắc của người chơi không bằng cách gọi `ElementHelper.GetElementThatCounters(pd.element_type)`. Nếu trùng, sát thương người chơi nhận tăng 30%:

```text
Nếu attackerElement khắc hệ người chơi:
    finalDamage = Round(rawDamage x 1.3)
Nếu không khắc:
    finalDamage = rawDamage
```

Bảng Ngũ Hành Tương Khắc áp dụng trong runtime (theo ElementHelper.GetCounteredElement()):

**Bảng 3.X — Quan hệ khắc chế Ngũ Hành trong runtime**

| Hệ tấn công | Hệ bị khắc |
|---|---|
| Kim — Metal | Mộc — Wood |
| Mộc — Wood | Thủy — Water |
| Thủy — Water | Hỏa — Fire |
| Hỏa — Fire | Thổ — Earth |
| Thổ — Earth | Kim — Metal |
| Phong — Wind | — (không tham gia vòng khắc chuẩn) |

Hybrid miễn khắc hệ trong runtime: Sau khi hoàn thành Hybrid Fusion, backend ghi chuỗi CSV vào trường `HybridImmuneElements` trong `info_char` theo cột `immune_elements` của bảng `gene_hybrid_config`. Khi người chơi bị tấn công bởi hệ nguyên tố, `NetworkPlayerHealth.TakeDamageWithElementInternal()` gọi `ElementHelper.IsImmuneToCounter(attackerElement, pd)` trước khi áp hệ số +30%. Nếu `attackerElement` nằm trong `HybridImmuneElements`, phần tăng bị bỏ qua và người chơi nhận đúng sát thương gốc:

```text
Nếu IsImmuneToCounter(attackerElement, pd) = true:
    finalDamage = rawDamage  // không áp +30%
```
Theo dữ liệu gene_hybrid_config và rule kiểm tra cặp PartnerMap trong GeneController, chỉ ba tổ hợp Hybrid được phép trong hệ thống hiện tại, kèm danh sách hệ miễn tương ứng:

**Bảng 3.X+1 — Ba tổ hợp Hybrid hợp lệ và hệ miễn khắc**

| Tổ hợp Hybrid | Tên Hybrid | Hệ miễn khắc (immune\_elements) |
|---|---|---|
| Hỏa (Fire) + Thổ (Earth) | Dung Nham Địa Hỏa | Thủy (Water), Mộc (Wood) |
| Thủy (Water) + Mộc (Wood) | Băng Độc Vĩnh Hằng | Kim (Metal), Hỏa (Fire) |
| Kim (Metal) + Phong (Wind) | Kim Phong Thoán Thế | Hỏa (Fire), Thổ (Earth) |

Nhân vật Hỏa+Thổ Hybrid sẽ không còn nhận thêm 30% khi bị Thủy hoặc Mộc tấn công. Nhân vật Thủy+Mộc Hybrid miễn với Kim và Hỏa. Nhân vật Kim+Phong Hybrid miễn với Hỏa và Thổ. Cơ chế này đã được hiện thực trực tiếp trong NetworkPlayerHealth.TakeDamageWithElementInternal(), cơ chế miễn khắc hệ này độc lập với `HybridAtkBonusPct` (bonus tấn công) vốn được áp riêng trong `CalcPlayerAttackDamage()`.

Dữ liệu tăng sát thương Hybrid: Khi nhân vật fuse Hybrid thành công, backend ghi danh sách hệ bị khắc vào `HybridBonusTargets` và phần trăm tăng sát thương vào `HybridAtkBonusPct`, ánh xạ từ cột `atk_bonus_percent` của bảng `gene_hybrid_config`. Hai trường này được trả về qua API mỗi lần client đồng bộ dữ liệu nhân vật. Sau khi hiện thực `DamageCalculator`, `hybrid_atk_bonus_pct` đã được áp dụng thực sự vào runtime combat thông qua `CalcPlayerAttackDamage()`: nếu hệ của enemy nằm trong `hybrid_bonus_targets`, sát thương được nhân thêm hệ số tương ứng.

```text
Sát thương sau Hybrid bonus = Round(damage × (1 + hybrid_atk_bonus_pct / 100))
                          (chỉ áp khi hệ enemy thuộc hybrid_bonus_targets)
```

Dữ liệu miễn/giảm khắc Hybrid: Như trình bày ở trên, cơ chế miễn khắc hệ đã được hiện thực trong `NetworkPlayerHealth.TakeDamageWithElementInternal()` thông qua `ElementHelper.IsImmuneToCounter()`. Trường `HybridImmuneElements` là chuỗi CSV được backend ghi vào `info_char` ngay khi fusion thành công (`cfg.ImmuneElements` từ bảng `gene_hybrid_config`), và được trả về qua API mỗi lần client đồng bộ dữ liệu nhân vật để zone server có thể kiểm tra khi xử lý sát thương.

Phản đòn của quái đặc biệt: Runtime phản đòn nằm trong `MobPatrolAI`. Sau khi nhận sát thương bằng `TakeDamageWithElement()`, nếu `counterRate > 0` và số ngẫu nhiên trong khoảng 0-100 nhỏ hơn counterRate, quái kích hoạt `CounterAttack()`. Sát thương phản đòn dùng field `baseDamage` trên component, với công thức:

```text
Sát thương phản đòn = Max(1, Round(baseDamage x 0.6))
```

Do `counter_rate` trong bảng enemy không được DTO spawn ánh xạ vào `MobPatrolAI.counterRate`, giá trị này được đặt thủ công trong prefab. Vì vậy, cơ chế phản đòn là đặc tính cấu hình theo từng prefab, không phải mọi enemy có `counter_rate` trong DB đều tự phản đòn trong runtime.

Tổng quát, các công thức có chứng cứ trực tiếp trong runtime hiện tại là:

```text
── Người chơi tấn công (DamageCalculator.CalcPlayerAttackDamage) ──
Đánh thường / projectile:
    damage = Round(baseDamage x (1 + attackBonusPct))    // AttackBuff
    if is_hybrid AND hệ enemy ∈ hybrid_bonus_targets:
        damage = Round(damage x (1 + hybrid_atk_bonus_pct / 100))  // Hybrid bonus

── Quái/Boss nhận damage từ người chơi ──
MobPatrolAI (DamageCalculator.CalcEnemyReceivedDamage):
    actual = Max(1, Round(rawDamage x (1 - resist / 100)))
    if isWeakened:
        actual = Round(actual x 1.3)

BossController (DamageCalculator.CalcBossReceivedDamage):
    if TryDodge() = true:
        finalDamage = 0
    else:
        finalDamage = Max(1, Round(rawDamage x (1 - resist / 100)))

Dungeon enemy (DamageCalculator.CalcDungeonEnemyReceivedDamage):
    damage = Max(1, rawDamage - Defense)

── Người chơi nhận damage có hệ nguyên tố (DamageCalculator.CalcPlayerReceivedElementDamage) ──
counterOf = ElementHelper.GetElementThatCounters(pd.element_type)
if attackerElement == counterOf:
    if ElementHelper.IsImmuneToCounter(attackerElement, pd):  // Hybrid miễn
        finalDamage = rawDamage
    else:
        finalDamage = Round(rawDamage x 1.3)  // khắc hệ +30%
else:
    finalDamage = rawDamage
```

Dưới đây là mã nguồn xử lý giảm sát thương theo kháng nguyên tố trong runtime AI của quái. Đây là phần trực tiếp sinh ra công thức Sát thương sau kháng ở trên:

```csharp
public void TakeDamageWithElement(int rawDamage, int element = 0)
{
    if (evasionRate > 0 && UnityEngine.Random.Range(0f, 100f) < evasionRate)
    {
        ShowFloatingText("Miss!");
        return;
    }

    float resist = GetResistance(element);
    int actual = DamageCalculator.CalcEnemyReceivedDamage(rawDamage, resist, isWeakened);

    _health.TakeDamage(actual);

    if (counterRate > 0 &&
        UnityEngine.Random.Range(0f, 100f) < counterRate)
    {
        StartCoroutine(CounterAttack());
    }
}
```

3.1.6Hệ thống kỹ năng, phó bản và Zone runtime

Project không vận hành toàn bộ thế giới như một scene đơn. Thay vào đó, map được chia thành các zone logic và các phòng phó bản độc lập. Cách tổ chức này giúp nhiều nhóm người chơi có thể hoạt động song song trong cùng một map hoặc trong các dungeon riêng.

Quản lý zone: Server duy trì registry room theo mapId, zoneId và custom room. Registry này cho phép tra cứu room hiện tại, tìm zone ít tải, kiểm tra hai client có cùng zone hay không và đăng ký room phó bản riêng.

Chuyển zone và vào phó bản: Luồng chuyển khu vực sử dụng ServerRpc cho các thao tác chuyển map, vào dungeon cá nhân, vào dungeon tổ đội và thoát dungeon. Khi người chơi qua cổng, server cập nhật room, lưu vị trí và gửi ClientRpc để client load scene/entry point phù hợp.

Lọc hiển thị theo zone: Hệ thống sử dụng cơ chế NetworkObject visibility của Unity Netcode. Nếu client không cùng zone hoặc không cùng custom room, server gọi NetworkHide để ẩn object khỏi client đó. Khi người chơi đổi zone, visibility được refresh để cập nhật lại danh sách object được nhìn thấy.

Phó bản theo wave: Runtime phó bản được quản lý theo từng zone encounter độc lập. Mỗi encounter có dungeonId, config, round hiện tại, thời gian còn lại và danh sách enemy đang sống. Session phó bản được lưu theo userId để hỗ trợ reconnect không mất tiến trình.

Phần thưởng phó bản: Unity zone server gửi request về backend bằng X-Zone-Api-Key để cộng vật phẩm và phần thưởng cho người chơi sau khi hoàn thành phó bản hoặc đạt mốc wave. Client không trực tiếp gọi API nhận thưởng.

----- [BẮT ĐẦU PHẦN THÊM MỚI] -----

3.1.6.1 Hệ thống kỹ năng chi tiết theo từng lớp nguyên tố

Hệ thống chiến đấu của trò chơi phân chia nhân vật thành 6 lớp nguyên tố, mỗi lớp sở hữu bộ 4 kỹ năng chủ động (phím tắt Q, W, E, R) với các hiệu ứng đặc trưng riêng biệt:

*   **Lớp Kim (Metal) - Sát thương vật lý & bạo kích:**
    *   *Kỹ năng Q - Kim Kiếm:* Bắn ra luồng phi kiếm kim loại xuyên thấu, gây sát thương vật lý và tăng 10% tỉ lệ bạo kích trong 5 giây.
    *   *Kỹ năng W - Kim Quang Trảm:* Chém nhanh hình cánh quạt phía trước, gây sát thương bộc phát và làm giảm 15% phòng ngự kẻ địch.
    *   *Kỹ năng E - Thiết Giáp:* Kích hoạt trạng thái kim loại hóa, tăng 30% chỉ số DEF trong thời gian 10 giây.
    *   *Kỹ năng R (Ultimate) - Vạn Kiếm Quy Tông:* Gọi mưa kiếm từ trên trời rơi xuống khu vực chỉ định, gây sát thương vật lý liên tục diện rộng và làm giảm 40% tốc độ chạy của mọi mục tiêu trúng đòn.
*   **Lớp Mộc (Wood) - Độc tố & hồi phục:**
    *   *Kỹ năng Q - Độc Diệp:* Phóng lá độc gây sát thương ban đầu và áp dụng hiệu ứng DoT Poison (rút HP theo giây) kéo dài 6 giây.
    *   *Kỹ năng W - Mộc Phược:* Rễ cây trồi lên từ mặt đất trói chân mục tiêu, gây hiệu ứng Choáng/Trói chân (Stun/Bind) trong 2 giây.
    *   *Kỹ năng E - Trị Liệu Sinh Mệnh:* Triệu hồi luồng sinh khí hồi phục 3% tối đa HP mỗi giây (Regeneration) cho bản thân và đồng đội trong bán kính nhỏ.
    *   *Kỹ năng R (Ultimate) - Mộc Thần Giáng Lâm:* Triệu hồi vùng rừng cây gai sắc nhọn, gây sát thương phép nguyên tố Mộc cực lớn và trói chân diện rộng toàn bộ kẻ địch trúng chiêu.
*   **Lớp Thủy (Water) - Làm chậm & đóng băng:**
    *   *Kỹ năng Q - Băng Thương:* Bắn thương băng tầm xa gây sát thương phép Thủy và làm chậm 35% tốc độ di chuyển của kẻ địch.
    *   *Kỹ năng W - Băng Giáp:* Tạo lớp lá chắn băng hấp thụ sát thương. Kẻ địch tấn công cận chiến vào lá chắn sẽ bị làm chậm tốc độ đánh 20%.
    *   *Kỹ năng E - Trị Liệu Thuật:* Hồi phục ngay lập tức một lượng HP lớn tương đương 15% Max HP của bản thân.
    *   *Kỹ năng R (Ultimate) - Thủy Long Trảo:* Triệu hồi rồng nước khổng lồ cuốn quét qua khu vực, gây sát thương phép diện rộng và đóng băng hoàn toàn (Freeze) kẻ địch trong 2.5 giây.
*   **Lớp Hỏa (Fire) - Thiêu đốt & bộc phát sát thương:**
    *   *Kỹ năng Q - Hỏa Cầu:* Phóng cầu lửa nổ gây sát thương phép nguyên tố Hỏa và kích hoạt hiệu ứng cháy (Burn, rút HP liên tục).
    *   *Kỹ năng W - Hỏa Bạo:* Gây nổ xung quanh bản thân, đẩy lùi (Knockback) toàn bộ kẻ địch đang tiếp cận cận chiến.
    *   *Kỹ năng E - Hỏa Giáp:* Kích hoạt hào quang lửa, tăng 20% chỉ số ATK của bản thân và phản lại 10% sát thương nhận vào dưới dạng sát thương lửa.
    *   *Kỹ năng R (Ultimate) - Hỏa Thần Phẫn Nộ:* Phun trào cột lửa khổng lồ từ lòng đất tại vị trí mục tiêu, gây sát thương phép diện rộng cực đại.
*   **Lớp Thổ (Earth) - Phòng ngự & khống chế cứng:**
    *   *Kỹ năng Q - Thạch Tiễn:* Bắn mũi tên đá cứng gây sát thương vật lý và đẩy lùi nhẹ mục tiêu.
    *   *Kỹ năng W - Địa Chấn:* Dậm chân mạnh xuống đất làm rung chuyển mặt đất xung quanh, gây sát thương Thổ và làm choáng kẻ địch trong 1.5 giây.
    *   *Kỹ năng E - Thạch Giáp:* Tạo một khiên đá bảo vệ hấp thụ sát thương tương đương 25% lượng máu tối đa (Max HP) của nhân vật.
    *   *Kỹ năng R (Ultimate) - Hộ Thể Quyền:* Hóa đá toàn thân, tăng 60% chỉ số phòng ngự DEF và miễn nhiễm hoàn toàn với mọi hiệu ứng khống chế trong vòng 8 giây.
*   **Lớp Phong (Wind) - Cơ động & né tránh:**
    *   *Kỹ năng Q - Phong Nhận:* Bắn ra luồng gió sắc bén xuyên qua nhiều kẻ địch trên đường thẳng.
    *   *Kỹ năng W - Phong Đao:* Chém ra các lốc xoáy nhỏ kéo (Pull) kẻ địch lại gần nhau để chuẩn bị cho combo đồng đội.
    *   *Kỹ năng E - Phong Linh Tốc:* Tăng 45% tốc độ di chuyển và cộng 20% tỉ lệ né tránh (Evasion) của bản thân trong 6 giây.
    *   *Kỹ năng R (Ultimate) - Bão Phong Loạn Vũ:* Tạo cơn bão gió xoáy cuộn quét liên tục tại vị trí chọn, gây sát thương diện rộng liên tục và hút nhẹ kẻ địch vào tâm bão.

----- [KẾT THÚC PHẦN THÊM MỚI] -----

3.1.7 Hệ thống chat, bạn bè và tổ đội trên client

Các chức năng xã hội trong project được triển khai bằng SignalR để tách khỏi luồng mô phỏng gameplay của Unity Netcode.

Chat nhiều kênh: Client kết nối SignalR tới /chathub, hỗ trợ World, Proximity, Clan, Class, Group và Private. Client đăng ký các event ReceiveWorldMessage, ReceiveProximityMessage, ReceiveClanMessage, ReceiveClassMessage, ReceiveGroupMessage, ReceivePrivateMessage và ReceiveSystemMessage.

Tổ đội realtime: Client kết nối SignalR tới /partyhub, tự động reconnect, gửi UpdatePresence mỗi 5 giây và xử lý các event PartyStateUpdated, PartyInviteReceived, PartyJoinRequestReceived, PartySearchResults, NearbyPlayersUpdated, PartyDungeonRequested và PartyError.

Đồng bộ chat nhóm khi vào party: Khi trạng thái party thay đổi, client tự join hoặc leave group chat tương ứng trên SignalR. Nhờ vậy, người chơi trong cùng tổ đội có thể trò chuyện riêng mà không ảnh hưởng tới kênh world hoặc proximity.

Vào phó bản tổ đội: Khi leader gọi StartPartyDungeon, SignalR Hub phát PartyDungeonRequested đến toàn bộ thành viên trong party. Sau đó client chuyển sang luồng ServerRpc vào phó bản tổ đội để toàn bộ thành viên được đưa vào cùng dungeon room.

Dưới đây là đoạn client đăng ký một số event SignalR của tổ đội:

```csharp
_client.On("PartyStateUpdated", json =>
{
    CurrentParty = PartyStatePayload.FromJson(json);
    SyncChatGroup();
    OnPartyStateChanged?.Invoke(CurrentParty);
});

_client.On("PartyDungeonRequested", json =>
{
    var payload = PartyDungeonRequestPayload.FromJson(json);
    OnPartyDungeonRequested?.Invoke(payload);
});
```

3.1.8Tổng kết phân hệ Client

Phân hệ client Unity của DoAn hiện thực hóa một game 2D online có khả năng chơi nhiều người thông qua Unity Netcode và SignalR. Unity Netcode chịu trách nhiệm điều khiển nhân vật, đồng bộ vị trí, NetworkVariable chỉ số, HP, sát thương và phó bản runtime. SignalR chịu trách nhiệm chat, party, presence và lời mời tổ đội. REST API đảm nhiệm dữ liệu lâu dài như Gene, inventory, kỹ năng, EXP và phần thưởng. Cách chia trách nhiệm này giúp gameplay nhiều người chơi có thể mở rộng mà không làm rối lớp giao diện.

3.2Hiện thực hóa phía máy chủ dịch vụ (Backend-Side - ASP.NET Core)

3.2.1Hệ thống quản lý tài khoản và xác thực

Backend của project được xây dựng bằng ASP.NET Core .NET 9, Entity Framework Core 9, Pomelo MySQL, JWT Bearer Authentication và SignalR. Tại tầng khởi động, hệ thống đăng ký ASP.NET Core Controller, OpenAPI, CORS, SignalR Hub, memory cache, DbContext, service nghiệp vụ và cơ chế xác thực lai HybridAuth.

Đăng ký và đăng nhập: Nhóm API xác thực cung cấp hai endpoint chính POST /api/auth/register và POST /api/auth/login. Khi đăng ký, backend kiểm tra username/email trùng, băm mật khẩu bằng BCrypt rồi lưu vào bảng users. Khi đăng nhập, backend kiểm tra mật khẩu, cập nhật LastLogin và trả về JWT token, user_id và username.

JWT cho client: Hệ thống sử dụng BCrypt.Net.BCrypt để băm mật khẩu với workFactor = 12 và JwtSecurityTokenHandler để tạo JWT. Token chứa các claim sub, unique_name và user_id, dùng issuer GameServerApi và audience GameClient.

HybridAuth cho nhiều loại request: ASP.NET Core Authentication được cấu hình theo chính sách lai. Request từ client dùng Bearer JWT, còn request nội bộ từ zone server dùng Zone API Key thông qua header X-Zone-Api-Key.

SignalR token: Với /chathub và /partyhub, JWT có thể được truyền qua query parameter access_token. Cách này phù hợp với cơ chế kết nối WebSocket của SignalR trong Unity.

Điểm tiếp nhận xác thực của backend là nhóm API Auth. Luồng đăng nhập dưới đây được rút từ endpoint POST /api/auth/login:

```csharp
[HttpPost("login")]
public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
{
    var user = await _db.Users
        .FirstOrDefaultAsync(u => u.Username == request.Username);

    if (user == null || !_authService.VerifyPassword(request.Password, user.PasswordHash))
        return Unauthorized("Sai username hoặc password.");

    user.LastLogin = DateTime.UtcNow;
    await _db.SaveChangesAsync();

    var token = _authService.GenerateJwtToken(user);
    return Ok(new { token, user_id = user.UserId, username = user.Username });
}
```

3.2.2Hệ thống dữ liệu nhân vật, inventory, trang bị và kỹ năng

Dữ liệu nhân vật được backend lưu trong hai bảng player_data và player2_data. Mỗi bản ghi chứa các cột JSON như info_char, equipment, inventory, skills, potential_stats và active_buffs. Cách lưu này giúp hệ thống dễ mở rộng thuộc tính nhân vật mà không phải thay đổi schema cho từng chỉ số nhỏ.

Tải dữ liệu nhân vật: Nhóm API Player cung cấp GET /api/player/{playerId}/data và GET /api/player/{playerId}/data2. Response trả về thông tin nhân vật, trang bị, inventory, skills, potential_stats, active_buffs và final_stats để Unity client dựng lại trạng thái đầy đủ.

Tạo nhân vật và slot Gene thứ hai: POST /api/player/create tạo nhân vật chính, còn POST /api/player/create2 tạo dữ liệu nhân vật slot 2 trong player2_data. Slot 2 có thể dùng hệ Gene khác, kỹ năng khác và dữ liệu phát triển riêng.

Inventory dạng JSON: Các endpoint /inventory/add, /inventory/clear, /inventory/sort và /inventory/use-item thao tác trên inventory JSON. Khi Unity server cần thêm vật phẩm từ dungeon hoặc lệnh hệ thống, request được gửi về backend để dữ liệu túi đồ được lưu thống nhất.

Trang bị và túi mở rộng: Các endpoint /equipment/equip, /equipment/unequip và /bag/unequip cập nhật equipment JSON, xử lý túi mở rộng, chỉ số trang bị và tính lại final_stats thông qua StatCalculator.

Kỹ năng và tiềm năng: Backend cung cấp /skills, /skills/upgrade, /potential, /potential/upgrade, /potential/allocate. Khi người chơi nâng kỹ năng hoặc cộng điểm tiềm năng, server kiểm tra điều kiện rồi ghi lại dữ liệu vào MySQL.

EXP và lên cấp: POST /api/player/{playerId}/gain-exp nhận lượng EXP từ Unity server, cập nhật experience/level và trả lại trạng thái mới. Đây là điểm nối giữa combat realtime và tiến trình nhân vật lâu dài.

3.2.3Hệ thống Gene Evolution, Gene phụ và Hybrid Fusion ở backend

Nhóm API Gene là trung tâm nghiệp vụ của hệ Gene, sử dụng route /api/gene và yêu cầu xác thực. Tầng controller xử lý cấu hình Gene chính, nâng cấp Gene, chọn Gene phụ, nâng Gene phụ, lấy danh sách Gene và dung hợp Hybrid.

Cấu hình Gene chính: GET /api/gene/config đọc gene_upgrade_config, item_template, gene_tier_stat_config và skill_template để trả về chi phí nâng cấp, Gene EXP yêu cầu, vật phẩm, số lượng tối thiểu/tối đa, tỉ lệ thành công, bonus stat và kỹ năng mở ở tier tiếp theo.

Nâng Gene chính: POST /api/gene/upgrade kiểm tra playerId, Gene EXP, vàng, vật phẩm trong inventory và giới hạn Tier 5. Tỉ lệ thành công được tính bằng baseSuccessRate nhân với tỉ lệ itemCount/itemsNeeded. Nếu thành công, server tăng GeneTier, cộng stat từ gene_tier_stat_config, hồi đầy HP/MP, mở skill mới và trả về final_stats.

Danh sách Gene: GET /api/gene/list trả về Gene chính, Gene phụ, EXP, trạng thái isHybrid, tên Hybrid, hybridBonusTargets, hybridImmuneElements, hybridAtkBonusPct và canFuse. Client dùng dữ liệu này để hiển thị toàn bộ trạng thái Gene.

Chọn Gene phụ: POST /api/gene/secondary/select chỉ cho chọn hệ phụ theo bảng ánh xạ hợp lệ: Fire với Earth, Water với Wood, Metal với Wind. Khi hợp lệ, server set SecondaryElement, SecondaryGeneTier = 1 và SecondaryGeneExp = 0.

Nâng Gene phụ: GET /api/gene/multi/config và POST /api/gene/secondary/upgrade sử dụng bảng gene_multi_config. Khi nâng thành công, hệ phụ tăng tier và cộng 50% bonus stat so với cấu hình gene_tier_stat_config.

Dung hợp Hybrid: GET /api/gene/hybrid/config kiểm tra điều kiện fuse. POST /api/gene/hybrid/fuse yêu cầu Gene chính Tier 5, Gene phụ Tier 5, cặp hệ hợp lệ, đủ vàng và đủ item fuse. Khi thành công, backend ghi trạng thái Hybrid vào info_char và cộng bonus stat từ gene_hybrid_config.

----- [BẮT ĐẦU PHẦN THÊM MỚI] ----- Phát triển Gene Tối Thượng (Ultimate Gene): Sau khi nhân vật hoàn tất dung hợp Hybrid, người chơi có thể tích lũy EXP Tối Thượng (`ultimate_gene_exp`) để đạt cấp tiến hóa cao nhất. Cấu hình ngưỡng kích hoạt (mặc định `1,000,000` EXP), hệ số nhân chỉ số (nhân x1.5 toàn bộ chỉ số HP, MP, ATK, DEF) và tài nguyên hào quang được lưu trong `GeneUltimateSettings` (tại `GeneUltimateConfig.cs`). Khi đạt đủ EXP qua diệt quái/boss hoặc sử dụng vật phẩm hỗ trợ, server kích hoạt `is_ultimate = true`, đồng thời `StatCalculator` tính toán lại và nhân x1.5 toàn bộ chỉ số thuộc tính cơ bản của nhân vật để đồng bộ qua mạng. ----- [KẾT THÚC PHẦN THÊM MỚI] -----

Đoạn xử lý tỉ lệ nâng Gene ở tầng backend được rút từ endpoint POST /api/gene/upgrade như sau:

```csharp
itemCount = Math.Clamp(itemCount, cfg.ItemsMin, cfg.ItemsNeeded);

float successRate = cfg.BaseSuccessRate *
    Math.Min((float)itemCount / cfg.ItemsNeeded, 1f);
successRate = Math.Clamp(successRate, 0f, 1f);
bool success = new Random().NextDouble() < successRate;

info.Gold -= cfg.GoldCost;
info.GeneExp = Math.Max(0, info.GeneExp - cfg.GeneExpRequired);
```

Khi Hybrid Fusion thành công, backend ghi dữ liệu khắc chế Hybrid vào info_char. Các trường dưới đây được rút từ luồng POST /api/gene/hybrid/fuse:

```csharp
info.IsHybrid = true;
info.HybridElementA = info.ElementType;
info.HybridElementB = info.SecondaryElement;
info.HybridBonusTargets = cfg.BonusTargetElements;
info.HybridImmuneElements = cfg.ImmuneElements;
info.HybridAtkBonusPct = cfg.AtkBonusPercent;
info.HybridId = cfg.HybridId;
info.HybridPrefabPath = cfg.PrefabPath;
```

3.2.4Hệ thống bạn bè, chat và tổ đội realtime

Hệ thống sử dụng kết hợp REST API và SignalR để hiện thực chức năng xã hội. REST API quản lý dữ liệu bạn bè qua HTTP, còn SignalR Hub xử lý chat, presence, lời mời tổ đội và cập nhật trạng thái realtime.

REST API bạn bè: Route /api/friends cung cấp GET /api/friends, POST /api/friends/request, PUT /api/friends/{id}/accept, DELETE /api/friends/{id} và GET /api/friends/search?q=. Backend lấy userId từ claim JWT để đảm bảo người chơi chỉ thao tác với quan hệ bạn bè của chính mình.

SignalR Hub cho chat: Hub /chathub hỗ trợ SendWorldMessage, SendProximityMessage, JoinMap, LeaveMap, SendClanMessage, SendClassMessage, SendGroupMessage, JoinGroup, LeaveGroup và SendPrivateMessage. Mỗi loại chat được phát tới group SignalR tương ứng như map_{mapId}, clan_{clanId}, class_{classType} hoặc group_{groupId}.

SignalR Hub cho tổ đội: Hub /partyhub lưu trạng thái runtime bằng ConcurrentDictionary, gồm Parties, PresenceByUser và ConnectionsByUser. Các hàm chính gồm UpdatePresence, CreateParty, InviteMember, RequestJoinParty, AcceptJoinRequest, RejectJoinRequest, LeaveParty, DisbandParty, SetLock, SetAutoAccept, GetPartiesInZone, GetNearbyPlayers và StartPartyDungeon.

Giới hạn tổ đội: Hub tổ đội đặt MaxPartyMembers = 4. Leader là người tạo party hoặc người còn lại được chuyển quyền khi leader rời. Khi party thay đổi, backend phát PartyStateUpdated đến SignalR group của party.

Tìm party và người chơi gần khu vực: Client gọi GetPartiesInZone(mapId, zoneId) hoặc GetNearbyPlayers(mapId, zoneId). Backend dựa vào presence do client gửi định kỳ để trả về các party còn slot và người chơi đang cùng map/zone.

Luồng mời người chơi vào party được hiện thực bằng SignalR. Khi leader gọi InviteMember, backend tạo hoặc lấy party hiện tại, kiểm tra số lượng thành viên rồi gửi PartyInviteReceived đến target user.

3.2.5Hệ thống dungeon, wave session và phần thưởng

Nhóm API Dungeon cung cấp dữ liệu cấu hình phó bản, session phó bản và wave dungeon cho Unity. Phần runtime diễn ra trên Unity zone server, còn backend giữ vai trò nguồn cấu hình và nơi lưu tiến trình/entry.

Danh sách và chi tiết phó bản: GET /api/dungeon/list và GET /api/dungeon/{dungeonId} trả về cấu hình phó bản để client hiển thị NPC phó bản và điều kiện tham gia.

Session phó bản: Các endpoint /session/create, /session/{sessionId}/join, /session/{sessionId}/leave và /session/{sessionId}/end dùng để quản lý session phó bản thông thường.

Cấu hình map và wave runtime: GET /api/dungeon/map/{mapId}/setup trả về cấu hình map. GET /api/dungeon/wave/{dungeonId}/config trả về cấu hình wave, enemy, boss và phần thưởng để Unity server spawn runtime.

Kiểm tra lượt vào wave: GET /api/dungeon/wave/{dungeonId}/entry-status/{playerId} kiểm tra trạng thái vào phó bản của người chơi. POST /api/dungeon/wave/{dungeonId}/enter ghi nhận lượt vào.

Cập nhật và kết thúc wave session: POST /api/dungeon/wave/{dungeonId}/session/update và /session/end được Unity server gọi để lưu tiến trình wave và kết quả phó bản.

Cấp thưởng an toàn: DungeonRewardController được bảo vệ bằng ZoneApiKey. Unity zone server gọi endpoint này để cấp vật phẩm sau khi người chơi hoàn thành nội dung, tránh việc client tự gọi nhận thưởng.

3.2.6Hệ thống Zone Server và đồng bộ hạ tầng runtime

Hệ thống sử dụng zone server Unity chạy runtime, sau đó đăng ký và gửi heartbeat về backend để backend biết server nào đang hoạt động, đang mở port nào và có bao nhiêu người chơi trong từng zone.

Đăng ký zone server: POST /api/zone/server/register nhận thông tin port và cấu hình server từ Unity. Request này yêu cầu X-Zone-Api-Key để chỉ server nội bộ mới được đăng ký.

Heartbeat định kỳ: Unity zone server gọi PUT /api/zone/server/heartbeat. Payload gồm port, tổng số player và danh sách zoneStats theo mapId, zoneId, players, max. Backend dùng dữ liệu này để theo dõi server còn sống và phân bổ người chơi vào zone phù hợp.

Hủy đăng ký server: DELETE /api/zone/server/deregister?port=... được gọi khi server dừng hoặc shutdown để backend xóa trạng thái server runtime.

Lọc visibility theo zone: Thông tin zone được Unity server quản lý bằng registry room và NetworkObject visibility. Backend không trực tiếp render đối tượng, nhưng heartbeat giúp lớp dịch vụ biết tải hiện tại của từng zone.

3.2.7Tổng kết phân hệ Backend

Backend của DoAn được tổ chức theo mô hình ASP.NET Core Controller, service, SignalR Hub và Entity Framework entity. Nhóm API Auth xử lý đăng nhập/đăng ký, nhóm API Player quản lý nhân vật, nhóm API Gene quản lý Gene và Hybrid, nhóm API Dungeon quản lý phó bản, nhóm API Friend quản lý bạn bè, SignalR Hub xử lý realtime social và nhóm API Zone Server giám sát zone server. Hệ thống sử dụng ASP.NET Core, JWT, SignalR và MySQL, phù hợp với kiến trúc game online nhiều người chơi có dữ liệu nhân vật lưu lâu dài.

3.3Đặc tả giao diện lập trình ứng dụng (RESTful API Specifications)

Hệ thống cung cấp tập hợp API REST dùng JSON làm định dạng payload. Các API công khai gồm đăng ký và đăng nhập. Các API người chơi thường dùng JWT qua header Authorization: Bearer <JWT_TOKEN>; API nội bộ của zone server dùng X-Zone-Api-Key ở các controller có cấu hình scheme tương ứng. Riêng DungeonController hiện tại chưa gắn [Authorize], nên các endpoint /api/dungeon/* trong code không tự bắt buộc JWT hoặc Zone API Key. Danh sách endpoint trong mục này được đối chiếu từ route và HTTP attribute của các ASP.NET Core Controller trong backend. Các JSON mẫu dưới đây giữ đúng tên field do controller trả về; giá trị động như token, id hoặc timestamp được ghi bằng dạng <...> để tránh nhầm thành dữ liệu seed thật.

3.3.1Nhóm API xác thực (Auth Controller)

3.3.1.1API đăng ký tài khoản - POST /api/auth/register

Mô tả: Tạo tài khoản mới, kiểm tra username/email trùng, băm mật khẩu và trả về JWT token.

Yêu cầu:

```json
{
  "username": "<username>",
  "email": "<email>",
  "password": "<password>"
}
```

Phản hồi thành công:

```json
{
  "token": "<jwt_token>",
  "user_id": "<user_id>",
  "message": "Register thành công."
}
```

3.3.1.2API đăng nhập - POST /api/auth/login

Mô tả: Kiểm tra username/password và cấp JWT token cho client.

Yêu cầu:

```json
{
  "username": "<username>",
  "password": "<password>"
}
```

Phản hồi thành công:

```json
{
  "token": "<jwt_token>",
  "user_id": "<user_id>",
  "username": "<username>"
}
```

3.3.2Nhóm API nhân vật và inventory (Player Controller)

Nhóm API nhân vật chịu trách nhiệm tạo nhân vật, tải hồ sơ nhân vật, lưu vị trí, thao tác túi đồ, trang bị vật phẩm và cộng EXP. Tất cả endpoint trong nhóm này sử dụng JSON payload. Các request từ client yêu cầu header Authorization: Bearer <JWT_TOKEN>; một số request nội bộ từ zone server có thể dùng X-Zone-Api-Key tùy luồng runtime.

3.3.2.1API tạo nhân vật chính - POST /api/player/create

Mô tả: Tạo nhân vật chính cho tài khoản hiện tại. Backend lấy user_id từ JWT, kiểm tra tài khoản đã có nhân vật hay chưa, validate element_type và character_name, tự suy ra gender theo hệ Gene, sau đó khởi tạo info_char mặc định gồm level, experience, gold, HP, MP, element_type, gene_tier, gene_exp, bag_slots, map_id, zone_id và vị trí ban đầu.

Endpoint: POST /api/player/create

Header:

```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "element_type": "Wind",
  "character_name": "Phong"
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "player_id": 16,
  "level": 1,
  "experience": 0,
  "gold": 0,
  "map_id": 0,
  "position_x": 0,
  "position_y": 0,
  "base_stats": {
    "hp": 100,
    "max_hp": 100,
    "mp": 50,
    "max_mp": 50,
    "attack": 10,
    "defense": 0
  },
  "final_stats": {
    "hp": 100,
    "max_hp": 100,
    "mp": 50,
    "max_mp": 50,
    "attack": 10,
    "defense": 0,
    "move_speed": 5
  },
  "inventory": [],
  "skills": [],
  "element_type": "Wind",
  "gene_tier": 1,
  "gene_exp": 0,
  "is_hybrid": false,
  "gender": "Female",
  "character_name": "Phong"
}
```

Lỗi phổ biến:
o400 Bad Request: Thiếu element_type, tên nhân vật rỗng, tên nhân vật không nằm trong khoảng 3-20 ký tự, hoặc element_type không thuộc Metal, Wood, Water, Fire, Earth, Wind.
o401 Unauthorized: Token không hợp lệ hoặc không lấy được user_id từ JWT.
o409 Conflict: Tài khoản đã có nhân vật chính.

3.3.2.2API tải dữ liệu nhân vật - GET /api/player/{playerId}/data

Mô tả: Tải toàn bộ dữ liệu nhân vật để Unity dựng lại trạng thái gameplay. Backend đọc player_data, parse info_char/equipment/inventory/skills/potential_stats/active_buffs, xử lý level-up nếu đủ EXP, áp dụng buff HP/MP còn hiệu lực và tính final_stats bằng StatCalculator.

Endpoint: GET /api/player/{playerId}/data

Header:

```http
Authorization: Bearer <JWT_TOKEN>
```

Tham số đường dẫn:
oplayerId: ID nhân vật cần tải.

Phản hồi thành công (Response 200 OK):

Ví dụ dưới đây lấy theo player_id = 16 trong gamedb.sql. Các mảng dài như inventory chỉ trích một phần tử đầu để tránh làm báo cáo quá dài; tên field và kiểu dữ liệu giữ theo response thật của PlayerController.

```json
{
  "player_id": 16,
  "user_id": 16,
  "level": 100,
  "experience": 3700,
  "exp_required_for_next_level": 0,
  "exp_at_current_level": 0,
  "gold": 14990,
  "silver": 398400000,
  "map_id": 0,
  "zone_id": 0,
  "position_x": 0,
  "position_y": 0,
  "base_stats": {
    "hp": 2335,
    "max_hp": 2335,
    "mp": 566,
    "max_mp": 566,
    "attack": 760,
    "defense": 200
  },
  "equipment": {
    "weapon": {
      "itemTemplateId": 203,
      "itemName": "Kiếm Hỏa Thần",
      "itemType": 1,
      "upgradeLevel": 1,
      "strOptions": ""
    },
    "helmet": {
      "itemTemplateId": 100,
      "itemName": "Mũ Da Nam",
      "itemType": 0,
      "upgradeLevel": 0,
      "strOptions": "3,30"
    },
    "armor": null,
    "pants": null,
    "boots": null,
    "accessory": {
      "itemTemplateId": 141,
      "itemName": "Nhẫn Bạc",
      "itemType": 5,
      "upgradeLevel": 8,
      "strOptions": ""
    }
  },
  "potential_stats": {
    "attack": 505,
    "hp": 0,
    "mp": 0,
    "defense": 0,
    "gene": 0
  },
  "final_stats": {
    "hp": 2335,
    "max_hp": 2365,
    "mp": 566,
    "max_mp": 566,
    "attack": 3285,
    "defense": 200,
    "move_speed": 5
  },
  "inventory": [
    {
      "slotIndex": 0,
      "itemTemplateId": 200,
      "quantity": 1,
      "upgradeLevel": 16,
      "strOptions": "1,12"
    }
  ],
  "skills": [],
  "skill_points_available": 300,
  "potential_points_available": 0,
  "element_type": "Wind",
  "gene_tier": 5,
  "gene_exp": 1000000,
  "is_hybrid": true,
  "gender": "Female",
  "character_name": "Phong",
  "bag_slots": 35,
  "bag_equipped_items": [
    {
      "quick_slot_index": 2,
      "item_template_id": 63,
      "item_name": "Túi Mở Rộng Cấp 3",
      "slot_bonus": 5
    }
  ],
  "secondary_element": "Metal",
  "secondary_gene_tier": 5,
  "secondary_gene_exp": 0,
  "hybrid_id": 13,
  "hybrid_element_a": "Wind",
  "hybrid_element_b": "Metal",
  "hybrid_bonus_targets": "Wood,Fire",
  "hybrid_immune_elements": "Fire,Earth",
  "hybrid_atk_bonus_pct": 0.5,
  "hybrid_prefab_path": "Prefabs/Player/Hybrid/Hybrid_Metal_Wind",
  "is_ultimate": true,
  "ultimate_gene_exp": 1005000,
  "ultimate_aura_path": "Prefabs/Player/Aura/UltimateAura3"
}
```

Lỗi phổ biến:
o404 Not Found: Player không tồn tại.
o401 Unauthorized: Request không có token hợp lệ.

3.3.2.3API cập nhật vị trí nhân vật - PUT /api/player/{playerId}/position

Mô tả: Lưu map, zone và tọa độ hiện tại của người chơi khi chuyển map, thoát game hoặc disconnect. Nếu request đến từ client, backend lấy user_id từ JWT để tránh giả mạo playerId. Nếu request đến từ zone server, backend chấp nhận playerId trên URL thông qua quyền GameServer của Zone API Key.

Endpoint: PUT /api/player/{playerId}/position

Header:

```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

Hoặc đối với zone server:

```http
X-Zone-Api-Key: <ZONE_API_KEY>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "map_id": 1,
  "zone_id": 2,
  "position_x": 12.5,
  "position_y": -3.2
}
```

Yêu cầu reset về map bắt đầu:

```json
{
  "reset_to_start_map": true
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "message": "Position updated successfully",
  "map_id": 1,
  "zone_id": 2,
  "position_x": 12.5,
  "position_y": -3.2
}
```

Lỗi phổ biến:
o400 Bad Request: Thiếu map_id, position_x hoặc position_y hợp lệ.
o401 Unauthorized: Không có JWT hoặc Zone API Key hợp lệ.
o404 Not Found: Player không tồn tại.

3.3.2.4API thêm vật phẩm vào inventory - POST /api/player/{playerId}/inventory/add

Mô tả: Thêm một hoặc nhiều vật phẩm vào inventory JSON của nhân vật. Backend lấy user_id từ JWT để xác định player thật, chuẩn hóa slotIndex, loại bỏ các field dư thừa cũ, tìm ô trống trong giới hạn bag_slots và lưu lại inventory mới.

Endpoint: POST /api/player/{playerId}/inventory/add

Header:

```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "items": [
    {
      "itemTemplateId": 410,
      "quantity": 3,
      "upgradeLevel": 0,
      "strOptions": ""
    }
  ]
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "message": "Đã thêm 1 item(s) vào inventory",
  "player_id": 16,
  "inventory": [
    {
      "slotIndex": 0,
      "itemTemplateId": 410,
      "quantity": 3,
      "upgradeLevel": 0,
      "strOptions": ""
    }
  ],
  "updated_at": "<server_utc_time>"
}
```

Lỗi phổ biến:
o400 Bad Request: Thiếu field items hoặc danh sách items rỗng.
o401 Unauthorized: Không lấy được user_id từ JWT.
o404 Not Found: Player không tồn tại.

3.3.2.5API trang bị vật phẩm - POST /api/player/{playerId}/equipment/equip

Mô tả: Trang bị vật phẩm từ một slot inventory vào slot trang bị tương ứng. Backend đọc item_template để xác định loại trang bị, tháo item cũ ở slot nếu cần, đưa item cũ về inventory, xóa item mới khỏi inventory và cập nhật equipment JSON.

Endpoint: POST /api/player/{playerId}/equipment/equip

Header:

```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "inventorySlotIndex": 0
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "message": "Đã trang bị Kiếm Hỏa Thần vào slot weapon",
  "player_id": 16,
  "equipment_slot": "weapon",
  "equipment": {
    "weapon": {
      "itemTemplateId": 203,
      "itemName": "Kiếm Hỏa Thần",
      "itemType": 1,
      "upgradeLevel": 1,
      "strOptions": ""
    }
  },
  "inventory": [],
  "updated_at": "<server_utc_time>"
}
```

Lỗi phổ biến:
o400 Bad Request: Thiếu inventorySlotIndex, không tìm thấy item ở slot, item không phải trang bị hoặc túi đồ không còn ô trống để trả item cũ.
o404 Not Found: Player hoặc item_template không tồn tại.

3.3.2.6API nhận EXP - POST /api/player/{playerId}/gain-exp

Mô tả: Cộng EXP cho nhân vật sau khi hạ quái hoặc hoàn thành nội dung. Backend kiểm tra amount là số nguyên dương, cộng vào experience, tự động xử lý level-up nếu đủ EXP, lưu lại info_char và trả về trạng thái level mới.

Endpoint: POST /api/player/{playerId}/gain-exp

Header:

```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "amount": 500
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "success": true,
  "experience": 4200,
  "level": 101,
  "leveled_up": true,
  "exp_at_current_level": 200,
  "exp_for_next_level": 5000
}
```

Lỗi phổ biến:
o400 Bad Request: Thiếu amount hoặc amount không phải số nguyên dương.
o404 Not Found: Player không tồn tại.

3.3.3Nhóm API Gene (Gene Controller)

Nhóm API Gene chịu trách nhiệm đọc cấu hình Gene, nâng Gene chính, quản lý Gene phụ và dung hợp Hybrid. Dữ liệu cấu hình được đọc từ các bảng gene_upgrade_config, gene_multi_config, gene_tier_stat_config, gene_hybrid_config, gene_hybrid_skill, item_template và skill_template.

3.3.3.1API lấy cấu hình Gene chính - GET /api/gene/config

Mô tả: Trả về cấu hình nâng Gene chính theo hệ hiện tại và tier hiện tại. Backend đọc chi phí Gene EXP, vàng, vật phẩm yêu cầu, số lượng tối thiểu/tối đa, tỉ lệ thành công, bonus stat của tier kế tiếp và danh sách skill sẽ mở khóa.

Endpoint: GET /api/gene/config?elementType=Fire&tier=1

Header:

```http
Authorization: Bearer <JWT_TOKEN>
```

Tham số query:
oelementType: Hệ Gene chính, ví dụ Fire, Water, Earth, Wood, Metal, Wind.
otier: Tier hiện tại, chỉ nhận giá trị từ 1 đến 4.

Phản hồi thành công (Response 200 OK):

```json
{
  "tierFrom": 1,
  "tierTo": 2,
  "elementType": "Fire",
  "geneExpRequired": 500,
  "goldCost": 10000,
  "itemId": 17,
  "itemName": "Linh Thạch Sơ Cấp",
  "itemIcon": 651,
  "itemsMin": 2,
  "itemsNeeded": 5,
  "baseSuccessRate": 0.8,
  "statBonus": {
    "hp": 200,
    "mp": 50,
    "attack": 25,
    "defense": 8
  },
  "skillsToUnlock": [
    {
      "skillId": 12,
      "skillName": "Hỏa Cầu",
      "elementType": "Fire",
      "iconId": "icon_fire_burst"
    }
  ]
}
```

Lỗi phổ biến:
o400 Bad Request: Thiếu elementType hoặc tier không nằm trong khoảng 1-4.
o404 Not Found: Không có cấu hình Gene cho elementType/tier tương ứng.

3.3.3.2API nâng Gene chính - POST /api/gene/upgrade

Mô tả: Kiểm tra playerId, Gene EXP, vàng và vật phẩm trong inventory. Backend clamp itemCount theo itemsMin/itemsNeeded, tính tỉ lệ thành công bằng baseSuccessRate x min(itemCount/itemsNeeded, 1), trừ vàng, trừ vật phẩm, trừ Gene EXP. Nếu thành công, backend tăng GeneTier, cộng stat từ gene_tier_stat_config, hồi đầy HP/MP, mở skill mới và trả final_stats cho client.

Endpoint: POST /api/gene/upgrade

Header:

```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "playerId": "<player_id>",
  "itemCount": 5
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "success": true,
  "newGeneTier": 2,
  "newGeneExp": 0,
  "gold": 95000,
  "message": "Gene Fire đã lên Tier 2!",
  "statBonus": {
    "hp": 200,
    "mp": 50,
    "attack": 25,
    "defense": 8
  },
  "final_stats": {
    "hp": 300,
    "max_hp": 300,
    "mp": 100,
    "max_mp": 100,
    "attack": 35,
    "defense": 8,
    "move_speed": 5
  },
  "newlyUnlockedSkills": [],
  "updatedInventory": []
}
```

Trường hợp thất bại tỉ lệ: API vẫn trả 200 OK với success = false, vàng/vật phẩm/Gene EXP đã bị trừ theo luật nâng cấp và newGeneTier giữ nguyên.

Lỗi phổ biến:
o400 Bad Request: Thiếu playerId, Gene đã đạt Tier 5, thiếu Gene EXP, thiếu vàng, thiếu item hoặc không có config Gene.
o404 Not Found: Player không tồn tại.

3.3.3.3API lấy danh sách Gene - GET /api/gene/list

Mô tả: Trả về trạng thái Gene hiện tại của nhân vật, bao gồm Gene chính, Gene phụ, trạng thái Hybrid, danh sách hệ bị tăng sát thương, hệ miễn/giảm khắc, phần trăm bonus và cờ canFuse.

Endpoint: GET /api/gene/list?playerId=16

Header:

```http
Authorization: Bearer <JWT_TOKEN>
```

Phản hồi thành công (Response 200 OK):

```json
{
  "primaryElement": "Wind",
  "primaryTier": 5,
  "primaryExp": 1000000,
  "secondaryElement": "Metal",
  "secondaryTier": 5,
  "secondaryExp": 0,
  "isHybrid": true,
  "hybridName": "Kim Phong Thoán Thế",
  "hybridBonusTargets": "Wood,Fire",
  "hybridImmuneElements": "Fire,Earth",
  "hybridAtkBonusPct": 0.5,
  "canFuse": false
}
```

Lỗi phổ biến:
o404 Not Found: Player không tồn tại.

3.3.3.4API chọn Gene phụ - POST /api/gene/secondary/select

Mô tả: Chọn hệ Gene phụ lần đầu cho nhân vật. Backend chỉ cho chọn theo bảng ánh xạ hợp lệ: Fire kết hợp Earth, Water kết hợp Wood, Metal kết hợp Wind. Sau khi chọn, hệ phụ được khởi tạo SecondaryGeneTier = 1 và SecondaryGeneExp = 0.

Endpoint: POST /api/gene/secondary/select

Header:

```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "playerId": "<player_id>",
  "secondaryElement": "Metal"
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "success": true,
  "primaryElement": "Wind",
  "secondaryElement": "Metal",
  "secondaryTier": 1,
  "message": "Đã chọn hệ phụ: Metal! Bắt đầu nâng cấp hệ phụ."
}
```

Lỗi phổ biến:
o400 Bad Request: Thiếu playerId, thiếu secondaryElement, đã chọn hệ phụ trước đó, hoặc hệ phụ không đúng cặp hợp lệ.
o404 Not Found: Player không tồn tại.

3.3.3.5API nâng Gene phụ - POST /api/gene/secondary/upgrade

Mô tả: Nâng cấp Gene phụ bằng cấu hình gene_multi_config. Luồng kiểm tra tài nguyên, tính tỉ lệ thành công và trừ vật phẩm tương tự Gene chính. Khi thành công, SecondaryGeneTier tăng lên và nhân vật nhận 50% bonus stat so với gene_tier_stat_config.

Endpoint: POST /api/gene/secondary/upgrade

Header:

```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "playerId": "<player_id>",
  "itemCount": 5
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "success": true,
  "secondaryElement": "Metal",
  "newSecondaryTier": 2,
  "newSecondaryExp": 0,
  "gold": 90000,
  "canFuse": false,
  "final_stats": {
    "hp": 250,
    "max_hp": 250,
    "mp": 125,
    "max_mp": 125,
    "attack": 40,
    "defense": 15
  },
  "updatedInventory": []
}
```

Lỗi phổ biến:
o400 Bad Request: Chưa chọn hệ phụ, Gene phụ đã đạt Tier 5, thiếu EXP/vàng/item hoặc không có cấu hình gene_multi_config.
o404 Not Found: Player không tồn tại.

3.3.3.6API lấy cấu hình Hybrid Fusion - GET /api/gene/hybrid/config

Mô tả: Kiểm tra điều kiện dung hợp Hybrid và trả về cấu hình fuse gồm tên Hybrid, mô tả, hai hệ thành phần, bonusTargets, immuneElements, atkBonusPercent, chi phí vàng, item fuse, số lượng item hiện có, trạng thái đủ item/vàng và bonus stat khi fuse.

Endpoint: GET /api/gene/hybrid/config?playerId=<eligible_player_id>

Header:

```http
Authorization: Bearer <JWT_TOKEN>
```

Ghi chú dữ liệu: Response thành công dưới đây dùng cấu hình thật của cặp Metal/Wind trong gene_hybrid_config. Người chơi gọi API phải chưa Hybrid, có Gene chính/phụ đúng cặp và cả hai Tier 5; nếu dùng player_id = 16 trong seed hiện tại thì controller trả lỗi vì nhân vật này đã là Hybrid.

Phản hồi thành công (Response 200 OK):

```json
{
  "hybridName": "Kim Phong Thoán Thế",
  "hybridDescription": "Kiếm kim loại sắc bén lướt theo cơn gió — tốc độ và sát thương phong trào vô song.",
  "elementA": "Wind",
  "elementB": "Metal",
  "elementATier": 5,
  "elementBTier": 5,
  "bonusTargets": ["Wood", "Fire"],
  "immuneElements": ["Fire", "Earth"],
  "atkBonusPercent": 0.5,
  "fusionGoldCost": 2000000,
  "fusionItemId": 52,
  "fusionItemName": "Lõi Đột Biến Phong",
  "fusionItemIcon": 324,
  "fusionItemCount": 5,
  "availableItems": "<available_item_count>",
  "itemSufficient": true,
  "goldSufficient": true,
  "playerGold": "<player_gold>",
  "canFuse": true,
  "statBonus": {
    "hp": 2000,
    "mp": 500,
    "attack": 750,
    "defense": 200
  }
}
```

Lỗi phổ biến:
o400 Bad Request: Đã là Hybrid, chưa chọn hệ phụ, cặp hệ không hợp lệ, Gene chính hoặc Gene phụ chưa đạt Tier 5.
o404 Not Found: Không tìm thấy config Hybrid cho cặp hệ.

3.3.3.7API dung hợp Hybrid - POST /api/gene/hybrid/fuse

Mô tả: Dung hợp Gene chính và Gene phụ khi cả hai đạt Tier 5. Backend kiểm tra cặp hệ hợp lệ, kiểm tra vàng và item fuse, trừ tài nguyên, cập nhật IsHybrid, HybridElementA, HybridElementB, HybridBonusTargets, HybridImmuneElements, HybridAtkBonusPct, HybridId, HybridPrefabPath, cộng stat bonus và cập nhật danh sách skill Hybrid.

Endpoint: POST /api/gene/hybrid/fuse

Header:

```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "playerId": "<eligible_player_id>"
}
```

Ghi chú dữ liệu: Response thành công dưới đây dùng cùng cấu hình thật Hybrid id 13. Các trường gold, final_stats và updatedInventory phụ thuộc trạng thái runtime của người chơi sau khi trừ tài nguyên.

Phản hồi thành công (Response 200 OK):

```json
{
  "success": true,
  "hybridName": "Kim Phong Thoán Thế",
  "hybridDescription": "Kiếm kim loại sắc bén lướt theo cơn gió — tốc độ và sát thương phong trào vô song.",
  "hybridId": 13,
  "hybridElementA": "Wind",
  "hybridElementB": "Metal",
  "prefabPath": "Prefabs/Player/Hybrid/Hybrid_Metal_Wind",
  "comboSkillCode": "<combo_skill_code>",
  "bonusTargets": ["Wood", "Fire"],
  "immuneElements": ["Fire", "Earth"],
  "atkBonusPercent": 0.5,
  "statBonus": {
    "hp": 2000,
    "mp": 500,
    "attack": 750,
    "defense": 200
  },
  "gold": "<gold_after_fusion>",
  "message": "HYBRID FUSION THÀNH CÔNG! Kim Phong Thoán Thế đã thức tỉnh!",
  "final_stats": {
    "hp": 2335,
    "max_hp": 2335,
    "mp": 566,
    "max_mp": 566,
    "attack": 760,
    "defense": 200
  },
  "updatedInventory": []
}
```

Lỗi phổ biến:
o400 Bad Request: Thiếu playerId, đã là Hybrid, chưa đủ Tier 5, thiếu vàng, thiếu item fuse hoặc cặp hệ không hợp lệ.
o404 Not Found: Player hoặc cấu hình Hybrid không tồn tại.

----- [BẮT ĐẦU PHẦN THÊM MỚI] -----

3.3.3.8 API tích lũy EXP và kích hoạt Gene Tối Thượng - POST /api/gene/ultimate/add-exp

Mô tả: Tích lũy EXP Tối Thượng (Ultimate Gene EXP) cho nhân vật sau khi đã dung hợp Hybrid thành công thông qua phần thưởng diệt quái/boss hoặc sử dụng vật phẩm hỗ trợ. Khi lượng EXP tích lũy đạt mốc 1,000,000, hệ thống tự động kích hoạt trạng thái Gene Tối Thượng (`is_ultimate = true`), nhân x1.5 toàn bộ các chỉ số thuộc tính cơ bản của nhân vật (HP, MP, ATK, DEF).

Endpoint: POST /api/gene/ultimate/add-exp

Header:
```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

Body mẫu (Request JSON):
```json
{
  "player_id": 16,
  "exp_added": 1500
}
```

Phản hồi thành công (Response 200 OK):
```json
{
  "success": true,
  "ultimate_gene_exp": 1001500,
  "is_ultimate": true,
  "message": "Trạng thái Gene Tối Thượng đã được kích hoạt! Hào quang thức tỉnh!",
  "final_stats": {
    "hp": 3502,
    "max_hp": 3502,
    "mp": 849,
    "max_mp": 849,
    "attack": 1140,
    "defense": 300
  }
}
```

Lỗi phổ biến:
o400 Bad Request: Nhân vật chưa dung hợp Hybrid (không thể tích lũy EXP Tối Thượng), hoặc lượng EXP cộng thêm không hợp lệ.
o404 Not Found: Nhân vật không tồn tại.

----- [KẾT THÚC PHẦN THÊM MỚI] -----

3.3.4Nhóm API phó bản (Dungeon Controller)

Nhóm API phó bản cung cấp cấu hình dungeon, cấu hình wave runtime, kiểm tra lượt vào, tạo session wave và lưu tiến trình phó bản. Các endpoint này được Unity client dùng để hiển thị NPC phó bản và được Unity zone server dùng để khởi tạo runtime hoặc lưu trạng thái reconnect. Trong code hiện tại, DungeonController chỉ có [ApiController] và [Route("api/[controller]")], chưa có [Authorize]; vì vậy báo cáo không ghi các endpoint /api/dungeon/* là bắt buộc JWT hoặc X-Zone-Api-Key. API cộng thưởng phó bản là controller riêng DungeonRewardController và mới là nơi yêu cầu ZoneApiKey.

3.3.4.1API lấy danh sách phó bản - GET /api/dungeon/list

Mô tả: Trả về danh sách phó bản đang active để client hiển thị tại NPC dungeon. Danh sách được sắp xếp theo min_level_required và dungeon_id.

Endpoint: GET /api/dungeon/list

Phản hồi thành công (Response 200 OK):

```json
{
  "dungeons": [
    {
      "dungeon_id": 6,
      "dungeon_name": "Phó Bản Sóng",
      "dungeon_type": "solo",
      "map_id": 110,
      "map_name": "Vòng lặp vô tận",
      "scene_name": "DungeonWaveScene",
      "max_players": 1,
      "min_level_required": 1,
      "time_limit_seconds": 0,
      "description": "",
      "thumbnail_icon_id": "",
      "boss_enemy_id": null,
      "reward_json": "{}"
    }
  ]
}
```

3.3.4.2API lấy chi tiết phó bản - GET /api/dungeon/{dungeonId}

Mô tả: Trả về chi tiết một phó bản, bao gồm thông tin map, scene, giới hạn người chơi, boss, spawn point và danh sách enemy spawn đã resolve theo map.

Endpoint: GET /api/dungeon/{dungeonId}

Phản hồi thành công (Response 200 OK):

```json
{
  "dungeon_id": 6,
  "dungeon_name": "Phó Bản Sóng",
  "dungeon_type": "solo",
  "map_id": 110,
  "map_name": "Vòng lặp vô tận",
  "scene_name": "DungeonWaveScene",
  "max_players": 1,
  "min_level_required": 1,
  "time_limit_seconds": 0,
  "description": "",
  "thumbnail_icon_id": "",
  "reward_json": "{}",
  "boss_enemy": null,
  "player_spawn_points": "[{\"x\":0,\"y\":0}]",
  "enemy_spawns": [
    {
      "spawn_id": -11100001,
      "enemy_type_id": 11,
      "spawn_x": -4,
      "spawn_y": -1.7,
      "max_spawn_count": 1,
      "respawn_time": 0,
      "enemy": {
        "enemy_id": 11,
        "enemy_name": "Đế Băng",
        "level": 15,
        "base_hp": 2200,
        "base_damage": 120,
        "base_defense": 35,
        "exp_reward": 900,
        "gold_reward": 380,
        "silver_reward": 1500,
        "drop_items_json": "[{\"item_id\":37,\"drop_chance\":0.5,\"qty_min\":1,\"qty_max\":2},{\"item_id\":207,\"drop_chance\":0.08,\"qty_min\":1,\"qty_max\":1},{\"item_id\":31,\"drop_chance\":0.05,\"qty_min\":1,\"qty_max\":1}]",
        "element_type": "Water",
        "enemy_type": "Normal"
      }
    }
  ]
}
```

Lỗi phổ biến:
o404 Not Found: Dungeon không tồn tại.

3.3.4.3API lấy cấu hình wave runtime - GET /api/dungeon/wave/{dungeonId}/config

Mô tả: Trả về cấu hình runtime cho phó bản dạng wave. Unity zone server sử dụng dữ liệu này để xác định số wave tối đa, thời gian mỗi wave, hệ số scale quái/boss, giới hạn lượt vào/ngày, item vé cộng lượt, milestone reward và danh sách enemy/boss spawn.

Endpoint: GET /api/dungeon/wave/{dungeonId}/config

Ghi chú dữ liệu: Map 110 trong gamedb.sql có nhiều spawn lấy từ map_spawn_config. JSON dưới đây rút gọn mảng enemy_spawns còn một phần tử đầu tiên để trình bày cấu trúc; runtime trả đủ danh sách spawn đã resolve.

Phản hồi thành công (Response 200 OK):

```json
{
  "dungeon_id": 6,
  "dungeon_name": "Phó Bản Sóng",
  "map_id": 110,
  "scene_name": "DungeonWaveScene",
  "max_waves": 20,
  "wave_time_seconds": 300,
  "enemy_scale_percent": 10,
  "boss_scale_percent": 15,
  "exp_gold_scale_percent": 10,
  "daily_entry_limit": 1,
  "entry_item_plus1_id": 409,
  "entry_item_plus2_id": 410,
  "milestone_rewards": [
    {"wave": 5, "exp": 5000, "gold": 500, "items": []},
    {"wave": 10, "exp": 15000, "gold": 1500, "items": []},
    {"wave": 15, "exp": 30000, "gold": 3000, "items": []},
    {"wave": 20, "exp": 50000, "gold": 5000, "items": [{"item_template_id": 31, "qty": 1}]}
  ],
  "enemy_spawns": [
    {
      "enemy_id": 11,
      "enemy_name": "Đế Băng",
      "spawn_x": -4,
      "spawn_y": -1.7,
      "is_boss": false,
      "level": 5,
      "max_hp": 110,
      "max_mp": 500,
      "base_damage": 120,
      "base_defense": 35,
      "exp_reward": 1000,
      "respawn_time": 0,
      "move_speed": 2,
      "can_fly": false,
      "element_type": "Water",
      "drops": [
        {"item_id": 37, "drop_chance": 0.5, "qty_min": 1, "qty_max": 2},
        {"item_id": 207, "drop_chance": 0.08, "qty_min": 1, "qty_max": 1},
        {"item_id": 31, "drop_chance": 0.05, "qty_min": 1, "qty_max": 1}
      ]
    }
  ],
  "boss_spawn": {
    "enemy_id": 12,
    "enemy_name": "Mộc Linh",
    "spawn_x": 18.55,
    "spawn_y": 5.88,
    "is_boss": true,
    "level": 10,
    "max_hp": 1100,
    "max_mp": 30,
    "base_damage": 16,
    "base_defense": 4,
    "exp_reward": 100000,
    "respawn_time": 0,
    "move_speed": 1.8,
    "can_fly": false,
    "element_type": "Wood",
    "drops": [
      {"item_id": 27, "drop_chance": 0.45, "qty_min": 1, "qty_max": 3},
      {"item_id": 25, "drop_chance": 0.08, "qty_min": 1, "qty_max": 1}
    ]
  }
}
```

Lỗi phổ biến:
o404 Not Found: Dungeon không tồn tại.

3.3.4.4API kiểm tra lượt vào wave - GET /api/dungeon/wave/{dungeonId}/entry-status/{playerId}

Mô tả: Trả về số lượt đã dùng, giới hạn lượt trong ngày và trạng thái session active nếu người chơi đang có phiên wave chưa đóng. Nếu có session active, backend tính thêm seconds_remaining_in_wave dựa trên wave_time_seconds trong dungeon_wave_config.

Endpoint: GET /api/dungeon/wave/{dungeonId}/entry-status/{playerId}

Phản hồi thành công (Response 200 OK):

```json
{
  "player_id": 16,
  "dungeon_id": 6,
  "entries_used": 0,
  "entries_limit": 1,
  "entries_remaining": 1,
  "has_active_session": false,
  "active_wave": null,
  "active_phase": null,
  "seconds_remaining_in_wave": null
}
```

3.3.4.5API vào wave dungeon - POST /api/dungeon/wave/{dungeonId}/enter

Mô tả: Ghi nhận một lượt vào phó bản wave. Backend kiểm tra player_id, tạo hoặc đọc bản ghi lượt vào trong ngày, kiểm tra giới hạn entries_used/entries_limit, hỗ trợ dùng vé cộng lượt use_ticket_item_id, đóng session active cũ nếu còn bỏ dở và tạo session wave mới.

Endpoint: POST /api/dungeon/wave/{dungeonId}/enter

Header theo code hiện tại: Không bắt buộc trong DungeonController. Unity client có thể gửi JWT nếu luồng client đã có sẵn token, nhưng controller không kiểm tra [Authorize].

```http
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "player_id": 16,
  "use_ticket_item_id": 410
}
```

Trường use_ticket_item_id có thể bỏ qua nếu người chơi còn lượt miễn phí trong ngày.

Phản hồi thành công (Response 200 OK):

```json
{
  "success": true,
  "session_id": "<session_id>",
  "entries_used": 1,
  "entries_limit": 3
}
```

Lỗi phổ biến:
o400 Bad Request: Thiếu player_id, dùng hết lượt trong ngày, item không phải vé hợp lệ hoặc không đủ vé trong túi đồ.
o404 Not Found: Player không tồn tại.

3.3.4.6API cập nhật wave session - POST /api/dungeon/wave/{dungeonId}/session/update

Mô tả: Unity zone server gọi API này mỗi khi bắt đầu wave mới hoặc đổi phase để backend lưu trạng thái reconnect. Backend tìm session active theo player_id và dungeonId, cập nhật current_wave/current_phase và thời gian cập nhật.

Endpoint: POST /api/dungeon/wave/{dungeonId}/session/update

Header theo code hiện tại: Không bắt buộc trong DungeonController. Nếu zone server gửi X-Zone-Api-Key thì header không bị controller này kiểm tra.

```http
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "player_id": 16,
  "current_wave": 5,
  "current_phase": "boss"
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "success": true,
  "current_wave": 5,
  "current_phase": "boss"
}
```

Lỗi phổ biến:
o400 Bad Request: Thiếu player_id.
o404 Not Found: Không có session active.

3.3.4.7API kết thúc wave session - POST /api/dungeon/wave/{dungeonId}/session/end

Mô tả: Unity zone server gọi khi phó bản hoàn thành, timeout hoặc người chơi rời phó bản. Backend đóng session active, ghi exit_reason và cập nhật kỷ lục best_wave của người chơi.

Endpoint: POST /api/dungeon/wave/{dungeonId}/session/end

Header theo code hiện tại: Không bắt buộc trong DungeonController. Nếu zone server gửi X-Zone-Api-Key thì header không bị controller này kiểm tra.

```http
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "player_id": 16,
  "exit_reason": "completed"
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "success": true
}
```

3.3.4.8API cấp thưởng phó bản - POST /api/dungeonreward/grant

Mô tả: API nội bộ để Unity zone server cộng vật phẩm thưởng vào inventory của người chơi sau khi hoàn thành phó bản hoặc milestone. Controller này khác với DungeonController vì có [Authorize(AuthenticationSchemes = ZoneApiKey)], chỉ chấp nhận request có X-Zone-Api-Key hợp lệ.

Endpoint: POST /api/dungeonreward/grant

Header:

```http
X-Zone-Api-Key: <ZONE_API_KEY>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "targetPlayerId": 16,
  "items": [
    {
      "itemTemplateId": 31,
      "quantity": 1,
      "upgradeLevel": 0,
      "strOptions": ""
    }
  ]
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "message": "Đã phát 1 item reward cho player 16.",
  "player_id": 16,
  "added": 1
}
```

Lỗi phổ biến:
o400 Bad Request: Thiếu targetPlayerId hợp lệ hoặc thiếu danh sách items.
o401/403: Thiếu hoặc sai X-Zone-Api-Key.
o404 Not Found: Player không tồn tại.

3.3.5Nhóm API bạn bè (Friend Controller)

Nhóm API bạn bè được bảo vệ bằng JWT. Backend lấy user hiện tại từ claim trong token để bảo đảm người chơi chỉ thao tác trên quan hệ bạn bè của chính mình.

3.3.5.1API lấy danh sách bạn bè - GET /api/friends

Mô tả: Trả về danh sách quan hệ bạn bè của người chơi, bao gồm bạn đã accepted, lời mời đã gửi và lời mời đang chờ nhận.

Endpoint: GET /api/friends

Header:

```http
Authorization: Bearer <JWT_TOKEN>
```

Phản hồi thành công (Response 200 OK):

```json
[
  {
    "relationId": 7,
    "friendUserId": 17,
    "username": "kim",
    "characterName": "kim",
    "status": "accepted"
  }
]
```

3.3.5.2API gửi lời mời kết bạn - POST /api/friends/request

Mô tả: Tạo quan hệ bạn bè ở trạng thái pending từ người chơi hiện tại tới target user.

Endpoint: POST /api/friends/request

Header:

```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "targetUserId": 17
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "message": "Đã gửi lời mời kết bạn.",
  "relationId": "<relation_id>"
}
```

Lỗi phổ biến:
o400 Bad Request: Không thể kết bạn với chính mình.
o404 Not Found: Người chơi không tồn tại.
o409 Conflict: Quan hệ đã tồn tại.

3.3.5.3API chấp nhận lời mời kết bạn - PUT /api/friends/{id}/accept

Mô tả: Chuyển lời mời kết bạn từ trạng thái pending sang accepted. Chỉ người nhận lời mời mới có quyền accept.

Endpoint: PUT /api/friends/{id}/accept

Header:

```http
Authorization: Bearer <JWT_TOKEN>
```

Phản hồi thành công (Response 200 OK):

```json
{
  "message": "Đã chấp nhận lời mời kết bạn."
}
```

Lỗi phổ biến:
o404 Not Found: Lời mời không tồn tại hoặc người gọi không phải người nhận.

3.3.5.4API xóa bạn hoặc hủy lời mời - DELETE /api/friends/{id}

Mô tả: Xóa quan hệ bạn bè hoặc hủy/từ chối lời mời kết bạn. Người gọi phải là một trong hai người thuộc quan hệ đó.

Endpoint: DELETE /api/friends/{id}

Header:

```http
Authorization: Bearer <JWT_TOKEN>
```

Phản hồi thành công (Response 200 OK):

```json
{
  "message": "Đã xóa."
}
```

Lỗi phổ biến:
o404 Not Found: Quan hệ không tồn tại.

3.3.5.5API tìm người chơi - GET /api/friends/search?q=

Mô tả: Tìm người chơi theo tên nhân vật để gửi lời mời kết bạn. Backend join bảng player_data với users, bỏ qua chính người gọi và giới hạn tối đa 10 kết quả.

Endpoint: GET /api/friends/search?q=kim

Header:

```http
Authorization: Bearer <JWT_TOKEN>
```

Phản hồi thành công (Response 200 OK):

```json
[
  {
    "userId": 17,
    "username": "kim",
    "characterName": "kim"
  }
]
```

Lỗi phổ biến:
o400 Bad Request: Từ khóa tìm kiếm ngắn hơn 2 ký tự.

3.3.6Nhóm API Zone Server

Nhóm API Zone Server chỉ dành cho Unity zone server nội bộ và yêu cầu header X-Zone-Api-Key. Backend dùng các API này để biết server runtime nào đang hoạt động, port nào đang mở và tải hiện tại của từng zone.

3.3.6.1API đăng ký zone server - POST /api/zone/server/register

Mô tả: Đăng ký một zone server mới vào registry runtime của backend. Backend lưu ip, port, số lượng map đang phục vụ và thời điểm đăng ký.

Endpoint: POST /api/zone/server/register

Header:

```http
X-Zone-Api-Key: <ZONE_API_KEY>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "ip": "127.0.0.1",
  "port": 7777,
  "mapCount": 3
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "success": true,
  "ip": "127.0.0.1",
  "port": 7777,
  "map_count": 3,
  "registered_at": "<server_utc_time>"
}
```

Lỗi phổ biến:
o400 Bad Request: port <= 0 hoặc ip rỗng.
o401/403: Thiếu hoặc sai X-Zone-Api-Key.

3.3.6.2API heartbeat zone server - PUT /api/zone/server/heartbeat

Mô tả: Zone server gửi heartbeat định kỳ để backend cập nhật trạng thái sống, số người chơi online và tải của từng zone. Nếu server đã tồn tại, backend cập nhật LastHeartbeatUtc; nếu chưa có, backend tạo hoặc cập nhật entry theo port.

Endpoint: PUT /api/zone/server/heartbeat

Header:

```http
X-Zone-Api-Key: <ZONE_API_KEY>
Content-Type: application/json
```

Yêu cầu (Request Body):

```json
{
  "port": 7777,
  "playerCount": 12,
  "zoneStats": [
    {
      "mapId": 1,
      "zoneId": 1,
      "players": 5,
      "max": 30
    }
  ]
}
```

Phản hồi thành công (Response 200 OK):

```json
{
  "success": true,
  "player_count": 12,
  "zones": 1,
  "updated_at": "<server_utc_time>"
}
```

Lỗi phổ biến:
o400 Bad Request: port <= 0.
o401/403: Thiếu hoặc sai X-Zone-Api-Key.

3.3.6.3API hủy đăng ký zone server - DELETE /api/zone/server/deregister

Mô tả: Xóa trạng thái zone server khỏi registry runtime khi Unity server shutdown hoặc dừng map host.

Endpoint: DELETE /api/zone/server/deregister?port=7777

Header:

```http
X-Zone-Api-Key: <ZONE_API_KEY>
```

Phản hồi thành công (Response 200 OK):

```json
{
  "success": true,
  "removed": true
}
```

Lỗi phổ biến:
o400 Bad Request: port <= 0.
o401/403: Thiếu hoặc sai X-Zone-Api-Key.

----- [BẮT ĐẦU PHẦN THÊM MỚI] -----

3.3.7 Nhóm API Bản đồ và Dịch chuyển (Map Controller)

Nhóm API Bản đồ và Dịch chuyển chịu trách nhiệm quản lý cấu hình các khu vực bản đồ (bảng `map_config`), phân phối danh sách cổng dịch chuyển (bảng `map_portal`), và đặc biệt là thực hiện xác thực điều kiện chuyển vùng của người chơi qua endpoint `/api/map/travel` nhằm ngăn chặn các hành vi gian lận tọa độ (teleport cheat) và bảo đảm tiến trình RPG được tuân thủ đúng luật.

3.3.7.1 API xác thực dịch chuyển qua cổng - POST /api/map/travel

Mô tả: Khi người chơi chạm vào vùng trigger của cổng dịch chuyển trên Unity client, client gửi yêu cầu lên endpoint này để kiểm tra tính hợp lệ. Server thực hiện chuỗi kiểm tra logic nghiệp vụ phức tạp trước khi cấp phép dịch chuyển về tọa độ đích trên map mới.

Endpoint: POST /api/map/travel

Header:
```http
Content-Type: application/json
```

Body mẫu (Request JSON):
```json
{
  "portal_id": 67,
  "player_id": 16,
  "current_map_id": 100,
  "player_x": 45.2,
  "player_y": -2.1
}
```

Quy trình xác thực nghiệp vụ trên server (MapController.cs):
1. **Kiểm tra sự tồn tại và trạng thái hoạt động của cổng**: Tìm kiếm cổng trong DB theo `portal_id`. Nếu không tồn tại hoặc `IsActive == false`, từ chối dịch chuyển.
2. **Kiểm tra bản đồ nguồn**: Đối chiếu `current_map_id` của yêu cầu với `SourceMapId` cấu hình của cổng. Nếu không khớp, từ chối.
3. **Kiểm tra khoảng cách (Anti-cheat)**: Nếu không phải là cổng biên (cổng trái/phải dùng vật lý kích hoạt trực tiếp), tính khoảng cách Euclide giữa người chơi (`player_x`, `player_y`) và vị trí cổng (`SrcX`, `SrcY`). Khoảng cách này không được vượt quá 2 lần bán kính kích hoạt của cổng (`SrcRadius * 2`) để phòng ngừa các hành vi gian lận sửa đổi tọa độ từ client.
4. **Kiểm tra vật phẩm chìa khóa (Key Item)**: Nếu cổng yêu cầu chìa khóa (`RequiredItemId.HasValue`), server giải mã chuỗi JSON túi đồ của người chơi (`InventoryJson`) để tìm kiếm xem có chứa ID vật phẩm tương ứng hay không. Nếu thiếu, từ chối và yêu cầu người chơi sở hữu vật phẩm chìa khóa.
5. **Kiểm tra cấp độ tối thiểu (Level Lock)**: Kiểm tra cấu hình bản đồ đích (`destMap.MinLevel`). Nếu cấp độ của nhân vật (`info.Level`) nhỏ hơn `MinLevel`, từ chối và thông báo cấp độ tối thiểu cần thiết để vào khu vực.
6. **Kiểm tra mốc nhiệm vụ (Quest Lock)**: Nếu bản đồ đích yêu cầu hoàn thành nhiệm vụ trước (`destMap.RequiredQuestId.HasValue`), server kiểm tra mảng danh sách nhiệm vụ đã hoàn thành của người chơi (`CompletedQuests`). Nếu chưa hoàn thành nhiệm vụ tương ứng, từ chối dịch chuyển và thông báo tên nhiệm vụ cốt truyện cần hoàn thành.

Phản hồi thành công (Response 200 OK):
```json
{
  "success": true,
  "dest_map_id": 101,
  "dest_scene_name": "Map02",
  "dest_x": -15.5,
  "dest_y": 1.2,
  "portal_type": "room_transition",
  "portal_name": "Cổng sang Map02"
}
```

Lỗi phổ biến:
o400 Bad Request: Cổng dịch chuyển không tồn tại, người chơi không đứng gần cổng, thiếu chìa khóa, không đủ cấp độ yêu cầu, hoặc chưa hoàn thành nhiệm vụ mốc cốt truyện bắt buộc.

----- [KẾT THÚC PHẦN THÊM MỚI] -----

3.4Kiến trúc Message Payload của SignalR và Unity Netcode

Hệ thống realtime được chia thành hai loại: SignalR cho chat/tổ đội và Unity Netcode cho gameplay trong zone. Các event và payload minh họa trong mục này được đối chiếu từ tên sự kiện SignalR và các gói dữ liệu mà client/server đang gửi nhận trong project.

3.4.1Payload SignalR của Chat Hub

Chat Hub hoạt động trên endpoint /chathub và yêu cầu JWT khi kết nối WebSocket. Sau khi kết nối thành công, backend lưu session theo connectionId, lấy userId/username từ claim JWT và gửi lại event Connected cho client hiện tại. Người chơi có thể cập nhật tên hiển thị runtime bằng UpdateDisplayName để chat hiển thị tên nhân vật thay vì username tài khoản.

Tin nhắn toàn server: Khi người chơi gửi tin nhắn bằng SendWorldMessage, backend kiểm tra message rỗng, giới hạn độ dài tối đa 300 ký tự, xử lý trước các chat command nội bộ như item <itemId> <sốLượng>, sau đó phát ReceiveWorldMessage đến toàn bộ client.

```json
{
  "senderId": "16",
  "senderName": "Phong",
  "channel": "world",
  "targetId": "",
  "message": "Xin chào",
  "timestamp": "08:00"
}
```

Tin nhắn theo khu vực: Với SendProximityMessage(mapId, message), backend phát ReceiveProximityMessage đến group map_{mapId}. Client chủ động JoinMap khi vào map và LeaveMap khi chuyển map, nhờ đó tin nhắn lân cận chỉ xuất hiện với người chơi đang cùng khu vực.

Tin nhắn theo nhóm và lớp nhân vật: Chat Hub hỗ trợ SendClanMessage, SendClassMessage và SendGroupMessage. Các kênh này lần lượt dùng group clan_{clanId}, class_{classType} và group_{groupId}. Khi trạng thái party thay đổi, client join/leave group chat tương ứng để đồng bộ kênh nhóm.

Tin nhắn riêng: Với SendPrivateMessage(targetUserId, message), backend tạo payload có channel = "private" và targetId bằng userId người nhận. SignalR gửi payload đến Clients.User(targetUserId), đồng thời echo lại Clients.Caller để người gửi nhìn thấy lịch sử chat riêng trên UI.

```json
{
  "senderId": "16",
  "senderName": "Phong",
  "channel": "private",
  "targetId": "21",
  "message": "Vào dungeon không?",
  "timestamp": "08:05"
}
```

Tin nhắn hệ thống: Khi chat command sai cú pháp hoặc thao tác thêm item thất bại, backend gửi ReceiveSystemMessage riêng cho người gọi. Payload vẫn dùng cùng cấu trúc ChatMessagePayload nhưng senderId = "0", senderName = "Hệ thống" và channel = "system".

3.4.2Payload SignalR của Party Hub

Party Hub hoạt động trên endpoint /partyhub và lưu trạng thái realtime bằng bộ nhớ runtime gồm danh sách party, presence của người chơi và mapping connection theo user. Hệ thống đặt giới hạn MaxPartyMembers = 4, leader là người tạo party hoặc người được chuyển quyền khi leader cũ rời nhóm.

Đồng bộ presence: Client gọi UpdatePresence theo chu kỳ để gửi characterName, level, className, elementType, mapId và zoneId. Backend dùng dữ liệu này cho tìm party theo khu vực, tìm người chơi gần và hiển thị online/offline trong party.

Cập nhật trạng thái party: Khi tạo party, mời thành viên, chấp nhận lời mời, rời nhóm, đổi lock hoặc autoAccept, backend phát PartyStateUpdated đến SignalR group party_{partyId}.

```json
{
  "partyId": "a1b2c3d4",
  "leaderUserId": "16",
  "isLocked": false,
  "autoAccept": false,
  "memberCount": 1,
  "maxMembers": 4,
  "members": [
    {
      "userId": "16",
      "characterName": "Phong",
      "level": 35,
      "className": "Warrior",
      "elementType": "Wind",
      "online": true
    }
  ]
}
```

Lời mời vào party: Khi leader gọi InviteMember(targetUserId), backend bảo đảm người gọi là leader hoặc tự tạo party nếu chưa có nhóm, sau đó gửi PartyInviteReceived đến đúng user được mời.

```json
{
  "partyId": "a1b2c3d4",
  "leaderUserId": "16",
  "leaderName": "Phong"
}
```

Yêu cầu xin vào party: Nếu party không bật autoAccept, người xin vào nhóm gửi RequestJoinParty(partyId), backend chuyển thành event PartyJoinRequestReceived cho leader. Payload chứa requesterUserId, requesterName, requesterLevel và requesterElementType để leader ra quyết định.

```json
{
  "partyId": "a1b2c3d4",
  "requesterUserId": "21",
  "requesterName": "Hoa",
  "requesterLevel": 34,
  "requesterElementType": "Fire"
}
```

Tìm party trong khu vực: Khi client gọi GetPartiesInZone(mapId, zoneId), backend trả PartySearchResults cho caller. Mỗi phần tử gồm partyId, leader, level, element, trạng thái lock/autoAccept, số thành viên và vị trí map/zone.

```json
{
  "parties": [
    {
      "partyId": "a1b2c3d4",
      "leaderUserId": "16",
      "leaderName": "Phong",
      "leaderLevel": 35,
      "leaderClassName": "Warrior",
      "leaderElementType": "Wind",
      "isLocked": false,
      "autoAccept": true,
      "memberCount": 2,
      "maxMembers": 4,
      "mapId": 1,
      "zoneId": 1
    }
  ]
}
```

Tìm người chơi gần: Khi client gọi GetNearbyPlayers(mapId, zoneId), backend trả NearbyPlayersUpdated. Payload này dùng để hiển thị danh sách người chơi có thể mời nhanh vào party.

```json
{
  "players": [
    {
      "userId": "21",
      "characterName": "Hoa",
      "level": 34,
      "className": "Mage",
      "elementType": "Fire",
      "mapId": 1,
      "zoneId": 1,
      "inParty": false,
      "isPartyLeader": false
    }
  ]
}
```

Vào phó bản tổ đội: Khi leader bắt đầu phó bản, Party Hub phát PartyDungeonRequested cho toàn bộ thành viên trong group party. Event này không tự đưa người chơi vào dungeon; nó là tín hiệu realtime để client chuyển sang luồng Unity Netcode và gọi ServerRpc vào cùng dungeon room.

```json
{
  "dungeonId": 3,
  "mapId": 10,
  "dungeonType": "wave"
}
```

Xử lý lỗi realtime: Các thao tác sai quyền, party đầy, party bị khóa hoặc requester không hợp lệ được trả về PartyError cho caller. Khi party bị giải tán, backend phát PartyDisbanded để client xóa trạng thái nhóm và rời group chat.

3.4.3Luồng gói tin Unity Netcode trong gameplay

Trong gameplay, các lệnh realtime sử dụng ServerRpc, ClientRpc và NetworkVariable thay vì HTTP trực tiếp từ UI. REST API chỉ được gọi bởi zone server hoặc lớp command service để đọc/ghi dữ liệu bền vững trong MySQL.

Di chuyển nhân vật: Owner đọc input cục bộ, cập nhật Rigidbody2D để tạo phản hồi tức thời rồi gửi MoveServerRpc lên server. Server cập nhật transform, ghi syncPosition và networkScaleX bằng NetworkVariable, đồng thời phát UpdateAnimationClientRpc để đồng bộ animation cho non-owner.

Đồng bộ dữ liệu nhân vật: Khi player spawn, zone server lấy dữ liệu nhân vật từ backend và ghi vào các NetworkVariable như playerId, elementType, level, HP, MP, attack, defense, moveSpeed, Gene Tier và partyId. Khi có thay đổi trang bị, item, buff hoặc Gene, server cập nhật lại các biến này để mọi client nhận trạng thái mới.

Nâng Gene trong zone: Client gửi UpgradeGeneServerRpc kèm requestJson đến zone server. Zone server resolve JWT theo clientId, gọi REST API /api/gene/upgrade, nhận kết quả thành công/thất bại rồi trả về đúng người gọi bằng GeneUpgradeResultClientRpc thông qua TargetClientIds.

Dùng item, trang bị và kỹ năng: Các thao tác nhạy cảm như UseInventoryItemServerRpc, EquipItemServerRpc, UpgradeSkillServerRpc và AllocatePotentialStatsServerRpc đi qua GameplayCommandService. Server gọi backend để xác thực dữ liệu, sau đó trả kết quả về client bằng các ClientRpc tương ứng như UseItemResultClientRpc, EquipResultClientRpc hoặc UpgradeSkillResultClientRpc.

Chuyển zone và map: Khi người chơi đi qua cổng, client gửi RequestZoneTransferServerRpc hoặc RequestMapPortalTransferServerRpc. Server kiểm tra room đích, cập nhật registry, lưu vị trí qua Zone API Key nếu cần, sau đó dùng ClientRpc nhắm đúng owner để chuyển scene hoặc teleport đến entry point.

Vào phó bản: Client gọi RequestDungeonEntryServerRpc hoặc RequestPartyDungeonEntryServerRpc. Server tạo custom room cho dungeon, chuyển toàn bộ thành viên hợp lệ vào cùng room, khởi tạo runtime encounter và gửi NotifyDungeonEnteredClientRpc cho từng client trong party.

Sát thương và hiệu ứng chiến đấu: Client chỉ gửi ý định đánh/kỹ năng. Server xác định mục tiêu, đọc chỉ số tấn công và buff, sau đó cập nhật HP bằng NetworkVariable. Với luồng boss và MobPatrolAI có truyền element, server mới áp dụng né tránh/kháng nguyên tố; còn NetworkEnemyHealth phổ biến chỉ nhận damage số đã tính trước. Các hiệu ứng hiển thị như bị đánh, chết, stun, shield, projectile animation hoặc visual boss được phát qua ClientRpc.

Lọc người nhìn thấy theo room: Khi player đổi zone hoặc vào dungeon, server refresh visibility của NetworkObject. Những client không cùng map/zone/custom room sẽ bị NetworkHide đối với object không liên quan, tránh việc nhận thừa trạng thái gameplay ngoài khu vực.

Nhờ tách REST API, SignalR và Unity Netcode theo đúng trách nhiệm, hệ thống vừa lưu được dữ liệu lâu dài trong MySQL, vừa giữ được phản hồi realtime trong gameplay nhiều người chơi.

3.5Tăng cường kiểm soát truy cập và bảo mật hệ thống

Sau khi hoàn thành các chức năng nghiệp vụ cốt lõi của hệ thống, nhóm phát triển tiến hành rà soát bảo mật toàn diện theo các tiêu chí của OWASP Top 10. Quá trình rà soát phát hiện một số điểm cần cải thiện, trong đó sáu biện pháp được ưu tiên hiện thực hóa trước khi triển khai lên môi trường production. Các biện pháp này tập trung vào năm lớp phòng thủ độc lập nhau: lớp transport (xác thực kết nối NGO), lớp network (giới hạn tốc độ yêu cầu), lớp application (kiểm soát truy cập endpoint và xác thực nội bộ), lớp data (kiểm định dữ liệu đầu vào hai tầng server và client), và lớp presentation (kiểm soát thông tin lỗi trả về client). Mỗi lớp bảo vệ một điểm tiếp xúc khác nhau, đảm bảo rằng việc vượt qua một lớp không tự động mang lại quyền truy cập vào toàn bộ hệ thống.

![Mô hình bảo mật nhiều lớp của hệ thống](extracted_images/image42.jpeg)

*Hình 3.1. Mô hình bảo mật nhiều lớp của hệ thống Mutants Arena*

3.5.1Kiểm soát truy cập toàn bộ endpoint API bằng thuộc tính `[Authorize]`

Trong quá trình rà soát, nhóm phát triển phát hiện lớp `EnemyController` — cung cấp ba endpoint tra cứu chỉ số kẻ địch (`GetAllEnemies`, `GetEnemy`, `GetEnemiesByLevel`) — chưa được bảo vệ bằng xác thực JWT. Hệ quả là bất kỳ client nào, kể cả client chưa đăng nhập, đều có thể gửi yêu cầu đến các endpoint này và tải xuống toàn bộ bộ chỉ số kẻ địch trong game. Đây là thông tin thiết kế nội bộ, bao gồm điểm máu, sát thương, tốc độ và hành vi của từng loại kẻ địch — những dữ liệu mà một người chơi trục lợi có thể sử dụng để tính toán cơ chế khai thác. Nguy cơ này thuộc nhóm "Broken Access Control" (A01) trong phân loại OWASP Top 10:2021.

Để khắc phục, thuộc tính `[Authorize]` được bổ sung ở cấp độ class, qua đó áp dụng ràng buộc xác thực cho đồng thời tất cả các action method mà không cần khai báo lặp lại trên từng phương thức:

```csharp
// GameServerApi/Controllers/EnemyController.cs
using GameServerApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]                       // yêu cầu JWT hợp lệ cho mọi action trong class này
public class EnemyController : ControllerBase
{
    private readonly GameDbContext _db;  // inject DbContext trực tiếp — không có service layer

    public EnemyController(GameDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllEnemies()
    {
        var enemies = await _db.Enemies.ToListAsync();
        return Ok(new { enemies });
    }

    [HttpGet("{enemyId}")]          // tên param là enemyId, không phải id
    public async Task<IActionResult> GetEnemy(int enemyId)
    {
        var enemy = await _db.Enemies.FindAsync(enemyId);
        if (enemy == null) return NotFound("Enemy không tồn tại.");
        return Ok(enemy);
    }

    [HttpGet("by-level/{level}")]   // route: by-level/{level}
    public async Task<IActionResult> GetEnemiesByLevel(int level)
    {
        var enemies = await _db.Enemies
            .Where(e => e.Level == level)
            .ToListAsync();
        return Ok(new { level, enemies });
    }
}
```

Nguyên tắc "đóng mặc định, mở có chọn lọc" được áp dụng nhất quán trên toàn bộ hệ thống: tất cả các controller xử lý dữ liệu người chơi — gồm `PlayerController`, `QuestController`, `UpgradeController`, `GeneController`, `NpcActionController`, `DungeonController` và `LeaderboardController` — đều khai báo `[Authorize]` ở cấp class. Chỉ hai action duy nhất được miễn xác thực là `AuthController.Register()` và `AuthController.Login()`, vì đây là điểm vào công khai cho phép người dùng chưa có tài khoản thực hiện đăng ký và nhận token lần đầu.

Khi một client gửi yêu cầu đến endpoint được bảo vệ mà không kèm header `Authorization: Bearer <token>` hợp lệ, middleware `JwtBearerAuthentication` trong pipeline ASP.NET Core tiến hành kiểm tra tuần tự ba điều kiện: chữ ký HMAC-SHA256 của token có khớp với khóa bí mật của server không; token có còn trong thời hạn hiệu lực (`exp` claim) không; giá trị `issuer` và `audience` có khớp với cấu hình không. Nếu bất kỳ điều kiện nào thất bại, middleware trả về HTTP 401 Unauthorized và dừng chuỗi xử lý — yêu cầu không bao giờ đến được controller, toàn bộ logic nghiệp vụ bên trong không được thực thi.

3.5.2Giới hạn tốc độ yêu cầu đăng nhập (Rate Limiting)

Endpoint `POST /api/auth/login` là mục tiêu phổ biến của tấn công brute-force mật khẩu: kẻ tấn công sử dụng danh sách mật khẩu phổ biến và gửi hàng nghìn yêu cầu liên tiếp để dò tài khoản người dùng. Không có biện pháp giới hạn tốc độ, khả năng thử tối đa chỉ bị ràng buộc bởi băng thông mạng — có thể lên tới hàng chục nghìn lần thử mỗi phút. Mặc dù BCrypt với work factor 12 làm chậm quá trình xác minh mật khẩu đến khoảng 250–400 ms mỗi lần, điều này vẫn không đủ để ngăn tấn công dò mật khẩu khi kẻ tấn công sử dụng nhiều luồng song song.

Để khắc phục, hệ thống tích hợp Rate Limiting thông qua middleware `AddRateLimiter` được cung cấp sẵn trong ASP.NET Core mà không cần phụ thuộc thư viện ngoài. Chính sách `FixedWindowLimiter` được lựa chọn vì phù hợp với yêu cầu: cửa sổ thời gian cố định 60 giây với ngưỡng tối đa 5 yêu cầu. Cấu hình được đăng ký trong `Program.cs` như sau:

```csharp
// GameServerApi/Program.cs
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.Window      = TimeSpan.FromSeconds(60);  // cửa sổ thời gian 60 giây
        opt.PermitLimit = 5;                          // tối đa 5 yêu cầu mỗi cửa sổ
        opt.QueueLimit  = 0;                          // từ chối ngay, không xếp hàng chờ
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Thứ tự trong pipeline: sau UseCors(), trước UseAuthentication()
app.UseRateLimiter();
```

Chính sách `"login"` sau đó được gắn vào action `Login()` trong `AuthController` bằng thuộc tính `[EnableRateLimiting]`, cho phép áp dụng chọn lọc mà không ảnh hưởng đến các action khác của cùng controller:

```csharp
// GameServerApi/Controllers/AuthController.cs
using Microsoft.AspNetCore.RateLimiting;

[HttpPost("login")]
[EnableRateLimiting("login")]
public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
{
    // Query trực tiếp qua EF Core — không có repository layer
    var user = await _db.Users
        .FirstOrDefaultAsync(u => u.Username == request.Username);
    if (user == null)
        return Unauthorized("Sai username hoặc password.");

    // _authService (IAuthService) xử lý cả BCrypt verify lẫn JWT generation
    if (!_authService.VerifyPassword(request.Password, user.PasswordHash))
        return Unauthorized("Sai username hoặc password.");

    var token = _authService.GenerateJwtToken(user);   // GenerateJwtToken, không phải GenerateToken
    return Ok(new { token, user_id = user.UserId, username = user.Username });
}
```

Khi một địa chỉ IP gửi yêu cầu thứ sáu trong vòng 60 giây, ASP.NET Core trả về HTTP 429 Too Many Requests ngay tại tầng middleware — không thực hiện truy vấn cơ sở dữ liệu, không gọi `BCrypt.Verify()`. Điều này vừa bảo vệ tài khoản khỏi bị dò mật khẩu, vừa giảm tải không cần thiết cho tầng xử lý phía sau. Tốc độ dò tối đa bị giới hạn xuống còn 5 lần thử mỗi phút, tức 300 lần thử mỗi giờ — giảm vài nghìn lần so với không có giới hạn.

3.5.3Xác thực nội bộ Zone Server bằng Zone API Key và so sánh hằng thời gian

NGO Dedicated Server (Zone Server) cần gọi đến REST API để thực hiện các thao tác ảnh hưởng đến dữ liệu lâu dài: cộng EXP sau khi tiêu diệt kẻ địch, cấp phần thưởng hoàn thành dungeon, hoặc cập nhật tiến trình phó bản. Tuy nhiên, Zone Server không phải người dùng — nó không có tài khoản, không đăng nhập và không nhận JWT theo luồng người chơi thông thường. Nếu sử dụng JWT của người chơi để thực hiện các cuộc gọi nội bộ này, API không thể phân biệt được yêu cầu từ client hợp lệ hay từ Zone Server, dẫn đến nguy cơ người chơi tự gọi vào endpoint cộng EXP mà không thông qua Zone Server.

Để phân tách rõ ràng hai loại nguồn gọi API, hệ thống triển khai sơ đồ xác thực lai (hybrid authentication scheme): nếu yêu cầu đến kèm header `X-Zone-Api-Key`, nó được chuyển hướng sang `ZoneApiKeyAuthenticationHandler`; ngược lại, luồng JWT Bearer tiêu chuẩn được áp dụng. Bên trong `ZoneApiKeyAuthenticationHandler`, phép so sánh khóa được thực hiện bằng `CryptographicOperations.FixedTimeEquals()` thay vì phép so sánh chuỗi thông thường (`==`):

```csharp
// GameServerApi/Auth/ZoneApiKeyAuthenticationHandler.cs
protected override Task<AuthenticateResult> HandleAuthenticateAsync()
{
    if (!Request.Headers.TryGetValue(HeaderName, out var headerValues))  // HeaderName = "X-Zone-Api-Key"
        return Task.FromResult(AuthenticateResult.NoResult());

    string providedKey = headerValues.ToString();
    string expectedKey = _configuration["ZoneApiKey"] ?? string.Empty;  // đọc từ IConfiguration

    if (string.IsNullOrWhiteSpace(providedKey))
        return Task.FromResult(AuthenticateResult.Fail("X-Zone-Api-Key trống."));

    if (string.IsNullOrWhiteSpace(expectedKey))
        return Task.FromResult(AuthenticateResult.Fail("ZoneApiKey chưa được cấu hình."));

    // So sánh hằng thời gian — chống timing attack
    if (!SecureEquals(providedKey, expectedKey))
        return Task.FromResult(AuthenticateResult.Fail("X-Zone-Api-Key không hợp lệ."));

    var claims   = new[] { new Claim(ClaimTypes.Role, "GameServer") };
    var identity = new ClaimsIdentity(claims, SchemeName);
    var ticket   = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
    return Task.FromResult(AuthenticateResult.Success(ticket));
}

private static bool SecureEquals(string left, string right)
{
    byte[] leftBytes  = Encoding.UTF8.GetBytes(left);
    byte[] rightBytes = Encoding.UTF8.GetBytes(right);
    if (leftBytes.Length != rightBytes.Length) return false;   // khác độ dài → từ chối ngay
    return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
}
```

Lý do kỹ thuật đằng sau `FixedTimeEquals()` là phép so sánh chuỗi thông thường sử dụng thuật toán "fail-fast": nó trả về `false` ngay khi gặp ký tự đầu tiên không khớp, khiến thời gian thực thi phụ thuộc vào vị trí ký tự sai đầu tiên. Một kẻ tấn công tinh vi có thể gửi nhiều khóa thử khác nhau, đo thời gian phản hồi và suy luận từng ký tự của khóa bí mật — đây là tấn công kênh bên theo thời gian (timing side-channel attack). `CryptographicOperations.FixedTimeEquals()` luôn duyệt đủ toàn bộ độ dài hai mảng byte bất kể nội dung, loại bỏ hoàn toàn sự khác biệt thời gian này.

Ngoài ra, các endpoint dành riêng cho Zone Server được đánh dấu thêm `[Authorize(AuthenticationSchemes = ZoneApiKeyAuthenticationHandler.SchemeName)]`. Thuộc tính này chỉ định rõ scheme xác thực được chấp nhận phải là `"ZoneApiKey"` — mọi JWT của người chơi thông thường đều bị bác bỏ tại tầng authentication, ngay cả khi token hoàn toàn hợp lệ, vì nó thuộc scheme `JwtBearer` khác scheme.

![Luồng xác thực nội bộ Zone Server bằng Zone API Key](extracted_images/image47.jpeg)

*Hình 3.2. Luồng xác thực nội bộ Zone Server bằng Zone API Key*

3.5.4Xác thực kết nối tại tầng transport của NGO Dedicated Server

Khi sử dụng Unity Netcode for GameObjects (NGO), mặc định bất kỳ client nào biết địa chỉ IP và cổng của Zone Server đều có thể khởi tạo kết nối. Điều này tạo ra nguy cơ: người dùng không hợp lệ hoặc bot có thể kết nối vào server để chiếm tài nguyên hoặc can thiệp vào trạng thái gameplay. NGO cung cấp cơ chế `ConnectionApprovalCallback` cho phép server kiểm tra và từ chối kết nối ở giai đoạn rất sớm, trước khi bất kỳ `NetworkObject` nào được khởi tạo và tài nguyên nào được cấp phát. Lớp `ZoneConnectionApproval` triển khai bốn bước xác thực tuần tự trong callback này:

```csharp
// Client/Assets/Scripts/Network/Server/ZoneConnectionApproval.cs
// Payload JSON (UTF-8): { "token": "<JWT>", "mapId": 0, "zoneId": 0, "geneSlot": 1 }
private const int MaxPayloadBytes = 2048;

private void HandleApproval(
    NetworkManager.ConnectionApprovalRequest  request,
    NetworkManager.ConnectionApprovalResponse response)
{
    // Bước 1 — Giới hạn kích thước payload (DoS prevention)
    if (request.Payload.Length > MaxPayloadBytes)
    { Reject(response, "Payload quá lớn"); return; }

    // Bước 2 — Giải mã UTF-8 và parse JSON thủ công (không dùng thư viện ngoài)
    string json;
    try { json = Encoding.UTF8.GetString(request.Payload); }
    catch { Reject(response, "Payload không phải UTF-8"); return; }

    if (!TryParsePayload(json, out string token, out int mapId, out int zoneId, out int geneSlot))
    { Reject(response, "Payload JSON không hợp lệ"); return; }

    // Bước 3 — Xác minh chữ ký JWT và thời hạn bằng JwtValidator nhẹ (tự hiện thực)
    string secret = _config.GetJwtSecret();
    var    result = JwtValidator.Validate(token, secret);
    if (!result.IsValid)
    { Reject(response, $"JWT không hợp lệ: {result.ErrorMessage}"); return; }

    // Bước 4 — Kiểm tra zone tồn tại và chưa đầy
    var registry = ZoneRoomRegistry.Instance;
    ZoneRoom room = registry.ResolveLoginRoom(mapId, zoneId);
    if (room == null)
    { Reject(response, $"Không tìm được zone cho map={mapId}, zone={zoneId}"); return; }

    if (room.IsFull)
    {
        ZoneRoom fallback = registry.FindLeastLoadedZone(room.MapId, room.ZoneId);
        if (fallback == null || fallback.IsFull)
        { Reject(response, "Server đầy"); return; }
        room = fallback;
    }

    // Ghi nhận session — ZonePlayerSessionManager lưu ánh xạ clientId → {userId, token, …}
    ulong clientId = request.ClientNetworkId;
    registry.AssignClientToRoom(clientId, room);
    ZonePlayerSessionManager.RegisterSessionOrQueue(
        clientId, result.UserId, result.Username, room.MapId, room.ZoneId, token, geneSlot);

    // Phê duyệt kết nối
    response.Approved           = true;
    response.CreatePlayerObject = false;
    Vector2 entry = room.GetEntryPoint(0);
    response.Position = new Vector3(entry.x, entry.y, 0f);
}

private static void Reject(NetworkManager.ConnectionApprovalResponse response, string reason)
{
    response.Approved = false;
    response.Reason   = reason;   // NGO 1.x trở lên hỗ trợ Reason string
}
```

`JwtValidator` là lớp tự hiện thực trong Unity, không phụ thuộc thư viện ngoài. Lớp này chỉ xác minh hai yếu tố cần thiết: chữ ký HMAC-SHA256 để xác nhận token chưa bị giả mạo, và giá trị `exp` claim để xác nhận token chưa hết hạn. Việc tự hiện thực thay vì dùng thư viện ngoài là quyết định có chủ đích nhằm tránh đưa thêm phụ thuộc bên ngoài vào dự án Unity.

Sau khi kết nối được chấp nhận, `ZonePlayerSessionManager` lưu ánh xạ `clientId → { userId, JWT, mapId, zoneId, geneSlot }`. Ánh xạ này được sử dụng về sau khi Zone Server cần thực hiện gọi API nhân danh người chơi — Zone Server tra cứu JWT tương ứng với `clientId` rồi đính kèm vào header `Authorization: Bearer <JWT>` khi gọi API Backend, đảm bảo luồng server-authoritative không bị phá vỡ.

![Luồng kiểm duyệt kết nối NGO Dedicated Server](extracted_images/image49.jpeg)

*Hình 3.3. Luồng kiểm duyệt kết nối NGO Dedicated Server*

3.5.5Ngăn lộ thông tin kỹ thuật nhạy cảm qua `ErrorHandlingMiddleware`

Trong môi trường phát triển, ASP.NET Core mặc định trả về trang "Developer Exception Page" chứa toàn bộ stack trace, tên class, tên bảng cơ sở dữ liệu và chuỗi kết nối khi xảy ra exception chưa xử lý. Nếu cấu hình môi trường bị thiết lập sai trên server production, hoặc nếu một exception bất ngờ xảy ra ngoài luồng xử lý thông thường, những thông tin kỹ thuật này có thể lộ ra ngoài client. Kẻ tấn công có thể khai thác thông tin này để hiểu rõ cấu trúc nội bộ của hệ thống và xác định các vector tấn công tiếp theo. Điều này thuộc nhóm "Security Misconfiguration" (A05) trong OWASP Top 10:2021.

Để phòng ngừa, hệ thống triển khai `ErrorHandlingMiddleware` và đăng ký nó ở vị trí đầu tiên trong pipeline, trước mọi middleware khác, nhằm đảm bảo tất cả exception từ bất kỳ tầng nào đều được bắt tại đây:

```csharp
// GameServerApi/Middleware/ErrorHandlingMiddleware.cs
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate                  _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Ghi log đầy đủ nội bộ — stack trace, message, context — để phục vụ chẩn đoán
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            context.Response.StatusCode  = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            // Chỉ trả về thông báo chung — không có stack trace, tên class, chuỗi kết nối
            // Dùng ApiResponse wrapper thống nhất với toàn bộ API: { success, error, errorCode }
            var response = ApiResponse.Fail("Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.", 500);
            var body = JsonSerializer.Serialize(response,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await context.Response.WriteAsync(body);
        }
    }
}
```

Middleware này được đăng ký đầu tiên trong `Program.cs`, trước `UseCors()`, `UseRateLimiter()`, `UseAuthentication()` và `UseAuthorization()`:

```csharp
// GameServerApi/Program.cs — thứ tự pipeline middleware
app.UseMiddleware<ErrorHandlingMiddleware>();   // ← đầu tiên, bắt mọi exception
app.UseCors("AllowAll");                        // tên policy khai báo trong AddCors()
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

Thiết kế hai cấp độ phản hồi này đảm bảo tính song hành giữa hai mục tiêu: thông tin đầy đủ về lỗi được lưu lại trong log nội bộ phục vụ nhóm vận hành chẩn đoán sự cố, trong khi client bên ngoài chỉ nhận được thông báo chung chung không tiết lộ bất kỳ chi tiết kỹ thuật nào về nguyên nhân hoặc cấu trúc hệ thống.

---

3.6Hiện thực hóa triển khai và vận hành với Docker Compose

Hệ thống Mutants Arena được đóng gói và triển khai bằng Docker Compose, cho phép vận hành toàn bộ hạ tầng trên bất kỳ máy chủ Linux VPS nào chỉ với điều kiện duy nhất là đã cài đặt Docker Engine. Chiến lược containerization này mang lại hai lợi ích chính: thứ nhất là đồng nhất hoàn toàn giữa môi trường phát triển và môi trường production, loại bỏ sự cố "chạy được trên máy lập trình viên nhưng không chạy được trên server"; thứ hai là cho phép cập nhật từng thành phần độc lập mà không ảnh hưởng đến các thành phần còn lại đang chạy.

![Kiến trúc triển khai Docker Compose của hệ thống](extracted_images/image50.jpeg)

*Hình 3.4. Kiến trúc triển khai Docker Compose của hệ thống*

3.6.1Kiến trúc ba container và phân tách mạng nội bộ

Toàn bộ hạ tầng được tổ chức thành ba container, mỗi container đảm nhiệm đúng một tầng trong kiến trúc hệ thống. Cấu hình cụ thể được trình bày trong Bảng 3.X dưới đây.

**Bảng 3.X — Cấu hình các container trong hệ thống Docker Compose**

| Container | Image | Cổng ánh xạ | Vai trò |
|-----------|-------|-------------|---------|
| `db` | `mariadb:10.6` | 3306 (chỉ mạng nội bộ, không mở ra host) | Lưu trữ dữ liệu bền vững |
| `api` | `.NET 9 (Dockerfile tùy chỉnh)` | 5000 → 5000 (host) | REST API + SignalR Hub |
| `unity` | `ubuntu:22.04` | 7777 UDP → 7777 (host) | NGO Dedicated Server headless |

Container `db` được cấu hình chỉ tham gia vào mạng nội bộ Docker (`internal: true`) và không được ánh xạ bất kỳ cổng nào ra ngoài máy chủ vật lý. Điều này có nghĩa: ngay cả khi kẻ tấn công xâm nhập được vào máy chủ qua các vector khác, cơ sở dữ liệu vẫn không thể bị kết nối trực tiếp từ bên ngoài mà không đi qua tầng API đã được xác thực.

Container `api` phụ thuộc vào `db` với điều kiện health check, đảm bảo MariaDB hoàn toàn sẵn sàng tiếp nhận kết nối trước khi ASP.NET Core khởi động và cố gắng thực hiện database migration. Cấu hình retry của Pomelo EF Core (`MaxRetryCount = 3`, `MaxRetryDelay = 5s`) xử lý trường hợp container `db` chậm khởi động hơn dự kiến do tải hệ thống.

3.6.2Quản lý thông tin bí mật qua biến môi trường

Một nguyên tắc bắt buộc trong triển khai production là không hardcode thông tin bí mật vào source code hay image Docker. Tất cả các giá trị nhạy cảm của hệ thống — bao gồm khóa ký JWT, Zone API Key và mật khẩu cơ sở dữ liệu — được truyền vào container tại thời điểm khởi động thông qua biến môi trường. Giá trị thực được lưu trong file `.env` trên máy chủ vật lý; file này được thêm vào `.gitignore` và không bao giờ được đưa vào repository:

```yaml
# docker-compose.yml — cấu hình môi trường cho container api
services:
  api:
    build: ./GameServerApi
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Jwt__Key=${JWT_SECRET}              # khóa ký JWT, đọc từ .env
      - Jwt__Issuer=GameServerApi           # hardcode — khớp với Program.cs
      - Jwt__Audience=GameClient
      - ZoneApiKey=${ZONE_API_KEY}          # khóa xác thực Zone Server
      - ConnectionStrings__GameDB=Server=db;Database=${MYSQL_DATABASE};User=${MYSQL_USER};Password=${MYSQL_PASSWORD};Port=3306
    depends_on:
      db:
        condition: service_healthy
```

File `appsettings.Production.json` trong source code chỉ khai báo cấu trúc, không chứa giá trị thực. Runtime của .NET 9 tự động đọc và ghi đè từ biến môi trường của container theo quy tắc ánh xạ tên (`Jwt__Key` trong environment tương ứng với `Jwt.Key` trong cấu hình). Bí mật thực chỉ tồn tại trên máy chủ vật lý và trong bộ nhớ container trong thời gian chạy, không xuất hiện trong bất kỳ file nào thuộc repository hay image Docker.

3.6.3Quy trình cập nhật hệ thống không gián đoạn

Để hỗ trợ cập nhật nhanh trong môi trường production mà không gây gián đoạn phiên chơi đang diễn ra, hệ thống sử dụng script `deploy.sh` tự động hóa toàn bộ quy trình theo ba bước tuần tự:

```bash
#!/bin/bash
# deploy.sh — Triển khai phiên bản mới lên môi trường production
set -e     # Dừng ngay nếu bất kỳ lệnh nào thất bại

# Bước 1: Lấy mã nguồn mới nhất từ repository
git pull --ff-only   # --ff-only: từ chối nếu không fast-forward, tránh merge conflict ẩn

# Bước 2: Rebuild và khởi động lại container api
#   --build   : buộc build lại image từ Dockerfile mới nhất
#   --no-deps : KHÔNG khởi động lại các container phụ thuộc (db, unity)
#   -d        : chạy ở chế độ nền (detached)
docker compose up -d --build --no-deps api

# Bước 3: Dọn dẹp image Docker cũ không còn được sử dụng để giải phóng dung lượng
docker image prune -f --filter "dangling=true"   # chỉ xóa untagged image, không xóa image đang dùng
```

Tham số `--no-deps` là điểm then chốt trong quy trình này: nó đảm bảo container `db` và container `unity` không bị khởi động lại trong suốt quá trình cập nhật. Người chơi đang trong phiên chơi tiếp tục kết nối bình thường với Zone Server trong khi container `api` đang được rebuild và restart. Chỉ các yêu cầu REST API đang được xử lý trong khoảng thời gian container `api` khởi động lại — thường dưới 5 giây — mới bị gián đoạn; Unity Client có cơ chế retry tự động cho các yêu cầu không nhận được phản hồi, đảm bảo người chơi không nhận thấy sự gián đoạn trong phần lớn trường hợp.

3.5.6Kiểm định dữ liệu đầu vào hai tầng (Input Validation)

Nguyên tắc bảo mật "Defense in Depth" yêu cầu dữ liệu đầu vào phải được kiểm tra tại hai điểm độc lập: phía client trước khi gửi yêu cầu, và phía server trước khi xử lý nghiệp vụ. Kiểm tra phía client cải thiện trải nghiệm người dùng bằng cách thông báo lỗi tức thì mà không cần round-trip mạng; kiểm tra phía server là tuyến phòng thủ bắt buộc vì client có thể bị bỏ qua hoặc giả mạo. Đây là biện pháp phòng chống nhóm "Injection" (A03) và "Security Misconfiguration" (A05) trong OWASP Top 10:2021.

#### Tầng server — Data Annotations trên DTO

Lớp `LoginRequest` và `RegisterRequest` trong `AuthDtos.cs` khai báo ràng buộc trực tiếp qua thuộc tính Data Annotations. Nhờ `[ApiController]` trên controller, ASP.NET Core tự động kiểm tra `ModelState` trước khi thực thi action method và trả về HTTP 400 Bad Request kèm danh sách lỗi nếu bất kỳ ràng buộc nào bị vi phạm — không cần viết code kiểm tra thủ công trong mỗi action:

```csharp
// GameServerApi/Models/DTOs/AuthDtos.cs
using System.ComponentModel.DataAnnotations;

public class LoginRequest
{
    [Required]
    [MinLength(3), MaxLength(30)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(6), MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required]
    [MinLength(3), MaxLength(30)]
    [RegularExpression(@"^[a-zA-Z0-9_]+$",
        ErrorMessage = "Username chỉ được chứa chữ cái, chữ số và dấu gạch dưới.")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6), MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}
```

`[RegularExpression]` trên trường `Username` ngăn các ký tự đặc biệt có thể được sử dụng để thực hiện tấn công injection thông qua tên tài khoản. `[EmailAddress]` xác nhận định dạng email đúng cú pháp. `[MinLength]` / `[MaxLength]` ngăn mật khẩu quá ngắn (dễ đoán) và các chuỗi cực dài có thể gây tốn tài nguyên khi BCrypt xử lý. Hệ thống EF Core + Pomelo sử dụng truy vấn tham số hóa cho mọi truy cập cơ sở dữ liệu, loại bỏ nguy cơ SQL Injection ngay tại tầng ORM.

#### Tầng client — Kiểm tra trong Unity trước khi gọi API

`RegisterController` và `LoginController` trong Unity thực hiện kiểm tra tương đương phía client trước khi gửi bất kỳ yêu cầu HTTP nào, đảm bảo người chơi nhận phản hồi lỗi tức thì thay vì phải chờ round-trip mạng:

```csharp
// Client/Assets/Scripts/UI/Auth/RegisterController.cs — trích đoạn OnRegisterClicked()
string username = usernameInput.text.Trim();
string email    = emailInput.text.Trim();

if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
{ ShowError("Vui lòng nhập đầy đủ thông tin!"); return; }

if (username.Length < 3 || username.Length > 30)
{ ShowError("Tên đăng nhập phải từ 3 đến 30 ký tự!"); return; }

if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
{ ShowError("Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới!"); return; }

if (!IsValidEmail(email))
{ ShowError("Email không hợp lệ!"); return; }

if (password.Length < 6)
{ ShowError("Mật khẩu phải có ít nhất 6 ký tự!"); return; }
```

```csharp
// Client/Assets/Scripts/UI/Auth/LoginController.cs — trích đoạn OnLoginClicked()
string username = usernameInput.text.Trim();

if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
{ ShowError("Vui lòng nhập đầy đủ thông tin!"); return; }

if (username.Length < 3 || username.Length > 30)
{ ShowError("Tên đăng nhập phải từ 3 đến 30 ký tự!"); return; }

if (password.Length < 6)
{ ShowError("Mật khẩu phải có ít nhất 6 ký tự!"); return; }

loginButton.interactable = false;   // vô hiệu hóa ngay, tránh gửi trùng lặp
```

Sau khi người dùng nhấn nút xác nhận, nút được vô hiệu hóa ngay lập tức (`interactable = false`) cho đến khi nhận được phản hồi từ server. Cơ chế này được áp dụng nhất quán trên toàn bộ các màn hình thực hiện yêu cầu mạng — bao gồm `LoginController`, `RegisterController`, `GeneUpgradePanel`, `UpgradePanel` (cường hóa trang bị) và `HybridFusionPanel` (tổng hợp Kim Phong) — ngăn người dùng gửi nhiều yêu cầu trùng lặp trong khi yêu cầu đầu tiên đang được xử lý.

---

3.7Bảng tổng hợp kiểm thử các biện pháp bảo mật

Sau khi hiện thực hóa, mỗi biện pháp bảo mật được kiểm thử bằng cách tái hiện trực tiếp kịch bản tấn công tương ứng nhằm xác nhận kết quả hoạt động đúng với thiết kế. Bảng 3.X dưới đây mô tả phương pháp kiểm thử thủ công và kết quả kỳ vọng cho từng biện pháp.

**Bảng 3.X — Kịch bản kiểm thử các biện pháp bảo mật đã hiện thực hóa**

| STT | Biện pháp bảo mật | Kịch bản kiểm thử | Kết quả kỳ vọng |
|-----|-------------------|-------------------|-----------------|
| 1 | `[Authorize]` trên `EnemyController` | Gửi `GET /api/enemy` không có header `Authorization` | HTTP 401 Unauthorized, body rỗng |
| 2 | `[Authorize]` trên `EnemyController` | Gửi `GET /api/enemy` với JWT hợp lệ | HTTP 200 OK, trả về danh sách kẻ địch |
| 3 | Rate Limiting đăng nhập | Gửi 6 yêu cầu `POST /api/auth/login` liên tiếp trong 60 giây từ cùng một IP | Yêu cầu thứ 6 nhận HTTP 429, không truy vấn cơ sở dữ liệu |
| 4 | Zone API Key — khóa sai | Gửi `X-Zone-Api-Key` sai đến endpoint nội bộ | HTTP 401 Unauthorized |
| 5 | Zone API Key — timing | Gửi nhiều khóa sai với số ký tự đúng tăng dần, đo thời gian phản hồi | Thời gian phản hồi không có xu hướng tăng theo số ký tự đúng |
| 6 | NGO Connection — payload quá lớn | Kết nối đến Zone Server với payload > 2.048 byte | Kết nối bị từ chối, `response.Approved = false` |
| 7 | NGO Connection — JWT không hợp lệ | Kết nối đến Zone Server với JWT sai chữ ký trong payload | Kết nối bị từ chối, `response.Approved = false` |
| 8 | NGO Connection — JWT hết hạn | Kết nối đến Zone Server với JWT đã quá 7 ngày | Kết nối bị từ chối, `response.Approved = false` |
| 9 | ErrorHandlingMiddleware | Kích hoạt exception chưa xử lý trong controller | HTTP 500, body chứa `{ "success": false, "error": "Đã xảy ra lỗi hệ thống...", "errorCode": 500 }`, không có stack trace |
| 10 | SQL Injection qua EF Core | Nhập `' OR '1'='1` vào tham số truy vấn | Truy vấn tham số hóa ngăn SQL injection, không có kết quả bất thường |
| 11 | Input Validation — server | Gửi `POST /api/auth/register` với `Username = "ab"` (2 ký tự) | HTTP 400 Bad Request, không lưu dữ liệu vào cơ sở dữ liệu |
| 12 | Input Validation — server | Gửi `POST /api/auth/register` với `Username = "abc!@#"` (ký tự đặc biệt) | HTTP 400 Bad Request, thông báo vi phạm `[RegularExpression]` |
| 13 | Input Validation — client | Nhập `Username = "ab"` trong màn hình đăng ký rồi nhấn Đăng Ký | Thông báo lỗi hiện ngay trên UI, không gửi yêu cầu HTTP |
| 14 | UI anti-spam | Nhấn nút Đăng Nhập nhiều lần liên tiếp trước khi có phản hồi | Nút bị vô hiệu hóa sau lần nhấn đầu tiên, không gửi yêu cầu trùng lặp |

Kết quả kiểm thử thủ công cho thấy tất cả mười bốn kịch bản đều hoạt động đúng theo thiết kế. Sáu biện pháp kết hợp tạo thành một tuyến phòng thủ nhiều lớp: lớp transport ngăn kết nối trái phép vào Zone Server ngay từ bước handshake; lớp network làm chậm tấn công dò mật khẩu xuống mức không khả thi về mặt thực tế; lớp application kiểm soát quyền truy cập từng endpoint và phân tách rõ ràng hai loại caller; lớp data kiểm định định dạng và giới hạn dữ liệu đầu vào tại cả server lẫn client; và lớp presentation ngăn lộ thông tin kỹ thuật nhạy cảm. Sự kết hợp này đáp ứng các yêu cầu phi chức năng về bảo mật đã đặt ra trong giai đoạn thiết kế hệ thống.


---

3.8Xây dựng giao diện chức năng hệ thống

Hệ thống được xây dựng trên nền Unity 2D, toàn bộ giao diện người dùng được tổ chức thành các Scene và Panel riêng biệt tương ứng với từng luồng nghiệp vụ. Dưới đây là đặc tả chi tiết từng giao diện dựa trực tiếp trên mã nguồn tại thư mục Client/Assets/Scripts.

3.8.1Giao diện xác thực tài khoản

a) Đăng nhập

![Giao diện đăng nhập](extracted_images/image51.png)

*Hình 3.5. Giao diện đăng nhập*

Giao diện đăng nhập (LoginController.cs) bao gồm:
- usernameInput và passwordInput: hai trường nhập liệu TMP_InputField nhận tên đăng nhập và mật khẩu.
- togglePasswordButton kết hợp togglePasswordLabel: ẩn/hiện mật khẩu theo yêu cầu người dùng.
- loginButton: gọi REST API POST /api/auth/login, nhận JWT token lưu vào PlayerPrefs.
- registerButton: chuyển sang scene Register.
- accountListButton và accountListPanel: mở danh sách tài khoản đã đăng nhập trước đó (LoginSavedAccountStore), mỗi dòng LoginSavedAccountRow tự động điền lại usernameInput khi được chọn.
- errorText: hiển thị thông báo sai mật khẩu, tài khoản không tồn tại hoặc lỗi mạng.

b) Đăng ký

![Giao diện đăng ký](extracted_images/image52.png)

*Hình 3.6. Giao diện đăng ký*

Giao diện đăng ký (RegisterController.cs) bao gồm:
- usernameInput, emailInput, passwordInput, confirmPasswordInput: bốn trường nhập liệu.
- Kiểm tra validation tại Client: thông báo lỗi trực tiếp qua errorText khi trường trống, email không hợp lệ hoặc mật khẩu xác nhận không khớp.
- registerButton: gọi REST API POST /api/auth/register, đăng ký tài khoản mới trong bảng users.
- successText: hiển thị xác nhận đăng ký thành công và hướng dẫn đăng nhập.
- backButton: quay về scene Login.

3.8.2Giao diện khởi tạo và lựa chọn nhân vật

a) Chọn hệ nguyên tố — tạo nhân vật lần đầu

![Giao diện chọn hệ nguyên tố](extracted_images/image53.png)

*Hình 3.7. Giao diện chọn hệ nguyên tố*

Giao diện SelectElement (SelectElementController.cs) là bước tạo nhân vật duy nhất khi tài khoản chưa có nhân vật nào. Giao diện bao gồm:
- characterButtons: mảng 6 nút tương ứng với 6 hệ nguyên tố (Kim, Mộc, Thủy, Hỏa, Thổ, Phong), mỗi nút mang elementId riêng.
- previewImage: cửa sổ xem trước nhân vật 3D render theo hệ đang chọn, sử dụng RenderTexture từ một camera riêng.
- characterNameInput: trường nhập tên nhân vật.
- instructionText: hướng dẫn người chơi các bước tạo nhân vật.
- confirmButton và goButton: xác nhận và chuyển tiếp sau khi đặt tên.
- errorText: báo lỗi khi tên trùng, ký tự không hợp lệ hoặc kết nối thất bại.

b) Chọn nhân vật / hệ Gene

![Giao diện chọn nhân vật](extracted_images/image54.png)

*Hình 3.8. Giao diện chọn nhân vật (SelectGene)*

![Giao diện tạo nhân vật Gene 2 mới](extracted_images/image55.png)

*Hình 3.9. Giao diện tạo nhân vật Gene 2 mới*

Giao diện SelectGene (SelectGeneController.cs + GeneSlotUI.cs) xuất hiện khi tài khoản đã mở khoá Gene thứ hai. Mỗi GeneSlotUI hiển thị một trong ba trạng thái:
- existingCharacterPanel: khi nhân vật đã tồn tại — hiện characterNameText, levelText, elementText, genderIcon và playButton.
- emptySlotPanel: khi slot trống — hiện createCharacterButton và emptySlotLabel.
- lockedPanel: khi slot chưa mở — hiện lockedLabel.
Khi người chơi nhấn tạo nhân vật Gene 2, SelectGeneController mở createGene2Panel gồm createNameInput, confirmCreateButton, cancelCreateButton và createErrorText để phản hồi lỗi từ API. Kết quả lựa chọn lưu vào PlayerPrefs khoá ACTIVE_GENE_SLOT.

c) Sảnh chính

Hình 3.7 Giao diện sảnh chính

Giao diện sảnh chính (MainMenuController.cs) là điểm xuất phát kết nối vào gameplay. Giao diện bao gồm:
- playerInfoText: hiển thị thông tin tài khoản dạng "Level: X | Gold: Y | EXP: Z/W" đọc từ GameManager.GetPlayerData().
- joinGameButton: khi nhấn sẽ gọi NetworkManagerCustom.ConnectToServer(), playerInfoText đổi thành "Đang kết nối đến server...".
- logoutButton: xoá token, reset trạng thái và chuyển về scene Login.

3.8.3Giao diện trong trận đấu (HUD)

a) Thanh trạng thái nhân vật

![Giao diện thanh trạng thái nhân vật](extracted_images/image56.png)

*Hình 3.10. Giao diện thanh trạng thái nhân vật (HealthBar / MpBar / PlayerInfoUI)*

HUD trạng thái nhân vật gồm các thành phần:
- HealthBar: healthSlider (Slider) phản chiếu HP thời gian thực qua NetworkPlayerDataSync; healthTextTMP hiển thị số HP hiện tại/tối đa; fillImage đổi màu từ xanh (fullHealthColor) sang đỏ (lowHealthColor) khi HP xuống dưới 30%.
- MpBar: thanh MP tương tự HealthBar, đồng bộ qua cùng NetworkPlayerDataSync.
- PlayerInfoUI: playerNameText, levelText, elementText và hpText/mpText cập nhật realtime.
- FlightMeter: thanh đo stamina / năng lượng bay hiển thị khi nhân vật dùng kỹ năng bay.

b) Thanh kỹ năng và hiệu ứng Buff

![Giao diện thanh kỹ năng](extracted_images/image57.png)

*Hình 3.11. Giao diện thanh kỹ năng và Buff (SkillHotbarUI / BuffHudPanel)*

- SkillHotbarUI: quản lý danh sách SkillSlotUI (các ô hotbar), tự động bind với PlayerSkillManager của owner sau khi spawn. Mỗi SkillSlotUI hiển thị icon kỹ năng, cooldown đếm ngược và trạng thái khoá/mở.
- BuffHudPanel: liệt kê các Buff/Debuff đang active theo hàng icon; mỗi icon có đồng hồ đếm ngược thời gian hiệu lực còn lại.
- OverheadStatusDisplay: hiển thị icon trạng thái đặc biệt (suy yếu, stun, shield) ngay trên đầu nhân vật trong không gian thế giới.

c) Thông tin quái khi được chọn

![Giao diện thông tin quái](extracted_images/image58.png)

*Hình 3.12. Giao diện thông tin quái (EnemyInfoPanel)*

EnemyInfoPanel (EnemyInfoPanel.cs) xuất hiện khi người chơi click chọn một kẻ địch. Panel gồm:
- nameText: tên quái (ví dụ "Linh dương Topi").
- elementText: badge nhỏ hiển thị hệ nguyên tố ("Thổ", "Hỏa"...).
- hpSlider và hpText: thanh HP và số liệu dạng "48140 / 48140".
- levelExpText: cấp độ và EXP thưởng dạng "Lv: 52 + 28045 Exp".
- PlayerWorldHpBar: thanh HP nhỏ hiển thị trên đầu từng nhân vật/quái trong không gian thế giới để dễ theo dõi trong chiến đấu nhóm.

d) Thông báo toàn màn hình

![Giao diện thông báo hệ thống](extracted_images/image59.png)

*Hình 3.13. Giao diện thông báo hệ thống (GlobalNotificationUI)*

GlobalNotificationUI hiển thị các thông báo nổi ở giữa màn hình (level up, phần thưởng, sự kiện hệ thống) và tự động ẩn sau một khoảng thời gian, không chặn thao tác gameplay của người chơi.

3.8.4Giao diện hệ thống Gene

a) Nâng cấp Gene chính

![Giao diện nâng cấp Gene chính](extracted_images/image60.png)

*Hình 3.14. Giao diện nâng cấp Gene chính (GeneUpgradePanel)*

GeneUpgradePanel (GeneUpgradePanel.cs) là panel trung tâm của luồng phát triển nhân vật. Panel tải cấu hình từ /api/gene/config và trình bày:
- tierDisplayText: chuỗi chuyển tier dạng "Gene Tier 1 → 2"; elementIcon: sprite nguyên tố từ ElementIconConfig.
- geneExpBar (Slider readonly) và geneExpText: tiến độ EXP dạng "1000 / 5000 exp".
- goldCostText "Cần: X vàng" và goldPlayerText "Bạn có: Y vàng" đặt cạnh nhau để so sánh trực tiếp.
- itemCostText "x2 Linh Thạch Sơ Cấp (tối đa x5)" và itemIcon: vật liệu yêu cầu.
- successRateText "Tỉ lệ: 48%"; itemCountSlider: cho phép người chơi kéo chọn số lượng vật liệu (min = stone_min, max = stone_needed); itemCountText cập nhật realtime.
- statHpText "+200 HP", statMpText "+50 MP", statAtkText "+20 ATK", statDefText "+10 DEF": xem trước chỉ số tăng khi nâng thành công.
- skillsContainer: liệt kê tên kỹ năng sẽ mở khoá tại tier mục tiêu.
- upgradeButton: gửi yêu cầu qua ServerRpc; statusText phản hồi kết quả; loadingOverlay che panel khi đang gọi API.

b) Chọn Gene phụ cố định

![Giao diện xác nhận Gene phụ cố định](extracted_images/image61.png)

*Hình 3.15. Giao diện xác nhận Gene phụ cố định (SecondaryGeneSelectPanel)*

SecondaryGeneSelectPanel (SecondaryGeneSelectPanel.cs) là bước xác nhận hệ phụ — thao tác một lần không thể hoàn tác. Hệ phụ được cố định theo cặp Hybrid thiết kế sẵn: Hỏa ↔ Thổ, Thủy ↔ Mộc, Kim ↔ Phong. Panel gồm:
- warningText: cảnh báo rõ về tính vĩnh viễn của lựa chọn.
- primaryIcon + primaryNameText và secondaryIcon + secondaryNameText: hiển thị cặp hệ sẽ được gắn.
- previewPanel (ẩn đến khi load xong): hybridNameText tên form Hybrid tương lai, statBonusText chỉ số bonus khi fuse, bonusTargetsText hệ bị tăng 50% sát thương, immuneText hệ được miễn khắc chế.
- confirmButton: gọi API ghi secondary_element vào info_char.

c) Nâng cấp Gene phụ

![Giao diện nâng cấp Gene phụ](extracted_images/image62.png)

*Hình 3.16. Giao diện nâng cấp Gene phụ (SecondaryGeneUpgradePanel)*

SecondaryGeneUpgradePanel (SecondaryGeneUpgradePanel.cs) có bố cục tương tự GeneUpgradePanel nhưng gọi endpoint /api/gene/secondary/upgrade. Điểm khác biệt:
- tierDisplayText hiển thị "Hệ Phụ [Tên] — Tier 1 → 2"; secondaryElemIcon thay cho elementIcon.
- Toàn bộ stat bonus (statHpText, statMpText, statAtkText, statDefText) chỉ bằng 50% so với Gene chính cùng tier, phản ánh trọng số thấp hơn của hệ phụ trong gene_multi_config.
- itemCountSlider, successRateText và loadingOverlay hoạt động theo cùng cơ chế.

d) Dung hợp Hybrid

![Giao diện dung hợp Hybrid](extracted_images/image63.png)

*Hình 3.17. Giao diện dung hợp Hybrid (HybridFusionPanel)*

HybridFusionPanel (HybridFusionPanel.cs) chỉ kích hoạt khi cả Gene chính và Gene phụ đạt Tier 5. Panel tải cấu hình từ /api/gene/hybrid/config và hiển thị:
- hybridNameText: tên dạng thơ của form Hybrid, ví dụ "Kim Phong Thoán Thế"; hybridDescText: mô tả đặc trưng chiến đấu.
- elementAIcon + elementANameText "Hỏa Tier 5" và elementBIcon + elementBNameText: hai hệ cần dung hợp.
- statHpText "+2000 HP", statMpText "+500 MP", statAtkText "+500 ATK", statDefText "+200 DEF": tổng chỉ số sau khi fuse.
- immuneElementsText "Thủy, Kim": các hệ được miễn thiệt hại khắc chế — ánh xạ từ hybrid_immune_elements trong gene_hybrid_config.
- bonusTargetsText "Thổ, Hỏa": các hệ sẽ nhận sát thương tăng cường — ánh xạ từ hybrid_bonus_targets.
- goldCostText "2,000,000 Vàng"; itemCostText "x5 Lõi Đột Biến"; itemCountText "Bạn có: 3/5 Lõi Đột Biến".
- fuseButton: gọi API /api/gene/hybrid/fuse qua ServerRpc; successEffect (Particle/animation): phát hiệu ứng chuyển đổi khi thành công.

3.8.5Giao diện thông tin nhân vật

a) Bảng tóm tắt nhân vật

![Giao diện bảng tóm tắt nhân vật](extracted_images/image64.png)

*Hình 3.18. Giao diện bảng tóm tắt nhân vật (CharacterMenuPanelUI)*

CharacterMenuPanelUI (CharacterMenuPanelUI.cs) là panel nhanh hiển thị trên màn hình gameplay. Panel gồm:
- avatarImage: ảnh đại diện theo hệ nguyên tố.
- accountNameText, characterNameText: tên tài khoản và tên nhân vật.
- levelText "Cấp: 54 (62%)"; expSlider (0→1) và expDetailText "12345 / 20000 EXP": trực quan tiến độ cấp độ.
- Các nút điều hướng: questButton (mở nhiệm vụ), relationButton (mở PartyPanelUI), settingButton, changeCharButton (trở về SelectGene), quitButton.

b) Tab chỉ số và trang bị

![Giao diện tab Chỉ số và Trang bị](extracted_images/image65.png)

*Hình 3.19. Giao diện tab Chỉ số và Trang bị (StatsTabUI)*

StatsTabUI (StatsTabUI.cs) hiển thị:
- txtCharacterName, txtLevel, txtElement: thông tin nhận diện nhân vật.
- hpBar (Slider) + txtHp và mpBar + txtMp: HP/MP đồng bộ realtime từ NetworkPlayerDataSync.
- txtAttack, txtMoveSpeed, txtGold: các chỉ số chiến đấu và kinh tế.
- equipListContainer: danh sách dòng EquipRowUI liệt kê từng món trang bị đang mặc, cấp nâng cấp hiện tại và nút "Nâng cấp" trực tiếp.

c) Tab kỹ năng

![Giao diện tab Kỹ năng](extracted_images/image66.png)

*Hình 3.20. Giao diện tab Kỹ năng (SkillTabUI / SkillDetailPanelUI)*

SkillTabUI (SkillTabUI.cs) liệt kê toàn bộ kỹ năng sở hữu dưới dạng các dòng SkillRowUI. Khi người chơi chọn một kỹ năng, SkillDetailPanelUI mở ra hiển thị mô tả kỹ năng, level hiện tại, yêu cầu nâng cấp và nút nâng cấp gửi lên server qua UpgradeSkillServerRpc.

d) Tab tiềm năng

![Giao diện tab Tiềm Năng](extracted_images/image67.png)

*Hình 3.21. Giao diện tab Tiềm Năng (PotentialTabUI)*

PotentialTabUI (PotentialTabUI.cs) cho phép phân bổ điểm tiềm năng vào các chỉ số nhân vật:
- txtPotentialPoints: số điểm tiềm năng còn dư.
- statListContainer: sinh các dòng PotentialStatRowUI, mỗi dòng có nút +/- và ▲ để điều chỉnh pending delta.
- btnHuy: huỷ toàn bộ thay đổi pending, khôi phục điểm gốc.
- btnCong: xác nhận gom toàn bộ delta gửi lên server qua AllocatePotentialStatsServerRpc.

3.8.6Giao diện xã hội

a) Trò chuyện đa kênh

![Giao diện chat đa kênh](extracted_images/image68.png)

*Hình 3.22. Giao diện chat đa kênh (ChatPanelUI)*

ChatPanelUI (ChatPanelUI.cs) triển khai hệ thống chat nhiều kênh kết nối SignalR. Giao diện gồm:
- messageScrollRect + messageContent: ScrollView hiển thị tối đa 80 tin nhắn đồng thời theo cơ chế Object Queue.
- ChatTabUI (tabBar): tabs Chung / Riêng / Gia tộc / Nhóm / Lớp.
- chatInputField và sendButton: nhập và gửi tin.
- ChatChannelDropdownUI: chuyển kênh nhanh ngay trên thanh nhập liệu kèm channelIconLabel ("LC") và channelNameLabel ("Lân cận").
- ProximityChatBubble: bong bóng thoại xuất hiện trên đầu nhân vật trong không gian thế giới khi có tin nhắn lân cận.

b) Danh sách bạn bè

![Giao diện danh sách bạn bè](extracted_images/image69.png)

*Hình 3.23. Giao diện danh sách bạn bè (FriendListUI)*

FriendListUI (FriendListUI.cs) nhúng trực tiếp trong friendListPanel của ChatPanelUI. Người chơi xem danh sách bạn bè đang online/offline, nhấn vào một bạn bè để mở PlayerProfilePanelUI — hiện thông tin cá nhân, nút gửi tin nhắn riêng và nút mời vào tổ đội mà không cần đóng cửa sổ chat.

c) Tổ đội

![Giao diện tổ đội](extracted_images/image70.png)

*Hình 3.24. Giao diện tổ đội (PartyPanelUI)*

PartyPanelUI (PartyPanelUI.cs) quản lý tương tác nhóm qua ba tab:
- Tab Tổ Đội: memberListRoot sinh các PartyMemberEntryUI; lockToggle (khoá nhóm); autoAcceptToggle (tự chấp nhận yêu cầu); actionButton đổi nhãn động theo trạng thái (Tạo nhóm / Rời nhóm); chatGroupButton mở kênh chat nhóm.
- Tab Tìm Nhóm: searchListRoot liệt kê PartySearchEntryUI, refreshSearchButton tải lại danh sách.
- Tab Gần Đây: nearbyListRoot sinh PartyNearbyEntryUI; nearbyPopulationText hiện số người cùng map.
Yêu cầu vào nhóm đẩy vào hàng đợi _pendingJoinRequests và hiện tuần tự qua PartyJoinRequestPopupUI để trưởng nhóm duyệt từng yêu cầu.

d) Bảng xếp hạng

![Giao diện bảng xếp hạng](extracted_images/image71.png)

*Hình 3.25. Giao diện bảng xếp hạng (LeaderboardPanelUI)*

LeaderboardPanelUI (LeaderboardPanelUI.cs) tổ chức hai tầng tab:
- 4 mainTabs: Đua Top / Sự Kiện / Tuần & Tháng / Thưởng.
- 5 subTabs: Cao Thủ / Nạp Vàng / Hoa Chi / Chuyên Cần / Phó Bản — tiêu đề cột giá trị trong headerCells thay đổi động theo sub-tab (Cấp / Vàng / N.Vu / Ngày / Wave).
- rowContent (ScrollRect) sinh các LeaderboardRowEntryUI qua LeaderboardService.
- emptyStateGroup + emptyStateText: hiển thị khi danh sách rỗng.
- loadingText: thông báo trạng thái tải.

3.8.7Giao diện phó bản

a) Danh sách phó bản

![Giao diện chọn phó bản](extracted_images/image72.png)

*Hình 3.26. Giao diện chọn phó bản (DungeonListUI)*

DungeonListUI (DungeonListUI.cs) là panel mở danh sách tất cả phó bản hiện có. Panel gồm:
- dungeonListContent (ScrollView): sinh các DungeonButtonItem từ dungeonItemPrefab, mỗi mục hiển thị tên phó bản, mô tả, cấp độ yêu cầu và trạng thái.
- loadingIndicator: spinner trong khi tải danh sách từ API.
- confirmDialog: hộp thoại xác nhận trước khi vào, hiện confirmDungeonName, confirmDesc, confirmYesBtn và confirmNoBtn.
- statusText: thông báo lỗi hoặc trạng thái không thoả điều kiện tham gia.

b) HUD phó bản wave

![Giao diện HUD phó bản wave](extracted_images/image73.png)

*Hình 3.27. Giao diện HUD phó bản wave (WaveHUD)*

WaveHUD (WaveHUD.cs) xuất hiện khi người chơi đang trong phó bản dạng Wave. Giao diện gồm:
- roundText: số vòng hiện tại và tổng số vòng, dạng "Vòng 2 / 5".
- timerText: thời gian còn lại trong vòng đếm ngược theo giây.
- hudRoot: ẩn hoàn toàn khi không ở trong dungeon, tự động hiện khi WaveDungeonRuntime được load.
- Script đọc trực tiếp NetworkVariable CurrentRound / RemainingSeconds / MaxRounds từ WaveDungeonRuntime mà không cần gán thủ công trong Inspector.

c) NPC trong phó bản

Hình 3.26 Giao diện NPC trong phó bản (DungeonNpcMenuUI)

DungeonNpcMenuUI (DungeonNpcMenuUI.cs) hiển thị menu tương tác với NPC đặc biệt bên trong phó bản. Mỗi lựa chọn được sinh ra dưới dạng DungeonNpcMenuEntryUI — hiển thị tên hành động, mô tả ngắn và nút xác nhận. NPC phó bản có thể cung cấp hồi HP/MP giữa các vòng, bán vật phẩm tăng cường tạm thời hoặc kích hoạt sự kiện đặc biệt trong dungeon.

3.8.8Giao diện nhiệm vụ và tương tác NPC thế giới

a) Widget theo dõi nhiệm vụ

![Giao diện widget nhiệm vụ góc màn hình](extracted_images/image74.png)

*Hình 3.28. Giao diện widget nhiệm vụ góc màn hình (QuestHudWidget)*

QuestHudWidget (QuestHudWidget.cs) là widget cố định ở góc màn hình theo dõi nhiệm vụ đang active:
- questNameText: tiêu đề nhiệm vụ chính đang theo dõi, dạng "Chính: [tên quest]".
- questStepText: bước hiện tại dạng "- [tên bước]: done/require" hoặc "- ✓ Tìm [npc_name] để nộp".
- btnNavigate "→": kích hoạt tính năng tự động di chuyển tới mục tiêu nhiệm vụ, script tính toán vị trí NPC/map đích và điều khiển nhân vật tự chạy đến.
- rootWidget: ẩn tự động khi có panel khác đang mở, hiện lại khi không còn panel nào.

b) NPC nhiệm vụ

Hình 3.28 Giao diện tương tác NPC nhiệm vụ (QuestNpcPanel)

QuestNpcPanel (QuestNpcPanel.cs) mở ra khi người chơi tương tác với NPC trong thế giới. Panel liệt kê toàn bộ nhiệm vụ mà NPC này cung cấp hoặc tiếp nhận nộp, trạng thái từng nhiệm vụ (chưa nhận / đang làm / hoàn thành) và phần thưởng tương ứng. Trạng thái nhiệm vụ được điều khiển bằng State Machine lưu phía Server, đảm bảo tiến trình không bị mất khi người chơi đăng xuất.

c) Menu NPC động và cửa hàng

![Giao diện menu NPC động và cửa hàng](extracted_images/image75.png)

*Hình 3.29. Giao diện menu NPC động và cửa hàng (NpcDynamicMenuUI / NpcMenuUI)*

NpcDynamicMenuUI (NpcDynamicMenuUI.cs) sinh menu tương tác với NPC theo cấu hình từ Backend, hỗ trợ nhiều loại hành động khác nhau (mở cửa hàng, nhiệm vụ, chức năng đặc biệt). Khi người chơi chọn mua hàng, NpcMenuUI (NpcMenuUI.cs) mở danh sách ShopItemRowUI — mỗi dòng hiển thị tên vật phẩm, biểu tượng hệ nguyên tố, giá vàng, số lượng tồn và nút mua trực tiếp.

3.8.9Giao diện hệ thống bản đồ thế giới

a) Di chuyển qua biên map (MapEdgeTrigger)

![Giao diện chuyển map qua biên](extracted_images/image76.png)

*Hình 3.30. Giao diện chuyển map qua biên (MapEdgeTrigger / MapTransitionButton)*

Thế giới game được chia thành 14 map liên tiếp (Map00–Map13), mỗi map tương ứng một Unity Scene riêng. Hệ thống điều hướng bản đồ gồm hai cơ chế:
- MapEdgeTrigger: BoxCollider2D isTrigger đặt tại rìa trái/phải của scene; khi LocalPlayer (phát hiện qua NetworkObject.IsOwner) bước vào vùng trigger, script gọi API GET /api/map/edge?mapId=X&direction=right để lấy destMapId và vị trí xuất hiện tương ứng, sau đó load scene đích với transitionDelay mặc định 0.5 giây.
- MapTransitionButton: nút mũi tên "←" / "→" trên HUD (isRightButton) phục vụ di chuyển thủ công hoặc trên thiết bị di động; khi nhấn, gọi cùng API và hiện loadingPanel + errorText nếu map kề không tồn tại.
- MapManager: Singleton DontDestroyOnLoad tự động gọi GET /api/map/by-scene?scene=... khi mỗi scene load để resolve mapId và mapName; cung cấp MapManager.Instance.GetMapId() cho tất cả các script khác trong scene.

b) Cổng dịch chuyển trong bản đồ và phó bản (MapPortalTrigger)

Hình 3.31 Cổng dịch chuyển phòng trong bản đồ và phó bản (MapPortalTrigger)

MapPortalTrigger (MapPortalTrigger.cs) là cổng đặt trực tiếp trong các scene thế giới và phó bản để chuyển dịch giữa các khu vực hoặc vào/thoát dungeon. Mỗi cổng mang:
- `portalId` và `currentMapId`: lấy từ bảng `map_portal` trong DB hoặc tự động điền bởi `DungeonManager.LoadPortalsFromServer()`.
- `portalType`: phân loại "enter_dungeon" | "room_transition" | "exit_dungeon".
- ----- [BẮT ĐẦU PHẦN THÊM MỚI] ----- Quy trình xác thực điều kiện qua cổng: Khi LocalPlayer chạm vào cổng, `MapPortalTrigger` gửi yêu cầu xác thực qua API `POST /api/map/travel` để backend kiểm tra:
  - **Cấp độ yêu cầu (`min_level`)**: Nếu bản đồ đích yêu cầu cấp độ tối thiểu lớn hơn cấp hiện tại của nhân vật, server từ chối và client hiển thị thông báo lỗi cấp độ.
  - **Mốc nhiệm vụ bắt buộc (`required_quest_id`)**: Nếu bản đồ đích yêu cầu hoàn thành một nhiệm vụ cốt truyện cụ thể, server đối chiếu với danh sách nhiệm vụ đã xong của người chơi. Nếu chưa hoàn thành, từ chối chuyển map và hiển thị tên nhiệm vụ cần thực hiện.
  - **Vật phẩm yêu cầu (`required_item_id`)**: Nếu cổng yêu cầu vật phẩm chìa khóa, server quét túi đồ JSON để xác nhận sự tồn tại của item. Nếu thiếu, client bật hiển thị `keyRequiredPrompt`. ----- [KẾT THÚC PHẦN THÊM MỚI] -----
- Khi API trả về `success = true`, client kích hoạt `transitionDelay` (mặc định 0.8 giây) chạy hiệu ứng fade-out và gọi `ZoneTransitionController.RequestMapPortalTransferServerRpc` để server di chuyển player sang map mới an toàn.
- `portalVisual`: Particle/sprite minh hoạ cổng; transitionDelay: 0.8 giây chờ hiệu ứng fade trước khi load scene mới.

3.9Tổng kết chương 3

Chương 3 đã hoàn thiện việc hiện thực hóa toàn bộ hệ thống ở cả ba tầng: Client (Unity), Backend (ASP.NET Core) và Zone Server (Unity Netcode). Nhờ áp dụng các cơ chế như đồng bộ NetworkVariable, ServerRpc/ClientRpc, SignalR Hub và REST API theo đúng phân công trách nhiệm, hệ thống liên kết chặt chẽ các quy trình từ đăng nhập, chọn Gene, nâng Gene chính — Gene phụ — dung hợp Hybrid, vào gameplay realtime, hoàn thành phó bản wave cho đến khi ghi nhận điểm số và xếp hạng. Chuỗi giao diện được đặc tả trong mục 3.5 phản ánh trực tiếp các script trong thư mục Client/Assets/Scripts, bảo đảm mỗi trường dữ liệu hiển thị đều có nguồn gốc xác định từ mã nguồn thực tế của đồ án.

