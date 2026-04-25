using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApp.Models
{
    public enum MovementType
    {
        In,   // Приход
        Out   // Расход
    }

    public class StockMovement
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        public MovementType Type { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть положительным")]
        public int Quantity { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        [MaxLength(500)]
        public string Comment { get; set; } = string.Empty;

        // Опционально: кто сделал операцию
        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
