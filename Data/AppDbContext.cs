using System;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Models;

namespace WarehouseApp.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Путь к файлу БД в папке приложения
            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "warehouse.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Уникальность SKU
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.SKU)
                .IsUnique();

            // При создании базы добавим начальные категории
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Электроника" },
                new Category { Id = 2, Name = "Мебель" },
                new Category { Id = 3, Name = "Канцелярия" }
            );
        }
    }
}