# Chapter 3 Image Mapping from DoAn.docx

**Para 971**: `[]` | Text: Hình 3.4. Kiến trúc triển khai Docker Compose của hệ thống

**Para 972**: `[]` | Text: Kiến trúc ba container và phân tách mạng nội bộ

**Para 973**: `[]` | Text: Toàn bộ hạ tầng được tổ chức thành ba container, mỗi container đảm nhiệm đúng một tầng trong kiến trúc hệ thống. Cấu hình cụ thể được trình bày trong Bảng 3.X dưới đây.

**Para 974**: `[]` | Text: Bảng 3.2 Cấu hình các container trong hệ thống Docker

**Para 976**: `[]` | Text: Container db được cấu hình chỉ tham gia vào mạng nội bộ Docker (internal: true) và không được ánh xạ bất kỳ cổng nào ra ngoài máy chủ vật lý. Điều này có nghĩa: ngay cả khi kẻ tấn công xâm nhập được vào máy chủ qua các vector khác, cơ sở dữ liệu vẫn không thể bị kết nối trực tiếp từ bên ngoài mà không đi qua tầng API đã được xác thực.

**Para 977**: `[]` | Text: Container api phụ thuộc vào db với điều kiện health check, đảm bảo MariaDB hoàn toàn sẵn sàng tiếp nhận kết nối trước khi ASP.NET Core khởi động và cố gắng thực hiện database migration. Cấu hình retry của Pomelo EF Core (MaxRetryCount = 3, MaxRetryDelay = 5s) xử lý trường hợp container db chậm khởi động hơn dự kiến do tải hệ thống.

**Para 979**: `[]` | Text: Bản đồ Giao diện người dùng

**Para 980**: `[]` | Text: Giao diện xác thực tài khoản

**Para 981**: `[]` | Text: Giao diện đăng nhập

**Para 982**: `['image51.png']` | Text: 

**Para 983**: `[]` | Text: Hình 3.5.Giao diện đăng nhập

**Para 984**: `[]` | Text: Giao diện đăng nhập là điểm khởi đầu của hệ thống, được hiện thực thông qua LoginController.cs. Giao diện này cho phép người chơi nhập thông tin tài khoản, xác thực với backend và lưu lại token phiên đăng nhập để sử dụng cho các request tiếp theo.

**Para 985**: `[]` | Text: UsernameInput và passwordInput là hai trường nhập liệu TMP_InputField dùng để nhận tên đăng nhập và mật khẩu của người chơi.

**Para 986**: `[]` | Text: TogglePasswordButton kết hợp với togglePasswordLabel cho phép người dùng ẩn hoặc hiện mật khẩu khi nhập liệu.

**Para 987**: `[]` | Text: LoginButton gửi yêu cầu đăng nhập đến REST API POST /api/auth/login. Khi đăng nhập thành công, client nhận JWT token và lưu vào PlayerPrefs để sử dụng cho các request cần xác thực.

**Para 988**: `[]` | Text: RegisterButton chuyển người dùng sang scene đăng ký tài khoản mới.

**Para 989**: `[]` | Text: AccountListButton và accountListPanel hiển thị danh sách các tài khoản đã từng đăng nhập trên thiết bị. Dữ liệu này được quản lý bởi LoginSavedAccountStore; mỗi dòng LoginSavedAccountRow có thể tự động điền lại usernameInput khi được chọn.

**Para 990**: `[]` | Text: ErrorText hiển thị lỗi xác thực, chẳng hạn tài khoản không tồn tại, mật khẩu không đúng hoặc không kết nối được đến server.

**Para 991**: `[]` | Text: Giao diện đăng ký

**Para 992**: `['image52.png']` | Text: 

**Para 993**: `[]` | Text: Hình 3.6.Giao diện đăng ký

**Para 994**: `[]` | Text: Giao diện đăng ký được triển khai bởi RegisterController.cs, cho phép người chơi tạo tài khoản mới trước khi vào hệ thống. Trước khi gửi dữ liệu lên server, client thực hiện một số kiểm tra cơ bản để giảm lỗi nhập liệu và cải thiện trải nghiệm người dùng.

**Para 995**: `[]` | Text: Usernameinput, emailinput, passwordinput và confirmpasswordinput là bốn trường dữ liệu chính để tạo tài khoản.

**Para 996**: `[]` | Text: Client kiểm tra tính hợp lệ của dữ liệu nhập, bao gồm trường bắt buộc, định dạng email và sự trùng khớp giữa passwordinput với confirmpasswordinput.

**Para 997**: `[]` | Text: Registerbutton gửi dữ liệu đến REST API POST /api/auth/register để tạo bản ghi tài khoản trong bảng users.

