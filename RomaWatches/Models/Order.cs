using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace RomaWatches.Models
{
    public enum OrderStatus
    {
        Unconfirmed, // Chưa xác nhận thanh toán (BankTransfer chưa ấn "Đã chuyển khoản")
        Pending,     // Chờ duyệt (cho BankTransfer đã xác nhận)
        Approved,    // Đã duyệt (COD, InStore hoặc BankTransfer đã duyệt)
        Completed,   // Hoàn thành
        Cancelled    // Đã hủy
    }

    public enum PaymentMethod
    {
        COD,            // Thanh toán khi nhận hàng
        InStore,        // Thanh toán trực tiếp tại cửa hàng
        BankTransfer    // Chuyển khoản ngân hàng
    }

    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Province { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Ward { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; } = 0;

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}

