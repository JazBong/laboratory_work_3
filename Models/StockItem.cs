namespace ConsoleApp5.Models
{
    public class StockItem
    {
        public int Id { get; set; }
        public int ShopId { get; set; }
        public int ProductId { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public Shop Shop { get; set; }
        public Product Product { get; set; }
    }
}