**Para 998**: `[]` | Text: Successtext hiển thị thông báo đăng ký thành công và hướng dẫn người chơi quay lại màn hình đăng nhập.

**Para 999**: `[]` | Text: Backbutton cho phép người dùng quay về scene Login nếu không muốn tiếp tục đăng ký.

**Para 1000**: `[]` | Text: Giao diện khởi tạo và lựa chọn nhân vật

**Para 1001**: `[]` | Text: Giao diện chọn hệ nguyên tố và tạo nhân vật lần đầu

**Para 1002**: `['image53.png']` | Text: 

**Para 1004**: `[]` | Text: Hình 3.7.Giao diện chọn hệ nguyên tố

**Para 1005**: `[]` | Text: Sau khi tài khoản đăng nhập thành công nhưng chưa có nhân vật, hệ thống chuyển người chơi đến giao diện SelectElement. Giao diện này được điều khiển bởi SelectElementController.cs và dùng để khởi tạo nhân vật đầu tiên của tài khoản.

**Para 1006**: `[]` | Text: Characterbuttons là danh sách sáu nút tương ứng với sáu hệ nguyên tố Kim, Mộc, Thủy, Hỏa, Thổ và Phong. Mỗi nút chứa elementid để xác định hệ được chọn.

**Para 1007**: `[]` | Text: Previewimage hiển thị hình ảnh xem trước nhân vật theo hệ nguyên tố đang chọn. Thành phần này giúp người chơi nhận biết trực quan nhân vật trước khi tạo.

**Para 1008**: `[]` | Text: Characternameinput cho phép nhập tên nhân vật. Dữ liệu này được kiểm tra trước khi gửi yêu cầu tạo nhân vật.

**Para 1009**: `[]` | Text: Instructiontext hướng dẫn các bước chọn hệ, đặt tên và xác nhận tạo nhân vật.

**Para 1010**: `[]` | Text: Confirmbutton và gobutton dùng để xác nhận lựa chọn và chuyển tiếp sang bước kế tiếp.

**Para 1011**: `[]` | Text: Errortext hiển thị các lỗi như tên nhân vật không hợp lệ, tên đã tồn tại hoặc không kết nối được đến backend.

**Para 1012**: `[]` | Text: Giao diện chọn nhân vật và Gene Slot

**Para 1013**: `['image54.png']` | Text: 

**Para 1014**: `[]` | Text: Hình 3.8.Giao diện chọn nhân vật (SelectGene)

**Para 1015**: `['image55.png']` | Text: 

**Para 1016**: `[]` | Text: Hình 3.9.Giao diện tạo nhân vật Gene 2

**Para 1017**: `[]` | Text: Khi tài khoản có nhiều slot nhân vật hoặc đã mở khóa Gene thứ hai, hệ thống sử dụng giao diện SelectGene để người chơi lựa chọn nhân vật trước khi vào game. Chức năng này được hiện thực bởi SelectGeneController.cs kết hợp với GeneSlotUI.cs.

**Para 1018**: `[]` | Text: Existingcharacterpanel được hiển thị khi slot đã có nhân vật. Panel này thể hiện characternametext, leveltext, elementtext, gendericon và playbutton.

**Para 1019**: `[]` | Text: Emptyslotpanel được hiển thị khi slot còn trống, gồm createcharacterbutton và emptyslotlabel để người chơi tạo nhân vật mới.

**Para 1020**: `[]` | Text: Lockedpanel được hiển thị khi slot chưa được mở khóa, dùng lockedlabel để thông báo trạng thái hiện tại.

**Para 1021**: `[]` | Text: Khi tạo nhân vật Gene 2, selectgenecontroller mở creategene2panel, trong đó có createnameinput, confirmcreatebutton, cancelcreatebutton và createerrortext để xử lý phản hồi từ API.

**Para 1022**: `[]` | Text: Kết quả lựa chọn được lưu vào playerprefs thông qua khóa ACTIVE_GENE_SLOT, giúp các scene tiếp theo xác định đúng nhân vật đang được sử dụng.

**Para 1023**: `[]` | Text: Giao diện trong trận đấu

**Para 1024**: `[]` | Text: Thanh trạng thái nhân vật

**Para 1025**: `['image56.png']` | Text: 

**Para 1026**: `[]` | Text: Hình 3.10.Giao diện thanh trạng thái nhân vật

**Para 1027**: `[]` | Text: HUD trạng thái nhân vật hiển thị các thông tin cần thiết trong quá trình chiến đấu, bao gồm máu, năng lượng, cấp độ, hệ nguyên tố và một số trạng thái đặc biệt. Các dữ liệu này được đồng bộ từ NetworkPlayerDataSync để bảo đảm giao diện phản ánh đúng trạng thái runtime.

