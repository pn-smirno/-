using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using WarehouseApp.Commands;
using WarehouseApp.Models;
using WarehouseApp.Repositories;

namespace WarehouseApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // Репозитории
        private readonly Repository<Product> _productRepo;
        private readonly Repository<Category> _categoryRepo;
        private readonly Repository<StockMovement> _movementRepo;

        // Коллекции для UI (обязательно ObservableCollection)
        private ObservableCollection<Category> _categories = new();
        private ObservableCollection<Product> _products = new();
        private ObservableCollection<StockMovement> _movements = new();

        // Выбранные элементы
        private Category? _selectedCategory;
        private Product? _selectedProduct;

        // Поля для операций прихода/расхода
        private int _movementQuantity;
        private string _movementComment = string.Empty;
        private string _errorMessage = string.Empty;  // для валидации

        public MainViewModel()
        {
            // Инициализация репозиториев
            var dbContext = new Data.AppDbContext();
            _productRepo = new Repository<Product>(dbContext);
            _categoryRepo = new Repository<Category>(dbContext);
            _movementRepo = new Repository<StockMovement>(dbContext);

            // Команды
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            AddProductCommand = new RelayCommand(_ => OpenAddProductWindow(), _ => SelectedCategory != null);
            AddMovementCommand = new RelayCommand(async _ => await AddMovementAsync(), _ => SelectedProduct != null && MovementQuantity != 0);
            DeleteProductCommand = new RelayCommand(async _ => await DeleteProductAsync(), _ => SelectedProduct != null);
            ShowHistoryCommand = new RelayCommand(_ => LoadMovementsForProduct(), _ => SelectedProduct != null);
            AddCategoryCommand = new RelayCommand(_ => OpenAddCategoryWindow());

            // Загрузка данных при старте
            Task.Run(async () => await LoadDataAsync());
        }

        // Свойства для биндинга (полные свойства с уведомлениями)
        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set
            {
                _categories = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Product> Products
        {
            get => _products;
            set
            {
                _products = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<StockMovement> Movements
        {
            get => _movements;
            set
            {
                _movements = value;
                OnPropertyChanged();
            }
        }

        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                // При смене категории загружаем товары этой категории
                Task.Run(async () => await LoadProductsByCategoryAsync());
            }
        }

        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged();
                if (value != null)
                    LoadMovementsForProduct();
            }
        }

        public int MovementQuantity
        {
            get => _movementQuantity;
            set
            {
                _movementQuantity = value;
                OnPropertyChanged();
                ((RelayCommand)AddMovementCommand).CanExecute(null); // Обновляем доступность команды
            }
        }

        public string MovementComment
        {
            get => _movementComment;
            set
            {
                _movementComment = value;
                OnPropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        // Команды
        public ICommand LoadDataCommand { get; }
        public ICommand AddProductCommand { get; }
        public ICommand AddMovementCommand { get; }
        public ICommand DeleteProductCommand { get; }
        public ICommand ShowHistoryCommand { get; }
        public ICommand AddCategoryCommand { get; }

        // Методы бизнес-логики
        private async Task LoadDataAsync()
        {
            try
            {
                // Загружаем категории с товарами (включая связанные данные)
                var categoriesFromDb = await _categoryRepo.GetAllAsync();

                // Обновляем коллекцию в UI-потоке
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Categories.Clear();
                    foreach (var cat in categoriesFromDb)
                        Categories.Add(cat);
                });
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private async Task LoadProductsByCategoryAsync()
        {
            if (SelectedCategory == null) return;

            try
            {
                // Используем FindAsync с фильтром по категории
                var productsFromDb = await _productRepo.FindAsync(p => p.CategoryId == SelectedCategory.Id);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Products.Clear();
                    foreach (var prod in productsFromDb)
                        Products.Add(prod);
                });
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки товаров: {ex.Message}");
            }
        }

        private void LoadMovementsForProduct()
        {
            if (SelectedProduct == null) return;

            Task.Run(async () =>
            {
                try
                {
                    var movementsFromDb = await _movementRepo.FindAsync(m => m.ProductId == SelectedProduct.Id);
                    // Сортируем по дате (новые сверху)
                    var sorted = movementsFromDb.OrderByDescending(m => m.Date).ToList();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Movements.Clear();
                        foreach (var mov in sorted)
                            Movements.Add(mov);
                    });
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка загрузки истории: {ex.Message}");
                }
            });
        }

        private void OpenAddProductWindow()
        {
            if (SelectedCategory == null)
            {
                ShowError("Выберите категорию для добавления товара");
                return;
            }

            // Простое окно ввода через диалог
            var name = Microsoft.VisualBasic.Interaction.InputBox("Введите название товара:", "Новый товар");
            if (string.IsNullOrWhiteSpace(name)) return;

            var sku = Microsoft.VisualBasic.Interaction.InputBox("Введите SKU (уникальный артикул):", "SKU");
            if (string.IsNullOrWhiteSpace(sku)) return;

            Task.Run(async () =>
            {
                try
                {
                    // Проверка уникальности SKU
                    var existing = await _productRepo.FindAsync(p => p.SKU == sku);
                    if (existing.Any())
                    {
                        ShowError("Товар с таким SKU уже существует!");
                        return;
                    }

                    var newProduct = new Product
                    {
                        Name = name,
                        SKU = sku,
                        Quantity = 0,
                        CategoryId = SelectedCategory.Id
                    };

                    await _productRepo.AddAsync(newProduct);
                    await _productRepo.SaveChangesAsync();

                    // Обновляем список товаров
                    await LoadProductsByCategoryAsync();
                    ShowError("Товар успешно добавлен", isError: false);
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка добавления: {ex.Message}");
                }
            });
        }
        private void OpenAddCategoryWindow()
        {
            var name = Microsoft.VisualBasic.Interaction.InputBox("Введите название категории:", "Новая категория");
            if (string.IsNullOrWhiteSpace(name)) return;

            Task.Run(async () =>
            {
                try
                {
                    var existing = await _categoryRepo.FindAsync(c => c.Name == name);
                    if (existing.Any())
                    {
                        ShowError("Категория с таким названием уже существует!");
                        return;
                    }

                    var newCategory = new Category { Name = name };
                    await _categoryRepo.AddAsync(newCategory);
                    await _categoryRepo.SaveChangesAsync();
                    await LoadDataAsync();
                    ShowError($"Категория '{name}' добавлена", false);
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка добавления категории: {ex.Message}");
                }
            });
        }
        private async Task AddMovementAsync()
        {
            if (SelectedProduct == null) return;

            // ВАЛИДАЦИЯ: остаток не может быть отрицательным
            if (MovementQuantity == 0)
            {
                ShowError("Количество должно быть положительным");
                return;
            }

            // Временно определяем тип движения по контексту (спросим у пользователя)
            // Определяем тип по знаку числа: положительное = приход, отрицательное = расход
            MovementType type;
            int actualQuantity;

            if (MovementQuantity > 0)
            {
                type = MovementType.In;  // Приход
                actualQuantity = MovementQuantity;
            }
            else
            {
                type = MovementType.Out;  // Расход
                actualQuantity = -MovementQuantity;  // Превращаем -5 в 5
            }

            // Проверка на отрицательный остаток при расходе
            if (type == MovementType.Out && SelectedProduct.Quantity - actualQuantity < 0)
            {
                ShowError($"Остаток не может стать отрицательным! Доступно: {SelectedProduct.Quantity}");
                return;
            }

            try
            {
                // Обновляем остаток
                if (type == MovementType.In)
                    SelectedProduct.Quantity += actualQuantity;
                else
                    SelectedProduct.Quantity -= actualQuantity;

                await _productRepo.UpdateAsync(SelectedProduct);

                // Добавляем запись движения
                var movement = new StockMovement
                {
                    ProductId = SelectedProduct.Id,
                    Type = type,
                    Quantity = actualQuantity,
                    Comment = MovementComment,
                    Date = DateTime.Now
                };

                await _movementRepo.AddAsync(movement);
                await _movementRepo.SaveChangesAsync();

                // Обновляем UI
                await LoadProductsByCategoryAsync();
                LoadMovementsForProduct();

                // Очищаем поля
                MovementQuantity = 0;
                MovementComment = string.Empty;

                ShowError("Операция выполнена успешно", isError: false);
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка операции: {ex.Message}");
            }
        }

        private async Task DeleteProductAsync()
        {
            if (SelectedProduct == null) return;

            var confirm = MessageBox.Show($"Удалить товар '{SelectedProduct.Name}'?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await _productRepo.DeleteAsync(SelectedProduct.Id);
                await _productRepo.SaveChangesAsync();

                await LoadProductsByCategoryAsync();
                Movements.Clear();
                ShowError("Товар удалён", isError: false);
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка удаления: {ex.Message}");
            }
        }

        private void ShowError(string message, bool isError = true)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ErrorMessage = message;
                MessageBox.Show(message, isError ? "Ошибка" : "Информация",
                    MessageBoxButton.OK, isError ? MessageBoxImage.Error : MessageBoxImage.Information);
                ErrorMessage = string.Empty;  
            });
        }

        // Реализация INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}