using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RomaWatches.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CartId { get; set; }

        [ForeignKey("CartId")]
        public virtual Cart Cart { get; set; } = null!;

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}

