using ConsoleApp5.BLL;
using ConsoleApp5.Configuration;
using ConsoleApp5.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp5
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var config = AppConfiguration.LoadConfiguration();

            var services = new ServiceCollection();

            if (config.DALImplementation == "Database")
            {
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(config.DefaultConnection));
                services.AddTransient<IDataAccessLayer, DbDataAccessLayer>();
            }
            else if (config.DALImplementation == "File")
            {
                services.AddTransient<IDataAccessLayer>(_ =>
                    new FileDataAccessLayer(config.ShopsFile, config.ProductsFile));
            }

            services.AddTransient<ShopService>();
            var serviceProvider = services.BuildServiceProvider();

            // Пример использования:
            var shopService = serviceProvider.GetRequiredService<ShopService>();

            /*1:*/
            await shopService.CreateShopAsync("SHOP003", "Tech Store 3", "123 Main St");

            /*2:*/
            await shopService.CreateProductAsync("PlayStation 1");

            /*3:*/
            await shopService.StockItemsAsync("SHOP001", "PlayStation 1", 10, 99.99m);

            /*4:*/
            var cheapestShop = await shopService.FindCheapestShopForProductAsync("PlayStation 1");
            Console.WriteLine($"Cheapest shop for Laptop: {cheapestShop?.Name}");

            /*5:*/
            var availableProducts = await shopService.GetProductsForBudgetAsync("SHOP001", 1000);
            Console.WriteLine($"Products available for $1000: {availableProducts[0].Quantity}");

            /*6:*/
            var purchaseResult = await shopService.PurchaseProductsAsync("SHOP001", new List<(string, int)>
            {
                ("Laptop", 2)
            });
            if (purchaseResult.Item1.HasValue)
                Console.WriteLine($"Purchase successful. Total cost: {purchaseResult.Item1}");
            else
                Console.WriteLine($"Purchase failed: {purchaseResult.Item2}");

            /*7:*/
            var batch = new List<(string productName, int quantity)>
                {
                    ("Nail", 10),
                    ("Screw", 20)
                };
            var cheapestShop1 = await shopService.FindCheapestShopForBatchAsync(batch);
            if (cheapestShop1 != null)
                Console.WriteLine($"Cheapest shop: {cheapestShop1.Name}, Address: {cheapestShop1.Address}");
        }
    }
}