**Para 1028**: `[]` | Text: HealthBar sử dụng healthSlider để phản chiếu HP thời gian thực. healthTextTMP hiển thị giá trị HP hiện tại và tối đa; fillImage có thể chuyển từ màu xanh sang màu cảnh báo khi HP xuống thấp.

**Para 1029**: `[]` | Text: MpBar hiển thị MP theo cơ chế tương tự HealthBar và đồng bộ qua cùng nguồn dữ liệu mạng.

**Para 1030**: `[]` | Text: Thanh kỹ năng và hiệu ứng Buff

**Para 1031**: `['image57.png']` | Text: 

**Para 1032**: `[]` | Text: Hình 3.11.Giao diện thanh kỹ năng

**Para 1033**: `[]` | Text: Thanh kỹ năng cho phép người chơi theo dõi các kỹ năng đang sở hữu, trạng thái mở khóa và thời gian hồi chiêu. Nhóm giao diện này được tổ chức qua SkillHotbarUI, SkillSlotUI, BuffHudPanel và OverheadStatusDisplay.

**Para 1034**: `[]` | Text: SkillHotbarUI quản lý danh sách SkillSlotUI và tự động gắn dữ liệu với PlayerSkillManager của nhân vật owner sau khi spawn.

**Para 1035**: `[]` | Text: Mỗi SkillSlotUI hiển thị biểu tượng kỹ năng, trạng thái khóa/mở và hiệu ứng cooldown đếm ngược.

**Para 1036**: `[]` | Text: BuffHudPanel liệt kê các Buff hoặc Debuff đang có hiệu lực dưới dạng hàng icon kèm thời gian còn lại.

**Para 1037**: `[]` | Text: OverheadStatusDisplay hiển thị trạng thái đặc biệt như suy yếu, stun hoặc shield ngay trên đầu nhân vật trong không gian thế giới.

**Para 1038**: `['image58.png']` | Text: Thông tin quái khi được chọn

**Para 1039**: `[]` | Text: Hình 3.12.Giao diện thông tin quái

**Para 1040**: `[]` | Text: EnemyInfoPanel.cs được sử dụng khi người chơi chọn một kẻ địch trong bản đồ. Panel này giúp người chơi nắm bắt nhanh thông tin mục tiêu trước khi tấn công hoặc sử dụng kỹ năng.

**Para 1041**: `[]` | Text: Nametext hiển thị tên quái, ví dụ “Linh dương Topi”.

**Para 1042**: `[]` | Text: Elementtext hiển thị hệ nguyên tố của quái dưới dạng badge nhỏ.

**Para 1043**: `[]` | Text: Hpslider và hptext thể hiện lượng máu hiện tại và tối đa của mục tiêu.

**Para 1044**: `[]` | Text: Levelexptext hiển thị cấp độ và lượng kinh nghiệm thưởng khi tiêu diệt mục tiêu.

**Para 1045**: `[]` | Text: Playerworldhpbar hiển thị thanh HP nhỏ trên đầu nhân vật hoặc quái trong không gian thế giới, giúp theo dõi dễ hơn trong chiến đấu nhóm.

**Para 1046**: `[]` | Text: Thông báo toàn màn hình

**Para 1047**: `['image59.png']` | Text: 

**Para 1048**: `[]` | Text: Hình 3.13.Giao diện thông báo hệ thống

**Para 1049**: `[]` | Text: GlobalNotificationUI dùng để hiển thị các thông báo nổi như lên cấp, nhận thưởng hoặc sự kiện hệ thống. Thông báo xuất hiện trong thời gian ngắn và tự động ẩn, không chặn thao tác gameplay của người chơi.

**Para 1050**: `[]` | Text: Giao diện hệ thống Gene

**Para 1051**: `[]` | Text: a) Giao diện nâng cấp Gene chính

**Para 1052**: `['image60.png']` | Text: 

**Para 1053**: `[]` | Text: Hình 3.14.Giao diện nâng cấp Gene chính

**Para 1054**: `[]` | Text: GeneUpgradePanel.cs là giao diện trung tâm của hệ thống phát triển nhân vật theo Gene. Panel này tải cấu hình nâng cấp từ API /api/gene/config và hiển thị đầy đủ điều kiện, tài nguyên, tỷ lệ thành công và chỉ số dự kiến sau khi nâng cấp.

**Para 1055**: `[]` | Text: Tierdisplaytext hiển thị quá trình chuyển tier, ví dụ “Gene Tier 1 → 2”; elementicon lấy biểu tượng nguyên tố từ elementiconconfig.

