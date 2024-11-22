using ConsoleApp5.DAL;
using ConsoleApp5.Models;
using System.Globalization;

public class FileDataAccessLayer : IDataAccessLayer
{
    private readonly string _shopsFilePath;
    private readonly string _productsFilePath;

    public FileDataAccessLayer(string shopsFilePath, string productsFilePath)
    {
        _shopsFilePath = shopsFilePath;
        _productsFilePath = productsFilePath;
    }

    public async Task AddShopAsync(Shop shop)
    {
        var lines = new List<string>();

        if (File.Exists(_shopsFilePath))
        {
            var fileLines = await File.ReadAllLinesAsync(_shopsFilePath);
            lines = fileLines.ToList();
        }
        int maxId = 0;
        if (lines.Any(line =>
        {
            var parts = line.Split(',');
            if (int.TryParse(parts[0], out int id) & id > maxId)
                maxId = id;
            return parts.Length > 1 && parts[1] == shop.Code;
        }))
        {
            throw new Exception($"Shop with code {shop.Code} already exists.");
        }

        lines.Add($"{maxId + 1},{shop.Code},{shop.Name},{shop.Address}");
        await File.WriteAllLinesAsync(_shopsFilePath, lines);
    }
    public async Task AddProductAsync(Product product)
    {
        var lines = new List<string>();

        if (File.Exists(_productsFilePath))
        {
            var fileLines = await File.ReadAllLinesAsync(_productsFilePath);
            lines = fileLines.ToList();
        }

        if (lines.Any(line =>
        {
            var parts = line.Split(',');
            return parts.Length > 1 && parts[1] == product.Name;
        }))
        {
            throw new Exception($"Product with name {product.Name} already exists.");
        }

        lines.Add($"{0},{product.Name},{0},{0}");
        await File.WriteAllLinesAsync(_productsFilePath, lines);
    }

    public async Task AddStockAsync(StockItem stockItem)
    {
        var lines = new List<string>();

        if (File.Exists(_productsFilePath))
        {
            var fileLines = await File.ReadAllLinesAsync(_productsFilePath);
            lines = fileLines.ToList();
        }

        var updated = false;
        for (var i = 0; i < lines.Count; i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length > 1 && int.Parse(parts[0]) == stockItem.ShopId && parts[1] == stockItem.Product.Name)
            {
                lines[i] = $"{stockItem.ShopId},{stockItem.Product.Name},{stockItem.Price.ToString(CultureInfo.InvariantCulture)},{stockItem.Quantity + int.Parse(parts[3])}";
                updated = true;
                break;
            }
        }
        if (!updated)
        {
            lines.Add($"{stockItem.ShopId},{stockItem.Product.Name},{stockItem.Price.ToString(CultureInfo.InvariantCulture)},{stockItem.Quantity}");
        }

