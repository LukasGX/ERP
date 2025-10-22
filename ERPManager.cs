using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spire.Barcode;
using System.Diagnostics;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;

// TODO: Add Company Information and PDF Generation to TUI

namespace ERP_Fix
{
    class Code
    {
        public static bool HideCredits = false;
        private static bool _terminalGuiInitialized = false;

        static void Main(string[] args)
        {
            // Catch any unhandled exception so the console doesn’t close instantly when launched by double-click.
            AppDomain.CurrentDomain.UnhandledException += (sender, evt) =>
            {
                try
                {
                    var ex = evt.ExceptionObject as Exception;
                    ShowFatalError("An unexpected error occurred.", ex);
                }
                catch { /* best effort */ }
            };

            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;

                if (args.Contains("--hide-credits"))
                {
                    HideCredits = true;
                }

                if (args.Contains("--show-completed-orders"))
                {
                    TUI.ShowCompletedOrders = true;
                }

                if (args.Contains("--show-cancelled-orders"))
                {
                    TUI.ShowCancelledOrders = true;
                }

                // start actual program
                if (args.Contains("--shell"))
                {
                    Shell shell = new Shell();
                    shell.Start();
                }
                else if (args.Contains("--newshell"))
                {
                    NewShell newShell = new NewShell();
                    newShell.Start();
                }
                else
                {
                    TUI tui = new TUI();
                    _terminalGuiInitialized = true;
                    tui.Start();
                }
            }
            catch (Exception ex)
            {
                ShowFatalError("A fatal error prevented the application from starting.", ex);
            }
            finally
            {
                // Attempt a graceful shutdown of Terminal.Gui if it was initialized
                try
                {
                    if (_terminalGuiInitialized)
                    {
                        Terminal.Gui.Application.Shutdown();
                    }
                }
                catch { /* ignore */ }
            }
        }

        private static void ShowFatalError(string header, Exception? ex)
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var logFile = Path.Combine(baseDir, "crash.log");
                var sb = new StringBuilder();
                sb.AppendLine($"==== {DateTime.Now:yyyy-MM-dd HH:mm:ss} ====");
                sb.AppendLine(header);
                if (ex != null)
                {
                    sb.AppendLine(ex.ToString());
                }
                File.AppendAllText(logFile, sb.ToString(), Encoding.UTF8);

