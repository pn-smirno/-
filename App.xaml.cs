using System.Windows;
using WarehouseApp.Data;

namespace WarehouseApp
{
    public partial class App : Application  // ← ДОЛЖНО БЫТЬ "partial"
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            using (var db = new AppDbContext())
            {
                db.Database.EnsureCreated();
            }
        }
    }
}
