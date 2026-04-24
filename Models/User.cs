using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WarehouseApp.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    }
}