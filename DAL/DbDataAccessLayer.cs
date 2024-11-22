using ConsoleApp5.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsoleApp5.DAL
{
    public class DbDataAccessLayer : IDataAccessLayer
    {
        private readonly AppDbContext _context;

        public DbDataAccessLayer(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddShopAsync(Shop shop)
        {
            _context.Shop.Add(shop);
            await _context.SaveChangesAsync();
        }

        public async Task AddProductAsync(Product product)
        {
            _context.Product.Add(product);
            await _context.SaveChangesAsync();
        }

        public async Task AddStockAsync(StockItem stockItem)
        {
            _context.StockItem.Add(stockItem);
            await _context.SaveChangesAsync();
        }
        public async Task<Shop?> GetCheapestShopForProductAsync(string productName)
        {
            var cheapestStock = await _context.StockItem
                .Include(s => s.Product)
                .Where(s => s.Product.Name == productName && s.Quantity > 0)
                .OrderBy(s => s.Price)
                .FirstOrDefaultAsync();

            if (cheapestStock == null)
                return null;

            return await _context.Shop.FirstOrDefaultAsync(s => s.Id == cheapestStock.ShopId);
        }

        public async Task<List<StockItem>> GetAvailableProductsForBudgetAsync(string shopCode, decimal budget)
        {
            return await _context.StockItem
                .Include(s => s.Shop)
                .Where(s => s.Shop.Code == shopCode && s.Quantity > 0)
                .Where(s => s.Price <= budget)
                .ToListAsync();
        }

        public async Task<(decimal?, string?)> PurchaseStockAsync(string shopCode, List<(string productName, int quantity)> purchaseList)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            decimal totalCost = 0;

            foreach (var (productName, quantity) in purchaseList)
            {
                var stock = await _context.StockItem
                    .Include(s => s.Product)
                    .Include(s => s.Shop)
                    .FirstOrDefaultAsync(s => s.Shop.Code == shopCode && s.Product.Name == productName);

                if (stock == null || stock.Quantity < quantity)
                {
                    await transaction.RollbackAsync();
                    return (null, $"Insufficient stock for {productName}");
                }
                totalCost += stock.Price * quantity;
                stock.Quantity -= quantity;
            }
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (totalCost, null);
        }

        public async Task<Shop?> GetCheapestShopForBatchAsync(List<(string productName, int quantity)> batch)
        {
            var shops = await _context.Shop.ToListAsync();
            Shop? cheapestShop = null;
            decimal minCost = decimal.MaxValue;

            foreach (var shop in shops)
            {
                decimal totalCost = 0;
                bool canPurchase = true;

                foreach (var (productName, quantity) in batch)
                {
                    var stock = await _context.StockItem
                        .Include(s => s.Product)
                        .Include(s => s.Shop)
                        .FirstOrDefaultAsync(s => s.Shop.Code == shop.Code && s.Product.Name == productName);
                    if (stock == null || stock.Quantity < quantity)
                    {
                        canPurchase = false;
                        break;
                    }
                    totalCost += stock.Price * quantity;
                }

                if (canPurchase && totalCost < minCost)
                {
                    minCost = totalCost;
                    cheapestShop = shop;
                }
            }

            return cheapestShop;
        }
        public async Task<Shop?> GetShopByCodeAsync(string shopCode)
        {
            return await _context.Shop.FirstOrDefaultAsync(s => s.Code == shopCode);
        }
        public async Task<Product?> GetProductByNameAsync(string productName)
        {
            return await _context.Product.FirstOrDefaultAsync(p => p.Name == productName);
        }

    }
}
