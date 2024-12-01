using ConsoleApp5.DAL;
using ConsoleApp5.DAL.Models;

namespace ConsoleApp5.BLL
{
    public class ShopService
    {
        private readonly IDataAccessLayer _dataAccessLayer;

        public ShopService(IDataAccessLayer dataAccessLayer)
        {
            _dataAccessLayer = dataAccessLayer;
        }

        public async Task CreateShopAsync(string code, string name, string address)
        {
            await _dataAccessLayer.AddShopAsync(new Shop { Code = code, Name = name, Address = address });
        }

        public async Task CreateProductAsync(string name)
        {
            await _dataAccessLayer.AddProductAsync(new Product { Name = name });
        }
        public async Task StockItemsAsync(string shopCode, string productName, int quantity, decimal price)
        {
            var shop = await _dataAccessLayer.GetShopByCodeAsync(shopCode);
            if (shop == null)
                throw new Exception($"Shop with code {shopCode} not found.");

            var product = await _dataAccessLayer.GetProductByNameAsync(productName);
            if (product == null)
                throw new Exception($"Product with name {productName} not found.");

            await _dataAccessLayer.AddStockAsync(new StockItem
            {
                ShopId = shop.Id,
                ProductId = product.Id,
                Quantity = quantity,
                Price = price,
                Product = new Product { Name = productName }
            });
        }

        public async Task<Shop?> FindCheapestShopForProductAsync(string productName)
        {
            return await _dataAccessLayer.GetCheapestShopForProductAsync(productName);
        }

        public async Task<List<StockItem>> GetProductsForBudgetAsync(string shopCode, decimal budget)
        {
            return await _dataAccessLayer.GetAvailableProductsForBudgetAsync(shopCode, budget);
        }

        public async Task<(decimal?, string?)> PurchaseProductsAsync(string shopCode, List<(string productName, int quantity)> purchaseList)
        {
            return await _dataAccessLayer.PurchaseStockAsync(shopCode, purchaseList);
        }

        public async Task<Shop?> FindCheapestShopForBatchAsync(List<(string productName, int quantity)> batch)
        {
            return await _dataAccessLayer.GetCheapestShopForBatchAsync(batch);
        }
    }
}