        await File.WriteAllLinesAsync(_productsFilePath, lines);
    }
    public async Task<List<StockItem>> GetAvailableProductsForBudgetAsync(string shopCode, decimal budget)
    {
        var shopId = await GetShopIdByCodeAsync(shopCode);
        if (shopId == null)
            throw new Exception($"Shop with code {shopCode} not found.");

        if (!File.Exists(_productsFilePath))
            return new List<StockItem>();

        var lines = await File.ReadAllLinesAsync(_productsFilePath);

        var result = new List<StockItem>();

        foreach (var line in lines)
        {
            var parts = line.Split(',');

            var stockItem = new StockItem
            {
                ShopId = int.Parse(parts[0]),
                Product = new Product { Name = parts[1] },
                Price = decimal.Parse(parts[2], CultureInfo.InvariantCulture),
                Quantity = int.Parse(parts[3])
            };

            if (stockItem.ShopId == shopId && stockItem.Price <= budget && stockItem.Quantity > 0)
            {
                var maxQuantity = Math.Min(stockItem.Quantity, (int)(budget / stockItem.Price));
                if (maxQuantity > 0)
                {
                    stockItem.Quantity = maxQuantity;
                    result.Add(stockItem);
                    budget -= maxQuantity * stockItem.Price;

                    if (budget <= 0)
                        break;
                }
            }
        }

        return result;
    }

    public async Task<(decimal?, string?)> PurchaseStockAsync(string shopCode, List<(string productName, int quantity)> purchaseList)
    {
        var shopId = await GetShopIdByCodeAsync(shopCode);
        if (shopId == null)
            return (null, $"Shop with code {shopCode} not found.");

        if (!File.Exists(_productsFilePath))
            return (null, "Product data file not found.");

        var lines = (await File.ReadAllLinesAsync(_productsFilePath)).ToList();
        decimal totalCost = 0;

        foreach (var (productName, quantity) in purchaseList)
        {

            var stockIndex = lines.FindIndex(line =>
            {
                var parts = line.Split(',');
                return parts[0] == shopId.ToString() && parts[1] == productName;
            });

            if (stockIndex == -1)
                return (null, $"Product {productName} is not available in shop {shopCode}.");

            var parts = lines[stockIndex].Split(',');
            var availableQuantity = int.Parse(parts[3]);
            var price = decimal.Parse(parts[2], CultureInfo.InvariantCulture);

            if (availableQuantity < quantity)
                return (null, $"Not enough stock for product {productName} in shop {shopCode}.");

            parts[3] = (availableQuantity - quantity).ToString();
            lines[stockIndex] = string.Join(",", parts);

            totalCost += price * quantity;
        }

        await File.WriteAllLinesAsync(_productsFilePath, lines);
        return (totalCost, null);
    }

    public async Task<Shop?> GetCheapestShopForProductAsync(string productName)
    {
        if (!File.Exists(_productsFilePath) || !File.Exists(_shopsFilePath))
            return null;

        var productLines = await File.ReadAllLinesAsync(_productsFilePath);
        var shopLines = await File.ReadAllLinesAsync(_shopsFilePath);
        var stockItems = await Task.WhenAll(productLines.Select(async line =>
        {
            var parts = line.Split(',');
            return new StockItem
            {
                ShopId = int.Parse(parts[0]),
                Product = new Product { Name = parts[1] },
                Price = decimal.Parse(parts[2], CultureInfo.InvariantCulture),
                Quantity = int.Parse(parts[3])
            };
        }));

        var cheapestStock = stockItems
            .Where(s => s.Product.Name == productName && s.Quantity > 0)
            .OrderBy(s => s.Price)
            .FirstOrDefault();

        if (cheapestStock == null)
            return null;

        var shop = shopLines.Select(line =>
        {
            var parts = line.Split(',');
            return new Shop
            {
                Id = int.Parse(parts[0]),
                Code = parts[1],
                Name = parts[2],
                Address = parts[3]
            };
        }).FirstOrDefault(s => s.Id == cheapestStock.ShopId);

        return shop;
    }

    private async Task<int?> GetShopIdByCodeAsync(string shopCode)
    {
        if (!File.Exists(_shopsFilePath))
            return null;

        var lines = await File.ReadAllLinesAsync(_shopsFilePath);
        foreach (var line in lines)
        {
            var parts = line.Split(',');
            if (parts.Length > 1 && parts[1] == shopCode)
                return int.Parse(parts[0]);
        }

        return null;
    }

    private async Task<int?> GetProductIdByNameAsync(string productName)
    {
        if (!File.Exists(_productsFilePath))
            return null;

        var lines = await File.ReadAllLinesAsync(_productsFilePath);
        foreach (var line in lines)
        {
            var parts = line.Split(',');
            if (parts.Length > 1 && parts[1] == productName)
                return int.Parse(parts[1]);
        }

        return null;
    }
    public async Task<Shop?> GetCheapestShopForBatchAsync(List<(string productName, int quantity)> batch)
    {
        if (!File.Exists(_productsFilePath) || !File.Exists(_shopsFilePath))
            return null;

        var productLines = await File.ReadAllLinesAsync(_productsFilePath);
        var shopLines = await File.ReadAllLinesAsync(_shopsFilePath);

        var shops = shopLines.Select(line =>
        {
            var parts = line.Split(',');
            return new Shop
            {
                Id = int.Parse(parts[0]),
                Code = parts[1],
                Name = parts[2],
                Address = parts[3]
            };
        }).ToList();

        Shop? cheapestShop = null;
        decimal minTotalCost = decimal.MaxValue;

        foreach (var shop in shops)
        {
            decimal totalCost = 0;
            var canPurchase = true;

            foreach (var (productName, quantity) in batch)
            {
                var stock = productLines.Select(line =>
                {
                    var parts = line.Split(',');
                    return new StockItem
                    {
                        ShopId = int.Parse(parts[0]),
                        Product = new Product { Name = parts[1] },
                        Price = decimal.Parse(parts[2], CultureInfo.InvariantCulture),
                        Quantity = int.Parse(parts[3])
                    };
                }).FirstOrDefault(s => s.ShopId == shop.Id && s.Product.Name == productName);

                if (stock == null || stock.Quantity < quantity)
                {
                    canPurchase = false;
                    break;
                }

                totalCost += stock.Price * quantity;
            }

            if (canPurchase && totalCost < minTotalCost)
            {
                minTotalCost = totalCost;
                cheapestShop = shop;
            }
        }

        return cheapestShop;
    }
    public async Task<Product?> GetProductByNameAsync(string productName)
    {
        if (!File.Exists(_productsFilePath))
            return null;

        var lines = await File.ReadAllLinesAsync(_productsFilePath);

        var productLine = lines.FirstOrDefault(line =>
        {
            var parts = line.Split(',');
            return parts.Length > 1 && parts[1] == productName;
        });

        if (productLine == null)
            return null;

        var productParts = productLine.Split(',');
        return new Product
        {
            Name = productParts[1]
        };
    }
    public async Task<Shop?> GetShopByCodeAsync(string shopCode)
    {
        if (!File.Exists(_shopsFilePath))
            return null;

        var lines = await File.ReadAllLinesAsync(_shopsFilePath);

        var shopLine = lines.FirstOrDefault(line =>
        {
            var parts = line.Split(',');
            return parts.Length > 1 && parts[1] == shopCode;
        });

        if (shopLine == null)
            return null;

        var shopParts = shopLine.Split(',');
        return new Shop
        {
            Id = int.Parse(shopParts[0]),
            Code = shopParts[1],
            Name = shopParts[2],
            Address = shopParts[3]
        };
    }

}
