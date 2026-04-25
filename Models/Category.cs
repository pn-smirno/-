using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WarehouseApp.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // Связь один-ко-многим с продуктами
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
