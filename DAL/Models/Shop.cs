namespace ConsoleApp5.DAL.Models
{
    public class Shop
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
    }
}