**Para 1056**: `[]` | Text: GeneExpBar và geneexptext thể hiện tiến độ Gene kinh nghiệm so với lượng kinh nghiệm yêu cầu.

**Para 1057**: `[]` | Text: Goldcosttext và goldplayertext đặt cạnh nhau để người chơi so sánh lượng vàng cần dùng và lượng vàng đang có.

**Para 1058**: `[]` | Text: Itemcosttext và itemicon hiển thị vật phẩm nâng Gene theo cấu hình, bao gồm số lượng tối thiểu và số lượng tối đa có thể sử dụng.

**Para 1059**: `[]` | Text: Successratetext hiển thị tỷ lệ thành công; itemcountslider cho phép người chơi chọn số lượng vật phẩm trong khoảng itemsmin đến itemsneeded; itemcounttext cập nhật theo giá trị người chơi chọn.

**Para 1060**: `[]` | Text: Stathptext, statmptext, statatktext và statdeftext trình bày phần chỉ số tăng thêm nếu nâng cấp thành công.

**Para 1061**: `[]` | Text: Skillscontainer liệt kê các kỹ năng sẽ được mở khóa tại tier mục tiêu.

**Para 1062**: `[]` | Text: Upgradebutton gửi yêu cầu nâng Gene qua serverrpc; statustext nhận phản hồi kết quả; loadingoverlay được bật trong thời gian chờ server xử lý.

**Para 1063**: `[]` | Text: Giao diện chọn Gene phụ cố định

**Para 1064**: `['image61.png']` | Text: 

**Para 1065**: `[]` | Text: Hình 3.15.Giao diện xác nhận Gene phụ cố định

**Para 1066**: `[]` | Text: SecondaryGeneSelectPanel.cs được dùng cho thao tác chọn hệ phụ. Đây là thao tác quan trọng vì hệ phụ được gắn với cặp Hybrid cố định và không nên cho phép thay đổi tùy tiện sau khi đã xác nhận.

**Para 1067**: `[]` | Text: Warningtext cảnh báo người chơi về tính vĩnh viễn của lựa chọn hệ phụ.

**Para 1068**: `[]` | Text: Primaryicon, primarynametext, secondaryicon và secondarynametext hiển thị cặp hệ chính - hệ phụ được chọn.

**Para 1069**: `[]` | Text: Previewpanel chỉ hiển thị sau khi dữ liệu cấu hình đã được tải xong, gồm hybridnametext, statbonustext, bonustargetstext và immunetext.

**Para 1070**: `[]` | Text: Các cặp hệ phụ được cấu hình theo thiết kế Hybrid: Hỏa - Thổ, Thủy - Mộc và Kim - Phong.

**Para 1071**: `[]` | Text: Confirmbutton gửi yêu cầu ghi secondary_element vào dữ liệu nhân vật trên backend.

**Para 1072**: `[]` | Text: Giao diện nâng cấp Gene phụ

**Para 1074**: `['image62.png']` | Text: 

**Para 1075**: `[]` | Text: Hình 3.16.Giao diện nâng cấp Gene phụ

**Para 1076**: `[]` | Text: SecondaryGeneUpgradePanel.cs có bố cục tương tự GeneUpgradePanel nhưng phục vụ luồng nâng cấp hệ phụ thông qua endpoint /api/gene/secondary/upgrade. Giao diện này thể hiện rõ sự khác biệt giữa Gene chính và Gene phụ để người chơi hiểu vai trò hỗ trợ của hệ phụ.

**Para 1077**: `[]` | Text: Tierdisplaytext hiển thị dạng “Hệ phụ [Tên] - Tier 1 → 2”; secondaryelemicon thay cho biểu tượng nguyên tố chính.

**Para 1078**: `[]` | Text: Các chỉ số tăng thêm như HP, MP, ATK và DEF chỉ chiếm tỷ lệ hỗ trợ so với Gene chính theo cấu hình của hệ thống.

**Para 1079**: `[]` | Text: Itemcountslider, successratetext, statustext và loadingoverlay hoạt động tương tự giao diện nâng cấp Gene chính.

**Para 1080**: `[]` | Text: Sau khi nâng cấp thành công, dữ liệu Gene phụ và chỉ số nhân vật được đồng bộ lại để các panel khác hiển thị thống nhất.

**Para 1081**: `[]` | Text: Giao diện dung hợp Hybrid

**Para 1082**: `['image63.png']` | Text: 

**Para 1083**: `[]` | Text: Hình 3.17. Giao diện dung hợp Hybrid

