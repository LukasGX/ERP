using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace ERP_Fix
{
    class Code
    {
        static void Main(string[] args)
        {
            //Shell shell = new Shell();
            //shell.Start();

            Program program = new Program();
            program.Start();
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

        // Orders
        private static int lastOrderItemId = 868434; // looks better
        private static object idLock = new object();

        // Sections
        private List<Section> sections = new List<Section>();
        private int lastSectionId = -1;

        // Employees
        private List<Employee> employees = new List<Employee>();
        private int lastEmployeeId = -1;

        // Customers
        private List<Customer> customers = new List<Customer>();
        private int lastCustomerId = -1;

        public void Start()
        {
            // Tests
            ArticleType boot = NewArticleType("Boot");
            ArticleType hat = NewArticleType("Hat");

            Article boots = NewArticle(0, 100);
            Article hats = NewArticle(1, 40);

            Customer customerJaneDoe = NewCustomer("Jane Doe");

            Order order = NewOrder(new List<OrderItem>()
            {
                NewOrderItem(0, 5)
            }, customerJaneDoe);
            ListOrders();
            FinishOrder(order);

            Order orderX = NewOrder(new List<OrderItem>()
            {
                NewOrderItem(1, 2)
            }, customerJaneDoe);
            ListOrders();

            CancelOrder(orderX);
            ListOrders();

            Prices prices = NewPrices(new Dictionary<ArticleType, double>()
            {
                { boot, 49.99 },
                { hat, 19.99 }
            });
            ListPrices();

            Bill? bill = NewBill(order, prices);
            ListBills();

            Section sectionClothing = NewSection("Clothing");
            Section sectionFootwear = NewSection("Footwear");
            ListSections();

            Employee employeeJohnDoe = NewEmployee("John Doe", sectionFootwear);
            ListEmployees();

            ListCustomers();
        }

        int GenerateSequentialId()
        {
            lock (idLock)
            {
                return ++lastOrderItemId;
            }
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

        private StorageSlot? FindStorageSlot(ArticleSimilar article)
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
            ArticleType? articleType = FindArticleType(typeId);
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

        public OrderItem NewOrderItem(int typeId, int stock, bool toList = true)
        {
            ArticleType? articleType = FindArticleType(typeId);
            if (articleType == null) // Ensure proper null checks
            {
                throw new ArgumentException($"Article type with ID {typeId} does not exist.");
            }

            int uniqueNumber = GenerateSequentialId();
            OrderItem generated = new OrderItem(uniqueNumber, articleType, stock);

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
            Article? article = FindArticle(id);
            if (article != null)
            {
                StorageSlot? slot = FindStorageSlotById(slotId);
                if (slot != null)
                    slot.Fill.Add(article);
            }
            else
            {
                Console.WriteLine($"Article with ID {id} not found.");
            }
        }

        public void DisplayInventory()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("======= Inventory =======");
            Console.ResetColor();
            foreach (Article item in articles)
            {
                StorageSlot? slot = FindStorageSlot(item);
                string slot_id;
                if (slot == null) slot_id = "unsorted";
                else slot_id = slot.Id.ToString();
                Console.WriteLine($"ArticleType-ID: {item.Type.Id}, Name: {item.Type.Name}, Article-ID: {item.Id}, Stock: {item.Stock}, In Slot {slot_id}");
            }
            Console.WriteLine("=========================");
        }

        public void ListStorageSlots()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("===== Storage Slots =====");
            Console.ResetColor();
            foreach (StorageSlot slot in storageSlots)
            {
                Console.WriteLine($"ID: {slot.Id}");
            }
            Console.WriteLine("=========================");
        }

        // Orders
        public Order NewOrder(List<OrderItem> orderArticles, Customer customer)
        {
            Order generated = new Order(lastOrderId + 1, orderArticles, customer);

            orders.Add(generated);
            lastOrderId += 1;

            return generated;
        }

        public void ListOrders(bool showFullNotPending = false)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("========= Orders ========");
            Console.ResetColor();
            foreach (Order order in orders)
            {
                Console.Write($"ID: {order.Id}, From: {order.Customer.Name}, Status: ");
                if (order.Status == OrderStatus.Pending)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                }
                else if (order.Status == OrderStatus.Completed)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                else if (order.Status == OrderStatus.Cancelled)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                Console.WriteLine(order.Status);
                Console.ResetColor();
                if (order.Status == OrderStatus.Pending || showFullNotPending)
                {
                    foreach (OrderItem item in order.Articles)
                    {
                        StorageSlot? slot = FindStorageSlot(item);
                        string slot_id;
                        if (slot == null) slot_id = "unsorted";
                        else slot_id = slot.Id.ToString();
                        Console.WriteLine($"ArticleType-ID: {item.Type.Id}, Article-ID: {item.Id}, Name: {item.Type.Name}, Stock: {item.Stock}, In Slot {slot_id}");
                    }
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
            order.Status = OrderStatus.Completed;
        }

        public void CancelOrder(Order order)
        {
            order.Status = OrderStatus.Cancelled;
            Console.WriteLine($"Order with ID {order.Id} has been cancelled.");
        }

        // Bills
        public Bill? NewBill(Order order, Prices prices)
        {
            double totalPrice = 0;

            foreach (OrderItem item in order.Articles)
            {
                if (!prices.PriceList.ContainsKey(item.Type))
                {
                    Console.WriteLine($"No price found for ArticleType {item.Type.Name}");
                    return null;
                }
                double price = prices.PriceList[item.Type];
                totalPrice += price * item.Stock;
            }

            totalPrice = Math.Round(totalPrice, 2);

            Bill generated = new Bill(lastBillId + 1, totalPrice, order);

            bills.Add(generated);
            lastBillId += 1;

            return generated;
        }

        public void ListBills()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("========= Bills =========");
            Console.ResetColor();
            foreach (Bill bill in bills)
            {
                Console.WriteLine($"ID: {bill.Id}, Total Price: {bill.TotalPrice}, From: {bill.Customer.Name}");

                foreach (OrderItem item in bill.Order.Articles)
                {
                    StorageSlot? slot = FindStorageSlot(item);
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
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("========= Prices ========");
            Console.ResetColor();
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

        // Sections
        public Section NewSection(string name)
        {
            Section generated = new Section(lastSectionId + 1, name);

            sections.Add(generated);
            lastSectionId += 1;

            return generated;
        }

        public void ListSections()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("======= Sections =======");
            Console.ResetColor();
            foreach (Section section in sections)
            {
                Console.WriteLine($"ID: {section.Id}, Name: {section.Name}");
            }
            Console.WriteLine("=========================");
        }

        // Employees
        public Employee NewEmployee(string name, Section worksIn)
        {
            Employee generated = new Employee(lastEmployeeId + 1, name, worksIn);

            employees.Add(generated);
            lastEmployeeId += 1;

            return generated;
        }
        public void ListEmployees()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("======= Employees =======");
            Console.ResetColor();
            foreach (Employee employee in employees)
            {
                Console.WriteLine($"ID: {employee.Id}, Name: {employee.Name}, Works in: {employee.worksIn.Name}");
                
            }
            Console.WriteLine("=========================");
        }

        // Customers
        public Customer NewCustomer(string name)
        {
            Customer generated = new Customer(lastCustomerId + 1, name);

            customers.Add(generated);
            lastCustomerId += 1;

            return generated;
        }

        public void ListCustomers()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("======= Customers =======");
            Console.ResetColor();
            foreach (Customer customer in customers)
            {
                Console.WriteLine($"ID: {customer.Id}, Name: {customer.Name}");
            }
            Console.WriteLine("=========================");
        }
    }

    public class ArticleSimilar
    {

    }

    public class Article : ArticleSimilar
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

    public enum OrderStatus
    {
        Pending,
        Completed,
        Cancelled
    }

    public class Order
    {
        public int Id { get; }
        public List<OrderItem> Articles { get; set; }
        public Customer Customer { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public Order(int id, List<OrderItem> articles, Customer customer)
        {
            Id = id;
            Articles = articles;
            Customer = customer;
        }
    }

    public class OrderItem : ArticleSimilar
    {
        public int Id { get; }
        public ArticleType Type { get; }
        public int Stock { get; set; }

        public OrderItem(int id, ArticleType type, int stock)
        {
            Id = id;
            Type = type;
            Stock = stock;
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
        public Customer Customer { get; set; }

        public Bill(int id, double totalPrice, Order order)
        {
            Id = id;
            TotalPrice = totalPrice;
            Order = order;
            Customer = order.Customer;
        }
    }

    public class Section
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public Section(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public enum PersonType
    {
        Employee = 0,
        Customer = 1
    }

    public abstract class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public PersonType Type { get; set; }
    }

    public class Employee : Person
    {
        public Section worksIn { get; set; }

        public Employee(int id, string name, Section worksIn)
        {
            Id = id;
            Name = name;
            Type = PersonType.Employee;
            this.worksIn = worksIn;
        }
    }

    public class Customer : Person
    {
        public Customer(int id, string name)
        {
            Id = id;
            Name = name;
            Type = PersonType.Customer;
        }
    }
}
