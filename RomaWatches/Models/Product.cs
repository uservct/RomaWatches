using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RomaWatches.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        //chất liệu vỏ
        [StringLength(100)]
        public string? CaseMaterial { get; set; }


        // Đường kính vỏ
        [StringLength(50)]
        public string? CaseDiameter { get; set; }

        //mặt số
        [StringLength(100)]
        public string? Dial { get; set; }

        // Loại máy: cơ, điện tử
        [StringLength(100)]
        public string? Movement { get; set; }
        // Dự trữ năng lượng
        [StringLength(50)]
        public string? PowerReserve { get; set; }
        
        // Chống nước
        [StringLength(50)]
        public string? WaterResistance { get; set; }
        
        //mặt kính
        [StringLength(100)]
        public string? Crystal { get; set; }

        // Giới tính: nam, nữ, đôi
        [StringLength(50)]
        public string? Gender { get; set; }

        // Loại dây: dây da, dây dù, dây silicone, thép không rỉ
        [StringLength(100)]
        public string? StrapType { get; set; }

        // Kháng nước (ATM): 3, 5, 10, 20
        public int? WaterResistanceAtm { get; set; }

        [Column(TypeName = "nvarchar(MAX)")]
        public string? Description { get; set; }

        [Required]
        [StringLength(100)]
        public string Brand { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public decimal Price { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}