                // Best-effort console output for users launching via double-click
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(header);
                Console.ResetColor();
                if (ex != null)
                {
                    Console.WriteLine(ex.Message);
                }
                Console.WriteLine($"A log was written to: {logFile}");
                Console.WriteLine("Press Enter to exit...");
                try { Console.ReadLine(); } catch { /* ignore */ }
            }
            catch { /* ignore all */ }
        }
    }

    public class ERPManager
    {
        // Instance naming
        public string InstanceName { get; }
        public string FileBaseName => MakeFileSafe(InstanceName).ToLowerInvariant();

        // Convenience default constructor for callers that don't pass an instance name
        public ERPManager() : this("default") { }

        public ERPManager(string instanceName)
        {
            if (string.IsNullOrWhiteSpace(instanceName))
                throw new ArgumentException("Instance name cannot be empty.", nameof(instanceName));
            InstanceName = instanceName;
        }

        // Company
        private Company? company;

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

        // scanner ids
        public List<long> ScannerIds = new List<long>();

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

            SaveInstance("instances/");

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

        public void SetCompany(string companyName, string street, string city, string postalCode, string country, string email, string phoneNumber, string bankName, string iban, string bic)
        {
            company = new Company(companyName, new Address(street, city, postalCode, country), email, phoneNumber, new BankInformation(bankName, iban, bic));
        }

        public Company? GetCompany()
        {
            return company;
        }

        public string FormatAmount(double amount)
        {
            return amount.ToString("C", currentCurrencyFormat);
        }

        // save and open instance
        public void SaveInstance(string path)
        {
            if (string.IsNullOrWhiteSpace(InstanceName))
                throw new InvalidOperationException("Instance must have a name before saving.");

            // Build a snapshot DTO to avoid issues with complex keys and references
            var snapshot = BuildSnapshot();

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            string json = JsonSerializer.Serialize(snapshot, options);

            // Determine output file path
            string fileName = $"{FileBaseName}.erp";
            string targetPath = path;

            try
            {
                if (string.IsNullOrWhiteSpace(targetPath))
                {
                    targetPath = fileName; // current directory
                }
                else if (Directory.Exists(targetPath) || !Path.HasExtension(targetPath))
                {
                    // Treat as directory (or a path without extension)
                    Directory.CreateDirectory(targetPath);
                    targetPath = Path.Combine(targetPath, fileName);
                }
                else
                {
                    // Treat as file; normalize extension to .erp
                    var dir = Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    targetPath = Path.ChangeExtension(targetPath, ".erp");
                }

                File.WriteAllText(targetPath, json, Encoding.UTF8);
                Console.WriteLine($"[INFO] Instance '{InstanceName}' saved to {targetPath}");

                // Also write a separate TOP SECRET secrets file with Company and Bank info
                var secrets = BuildSecretsSnapshot();
                var secretsJson = JsonSerializer.Serialize(secrets, options);
                var secretsPath = Path.Combine(
                    Path.GetDirectoryName(targetPath) ?? Directory.GetCurrentDirectory(),
                    $"{Path.GetFileNameWithoutExtension(targetPath)}.erps");
                File.WriteAllText(secretsPath, secretsJson, Encoding.UTF8);
                Console.WriteLine($"[INFO] Saved secrets to '{secretsPath}'. Keep this file PRIVATE.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to save instance: {ex.Message}");
                throw;
            }
        }

        public static ERPManager OpenInstance(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path must not be empty.", nameof(path));

            // Do NOT accept a directory here; force the caller to pass a file path
            if (Directory.Exists(path))
                throw new InvalidOperationException($"'{path}' is a directory. Please provide a .erp file path, e.g., instances/std.erp.");

            string targetPath = path;

            // Convenience: if no extension is provided, try appending .erp
            if (!Path.HasExtension(targetPath))
            {
                var withExt = Path.ChangeExtension(targetPath, ".erp");
                if (File.Exists(withExt))
                {
                    targetPath = withExt;
                }
            }

            if (!File.Exists(targetPath))
                throw new FileNotFoundException($".erp file not found: {targetPath}");

            // Enforce .erp extension
            if (!string.Equals(Path.GetExtension(targetPath), ".erp", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"File must have .erp extension: {targetPath}");

            try
            {
                string json = File.ReadAllText(targetPath, Encoding.UTF8);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var snapshot = JsonSerializer.Deserialize<ERPInstanceSnapshot>(json, options)
                               ?? throw new InvalidDataException("Failed to parse .erp snapshot.");

                var mgr = FromSnapshot(snapshot);

                // Try to hydrate company + bank info from the optional .erps (secrets) file
                try
                {
                    string secretsPath = Path.Combine(
                        Path.GetDirectoryName(targetPath) ?? Directory.GetCurrentDirectory(),
                        $"{Path.GetFileNameWithoutExtension(targetPath)}.erps");
                    if (File.Exists(secretsPath))
                    {
                        string secretsJson = File.ReadAllText(secretsPath, Encoding.UTF8);
                        var secrets = JsonSerializer.Deserialize<ERPSecretsSnapshot>(secretsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        var c = secrets?.Company;
                        if (c != null)
                        {
                            var addr = c.Address;
                            var bank = c.BankInfo;
                            mgr.company = new Company(
                                c.Name ?? string.Empty,
                                new Address(addr?.Street ?? string.Empty, addr?.City ?? string.Empty, addr?.PostalCode ?? string.Empty, addr?.Country ?? string.Empty),
                                c.Email ?? string.Empty,
                                c.PhoneNumber ?? string.Empty,
                                new BankInformation(bank?.BankName ?? string.Empty, bank?.IBAN ?? string.Empty, bank?.BIC ?? string.Empty)
                            );
                        }
                    }
                }
                catch (Exception sex)
                {
                    Console.WriteLine($"[WARN] Failed to read secrets file: {sex.Message}");
                }

                return mgr;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to open instance: {ex.Message}");
                throw;
            }
        }

        private static ERPManager FromSnapshot(ERPInstanceSnapshot snapshot)
        {
            var instanceName = string.IsNullOrWhiteSpace(snapshot.InstanceName) ? "Unnamed" : snapshot.InstanceName;
            var mgr = new ERPManager(instanceName);

            // Own capital
            mgr.ownCapital = snapshot.OwnCapital;
            mgr.ownCapitalSet = snapshot.OwnCapitalSet;

            // Article types
            var typeById = new Dictionary<int, ArticleType>();
            foreach (var t in snapshot.ArticleTypes)
            {
                var at = new ArticleType(t.Id, t.Name);
                typeById[t.Id] = at;
                mgr.articleTypes.Add(at);
            }

            // Articles
            var articleById = new Dictionary<int, Article>();
            foreach (var a in snapshot.Articles)
            {
                if (!typeById.TryGetValue(a.TypeId, out var at))
                {
                    Console.WriteLine($"[WARN] Unknown ArticleTypeId {a.TypeId} for Article {a.Id}. Skipping.");
                    continue;
                }
                // Ensure ScannerId is maintained; generate one if missing/zero for backward compatibility
                long scannerId = a.ScannerId != 0 ? a.ScannerId : mgr.GenerateScannerId();
                var art = new Article(a.Id, at, a.Stock, scannerId);
                if (!mgr.ScannerIds.Contains(scannerId))
                {
                    mgr.ScannerIds.Add(scannerId);
                }
                articleById[a.Id] = art;
                mgr.articles.Add(art);
            }

            // Storage slots
            foreach (var s in snapshot.StorageSlots)
            {
                var fill = new List<Article>();
                foreach (var aid in s.FillArticleIds)
                {
                    if (articleById.TryGetValue(aid, out var art))
                        fill.Add(art);
                    else
                        Console.WriteLine($"[WARN] Unknown ArticleId {aid} in StorageSlot {s.Id}.");
                }
                mgr.storageSlots.Add(new StorageSlot(s.Id, fill));
            }

            // Sections
            var sectionById = new Dictionary<int, Section>();
            foreach (var s in snapshot.Sections)
            {
                var sec = new Section(s.Id, s.Name);
                sectionById[s.Id] = sec;
                mgr.sections.Add(sec);
            }

            // Customers
            var customerById = new Dictionary<int, Customer>();
            foreach (var c in snapshot.Customers)
            {
                var cust = new Customer(
                    c.Id,
                    c.Name,
                    c.Street,
                    c.City,
                    c.PostalCode,
                    c.Country,
                    c.Email,
                    c.PhoneNumber
                );
                customerById[c.Id] = cust;
                mgr.customers.Add(cust);
            }

            // Employees
            foreach (var e in snapshot.Employees)
            {
                if (!sectionById.TryGetValue(e.WorksInSectionId, out var sec))
                {
                    Console.WriteLine($"[WARN] Unknown SectionId {e.WorksInSectionId} for Employee {e.Id}. Skipping.");
                    continue;
                }
                var emp = new Employee(
                    e.Id,
                    e.Name,
                    sec,
                    e.Street,
                    e.City,
                    e.PostalCode,
                    e.Country,
                    e.Email,
                    e.PhoneNumber
                );
                mgr.employees.Add(emp);
            }

            // Orders
            var orderById = new Dictionary<int, Order>();
            foreach (var o in snapshot.Orders)
            {
                if (!customerById.TryGetValue(o.CustomerId, out var cust))
                {
                    Console.WriteLine($"[WARN] Unknown CustomerId {o.CustomerId} for Order {o.Id}. Skipping.");
                    continue;
                }
                var items = new List<OrderItem>();
                foreach (var oi in o.Articles)
                {
                    if (!typeById.TryGetValue(oi.TypeId, out var at))
                    {
                        Console.WriteLine($"[WARN] Unknown ArticleTypeId {oi.TypeId} for OrderItem {oi.Id}.");
                        continue;
                    }
                    items.Add(new OrderItem(oi.Id, at, oi.Stock));
                }
                var order = new Order(o.Id, items, cust);
                if (Enum.TryParse<OrderStatus>(o.Status, out var st))
                    order.Status = st;
                mgr.orders.Add(order);
                orderById[o.Id] = order;
            }

            // SelfOrders
            foreach (var so in snapshot.SelfOrders)
            {
                var items = new List<OrderItem>();
                foreach (var oi in so.Articles)
                {
                    if (!typeById.TryGetValue(oi.TypeId, out var at))
                    {
                        Console.WriteLine($"[WARN] Unknown ArticleTypeId {oi.TypeId} for SelfOrderItem {oi.Id}.");
                        continue;
                    }
                    items.Add(new OrderItem(oi.Id, at, oi.Stock));
                }
                var selfOrder = new SelfOrder(so.Id, items);
                if (Enum.TryParse<OrderStatus>(so.Status, out var st))
                    selfOrder.Status = st;
                foreach (var ai in so.Arrived)
                {
                    if (!typeById.TryGetValue(ai.TypeId, out var at))
                    {
                        Console.WriteLine($"[WARN] Unknown ArticleTypeId {ai.TypeId} for ArrivedItem {ai.Id}.");
                        continue;
                    }
                    selfOrder.Arrived.Add(new OrderItem(ai.Id, at, ai.Stock));
                }
                mgr.selfOrders.Add(selfOrder);
            }

            // Prices
            var pricesById = new Dictionary<int, Prices>();
            foreach (var p in snapshot.Prices)
            {
                var dict = new Dictionary<ArticleType, double>();
                foreach (var kvp in p.PriceListByTypeId)
                {
                    if (typeById.TryGetValue(kvp.Key, out var at))
                        dict[at] = kvp.Value;
                    else
                        Console.WriteLine($"[WARN] Unknown ArticleTypeId {kvp.Key} in Prices {p.Id}.");
                }
                var pr = new Prices(p.Id, dict);
                mgr.prices.Add(pr);
                pricesById[p.Id] = pr;
            }

            // Payment terms (load before bills so bills can reference them)
            var termsById = new Dictionary<int, PaymentTerms>();
            foreach (var pt in snapshot.PaymentTerms)
            {
                var terms = new PaymentTerms(pt.Id, pt.Name, pt.DaysUntilDue, pt.AbsolutePenalty, pt.DiscountDays, pt.DiscountPercent, pt.PenaltyRate);
                mgr.paymentTerms.Add(terms);
                termsById[pt.Id] = terms;
            }

            // Bills
            foreach (var b in snapshot.Bills)
            {
                if (!orderById.TryGetValue(b.OrderId, out var ord))
                {
                    Console.WriteLine($"[WARN] Unknown OrderId {b.OrderId} for Bill {b.Id}. Skipping.");
                    continue;
                }
                if (!customerById.TryGetValue(b.CustomerId, out var cust))
                {
                    Console.WriteLine($"[WARN] Unknown CustomerId {b.CustomerId} for Bill {b.Id}. Skipping.");
                    continue;
                }

                // Resolve payment terms; if missing create a sensible default
                PaymentTerms termsForBill;
                if (b.PaymentTermsId != 0 && termsById.TryGetValue(b.PaymentTermsId, out var foundTerms))
                {
                    termsForBill = foundTerms;
                }
                else
                {
                    // Backward compatibility for old snapshots without PaymentTermsId
                    const int defaultTermsId = 0;
                    if (!termsById.TryGetValue(defaultTermsId, out var def))
                    {
                        def = new PaymentTerms(defaultTermsId, "Standard", 30, 0.0);
                        mgr.paymentTerms.Add(def);
                        termsById[defaultTermsId] = def;
                    }
                    termsForBill = def;
                }

                // Attach a Prices reference; prefer the specific PricesId if available (schema v5+), else use a smarter fallback
                Prices pricesForBill;
                if (b.PricesId != 0 && pricesById.TryGetValue(b.PricesId, out var prFound))
                {
                    pricesForBill = prFound;
                }
                else
                {
                    // Fallback strategy:
                    // 1) If there's exactly one Prices entry, use it.
                    // 2) Otherwise, pick the Prices whose price list covers the most article types in the order (best coverage).
                    //    Break ties by choosing the lowest Id to keep it deterministic.
                    // 3) If none found (no prices in snapshot), create an empty Prices with Id 0.
                    if (mgr.prices.Count == 1)
                    {
                        pricesForBill = mgr.prices[0];
                        Console.WriteLine($"[WARN] Bill {b.Id}: PricesId missing/not found. Falling back to the only available Prices {pricesForBill.Id}.");
                    }
                    else if (mgr.prices.Count > 1)
                    {
                        var requiredTypes = new HashSet<ArticleType>(ord.Articles.Select(ai => ai.Type));
                        var best = mgr.prices
                            .Select(pr => new { pr, coverage = pr.PriceList.Keys.Count(k => requiredTypes.Contains(k)) })
                            .OrderByDescending(x => x.coverage)
                            .ThenBy(x => x.pr.Id)
                            .FirstOrDefault();

                        pricesForBill = best?.pr ?? mgr.prices.OrderBy(x => x.Id).First();
                        Console.WriteLine($"[WARN] Bill {b.Id}: PricesId {(b.PricesId == 0 ? "missing" : $"{b.PricesId} not found")} in snapshot. Selected Prices {pricesForBill.Id} by fallback.");
                    }
                    else
                    {
                        pricesForBill = new Prices(0, new Dictionary<ArticleType, double>());
                        Console.WriteLine($"[WARN] Bill {b.Id}: No Prices defined in snapshot. Using empty Prices.");
                    }
                }

                var bill = new Bill(b.Id, b.TotalPrice, ord, termsForBill, pricesForBill);
                mgr.bills.Add(bill);
            }

            // Wanted stock
            mgr.wantedStock.Clear();
            foreach (var kvp in snapshot.WantedStockByTypeId)
            {
                if (typeById.TryGetValue(kvp.Key, out var at))
                    mgr.wantedStock[at] = kvp.Value;
            }

            // Last IDs
            if (snapshot.LastIds != null)
            {
                mgr.lastStockId = snapshot.LastIds.LastStockId;
                mgr.lastSlotId = snapshot.LastIds.LastSlotId;
                mgr.lastArticleTypeId = snapshot.LastIds.LastArticleTypeId;
                mgr.lastOrderId = snapshot.LastIds.LastOrderId;
                mgr.lastSelfOrderId = snapshot.LastIds.LastSelfOrderId;
                mgr.lastPricesId = snapshot.LastIds.LastPricesId;
                mgr.lastBillId = snapshot.LastIds.LastBillId;
                mgr.lastPaymentTermsId = snapshot.LastIds.LastPaymentTermsId;
                mgr.lastSectionId = snapshot.LastIds.LastSectionId;
                mgr.lastEmployeeId = snapshot.LastIds.LastEmployeeId;
                mgr.lastCustomerId = snapshot.LastIds.LastCustomerId;
                lastOrderItemId = snapshot.LastIds.LastOrderItemId;
            }

            return mgr;
        }

        private static string MakeFileSafe(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Trim());
            foreach (var ch in invalid)
                sb.Replace(ch.ToString(), "");
            // Replace spaces with '-'
            return sb.ToString().Replace(' ', '-');
        }

        private ERPInstanceSnapshot BuildSnapshot()
        {
            // Maps for quick lookups
            var articleTypeIds = articleTypes.ToDictionary(t => t, t => t.Id);
            var articlesById = articles.ToDictionary(a => a.Id);

            // DTO conversions
            var dtoArticleTypes = articleTypes.Select(t => new DTO_ArticleType
            {
                Id = t.Id,
                Name = t.Name
            }).ToList();

            var dtoArticles = articles.Select(a => new DTO_Article
            {
                Id = a.Id,
                TypeId = a.Type.Id,
                Stock = a.Stock,
                ScannerId = a.ScannerId
            }).ToList();

            var dtoSlots = storageSlots.Select(s => new DTO_StorageSlot
            {
                Id = s.Id,
                FillArticleIds = s.Fill.Select(x => x.Id).ToList()
            }).ToList();

            DTO_OrderItem ToDtoOrderItem(OrderItem oi) => new DTO_OrderItem
            {
                Id = oi.Id,
                TypeId = oi.Type.Id,
                Stock = oi.Stock
            };

            var dtoOrders = orders.Select(o => new DTO_Order
            {
                Id = o.Id,
                CustomerId = o.Customer.Id,
                Status = o.Status.ToString(),
                Articles = o.Articles.Select(ToDtoOrderItem).ToList()
            }).ToList();

            var dtoSelfOrders = selfOrders.Select(so => new DTO_SelfOrder
            {
                Id = so.Id,
                Status = so.Status.ToString(),
                Articles = so.Articles.Select(ToDtoOrderItem).ToList(),
                Arrived = so.Arrived.Select(ToDtoOrderItem).ToList()
            }).ToList();

            // Ensure any Prices referenced by Bills are also saved, even if not present in the global prices list
            var allPrices = new List<Prices>(prices);
            foreach (var b in bills)
            {
                if (b?.Prices != null && !allPrices.Any(pr => pr.Id == b.Prices.Id))
                    allPrices.Add(b.Prices);
            }

            var dtoPrices = allPrices.Select(p => new DTO_Prices
            {
                Id = p.Id,
                PriceListByTypeId = p.PriceList.ToDictionary(kvp => kvp.Key.Id, kvp => kvp.Value)
            }).ToList();

            var dtoBills = bills.Select(b => new DTO_Bill
            {
                Id = b.Id,
                TotalPrice = b.TotalPrice,
                OrderId = b.Order.Id,
                CustomerId = b.Customer.Id,
                PaymentTermsId = b.PaymentTerms.Id,
                PricesId = b.Prices?.Id ?? 0
            }).ToList();

            var dtoPaymentTerms = paymentTerms.Select(pt => new DTO_PaymentTerms
            {
                Id = pt.Id,
                Name = pt.Name,
                DaysUntilDue = pt.DaysUntilDue,
                DiscountDays = pt.DiscountDays,
                DiscountPercent = pt.DiscountPercent,
                PenaltyRate = pt.PenaltyRate,
                AbsolutePenalty = pt.AbsolutePenalty,
                UsingPenaltyRate = pt.UsingPenaltyRate
            }).ToList();

            var dtoSections = sections.Select(s => new DTO_Section
            {
                Id = s.Id,
                Name = s.Name
            }).ToList();

            var dtoEmployees = employees.Select(e => new DTO_Employee
            {
                Id = e.Id,
                Name = e.Name ?? string.Empty,
                WorksInSectionId = e.worksIn.Id,
                Street = e.Information?.Address?.Street ?? string.Empty,
                City = e.Information?.Address?.City ?? string.Empty,
                PostalCode = e.Information?.Address?.PostalCode ?? string.Empty,
                Country = e.Information?.Address?.Country ?? string.Empty,
                Email = e.Information?.ContactInformation?.Email ?? string.Empty,
                PhoneNumber = e.Information?.ContactInformation?.PhoneNumber ?? string.Empty
            }).ToList();

            var dtoCustomers = customers.Select(c => new DTO_Customer
            {
                Id = c.Id,
                Name = c.Name ?? string.Empty,
                Street = c.Information?.Address?.Street ?? string.Empty,
                City = c.Information?.Address?.City ?? string.Empty,
                PostalCode = c.Information?.Address?.PostalCode ?? string.Empty,
                Country = c.Information?.Address?.Country ?? string.Empty,
                Email = c.Information?.ContactInformation?.Email ?? string.Empty,
                PhoneNumber = c.Information?.ContactInformation?.PhoneNumber ?? string.Empty
            }).ToList();

            var dtoWantedStock = wantedStock.ToDictionary(kvp => kvp.Key.Id, kvp => kvp.Value);

            return new ERPInstanceSnapshot
            {
                SchemaVersion = 5,
                InstanceName = this.InstanceName,
                FileBaseName = this.FileBaseName,
                OwnCapital = this.ownCapital,
                OwnCapitalSet = this.ownCapitalSet,
                WantedStockByTypeId = dtoWantedStock,

                LastIds = new DTO_LastIds
                {
                    LastStockId = lastStockId,
                    LastSlotId = lastSlotId,
                    LastArticleTypeId = lastArticleTypeId,
                    LastOrderId = lastOrderId,
                    LastSelfOrderId = lastSelfOrderId,
                    LastPricesId = lastPricesId,
                    LastBillId = lastBillId,
                    LastPaymentTermsId = lastPaymentTermsId,
                    LastSectionId = lastSectionId,
                    LastEmployeeId = lastEmployeeId,
                    LastCustomerId = lastCustomerId,
                    LastOrderItemId = lastOrderItemId
                },

                ArticleTypes = dtoArticleTypes,
                Articles = dtoArticles,
                StorageSlots = dtoSlots,
                Orders = dtoOrders,
                SelfOrders = dtoSelfOrders,
                Prices = dtoPrices,
                Bills = dtoBills,
                PaymentTerms = dtoPaymentTerms,
                Sections = dtoSections,
                Employees = dtoEmployees,
                Customers = dtoCustomers
            };
        }

        private ERPSecretsSnapshot BuildSecretsSnapshot()
        {
            DTO_Company? dtoCompany = null;
            if (company != null)
            {
                dtoCompany = new DTO_Company
                {
                    Name = company.Name ?? string.Empty,
                    Email = company.Email ?? string.Empty,
                    PhoneNumber = company.PhoneNumber ?? string.Empty,
                    Address = new DTO_Address
                    {
                        Street = company.Address?.Street ?? string.Empty,
                        City = company.Address?.City ?? string.Empty,
                        PostalCode = company.Address?.PostalCode ?? string.Empty,
                        Country = company.Address?.Country ?? string.Empty
                    },
                    BankInfo = new DTO_BankInformation
                    {
                        BankName = company.BankInfo?.BankName ?? string.Empty,
                        IBAN = company.BankInfo?.IBAN ?? string.Empty,
                        BIC = company.BankInfo?.BIC ?? string.Empty
                    }
                };
            }

            return new ERPSecretsSnapshot
            {
                SchemaVersion = 1,
                InstanceName = this.InstanceName,
                FileBaseName = this.FileBaseName,
                Warning = "TOP SECRET: This file contains sensitive company and bank information. Protect it and do NOT share or commit it.",
                Company = dtoCompany
            };
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
        public List<Article> GetAllArticles()
        {
            return articles;
        }
        public int GetArticleTypeCount()
        {
            return articleTypes.Count;
        }
        public List<ArticleType> GetAllArticleTypes()
        {
            return articleTypes;
        }
        public int GetStorageSlotCount()
        {
            return storageSlots.Count;
        }
        public List<StorageSlot> GetAllStorageSlots()
        {
            return storageSlots;
        }
        public int GetOrderCount()
        {
            return orders.Count;
        }
        public List<Order> GetAllOrders()
        {
            return orders;
        }
        public int GetSelfOrderCount()
        {
            return selfOrders.Count;
        }
        public List<SelfOrder> GetAllSelfOrders()
        {
            return selfOrders;
        }
        public int GetPricesCount()
        {
            return prices.Count;
        }
        public List<Prices> GetAllPrices()
        {
            return prices;
        }
        public int GetBillCount()
        {
            return bills.Count;
        }
        public List<Bill> GetAllBills()
        {
            return bills;
        }
        public int GetPaymentTermsCount()
        {
            return paymentTerms.Count;
        }
        public List<PaymentTerms> GetAllPaymentTerms()
        {
            return paymentTerms;
        }
        public int GetSectionCount()
        {
            return sections.Count;
        }
        public List<Section> GetAllSections()
        {
            return sections;
        }
        public int GetEmployeeCount()
        {
            return employees.Count;
        }
        public List<Employee> GetAllEmployees()
        {
            return employees;
        }
        public int GetCustomerCount()
        {
            return customers.Count;
        }
        public List<Customer> GetAllCustomers()
        {
            return customers;
        }

        // Warehousing
        public Article? FindArticle(int id)
        {
            return articles.FirstOrDefault(a => a.Id == id);
        }

        public Article? FindArticleByScannerId(long scannerId)
        {
            return articles.FirstOrDefault(a => a.ScannerId == scannerId);
        }

        public Section? FindSection(int id)
        {
            return sections.FirstOrDefault(s => s.Id == id);
        }

        public Customer? FindCustomer(int id)
        {
            return customers.FirstOrDefault(c => c.Id == id);
        }

        public List<Article> GetArticlesByType(ArticleType type)
        {
            return articles.Where(article => article.Type == type).ToList();
        }

        public ArticleType? FindArticleType(int id)
        {
            return articleTypes.FirstOrDefault(t => t.Id == id);
        }

        public StorageSlot? FindStorageSlot(ArticleSimilar article)
        {
            return storageSlots.FirstOrDefault(slot => slot.Fill.Contains(article));
        }

        private StorageSlot? FindStorageSlotById(int id)
        {
            return storageSlots.FirstOrDefault(t => t.Id == id);
        }

        public long GenerateScannerId()
        {
            Random rnd = new();
            long newId;

            do
            {
                newId = rnd.NextInt64(100000000000000, 999999999999999);
            }
            while (ScannerIds.Contains(newId));

            ScannerIds.Add(newId);
            return newId;
        }

        public ArticleType NewArticleType(string name)
        {
            ArticleType generated = new ArticleType(lastArticleTypeId + 1, name);

            articleTypes.Add(generated);
            lastArticleTypeId += 1;

            return generated;
        }

        public void DeleteArticleType(int id)
        {
            // Delegate to the overload for a single implementation path
            DeleteArticleType(FindArticleType(id));
        }

        public void DeleteArticleType(ArticleType? articleType)
        {
            if (articleType == null)
            {
                Console.WriteLine("[ERROR] Article type not found.");
                return;
            }

            // Safety: prevent deletion if the type is still referenced by Articles, Orders, or Prices
            bool referenced =
                articles.Any(a => a.Type.Id == articleType.Id)
                || orders.Any(o => o.Articles.Any(i => i.Type.Id == articleType.Id))
                || prices.Any(p => p.PriceList.Keys.Any(t => t.Id == articleType.Id));
            if (referenced)
            {
                Console.WriteLine($"[ERROR] Cannot delete ArticleType {articleType.Id} because it is referenced by Articles, Orders, or Prices.");
                return;
            }

            // Prefer removal by Id to avoid reference mismatch
            var toRemove = articleTypes.FirstOrDefault(t => t.Id == articleType.Id) ?? articleType;
            if (articleTypes.Remove(toRemove))
            {
                Console.WriteLine($"[INFO] Article type with ID {toRemove.Id} has been deleted.");
            }
            else
            {
                Console.WriteLine($"[ERROR] Article type with ID {articleType.Id} not found.");
            }
        }

        public StorageSlot NewStorageSlot()
        {
            StorageSlot generated = new StorageSlot(lastSlotId + 1, new List<Article>());

            storageSlots.Add(generated);
            lastSlotId += 1;

            return generated;
        }

        public void DeleteStorageSlot(int id)
        {
            var slot = storageSlots.FirstOrDefault(s => s.Id == id);
            DeleteStorageSlot(slot);
        }

        public void DeleteStorageSlot(StorageSlot? slot)
        {
            if (slot == null)
            {
                Console.WriteLine("[ERROR] Storage slot not found.");
                return;
            }
            if (storageSlots.Remove(slot))
            {
                Console.WriteLine($"[INFO] Storage slot with ID {slot.Id} has been deleted.");
            }
            else
            {
                Console.WriteLine($"[ERROR] Storage slot with ID {slot.Id} not found.");
            }
        }

        public Article NewArticle(int typeId, int stock, bool toList = true)
        {
            ArticleType? articleType = FindArticleType(typeId);
            if (articleType == null) // Ensure proper null checks
            {
                throw new ArgumentException($"Article type with ID {typeId} does not exist.");
            }

            long scannerId = GenerateScannerId();

            Article generated = new Article(lastStockId + 1, articleType, stock, scannerId);
            if (toList)
            {
                articles.Add(generated);
            }
            lastStockId += 1;

            return generated;
        }

        public void DeleteArticle(int id)
        {
            DeleteArticle(FindArticle(id));
        }

        public void DeleteArticle(Article? article)
        {
            if (article == null)
            {
                Console.WriteLine("[ERROR] Article not found.");
                return;
            }

            // Remove from any storage slots that contain this article
            foreach (var slot in storageSlots)
            {
                while (slot.Fill.Remove(article)) { }
            }

            if (articles.Remove(article))
            {
                Console.WriteLine($"[INFO] Article with ID {article.Id} has been deleted.");
            }
            else
            {
                Console.WriteLine($"[ERROR] Article with ID {article.Id} not found.");
            }
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

        public void DeleteOrder(int id)
        {
            var order = orders.FirstOrDefault(o => o.Id == id);
            DeleteOrder(order);
        }

        public void DeleteOrder(Order? order)
        {
            if (order == null)
            {
                Console.WriteLine("[ERROR] Order not found.");
                return;
            }

            // Safety: prevent deletion if referenced by a bill
            if (bills.Any(b => b.Order.Id == order.Id))
            {
                Console.WriteLine($"[ERROR] Cannot delete Order {order.Id} because it is referenced by a Bill.");
                return;
            }

            if (orders.Remove(order))
            {
                Console.WriteLine($"[INFO] Order with ID {order.Id} has been deleted.");
            }
            else
            {
                Console.WriteLine($"[ERROR] Order with ID {order.Id} not found.");
            }
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

        public void DeleteSelfOrder(int id)
        {
            var so = selfOrders.FirstOrDefault(s => s.Id == id);
            DeleteSelfOrder(so);
        }

        public void DeleteSelfOrder(SelfOrder? selfOrder)
        {
            if (selfOrder == null)
            {
                Console.WriteLine("[ERROR] Self order not found.");
                return;
            }
            if (selfOrders.Remove(selfOrder))
            {
                Console.WriteLine($"[INFO] Self order with ID {selfOrder.Id} has been deleted.");
            }
            else
            {
                Console.WriteLine($"[ERROR] Self order with ID {selfOrder.Id} not found.");
            }
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
        public Bill? NewBill(Order order, Prices prices, PaymentTerms terms)
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

            Bill generated = new Bill(lastBillId + 1, totalPrice, order, terms, prices);

            bills.Add(generated);
            lastBillId += 1;

            return generated;
        }

        public void DeleteBill(int id)
        {
            var bill = bills.FirstOrDefault(b => b.Id == id);
            DeleteBill(bill);
        }

        public void DeleteBill(Bill? bill)
        {
            if (bill == null)
            {
                Console.WriteLine("[ERROR] Bill not found.");
                return;
            }
            if (bills.Remove(bill))
            {
                Console.WriteLine($"[INFO] Bill with ID {bill.Id} has been deleted.");
            }
            else
            {
                Console.WriteLine($"[ERROR] Bill with ID {bill.Id} not found.");
            }
        }

        public double CalculateOrderTotal(Order order, Prices prices)
        {
            double total = 0;
            foreach (var item in order.Articles)
            {
                if (!prices.PriceList.TryGetValue(item.Type, out var unit))
                {
                    throw new InvalidOperationException($"No price found for ArticleType {item.Type.Name}.");
                }
                total += unit * item.Stock;
            }
            return Math.Round(total, 2);
        }

        public void ListBills()
        {
            Console.ForegroundColor = SECTION_INDICATOR_COLOR;
            Console.WriteLine("========= Bills =========");
            Console.ResetColor();
            foreach (Bill bill in bills)
            {
                Console.WriteLine($"ID: {bill.Id}, Total Price: {FormatAmount(bill.TotalPrice)}, From: {bill.Customer.Name}, Terms: {bill.PaymentTerms.Name}");

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

        public void DeletePrices(int id)
        {
            var prs = prices.FirstOrDefault(p => p.Id == id);
            DeletePrices(prs);
        }

        public void DeletePrices(Prices? pricesEntry)
        {
            if (pricesEntry == null)
            {
                Console.WriteLine("[ERROR] Prices entry not found.");
                return;
            }
            if (prices.Remove(pricesEntry))
            {
                Console.WriteLine($"[INFO] Prices with ID {pricesEntry.Id} has been deleted.");
            }
            else
            {
                Console.WriteLine($"[ERROR] Prices with ID {pricesEntry.Id} not found.");
            }
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
            lastPaymentTermsId += 1;

            return generated;
        }

        public void DeletePaymentTerms(int id)
        {
            var terms = paymentTerms.FirstOrDefault(t => t.Id == id);
            DeletePaymentTerms(terms);
        }

        public void DeletePaymentTerms(PaymentTerms? terms)
        {
            if (terms == null)
            {
                Console.WriteLine("[ERROR] Payment terms not found.");
                return;
            }
            // Safety: prevent deletion if referenced by bills
            if (bills.Any(b => b.PaymentTerms.Id == terms.Id))
            {
                Console.WriteLine($"[ERROR] Cannot delete PaymentTerms {terms.Id} because it is referenced by a Bill.");
                return;
            }
            if (paymentTerms.Remove(terms))
            {
                Console.WriteLine($"[INFO] Payment terms with ID {terms.Id} has been deleted.");
            }
            else
            {
                Console.WriteLine($"[ERROR] Payment terms with ID {terms.Id} not found.");
            }
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

        public void DeleteSection(int id)
        {
            var sec = sections.FirstOrDefault(s => s.Id == id);
            DeleteSection(sec);
        }

        public void DeleteSection(Section? section)
        {
            if (section == null)
            {
                Console.WriteLine("[ERROR] Section not found.");
                return;
            }
            // Safety: prevent deletion if referenced by employees
            if (employees.Any(e => e.worksIn.Id == section.Id))
            {
                Console.WriteLine($"[ERROR] Cannot delete Section {section.Id} because it is referenced by one or more Employees.");
                return;
            }
            if (sections.Remove(section))
            {
                Console.WriteLine($"[INFO] Section with ID {section.Id} has been deleted.");
            }
            else
            {
                Console.WriteLine($"[ERROR] Section with ID {section.Id} not found.");
            }
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
        public Employee NewEmployee(string name, Section worksIn, string Street, string City, string PostalCode, string Country, string Email, string PhoneNumber)
        {
            Employee generated = new Employee(lastEmployeeId + 1, name, worksIn, Street, City, PostalCode, Country, Email, PhoneNumber);

            employees.Add(generated);
            lastEmployeeId += 1;

            return generated;
        }

        public void DeleteEmployee(int id)
        {
            var emp = employees.FirstOrDefault(e => e.Id == id);
            DeleteEmployee(emp);
        }

        public void DeleteEmployee(Employee? employee)
        {
            if (employee == null)
            {
                Console.WriteLine("[ERROR] Employee not found.");
                return;
            }
            if (employees.Remove(employee))
            {
                Console.WriteLine($"[INFO] Employee with ID {employee.Id} has been deleted.");
            }
            else
            {
                Console.WriteLine($"[ERROR] Employee with ID {employee.Id} not found.");
            }
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
        public Customer NewCustomer(string name, string Street, string City, string PostalCode, string Country, string Email, string PhoneNumber)
        {
            Customer generated = new Customer(lastCustomerId + 1, name, Street, City, PostalCode, Country, Email, PhoneNumber);

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

        public void DeleteCustomer(int id)
        {
            var cust = customers.FirstOrDefault(c => c.Id == id);
            DeleteCustomer(cust);
        }

        public void DeleteCustomer(Customer? customer)
        {
            if (customer == null)
            {
                Console.WriteLine("[ERROR] Customer not found.");
                return;
            }
            // Safety: prevent deletion if referenced by orders or bills
            if (orders.Any(o => o.Customer.Id == customer.Id) || bills.Any(b => b.Customer.Id == customer.Id))
            {
                Console.WriteLine($"[ERROR] Cannot delete Customer {customer.Id} because it is referenced by Orders or Bills.");
                return;
            }
            if (customers.Remove(customer))
            {
                Console.WriteLine($"[INFO] Customer with ID {customer.Id} has been deleted.");
            }
            else
            {
                Console.WriteLine($"[ERROR] Customer with ID {customer.Id} not found.");
            }
        }
    }

    public class Company
    {
        public string Name { get; set; }
        public Address Address { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public BankInformation BankInfo { get; set; }

        public Company(string name, Address address, string email, string phoneNumber, BankInformation bankInfo)
        {
            Name = name;
            Address = address;
            Email = email;
            PhoneNumber = phoneNumber;
            BankInfo = bankInfo;
        }
    }

    public class BankInformation
    {
        public string BankName { get; set; }
        public string IBAN { get; set; }
        public string BIC { get; set; }

        public BankInformation(string bankName, string iban, string bic)
        {
            BankName = bankName;
            IBAN = iban;
            BIC = bic;
        }
    }

    // superior class
    public class ERPItem
    {

    }

    public class ArticleSimilar : ERPItem
    {

    }

    public class Article : ArticleSimilar
    {
        public int Id { get; }
        public long ScannerId { get; set; }
        public ArticleType Type { get; }
        public int Stock { get; set; }

        public Article(int id, ArticleType type, int stock, long scannerId)
        {
            Id = id;
            Type = type;
            Stock = stock;
            ScannerId = scannerId;
        }

        public string GenerateBarCode()
        {
            string name = Type.Name + "-" + ScannerId + ".png";

            BarcodeSettings bs = new BarcodeSettings();
            bs.Type = BarCodeType.Code128;
            bs.Data = ScannerId.ToString();

            BarCodeGenerator bg = new BarCodeGenerator(bs);
            if (OperatingSystem.IsWindows())
            {
                bg.GenerateImage().Save(name);
            }
            else
            {
                // On non-Windows platforms, System.Drawing may not be supported. Skip saving.
            }

            return name;
        }
    }

    public class ArticleType : ERPItem
    {
        public int Id { get; }
        public string Name { get; }

        public ArticleType(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public class StorageSlot : ERPItem
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

    public class Order : ERPItem
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

    public class SelfOrder : ERPItem
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

    public class Prices : ERPItem
    {
        public int Id { get; }
        public Dictionary<ArticleType, double> PriceList { get; }

        public Prices(int id, Dictionary<ArticleType, double> priceList)
        {
            Id = id;
            PriceList = priceList;
        }
    }

    public class Bill : ERPItem
    {
        public int Id { get; }
        public double TotalPrice { get; }
        public Order Order { get; }
        public Customer Customer { get; set; }
        public PaymentTerms PaymentTerms { get; set; }
        public Prices Prices { get; set; }
        public DateOnly BillDate { get; set; }

        public Bill(int id, double totalPrice, Order order, PaymentTerms paymentTerms, Prices prices)
        {
            Id = id;
            TotalPrice = totalPrice;
            Order = order;
            Customer = order.Customer;
            PaymentTerms = paymentTerms;
            BillDate = DateOnly.FromDateTime(DateTime.Now);
            Prices = prices;
        }
    }

    public class PaymentTerms : ERPItem
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

        public static DateOnly GetDueDate(DateOnly InvoiceDate, int DaysUntilDue)
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

    public class Section : ERPItem
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

    public abstract class Person : ERPItem
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public PersonType Type { get; set; }
        public PersonInformation? Information { get; set; }
    }

    public class Employee : Person
    {
        public Section worksIn { get; set; }

        public Employee(int id, string name, Section worksIn, string Street, string City, string PostalCode, string Country, string Email, string PhoneNumber)
        {
            Id = id;
            Name = name;
            Type = PersonType.Employee;
            this.worksIn = worksIn;

            Information = new PersonInformation(
                new Address(Street, City, PostalCode, Country),
                new ContactInformation(Email, PhoneNumber)
            );
        }
    }

    public class Customer : Person
    {
        public Customer(int id, string name, string Street, string City, string PostalCode, string Country, string Email, string PhoneNumber)
        {
            Id = id;
            Name = name;
            Type = PersonType.Customer;
            Information = new PersonInformation(
                new Address(Street, City, PostalCode, Country),
                new ContactInformation(Email, PhoneNumber)
            );
        }
    }

    public class PersonInformation : ERPItem
    {
        public Address Address { get; set; }
        public ContactInformation ContactInformation { get; set; }

        public PersonInformation(Address address, ContactInformation contactInformation)
        {
            Address = address;
            ContactInformation = contactInformation;
        }
    }

    public class Address : ERPItem
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }

        public Address(string street, string city, string postalCode, string country)
        {
            Street = street;
            City = city;
            PostalCode = postalCode;
            Country = country;
        }
    }

    public class ContactInformation : ERPItem
    {
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        public ContactInformation(string email, string phoneNumber)
        {
            Email = email;
            PhoneNumber = phoneNumber;
        }
    }

    // =========================
    // DTOs for .erp JSON format
    // =========================
    internal class ERPInstanceSnapshot
    {
        public int SchemaVersion { get; set; }
        public string InstanceName { get; set; } = string.Empty;
        public string FileBaseName { get; set; } = string.Empty;

        public double OwnCapital { get; set; }
        public bool OwnCapitalSet { get; set; }
        public Dictionary<int, int> WantedStockByTypeId { get; set; } = new();

        public DTO_LastIds LastIds { get; set; } = new DTO_LastIds();

        public List<DTO_ArticleType> ArticleTypes { get; set; } = new();
        public List<DTO_Article> Articles { get; set; } = new();
        public List<DTO_StorageSlot> StorageSlots { get; set; } = new();
        public List<DTO_Order> Orders { get; set; } = new();
        public List<DTO_SelfOrder> SelfOrders { get; set; } = new();
        public List<DTO_Prices> Prices { get; set; } = new();
        public List<DTO_Bill> Bills { get; set; } = new();
        public List<DTO_PaymentTerms> PaymentTerms { get; set; } = new();
        public List<DTO_Section> Sections { get; set; } = new();
        public List<DTO_Employee> Employees { get; set; } = new();
        public List<DTO_Customer> Customers { get; set; } = new();
    }

    // =========================
    // DTOs for .erps (secrets) JSON format
    // =========================
    internal class ERPSecretsSnapshot
    {
        public int SchemaVersion { get; set; }
        public string InstanceName { get; set; } = string.Empty;
        public string FileBaseName { get; set; } = string.Empty;
        public string Warning { get; set; } = string.Empty;
        public DTO_Company? Company { get; set; }
    }

    internal class DTO_Company
    {
        public string Name { get; set; } = string.Empty;
        public DTO_Address? Address { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DTO_BankInformation? BankInfo { get; set; }
    }

    internal class DTO_Address
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    internal class DTO_BankInformation
    {
        public string BankName { get; set; } = string.Empty;
        public string IBAN { get; set; } = string.Empty;
        public string BIC { get; set; } = string.Empty;
    }

    internal class DTO_LastIds
    {
        public int LastStockId { get; set; }
        public int LastSlotId { get; set; }
        public int LastArticleTypeId { get; set; }
        public int LastOrderId { get; set; }
        public int LastSelfOrderId { get; set; }
        public int LastPricesId { get; set; }
        public int LastBillId { get; set; }
        public int LastPaymentTermsId { get; set; }
        public int LastSectionId { get; set; }
        public int LastEmployeeId { get; set; }
        public int LastCustomerId { get; set; }
        public int LastOrderItemId { get; set; }
    }

    internal class DTO_ArticleType { public int Id { get; set; } public string Name { get; set; } = string.Empty; }
    internal class DTO_Article { public int Id { get; set; } public int TypeId { get; set; } public int Stock { get; set; } public long ScannerId { get; set; } }
    internal class DTO_StorageSlot { public int Id { get; set; } public List<int> FillArticleIds { get; set; } = new(); }
    internal class DTO_OrderItem { public int Id { get; set; } public int TypeId { get; set; } public int Stock { get; set; } }
    internal class DTO_Order { public int Id { get; set; } public int CustomerId { get; set; } public string Status { get; set; } = string.Empty; public List<DTO_OrderItem> Articles { get; set; } = new(); }
    internal class DTO_SelfOrder { public int Id { get; set; } public string Status { get; set; } = string.Empty; public List<DTO_OrderItem> Articles { get; set; } = new(); public List<DTO_OrderItem> Arrived { get; set; } = new(); }
    internal class DTO_Prices { public int Id { get; set; } public Dictionary<int, double> PriceListByTypeId { get; set; } = new(); }
    internal class DTO_Bill { public int Id { get; set; } public double TotalPrice { get; set; } public int OrderId { get; set; } public int CustomerId { get; set; } public int PaymentTermsId { get; set; } public int PricesId { get; set; } }
    internal class DTO_PaymentTerms { public int Id { get; set; } public string Name { get; set; } = string.Empty; public int DaysUntilDue { get; set; } public int? DiscountDays { get; set; } public double? DiscountPercent { get; set; } public double? PenaltyRate { get; set; } public double AbsolutePenalty { get; set; } public bool UsingPenaltyRate { get; set; } }
    internal class DTO_Section { public int Id { get; set; } public string Name { get; set; } = string.Empty; }
    internal class DTO_Employee { public int Id { get; set; } public string Name { get; set; } = string.Empty; public int WorksInSectionId { get; set; } public string Street { get; set; } = string.Empty; public string City { get; set; } = string.Empty; public string PostalCode { get; set; } = string.Empty; public string Country { get; set; } = string.Empty; public string Email { get; set; } = string.Empty; public string PhoneNumber { get; set; } = string.Empty; }
    internal class DTO_Customer { public int Id { get; set; } public string Name { get; set; } = string.Empty; public string Street { get; set; } = string.Empty; public string City { get; set; } = string.Empty; public string PostalCode { get; set; } = string.Empty; public string Country { get; set; } = string.Empty; public string Email { get; set; } = string.Empty; public string PhoneNumber { get; set; } = string.Empty; }

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
            Console.WriteLine($"Name: {sellerNode?.SelectSingleNode("ram:Name", nsMgr)?.InnerText}");
            Console.WriteLine($"Street: {sellerNode?.SelectSingleNode("ram:PostalTradeAddress/ram:LineOne", nsMgr)?.InnerText}");
            Console.WriteLine($"ZIP: {sellerNode?.SelectSingleNode("ram:PostalTradeAddress/ram:PostcodeCode", nsMgr)?.InnerText}");
            Console.WriteLine($"City: {sellerNode?.SelectSingleNode("ram:PostalTradeAddress/ram:CityName", nsMgr)?.InnerText}");
            Console.WriteLine($"Country: {sellerNode?.SelectSingleNode("ram:PostalTradeAddress/ram:CountryID", nsMgr)?.InnerText}");
            Console.WriteLine($"VAT ID: {sellerNode?.SelectSingleNode("ram:SpecifiedTaxRegistration/ram:ID", nsMgr)?.InnerText}");

            // Buyer
            Console.WriteLine("\n=== Buyer ===");
            var buyerNode = xmlDoc.SelectSingleNode("//ram:BuyerTradeParty", nsMgr);
            Console.WriteLine($"Name: {buyerNode?.SelectSingleNode("ram:Name", nsMgr)?.InnerText}");
            Console.WriteLine($"Street: {buyerNode?.SelectSingleNode("ram:PostalTradeAddress/ram:LineOne", nsMgr)?.InnerText}");
            Console.WriteLine($"ZIP: {buyerNode?.SelectSingleNode("ram:PostalTradeAddress/ram:PostcodeCode", nsMgr)?.InnerText}");
            Console.WriteLine($"City: {buyerNode?.SelectSingleNode("ram:PostalTradeAddress/ram:CityName", nsMgr)?.InnerText}");
            Console.WriteLine($"Country: {buyerNode?.SelectSingleNode("ram:PostalTradeAddress/ram:CountryID", nsMgr)?.InnerText}");

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
            if (taxNodes != null)
            {
                foreach (XmlNode tax in taxNodes)
                {
                    Console.WriteLine("- VAT Info:");
                    Console.WriteLine($"  Tax Amount: {tax.SelectSingleNode("ram:CalculatedAmount", nsMgr)?.InnerText}");
                    Console.WriteLine($"  Tax Type: {tax.SelectSingleNode("ram:TypeCode", nsMgr)?.InnerText}");
                    Console.WriteLine($"  Tax Basis: {tax.SelectSingleNode("ram:BasisAmount", nsMgr)?.InnerText}");
                    Console.WriteLine($"  Tax Rate: {tax.SelectSingleNode("ram:RateApplicablePercent", nsMgr)?.InnerText}%");
                }
            }

            // All positions
            Console.WriteLine("\n=== Line Items ===");
            var lineItems = xmlDoc.SelectNodes("//ram:IncludedSupplyChainTradeLineItem", nsMgr);
            if (lineItems != null)
            {
                foreach (XmlNode item in lineItems)
                {
                    var lineId = item.SelectSingleNode("ram:AssociatedDocumentLineDocument/ram:LineID", nsMgr);
                    var productName = item.SelectSingleNode("ram:SpecifiedTradeProduct/ram:Name", nsMgr);
                    var productDesc = item.SelectSingleNode("ram:SpecifiedTradeProduct/ram:Description", nsMgr);
                    var buyerAssignedId = item.SelectSingleNode("ram:SpecifiedTradeProduct/ram:BuyerAssignedID", nsMgr);
                    var quantity = item.SelectSingleNode("ram:SpecifiedLineTradeDelivery/ram:BilledQuantity", nsMgr);
                    var unitAttr = quantity?.Attributes?["unitCode"];
                    var unit = unitAttr != null ? unitAttr.Value : string.Empty;
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

    public class PDF
    {
        private static void Settings()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static void Bill(Company company, Bill bill)
        {
            Settings();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11));
                    page.Header().Row(header =>
                    {
                        // Left: Invoice title and company
                        header.RelativeItem().Column(col =>
                        {
                            col.Item().Text("RECHNUNG").FontSize(24).Bold().FontColor(Colors.Blue.Medium);
                            col.Item().Text(company.Name).Bold();
                            col.Item().Text($"{company.Address.Street}\n{company.Address.PostalCode} {company.Address.City}, {company.Address.Country}").FontSize(10);
                            col.Item().Text($"{company.Email}\n{company.PhoneNumber}");
                        });

                        // Right: Invoice metadata
                        header.ConstantItem(200).Column(col =>
                        {
                            col.Item().Text("Rechnungsnr: 0001").AlignRight();
                            col.Item().Text($"Datum: {bill.BillDate}").AlignRight();
                            col.Item().Text($"Kunde: {bill.Customer.Name}").AlignRight();
                            var addr = bill.Customer?.Information?.Address;
                            var addrText = addr != null
                                ? $"{addr.Street}\n{addr.PostalCode} {addr.City}, {addr.Country}"
                                : "";
                            col.Item().Text(addrText).AlignRight();
                        });
                    });

                    page.Content().PaddingVertical(20).Column(content =>
                    {
                        // Table header
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            // Header row
                            table.Cell().Element(CellStyleHeader).Text("Produkt").SemiBold();
                            table.Cell().Element(CellStyleHeader).Text("Anzahl").SemiBold();
                            table.Cell().Element(CellStyleHeader).Text("Stückpreis").SemiBold();
                            table.Cell().Element(CellStyleHeader).Text("Preis").SemiBold();

                            bill.Order.Articles.ForEach(article =>
                            {
                                var typeName = article.Type.Name;
                                var qty = article.Stock;
                                // Lookup unit price; fall back to 0.00 if not present
                                double unitPrice = 0.0;
                                if (bill.Prices?.PriceList != null && bill.Prices.PriceList.TryGetValue(article.Type, out var found))
                                    unitPrice = found;
                                var lineTotal = unitPrice * qty;

                                table.Cell().Element(CellStyle).Text(typeName);
                                table.Cell().Element(CellStyle).Text(qty.ToString());
                                table.Cell().Element(CellStyle).Text(unitPrice.ToString("C2", CultureInfo.CurrentCulture));
                                table.Cell().Element(CellStyle).Text(lineTotal.ToString("C2", CultureInfo.CurrentCulture));
                            });

                            // Total row
                            table.Cell().ColumnSpan(3).Element(CellStyle).AlignRight().Text("Gesamt:").SemiBold();
                            table.Cell().Element(CellStyle).Text(bill.TotalPrice.ToString("C2", CultureInfo.CurrentCulture)).SemiBold();
                        });

                        // Payment info & thanks
                        content.Item().PaddingTop(25).Text($"Bitte überweisen Sie den Betrag innerhalb der nächsten {bill.PaymentTerms.DaysUntilDue} Tage an uns:\nIBAN: {company.BankInfo.IBAN}\nBIC: {company.BankInfo.BIC}\nBank: {company.BankInfo.BankName}").FontSize(10);

                        content.Item().PaddingTop(10).Text("Vielen Dank für Ihren Auftrag!").Italic().FontColor(Colors.Grey.Darken2);
                    });

                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span($"{company.Name}\nErstellt mit ERP").FontSize(8);
                    });
                });
            })
            .GeneratePdf($"Bill-{((bill.Customer?.Name ?? "Customer").Replace(" ", ""))}-{bill.Id}.pdf");
        }

        static IContainer CellStyle(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).PaddingHorizontal(2);

        static IContainer CellStyleHeader(IContainer container) =>
        container.Background(Colors.Grey.Lighten4).PaddingVertical(5).PaddingHorizontal(2);
    }
}