**Para 1084**: `[]` | Text: HybridFusionPanel.cs chỉ được kích hoạt khi Gene chính và Gene phụ đều đạt Tier 5, đồng thời cặp nguyên tố phù hợp với cấu hình Hybrid. Panel tải dữ liệu từ API /api/gene/hybrid/config để hiển thị điều kiện dung hợp trước khi người chơi xác nhận.

**Para 1085**: `[]` | Text: Hybridnametext hiển thị tên dạng đặc trưng của Hybrid, ví dụ “Kim Phong Thoán Thế”; hybriddesctext mô tả phong cách chiến đấu của dạng Hybrid.

**Para 1086**: `[]` | Text: Elementaicon, elementanametext, elementbicon và elementbnametext thể hiện hai hệ nguyên tố tham gia dung hợp cùng tier tương ứng.

**Para 1087**: `[]` | Text: Stathptext, statmptext, statatktext và statdeftext hiển thị phần chỉ số tăng thêm sau khi dung hợp thành công.

**Para 1088**: `[]` | Text: Immuneelementstext hiển thị danh sách hệ miễn hoặc giảm khắc chế theo dữ liệu hybrid_immune_elements trong cấu hình.

**Para 1089**: `[]` | Text: Bonustargetstext hiển thị danh sách hệ mục tiêu được cấu hình nhận bonus sát thương theo hybrid_bonus_targets.

**Para 1090**: `[]` | Text: Goldcosttext, itemcosttext và itemcounttext cho biết chi phí vàng, vật phẩm yêu cầu và số lượng vật phẩm người chơi đang có.

**Para 1091**: `[]` | Text: Fusebutton gửi yêu cầu /api/gene/hybrid/fuse qua serverrpc; successeffect phát hiệu ứng chuyển đổi khi dung hợp thành công.

**Para 1092**: `[]` | Text: Giao diện thông tin nhân vật

**Para 1093**: `[]` | Text: Bảng tóm tắt nhân vật

**Para 1094**: `['image64.png']` | Text: 

**Para 1095**: `[]` | Text: Hình 3.18. Giao diện bảng tóm tắt nhân vật

**Para 1096**: `[]` | Text: CharacterMenuPanelUI.cs là panel tóm tắt nhanh trên màn hình gameplay. Panel này giúp người chơi xem thông tin cơ bản và truy cập nhanh đến các nhóm chức năng khác.

**Para 1097**: `[]` | Text: Avatarimage hiển thị ảnh đại diện theo hệ nguyên tố hoặc dạng nhân vật hiện tại.

**Para 1098**: `[]` | Text: Accountnametext và characternametext hiển thị tên tài khoản và tên nhân vật.

**Para 1099**: `[]` | Text: Leveltext, expslider và expdetailtext thể hiện cấp độ, phần trăm kinh nghiệm và chi tiết kinh nghiệm

**Para 1100**: `[]` | Text: hiện tại.

**Para 1101**: `[]` | Text: Các nút questbutton, relationbutton, settingbutton, changecharbutton và quitbutton lần lượt mở giao diện nhiệm vụ, quan hệ/tổ đội, cài đặt, đổi nhân vật và thoát.

**Para 1102**: `[]` | Text: Tab chỉ số và trang bị

**Para 1103**: `['image65.png']` | Text: 

**Para 1104**: `[]` | Text: Hình 3.19. Giao diện tab Chỉ số và Trang bị

**Para 1105**: `[]` | Text: StatsTabUI.cs hiển thị thông tin chi tiết về chỉ số chiến đấu và trang bị đang mặc. Đây là giao diện quan trọng để người chơi theo dõi sự thay đổi sức mạnh sau khi lên cấp, nâng trang bị hoặc phát triển Gene.

**Para 1106**: `[]` | Text: Txtcharactername, txtlevel và txtelement hiển thị thông tin định danh của nhân vật.

**Para 1107**: `[]` | Text: Hpbar, txthp, mpbar và txtmp phản ánh HP/MP hiện tại và tối đa, được đồng bộ từ networkplayerdatasync.

**Para 1108**: `[]` | Text: Txtattack, txtmovespeed và txtgold hiển thị các chỉ số chiến đấu và tài nguyên kinh tế chính.

**Para 1109**: `[]` | Text: Equiplistcontainer sinh các dòng equiprowui để liệt kê từng món trang bị đang mặc, cấp nâng cấp hiện tại và nút nâng cấp tương ứng.

**Para 1110**: `[]` | Text: Tab kỹ năng

**Para 1111**: `['image66.png']` | Text: 

**Para 1112**: `[]` | Text: Hình 3.20. Giao diện tab Kỹ năng

