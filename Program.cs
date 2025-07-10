using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ERP_Fix
{
    class Code
    {
        static void Main(string[] args)
        {
            Shell shell = new Shell();
            shell.Main();
        }
    }

    public class Program
    {
        // Warehousing
        private List<Article> articles = new List<Article>();
        private List<ArticleType> articleTypes = new List<ArticleType>();
        private List<StorageSlot> storageSlots = new List<StorageSlot>();

        private int lastStockId = -1;
        private int lastSlotId = -1;
        private int lastArticleTypeId = -1;

        // Orders
        private List<Order> orders = new List<Order>();
        private int lastOrderId = -1;

        // Prices
        private List<Prices> prices = new List<Prices>();
        private int lastPricesId = -1;

        // Bills
        private List<Bill> bills = new List<Bill>();
        private int lastBillId = -1;

        public void Main()
        {
            // Tests
            ArticleType boot = NewArticleType("Boot");
            ArticleType hat = NewArticleType("Hat");

            Article boots = NewArticle(0, 100);
            Article hats = NewArticle(1, 40);

            Order order = NewOrder(new List<Article>()
            {
                boots,
                hats
            });
            ListOrders();

            Prices prices = NewPrices(new Dictionary<ArticleType, double>()
            {
                { boot, 49.99 },
                { hat, 19.99 }
            });
            ListPrices();

            Bill bill = NewBill(order, prices);
            ListBills();
        }

        // Warehousing
        private Article? FindArticle(int id)
        {
            return articles.FirstOrDefault(a => a.Id == id);
        }

        private ArticleType? FindArticleType(int id)
        {
            return articleTypes.FirstOrDefault(t => t.Id == id);
        }

        private StorageSlot? FindStorageSlot(Article article)
        {
            return storageSlots.FirstOrDefault(slot => slot.Fill.Contains(article));
        }

        private StorageSlot? FindStorageSlotById(int id)
        {
            return storageSlots.FirstOrDefault(t => t.Id == id);
        }

        public ArticleType NewArticleType(string name)
        {
            ArticleType generated = new ArticleType(lastArticleTypeId + 1, name);

            articleTypes.Add(generated);
            lastArticleTypeId += 1;

            return generated;
        }

        public StorageSlot NewStorageSlot()
        {
            StorageSlot generated = new StorageSlot(lastSlotId + 1, new List<Article>());

            storageSlots.Add(generated);
            lastSlotId += 1;

            return generated;
        }

        public Article NewArticle(int typeId, int stock, bool toList = true)
        {
            ArticleType articleType = FindArticleType(typeId);
            if (articleType == null) // Ensure proper null checks
            {
                throw new ArgumentException($"Article type with ID {typeId} does not exist.");
            }

            Article generated = new Article(lastStockId + 1, articleType, stock);
            if (toList)
            {
                articles.Add(generated);
            }
            lastStockId += 1;

            return generated;
        }

        public void RestockArticle(int id, int amount)
        {
            Article? article = FindArticle(id);
            if (article != null)
            {
                article.Stock += amount;
            }
            else
            {
                Console.WriteLine($"Article with ID {id} not found.");
            }
        }

        public void WithdrawArticle(int id, int amount)
        {
            Article? article = FindArticle(id);
            if (article != null)
            {
                if (article.Stock >= amount)
                {
                    article.Stock -= amount;
                }
                else
                {
                    Console.WriteLine($"Not enough stock to withdraw! Current: {article.Stock}, Requested: {amount}");
                }
            }
            else
            {
                Console.WriteLine($"Article with ID {id} not found.");
            }
        }

        public void SortArticle(int id, int slotId)
        {
            Article article = FindArticle(id);
            if (article != null)
            {
                StorageSlot slot = FindStorageSlotById(slotId);
                slot.Fill.Add(article);
            }
            else
            {
                Console.WriteLine($"Article with ID {id} not found.");
            }
        }

        public void DisplayInventory()
        {
            Console.WriteLine("======= Inventory =======");
            foreach (Article item in articles)
            {
                StorageSlot slot = FindStorageSlot(item);
                string slot_id;
                if (slot == null) slot_id = "unsorted";
                else slot_id = slot.Id.ToString();
                Console.WriteLine($"ArticleType-ID: {item.Type.Id}, Name: {item.Type.Name}, Article-ID: {item.Id}, Stock: {item.Stock}, In Slot {slot_id}");
            }
            Console.WriteLine("=========================");
        }

        public void ListStorageSlots()
        {
            Console.WriteLine("===== Storage Slots =====");
            foreach (StorageSlot slot in storageSlots)
            {
                Console.WriteLine($"ID: {slot.Id}");
            }
            Console.WriteLine("=========================");
        }

        // Orders
        public Order NewOrder(List<Article> orderArticles)
        {
            Order generated = new Order(lastOrderId + 1, orderArticles);

            orders.Add(generated);
            lastOrderId += 1;

            return generated;
        }

        public void ListOrders()
        {
            Console.WriteLine("========= Orders ========");
            foreach (Order order in orders)
            {
                Console.WriteLine($"ID: {order.Id}");
                foreach (Article item in order.Articles)
                {
                    StorageSlot slot = FindStorageSlot(item);
                    string slot_id;
                    if (slot == null) slot_id = "unsorted";
                    else slot_id = slot.Id.ToString();
                    Console.WriteLine($"ArticleType-ID: {item.Type.Id}, Article-ID: {item.Id}, Name: {item.Type.Name}, Stock: {item.Stock}, In Slot {slot_id}");
                }
            }
            Console.WriteLine("=========================");
        }

        public Order? NewestOrder()
        {
            return orders.OrderBy(o => o.Id).FirstOrDefault();
        }

        public void FinishOrder(Order order)
        {
            orders.Remove(order);
        }

        // Bills
        public Bill? NewBill(Order order, Prices prices)
        {
            double totalPrice = 0;

            foreach (Article item in order.Articles)
            {
                if (!prices.PriceList.ContainsKey(item.Type))
                {
                    Console.WriteLine($"No price found for ArticleType {item.Type.Name}");
                    return null;
                }
                double price = prices.PriceList[item.Type];
                totalPrice += price * item.Stock;
            }

            Bill generated = new Bill(lastBillId + 1, totalPrice, order);

            bills.Add(generated);
            lastBillId += 1;

            return generated;
        }

        public void ListBills()
        {
            Console.WriteLine("========= Bills =========");
            foreach (Bill bill in bills)
            {
                Console.WriteLine($"ID: {bill.Id}, Total Price: {bill.TotalPrice}");

                foreach (Article item in bill.Order.Articles)
                {
                    StorageSlot slot = FindStorageSlot(item);
                    string slot_id;
                    if (slot == null) slot_id = "unsorted";
                    else slot_id = slot.Id.ToString();
                    Console.WriteLine($"ArticleType-ID: {item.Type.Id}, Article-ID: {item.Id}, Name: {item.Type.Name}, Stock: {item.Stock}, In Slot {slot_id}");
                }
            }
            Console.WriteLine("=========================");
        }

        // Prices
        public Prices NewPrices(Dictionary<ArticleType, double> priceList)
        {
            Prices generated = new Prices(lastPricesId + 1, priceList);

            prices.Add(generated);
            lastPricesId += 1;

            return generated;
        }

        public void ListPrices()
        {
            Console.WriteLine("========= Prices ========");
            foreach (Prices price in prices)
            {
                Console.WriteLine($"ID: {price.Id}");
                foreach (var entry in price.PriceList)
                {
                    Console.WriteLine($"ArticleType-ID: {entry.Key.Id}, Name: {entry.Key.Name}, Price: {entry.Value}");
                }
            }
            Console.WriteLine("=========================");
        }
    }

    public class Article
    {
        public int Id { get; }
        public ArticleType Type { get; }
        public int Stock { get; set; }

        public Article(int id, ArticleType type, int stock)
        {
            Id = id;
            Type = type;
            Stock = stock;
        }
    }

    public class ArticleType
    {
        public int Id { get; }
        public string Name { get; }

        public ArticleType(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public class StorageSlot
    {
        public int Id { get; }
        public List<Article> Fill { get; set; }

        public StorageSlot(int id, List<Article> fill)
        {
            Id = id;
            Fill = fill;
        }
    }

    public class Order
    {
        public int Id { get; }
        public List<Article> Articles { get; set; }

        public Order(int id, List<Article> articles)
        {
            Id = id;
            Articles = articles;
        }
    }

    public class Prices
    {
        public int Id { get; }
        public Dictionary<ArticleType, double> PriceList { get; }

        public Prices(int id, Dictionary<ArticleType, double> priceList)
        {
            Id = id;
            PriceList = priceList;
        }
    }

    public class Bill
    {
        public int Id { get; }
        public double TotalPrice { get; }
        public Order Order { get; }

        public Bill(int id, double totalPrice, Order order)
        {
            Id = id;
            TotalPrice = totalPrice;
            Order = order;
        }
    }
}
