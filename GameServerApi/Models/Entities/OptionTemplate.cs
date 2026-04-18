namespace GameServerApi.Models.Entities
{
    /// <summary>
    /// Bảng option_template — Định nghĩa các chỉ số tùy chọn trên trang bị.
    ///
    /// Mỗi option có 20 giá trị (cách nhau bằng ;) tương ứng upgrade level.
    /// type: 0=tấn công, 2=phòng thủ/phụ, 3=unlock ở +4, 4=unlock ở +8, 5=unlock ở +12, 6=unlock ở +16
    /// name: Tên hiển thị với # là placeholder cho giá trị. Ví dụ: "Tấn công: +#"
    /// </summary>
    public class OptionTemplate
    {
        public int Id { get; set; }

        /// <summary>Tên hiển thị. # = placeholder giá trị. VD: "Tấn công: +#"</summary>
        public string Name { get; set; } = "";

        /// <summary>Loại option: 0=tấn công, 2=phòng thủ, 3=+4, 4=+8, 5=+12, 6=+16</summary>
        public int Type { get; set; } = 0;

        /// <summary>Min upgradeLevel để kích hoạt option này</summary>
        public int Level { get; set; } = 0;

        /// <summary>20 giá trị cách nhau bằng ; — giá trị tại mỗi upgrade level</summary>
        public string StrOption { get; set; } = "";
    }
}