**Para 1113**: `[]` | Text: SkillTabUI.cs liệt kê toàn bộ kỹ năng mà nhân vật sở hữu dưới dạng các dòng SkillRowUI. Khi người chơi chọn một kỹ năng, SkillDetailPanelUI hiển thị mô tả, cấp hiện tại, điều kiện nâng cấp và nút nâng cấp. Yêu cầu nâng kỹ năng được gửi đến server thông qua UpgradeSkillServerRpc.

**Para 1114**: `[]` | Text: Tab tiềm năng

**Para 1115**: `['image67.png']` | Text: 

**Para 1116**: `[]` | Text: Hình 3.21. Giao diện tab Tiềm Năng

**Para 1117**: `[]` | Text: PotentialTabUI.cs cho phép người chơi phân bổ điểm tiềm năng vào các chỉ số nhân vật. Giao diện này sử dụng cơ chế thay đổi tạm thời trước khi xác nhận, giúp người chơi kiểm tra lại lựa chọn trước khi gửi lên server.

**Para 1118**: `[]` | Text: Txtpotentialpoints hiển thị số điểm tiềm năng còn dư.

**Para 1119**: `[]` | Text: Statlistcontainer sinh các dòng potentialstatrowui. Mỗi dòng có nút tăng/giảm để điều chỉnh lượng điểm dự kiến cộng vào từng chỉ số.

**Para 1120**: `[]` | Text: Btnhuy hủy toàn bộ thay đổi tạm thời và khôi phục trạng thái ban đầu.

**Para 1121**: `[]` | Text: Btncong xác nhận toàn bộ thay đổi và gửi yêu cầu allocatepotentialstatsserverrpc đến server.

**Para 1122**: `[]` | Text: Giao diện xã hội

**Para 1123**: `[]` | Text: Giao diện trò chuyện đa kênh

**Para 1124**: `['image68.png']` | Text: 

**Para 1125**: `[]` | Text: Hình 3.22. Giao diện chat đa kênh

**Para 1126**: `[]` | Text: ChatPanelUI.cs triển khai hệ thống trò chuyện nhiều kênh thông qua SignalR. Giao diện này giúp người chơi giao tiếp trong thế giới game, trong nhóm, với bạn bè hoặc theo phạm vi lân cận.

**Para 1127**: `[]` | Text: Messagescrollrect và messagecontent tạo vùng scrollview hiển thị danh sách tin nhắn. Hệ thống giới hạn số lượng tin nhắn hiển thị đồng thời để tránh quá tải UI.

**Para 1128**: `[]` | Text: Chattabui tổ chức các tab như Chung, Riêng, Gia tộc, Nhóm và Lớp.

**Para 1129**: `[]` | Text: Chatinputfield và sendbutton dùng để nhập và gửi tin nhắn.

**Para 1130**: `[]` | Text: Chatchanneldropdownui cho phép chuyển nhanh kênh chat ngay trên thanh nhập liệu, đồng thời hiển thị channeliconlabel và channelnamelabel.

**Para 1131**: `[]` | Text: Proximitychatbubble hiển thị bong bóng thoại trên đầu nhân vật trong thế giới khi có tin nhắn lân cận.

**Para 1132**: `[]` | Text: Giao diện danh sách bạn bè

**Para 1134**: `['image69.png']` | Text: 

**Para 1135**: `[]` | Text: Hình 3.23. Giao diện danh sách bạn bè

**Para 1136**: `[]` | Text: FriendListUI.cs được nhúng trong friendListPanel của ChatPanelUI. Người chơi có thể xem danh sách bạn bè online/offline, chọn một người bạn để mở PlayerProfilePanelUI, gửi tin nhắn riêng hoặc mời vào tổ đội mà không cần rời khỏi cửa sổ chat.

**Para 1137**: `[]` | Text: Giao diện tổ đội

**Para 1138**: `['image70.png']` | Text: 

**Para 1139**: `[]` | Text: Hình 3.24. Giao diện tổ đội

**Para 1140**: `[]` | Text: PartyPanelUI.cs quản lý toàn bộ tương tác tổ đội. Giao diện được chia thành ba nhóm tab để người chơi tạo tổ đội, tìm tổ đội hoặc xem người chơi gần mình.

**Para 1141**: `[]` | Text: Tab Tổ đội sử dụng memberListRoot để sinh các PartyMemberEntryUI, đồng thời có lockToggle, autoAcceptToggle, actionButton và chatGroupButton để quản lý trạng thái nhóm.

**Para 1142**: `[]` | Text: Tab Tìm nhóm sử dụng searchListRoot để liệt kê PartySearchEntryUI; refreshSearchButton tải lại danh sách party còn khả dụng.

