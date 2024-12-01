using ConsoleApp5.DAL.Models;

namespace ConsoleApp5.DAL
{
    public interface IDataAccessLayer
    {
        Task AddShopAsync(Shop shop);
        Task AddProductAsync(Product product);
        Task AddStockAsync(StockItem stockItem);
        Task<Shop?> GetCheapestShopForProductAsync(string productName);
        Task<List<StockItem>> GetAvailableProductsForBudgetAsync(string shopCode, decimal budget);
        Task<(decimal?, string?)> PurchaseStockAsync(string shopCode, List<(string productName, int quantity)> purchaseList);
        Task<Shop?> GetCheapestShopForBatchAsync(List<(string productName, int quantity)> batch);
        Task<Shop?> GetShopByCodeAsync(string shopCode);
        Task<Product?> GetProductByNameAsync(string productName);
    }
}
