using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace ERP_Fix
{
    class Code
    {
        public static bool HideCredits = false;

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            if (args.Contains("--hide-credits"))
            {
                HideCredits = true;
            }

            //Shell shell = new Shell();
            //shell.Start();

            NewShell newShell = new NewShell();
            newShell.Start();

            // ERPManager erpManager = new ERPManager();
            // erpManager.Start();
        }
    }

    public class ERPManager
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

        // SelfOrders
        private List<SelfOrder> selfOrders = new List<SelfOrder>();
        private int lastSelfOrderId = -1;

        // Prices
        private List<Prices> prices = new List<Prices>();
        private int lastPricesId = -1;

        // Bills
        private List<Bill> bills = new List<Bill>();
        private int lastBillId = -1;

        // Payment Terms
        private List<PaymentTerms> paymentTerms = new List<PaymentTerms>();
        private int lastPaymentTermsId = -1;

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

        // currency
        private NumberFormatInfo currentCurrencyFormat = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();

        // own capital
        public double ownCapital;
        public bool ownCapitalSet = false;

        // for order suggestions
        public Dictionary<ArticleType, int> wantedStock = new Dictionary<ArticleType, int>();
        public const int WANTED_STOCK_DEFAULT = 100;

        public const ConsoleColor SECTION_INDICATOR_COLOR = ConsoleColor.Cyan;

        public void Start()
        {
            // Tests
            SetCurrency("de-DE");
            SetOwnCapital(200000.00);

            ArticleType boot = NewArticleType("Boot");
            ArticleType hat = NewArticleType("Hat");

            Article boots = NewArticle(0, 100);
            Article hats = NewArticle(1, 40);

            wantedStock.Add(boot, 1000);
            //wantedStock.Add(hat, 1000);

            Dictionary<ArticleType, int> suggestions = SuggestOrders();

            // we respect the order suggestions
            List<OrderItem> toSelfOrder = new List<OrderItem>();
            foreach (var suggestion in suggestions)
            {
                toSelfOrder.Add(NewOrderItem(suggestion.Key.Id, suggestion.Value));
            }

            SelfOrder selfOrder = NewSelfOrder(toSelfOrder);
            ListSelfOrders();
            selfOrder.Arrive(NewOrderItem(0, 200));
            selfOrder.Arrive(NewOrderItem(1, 60));
            ListSelfOrders();
            selfOrder.Arrive(NewOrderItem(0, 700));
            ListSelfOrders();

            /*
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

            DateTime date = DateTime.ParseExact("20.07.2025", "dd.MM.yyyy", CultureInfo.InvariantCulture);
            PaymentTerms? testPaymentTerms = NewPaymentTerms("30 Days 2% Discount", date, 20.0, penaltyRate: 0.03);

            ListPaymentTerms();*/
        }

        int GenerateSequentialId()
        {
            lock (idLock)
            {
                return ++lastOrderItemId;
            }
        }

        // Own capital
        public void SetOwnCapital(double newOwnCapital)
        {
            ownCapital = Math.Round(newOwnCapital, 2);
            Console.WriteLine($"[INFO] Own capital set to {FormatAmount(ownCapital)}");
        }

        public void AddOwnCapital(double capitalToAdd)
        {
            ownCapital += Math.Round(capitalToAdd, 2);
            Console.WriteLine($"[INFO] Added {FormatAmount(capitalToAdd)} to own capital. Own capital: {FormatAmount(ownCapital)}");
        }

        public void RemoveOwnCapital(double capitalToRemove)
        {
            if (ownCapital >= capitalToRemove)
            {
                ownCapital -= Math.Round(capitalToRemove, 2);
                Console.WriteLine($"[INFO] Removed {FormatAmount(capitalToRemove)} from own capital. Own capital: {FormatAmount(ownCapital)}");
            }
            else
                Console.WriteLine($"[ERROR] Can't remove {FormatAmount(capitalToRemove)} from {FormatAmount(ownCapital)} own capital");
        }

        public void SetCurrency(string cultureName)
        {
            var cultureInfo = CultureInfo.CurrentCulture;

            try
            {
                cultureInfo = new CultureInfo(cultureName);
            }
            catch (Exception)
            {
                Console.WriteLine($"[ERROR] Invalid culture name '{cultureName}'");
                return;
            }

            currentCurrencyFormat = (NumberFormatInfo)cultureInfo.NumberFormat.Clone();

            currentCurrencyFormat.CurrencyDecimalDigits = 2;
        }

        public string FormatAmount(double amount)
        {
            return amount.ToString("C", currentCurrencyFormat);
        }

        // jobs
        public Dictionary<ArticleType, int> SuggestOrders()
        {
            Dictionary<ArticleType, int> generated = new Dictionary<ArticleType, int>();

            foreach (ArticleType type in articleTypes)
            {
                List<Article> articles = GetArticlesByType(type);

                int fullStock = 0;
                foreach (Article article in articles)
                    fullStock += article.Stock;

                if (wantedStock.ContainsKey(type) && fullStock < wantedStock[type])
                {
                    Console.WriteLine($"Suggested Order: {wantedStock[type] - fullStock} of article type {type.Name} ({type.Id})");
                    generated[type] = wantedStock[type] - fullStock;
                }
                else if (!wantedStock.ContainsKey(type) && fullStock < WANTED_STOCK_DEFAULT)
                {
                    Console.WriteLine($"Suggested Order: {WANTED_STOCK_DEFAULT - fullStock} of article type {type.Name} ({type.Id}). (Used default as no suitable wantedStock entry was found)");
                    generated[type] = WANTED_STOCK_DEFAULT - fullStock;
                }
            }

            return generated;
        }

        // reveal internal lists
        public int GetArticleCount()
        {
            return articles.Count;
        }
        public int GetArticleTypeCount()
        {
            return articleTypes.Count;
        }
        public int GetStorageSlotCount()
        {
            return storageSlots.Count;
        }
        public int GetOrderCount()
        {
            return orders.Count;
        }
        public int GetSelfOrderCount()
        {
            return selfOrders.Count;
        }
        public int GetPricesCount()
        {
            return prices.Count;
        }
        public int GetBillCount()
        {
            return bills.Count;
        }
        public int GetPaymentTermsCount()
        {
            return paymentTerms.Count;
        }
        public int GetSectionCount()
        {
            return sections.Count;
        }
        public int GetEmployeeCount()
        {
            return employees.Count;
        }
        public int GetCustomerCount()
        {
            return customers.Count;
        }

        // Warehousing
        private Article? FindArticle(int id)
        {
            return articles.FirstOrDefault(a => a.Id == id);
        }

        public List<Article> GetArticlesByType(ArticleType type)
        {
            return articles.Where(article => article.Type == type).ToList();
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
                Console.WriteLine($"[ERROR] Article with ID {id} not found.");
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
                    Console.WriteLine($"[ERROR] Not enough stock to withdraw! Current: {article.Stock}, Requested: {amount}");
                }
            }
            else
            {
                Console.WriteLine($"[ERROR] Article with ID {id} not found.");
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
                Console.WriteLine($"[ERROR] Article with ID {id} not found.");
            }
        }

        public void DisplayInventory()
        {
            Console.ForegroundColor = SECTION_INDICATOR_COLOR;
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
            Console.ForegroundColor = SECTION_INDICATOR_COLOR;
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
            Console.ForegroundColor = SECTION_INDICATOR_COLOR;
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
            Console.WriteLine($"[INFO] Order with ID {order.Id} has been cancelled.");
        }

        // SelfOrders
        public SelfOrder NewSelfOrder(List<OrderItem> orderArticles)
        {
            SelfOrder generated = new SelfOrder(lastSelfOrderId + 1, orderArticles);

            selfOrders.Add(generated);
            lastSelfOrderId += 1;

            return generated;
        }

        public void ListSelfOrders(bool showFullNotPending = false)
        {
            Console.ForegroundColor = SECTION_INDICATOR_COLOR;
            Console.WriteLine("======= Self Orders =======");
            Console.ResetColor();
            foreach (SelfOrder selfOrder in selfOrders)
            {
                Console.Write($"ID: {selfOrder.Id}, Status: ");
                if (selfOrder.Status == OrderStatus.Pending)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                }
                else if (selfOrder.Status == OrderStatus.Completed)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                else if (selfOrder.Status == OrderStatus.Cancelled)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                Console.WriteLine(selfOrder.Status);
                Console.ResetColor();

                if (selfOrder.Status == OrderStatus.Pending || showFullNotPending)
                {
                    foreach (OrderItem item in selfOrder.Articles)
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

        public void FinishSelfOrder(SelfOrder selfOrder)
        {
            selfOrder.Status = OrderStatus.Completed;
        }

        public void CancelSelfOrder(SelfOrder selfOrder)
        {
            selfOrder.Status = OrderStatus.Cancelled;
            Console.WriteLine($"[INFO] Order with ID {selfOrder.Id} has been cancelled.");
        }

        // Bills
        public Bill? NewBill(Order order, Prices prices)
        {
            double totalPrice = 0;

            foreach (OrderItem item in order.Articles)
            {
                if (!prices.PriceList.ContainsKey(item.Type))
                {
                    Console.WriteLine($"[ERROR] No price found for ArticleType {item.Type.Name}");
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
            Console.ForegroundColor = SECTION_INDICATOR_COLOR;
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
            Console.ForegroundColor = SECTION_INDICATOR_COLOR;
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

        // Payment Terms
        public PaymentTerms? NewPaymentTerms(string name, object daysUntilDue, double absolutePenalty, int? discountDays = 0, double? discountPercent = 0.00, double? penaltyRate = 0.00)
        {
            int cDaysUntilDue;
            if (daysUntilDue is int)
                cDaysUntilDue = (int)daysUntilDue;
            else if (daysUntilDue is DateTime)
                cDaysUntilDue = Math.Abs((DateTime.Now.Date - (DateTime)daysUntilDue).Days);
            else
            {
                Console.WriteLine($"[ERROR] daysUntilDue must be an int or DateTime => {daysUntilDue.GetType()} is not allowed");
                return null;
            }

            PaymentTerms generated = new PaymentTerms(lastPaymentTermsId + 1, name, cDaysUntilDue, absolutePenalty, discountDays, discountPercent, penaltyRate);

            paymentTerms.Add(generated);
            lastBillId += 1;

            return generated;
        }

        public void ListPaymentTerms()
        {
            Console.ForegroundColor = SECTION_INDICATOR_COLOR;
            Console.WriteLine("===== Payment Terms =====");
            Console.ResetColor();
            foreach (PaymentTerms terms in paymentTerms)
            {
                Console.WriteLine($"ID: {terms.Id}, Name: {terms.Name}, Days Until Due: {terms.DaysUntilDue}, Discount Days: {terms.DiscountDays}, Discount Percent: {terms.DiscountPercent}, Penalty Rate: {terms.PenaltyRate}, Absolute Penalty: {terms.AbsolutePenalty}, Using Penalty Rate: {terms.UsingPenaltyRate.ToString()}");
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
            Console.ForegroundColor = SECTION_INDICATOR_COLOR;
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
            Console.ForegroundColor = SECTION_INDICATOR_COLOR;
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
            Console.ForegroundColor = SECTION_INDICATOR_COLOR;
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

    public class SelfOrder
    {
        public int Id { get; }
        public List<OrderItem> Articles { get; set; }
        public List<OrderItem> Arrived { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public SelfOrder(int id, List<OrderItem> articles)
        {
            Id = id;
            Articles = articles;
            Arrived = new List<OrderItem>();
        }

        public void Arrive(OrderItem item)
        {
            var existing = Articles.FirstOrDefault(a => a.Type == item.Type);
            if (existing == null)
            {
                Console.WriteLine($"[ERROR] Item {item.Type.Name} not found in self-order {Id}");
                return;
            }
            if (item.Stock <= 0)
            {
                Console.WriteLine($"[ERROR] Delivered quantity must be greater than 0.");
                return;
            }
            if (item.Stock > existing.Stock)
            {
                Console.WriteLine($"[WARN] Delivered quantity ({item.Stock}) exceeds remaining stock ({existing.Stock}) in self-order {Id}. Capping to {existing.Stock}.");
                item.Stock = existing.Stock;
            }
            existing.Stock -= item.Stock;

            OrderItem arrivedItem = new OrderItem(item.Id, existing.Type, item.Stock);
            Arrived.Add(arrivedItem);
            if (existing.Stock == 0)
            {
                Articles.Remove(existing);
            }
            Console.WriteLine($"[INFO] Item {item.Type.Name} arrived with quantity {item.Stock}.");

            if (Articles.Count == 0)
            {
                Status = OrderStatus.Completed;
                Console.WriteLine($"[INFO] Self-order {Id} is now completed as all items have arrived.");
            }
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

    public class PaymentTerms
    {

        public int Id { get; }
        public string Name { get; set; }
        public int DaysUntilDue { get; set; }
        public int? DiscountDays { get; set; }
        public double? DiscountPercent { get; set; }

        public double? PenaltyRate { get; set; } // default interest
        public double AbsolutePenalty { get; set; }
        public bool UsingPenaltyRate { get; }

        public PaymentTerms(int id, string name, int daysUntilDue, double absolutePenalty, int? discountDays = null, double? discountPercent = null, double? penaltyRate = null)
        {
            Id = id;
            Name = name;
            DaysUntilDue = daysUntilDue;
            DiscountDays = discountDays;
            DiscountPercent = discountPercent;
            PenaltyRate = penaltyRate;
            AbsolutePenalty = absolutePenalty;
            UsingPenaltyRate = penaltyRate.HasValue && penaltyRate.Value > 0;
        }

        public static DateTime GetDueDate(DateTime InvoiceDate, int DaysUntilDue)
        {
            return InvoiceDate.AddDays(DaysUntilDue);
        }

        public static DateTime? GetDiscountDate(DateTime invoiceDate, int? discountDays)
        {
            return discountDays.HasValue ? invoiceDate.AddDays(discountDays.Value) : null;
        }

        public static double GetDiscountAmount(double totalAmount, double? discountPercent)
        {
            return discountPercent.HasValue ? totalAmount * (double)discountPercent : 0;
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
        public string? Name { get; set; }
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

    public class BillReader
    {
        public static void xml(string filePath)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(filePath);

            XmlNamespaceManager nsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsMgr.AddNamespace("rsm", "urn:ferd:CrossIndustryInvoice:invoice:1p0");
            nsMgr.AddNamespace("ram", "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:12");
            nsMgr.AddNamespace("udt", "urn:un:unece:uncefact:data:standard:UnqualifiedDataType:15");

            // Invoice Header
            Console.WriteLine("=== Invoice Header ===");
            var idNode = xmlDoc.SelectSingleNode("//rsm:ExchangedDocument/ram:ID", nsMgr);
            var typeCodeNode = xmlDoc.SelectSingleNode("//rsm:ExchangedDocument/ram:TypeCode", nsMgr);
            var issueDateNode = xmlDoc.SelectSingleNode("//rsm:ExchangedDocument/ram:IssueDateTime/udt:DateTimeString", nsMgr);
            var noteNode = xmlDoc.SelectSingleNode("//rsm:ExchangedDocument/ram:IncludedNote/ram:Content", nsMgr);

            Console.WriteLine($"Invoice Number: {idNode?.InnerText}");
            Console.WriteLine($"Type Code: {typeCodeNode?.InnerText}");
            Console.WriteLine($"Issue Date: {issueDateNode?.InnerText}");
            if (noteNode != null)
                Console.WriteLine($"Note: {noteNode.InnerText}");

            // Seller
            Console.WriteLine("\n=== Seller ===");
            var sellerNode = xmlDoc.SelectSingleNode("//ram:SellerTradeParty", nsMgr);
            Console.WriteLine($"Name: {sellerNode.SelectSingleNode("ram:Name", nsMgr)?.InnerText}");
            Console.WriteLine($"Street: {sellerNode.SelectSingleNode("ram:PostalTradeAddress/ram:LineOne", nsMgr)?.InnerText}");
            Console.WriteLine($"ZIP: {sellerNode.SelectSingleNode("ram:PostalTradeAddress/ram:PostcodeCode", nsMgr)?.InnerText}");
            Console.WriteLine($"City: {sellerNode.SelectSingleNode("ram:PostalTradeAddress/ram:CityName", nsMgr)?.InnerText}");
            Console.WriteLine($"Country: {sellerNode.SelectSingleNode("ram:PostalTradeAddress/ram:CountryID", nsMgr)?.InnerText}");
            Console.WriteLine($"VAT ID: {sellerNode.SelectSingleNode("ram:SpecifiedTaxRegistration/ram:ID", nsMgr)?.InnerText}");

            // Buyer
            Console.WriteLine("\n=== Buyer ===");
            var buyerNode = xmlDoc.SelectSingleNode("//ram:BuyerTradeParty", nsMgr);
            Console.WriteLine($"Name: {buyerNode.SelectSingleNode("ram:Name", nsMgr)?.InnerText}");
            Console.WriteLine($"Street: {buyerNode.SelectSingleNode("ram:PostalTradeAddress/ram:LineOne", nsMgr)?.InnerText}");
            Console.WriteLine($"ZIP: {buyerNode.SelectSingleNode("ram:PostalTradeAddress/ram:PostcodeCode", nsMgr)?.InnerText}");
            Console.WriteLine($"City: {buyerNode.SelectSingleNode("ram:PostalTradeAddress/ram:CityName", nsMgr)?.InnerText}");
            Console.WriteLine($"Country: {buyerNode.SelectSingleNode("ram:PostalTradeAddress/ram:CountryID", nsMgr)?.InnerText}");

            // Reference (Order)
            var buyerRefNode = xmlDoc.SelectSingleNode("//ram:BuyerReference", nsMgr);
            var orderRefNode = xmlDoc.SelectSingleNode("//ram:ContractReferencedDocument/ram:ID", nsMgr);
            if (buyerRefNode != null)
                Console.WriteLine($"Buyer Reference: {buyerRefNode.InnerText}");
            if (orderRefNode != null)
                Console.WriteLine($"Order Reference: {orderRefNode.InnerText}");

            // Delivery
            var deliveryDateNode = xmlDoc.SelectSingleNode("//ram:ActualDeliverySupplyChainEvent/ram:OccurrenceDateTime/udt:DateTimeString", nsMgr);
            if (deliveryDateNode != null)
            {
                Console.WriteLine("\n=== Delivery ===");
                Console.WriteLine($"Delivery Date: {deliveryDateNode.InnerText}");
            }

            // Payment
            Console.WriteLine("\n=== Payment / Settlement ===");
            var paymentRefNode = xmlDoc.SelectSingleNode("//ram:PaymentReference", nsMgr);
            var currencyNode = xmlDoc.SelectSingleNode("//ram:InvoiceCurrencyCode", nsMgr);
            var paymentMeansNode = xmlDoc.SelectSingleNode("//ram:SpecifiedTradeSettlementPaymentMeans", nsMgr);
            Console.WriteLine($"Payment Reference: {paymentRefNode?.InnerText}");
            Console.WriteLine($"Currency: {currencyNode?.InnerText}");
            if (paymentMeansNode != null)
            {
                Console.WriteLine($"Payment Type: {paymentMeansNode.SelectSingleNode("ram:TypeCode", nsMgr)?.InnerText}");
                Console.WriteLine($"Payment Info: {paymentMeansNode.SelectSingleNode("ram:Information", nsMgr)?.InnerText}");
            }

            // Totals
            Console.WriteLine("\n=== Monetary Summation ===");
            var lineTotalNode = xmlDoc.SelectSingleNode("//ram:SpecifiedTradeSettlementMonetarySummation/ram:LineTotalAmount", nsMgr);
            var taxTotalNode = xmlDoc.SelectSingleNode("//ram:SpecifiedTradeSettlementMonetarySummation/ram:TaxTotalAmount", nsMgr);
            var grandTotalNode = xmlDoc.SelectSingleNode("//ram:SpecifiedTradeSettlementMonetarySummation/ram:GrandTotalAmount", nsMgr);
            Console.WriteLine($"Line Total: {lineTotalNode?.InnerText} EUR");
            Console.WriteLine($"Tax Total: {taxTotalNode?.InnerText} EUR");
            Console.WriteLine($"Grand Total: {grandTotalNode?.InnerText} EUR");

            // Tax breakdown
            Console.WriteLine("\n=== Tax Details ===");
            var taxNodes = xmlDoc.SelectNodes("//ram:ApplicableTradeTax", nsMgr);
            foreach (XmlNode tax in taxNodes)
            {
                Console.WriteLine("- VAT Info:");
                Console.WriteLine($"  Tax Amount: {tax.SelectSingleNode("ram:CalculatedAmount", nsMgr)?.InnerText}");
                Console.WriteLine($"  Tax Type: {tax.SelectSingleNode("ram:TypeCode", nsMgr)?.InnerText}");
                Console.WriteLine($"  Tax Basis: {tax.SelectSingleNode("ram:BasisAmount", nsMgr)?.InnerText}");
                Console.WriteLine($"  Tax Rate: {tax.SelectSingleNode("ram:RateApplicablePercent", nsMgr)?.InnerText}%");
            }

            // All positions
            Console.WriteLine("\n=== Line Items ===");
            var lineItems = xmlDoc.SelectNodes("//ram:IncludedSupplyChainTradeLineItem", nsMgr);
            foreach (XmlNode item in lineItems)
            {
                var lineId = item.SelectSingleNode("ram:AssociatedDocumentLineDocument/ram:LineID", nsMgr);
                var productName = item.SelectSingleNode("ram:SpecifiedTradeProduct/ram:Name", nsMgr);
                var productDesc = item.SelectSingleNode("ram:SpecifiedTradeProduct/ram:Description", nsMgr);
                var buyerAssignedId = item.SelectSingleNode("ram:SpecifiedTradeProduct/ram:BuyerAssignedID", nsMgr);
                var quantity = item.SelectSingleNode("ram:SpecifiedLineTradeDelivery/ram:BilledQuantity", nsMgr);
                var unit = quantity?.Attributes["unitCode"]?.Value ?? "";
                var grossPrice = item.SelectSingleNode("ram:SpecifiedLineTradeAgreement/ram:GrossPriceProductTradePrice/ram:ChargeAmount", nsMgr);
                var netPrice = item.SelectSingleNode("ram:SpecifiedLineTradeAgreement/ram:NetPriceProductTradePrice/ram:ChargeAmount", nsMgr);
                var lineTotal = item.SelectSingleNode("ram:SpecifiedLineTradeSettlement/ram:SpecifiedTradeSettlementLineMonetarySummation/ram:LineTotalAmount", nsMgr);
                var taxDetail = item.SelectSingleNode("ram:SpecifiedLineTradeSettlement/ram:ApplicableTradeTax", nsMgr);
                var taxRate = taxDetail?.SelectSingleNode("ram:RateApplicablePercent", nsMgr);

                Console.WriteLine($"\nLine {lineId?.InnerText}:");
                Console.WriteLine($"  Product: {productName?.InnerText}");
                Console.WriteLine($"  Description: {productDesc?.InnerText}");
                Console.WriteLine($"  Product Number: {buyerAssignedId?.InnerText}");
                Console.WriteLine($"  Quantity: {quantity?.InnerText} {unit}");
                Console.WriteLine($"  Gross Price per Unit: {grossPrice?.InnerText} EUR");
                Console.WriteLine($"  Net Price per Unit: {netPrice?.InnerText} EUR");
                Console.WriteLine($"  Line Total: {lineTotal?.InnerText} EUR");
                Console.WriteLine($"  VAT Rate: {taxRate?.InnerText}%");
            }
        }
    }
}