**Para 1143**: `[]` | Text: Tab Gần đây sử dụng nearbyListRoot để sinh PartyNearbyEntryUI và nearbyPopulationText để hiển thị số người cùng map/zone.

**Para 1144**: `[]` | Text: Các yêu cầu vào nhóm được đẩy vào hàng đợi _pendingJoinRequests và hiển thị tuần tự qua PartyJoinRequestPopupUI để trưởng nhóm duyệt.

**Para 1145**: `[]` | Text: Giao diện bảng xếp hạng

**Para 1146**: `['image71.png']` | Text: 

**Para 1147**: `[]` | Text: Hình 3.25. Giao diện bảng xếp hạng

**Para 1148**: `[]` | Text: LeaderboardPanelUI.cs tổ chức bảng xếp hạng theo hai tầng tab, giúp người chơi theo dõi nhiều loại thành tích khác nhau trong hệ thống.

**Para 1149**: `[]` | Text: Bốn maintabs gồm đua top, sự kiện, tuần & tháng và thưởng.

**Para 1150**: `[]` | Text: Năm subtabs gồm cao thủ, nạp vàng, hoa chi, chuyên cần và phó bản. Tiêu đề cột giá trị trong headercells thay đổi theo sub-tab đang chọn.

**Para 1151**: `[]` | Text: Rowcontent sinh danh sách leaderboardrowentryui thông qua leaderboardservice.

**Para 1152**: `[]` | Text: Emptystategroup và emptystatetext hiển thị khi không có dữ liệu xếp hạng.

**Para 1153**: `[]` | Text: Loadingtext thông báo trạng thái tải dữ liệu từ server.

**Para 1154**: `[]` | Text: Giao diện phó bản

**Para 1155**: `[]` | Text: Giao diện danh sách phó bản

**Para 1159**: `['image72.png']` | Text: 

**Para 1160**: `[]` | Text: Hình 3.26. Giao diện chọn phó bản

**Para 1161**: `[]` | Text: Phó bảnListUI.cs là panel hiển thị danh sách các phó bản mà người chơi có thể tham gia. Giao diện này giúp người chơi xem điều kiện, mô tả và xác nhận trước khi vào phó bản.

**Para 1162**: `[]` | Text: Phó bảnlistcontent là scrollview sinh các phó bảnbuttonitem từ phó bảnitemprefab. Mỗi mục hiển thị tên phó bản, mô tả, cấp độ yêu cầu và trạng thái tham gia.

**Para 1163**: `[]` | Text: Loadingindicator được bật trong thời gian tải danh sách phó bản từ API.

**Para 1164**: `[]` | Text: Confirmdialog hiển thị hộp thoại xác nhận trước khi vào, bao gồm confirmphó bảnname, confirmdesc, confirmyesbtn và confirmnobtn.

**Para 1165**: `[]` | Text: Statustext dùng để hiển thị lỗi hoặc thông báo khi người chơi chưa đủ điều kiện tham gia.

**Para 1166**: `[]` | Text: Giao diện HUD phó bản wave

**Para 1167**: `['image73.png']` | Text: 

**Para 1168**: `[]` | Text: Hình 3.27. Giao diện HUD phó bản wave

**Para 1169**: `[]` | Text: WaveHUD.cs xuất hiện khi người chơi đang ở trong phó bản dạng wave. Giao diện này hiển thị số vòng hiện tại, tổng số vòng và thời gian còn lại của vòng đang diễn ra.

**Para 1170**: `[]` | Text: Roundtext hiển thị số vòng hiện tại và tổng số vòng theo dạng “Vòng 2 / 5”.

**Para 1171**: `[]` | Text: Timertext đếm ngược thời gian còn lại của vòng theo giây.

**Para 1172**: `[]` | Text: Hudroot tự động ẩn khi người chơi không ở trong phó bản và hiện lại khi wavephó bảnruntime được load.

**Para 1173**: `[]` | Text: Script đọc trực tiếp các networkvariable như currentround, remainingseconds và maxrounds từ wavephó bảnruntime, giúp giảm thao tác gán thủ công trong Inspector.

**Para 1174**: `[]` | Text: Giao diện nhiệm vụ và tương tác NPC thế giới

**Para 1175**: `[]` | Text: Widget theo dõi nhiệm vụ

**Para 1176**: `['image74.png']` | Text: 

**Para 1177**: `[]` | Text: Hình 3.28. Giao diện widget nhiệm vụ góc màn hình

**Para 1178**: `[]` | Text: QuestHudWidget.cs là widget cố định ở góc màn hình, dùng để theo dõi nhiệm vụ đang active mà không cần mở bảng nhiệm vụ đầy đủ.

**Para 1179**: `[]` | Text: Questnametext hiển thị tiêu đề nhiệm vụ chính đang theo dõi.

**Para 1180**: `[]` | Text: Queststeptext hiển thị bước hiện tại, số lượng đã hoàn thành so với yêu cầu hoặc chỉ dẫn nộp nhiệm vụ.

**Para 1181**: `[]` | Text: Btnnavigate kích hoạt chức năng tự động di chuyển tới mục tiêu nhiệm vụ. Script tính toán vị trí NPC hoặc map đích và điều khiển nhân vật di chuyển.

**Para 1182**: `[]` | Text: Rootwidget tự động ẩn khi có panel khác đang mở và hiện lại khi không còn cửa sổ giao diện nào che phủ.

**Para 1183**: `[]` | Text: Giao diện NPC nhiệm vụ

**Para 1184**: `[]` | Text: Hình 3.28. Giao diện tương tác NPC nhiệm vụ

**Para 1185**: `[]` | Text: QuestNpcPanel.cs được mở khi người chơi tương tác với NPC trong thế giới. Panel liệt kê các nhiệm vụ mà NPC cung cấp hoặc tiếp nhận, trạng thái nhiệm vụ như chưa nhận, đang thực hiện, hoàn thành và phần thưởng tương ứng. Trạng thái nhiệm vụ được quản lý phía server nhằm bảo đảm tiến trình không bị mất khi người chơi đăng xuất.

**Para 1186**: `[]` | Text: Giao diện menu NPC động và cửa hàng

**Para 1187**: `['image75.png']` | Text: 

**Para 1188**: `[]` | Text: Hình 3.29.Giao diện menu NPC động và cửa hàng

**Para 1189**: `[]` | Text: NpcDynamicMenuUI.cs sinh menu tương tác với NPC theo cấu hình từ backend, hỗ trợ nhiều loại hành động như mở cửa hàng, nhiệm vụ hoặc chức năng đặc biệt. Khi người chơi chọn mua hàng, NpcMenuUI.cs mở danh sách ShopItemRowUI; mỗi dòng hiển thị tên vật phẩm, biểu tượng hệ nguyên tố nếu có, giá vàng, số lượng tồn và nút mua trực tiếp.

**Para 1190**: `[]` | Text: Giao diện hệ thống bản đồ thế giới

**Para 1191**: `['image76.png']` | Text: 

**Para 1192**: `[]` | Text: Hình 3.30. Giao diện chuyển map qua biên

**Para 1193**: `[]` | Text: Thế giới game được tổ chức thành nhiều scene bản đồ, trong đó các map liền kề có thể chuyển qua lại bằng vùng trigger ở rìa bản đồ hoặc nút chuyển map trên HUD. Nhóm chức năng này được hiện thực bởi MapEdgeTrigger, MapTransitionButton và MapManager.

**Para 1194**: `[]` | Text: MapEdgeTrigger là BoxCollider2D đặt tại rìa trái hoặc phải của scene. Khi Player đi vào vùng trigger, script gọi API GET /api/map/edge?mapId=X&direction=right/left để lấy map đích và vị trí xuất hiện.

**Para 1195**: `[]` | Text: MapTransitionButton là nút chuyển map thủ công trên HUD, phù hợp cho điều khiển bằng chuột hoặc thiết bị di động. Khi nhấn, nút gọi cùng API chuyển map và hiển thị loadingPanel trong thời gian chờ.

**Para 1196**: `[]` | Text: MapManager là Singleton dạng DontDestroyOnLoad, tự động gọi GET /api/map/by-scene?scene=... khi scene được load để xác định mapId và mapName hiện tại.

**Para 1197**: `[]` | Text: Sau khi lấy được thông tin map đích, client thực hiện chuyển scene theo transitionDelay và đặt lại vị trí nhân vật theo dữ liệu server trả về.

**Para 1199**: `[]` | Text: Tổng kết chương 3

**Para 1200**: `[]` | Text: Chương 3 đã trình bày quá trình triển khai hệ thống trò chơi Mutants Arena dựa trên các yêu cầu và thiết kế đã xác định ở Chương 2. Về phía client, chương đã mô tả các phân hệ chính như khởi tạo phiên chơi, kết nối multiplayer, điều khiển và đồng bộ nhân vật, trạng thái chiến đấu, tương tác giữa người chơi, hệ thống Gene chính, Gene phụ, Hybrid Fusion, kỹ năng, phó bản, Zone runtime, chat, bạn bè và tổ đội. Các nội dung này cho thấy Unity Client không chỉ đóng vai trò hiển thị giao diện mà còn tham gia tổ chức trải nghiệm gameplay theo thời gian thực.

