using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using ERP_Fix;

namespace ERP_Fix
{
    class Shell
    {
        public void Start()
        {
            Console.WriteLine("Welcome to my ERP System!");
            Console.WriteLine("Possible commands:");

            Console.WriteLine("");

            ConsoleColor color = ConsoleColor.Cyan;

            Console.ForegroundColor = color;
            Console.Write("create-storage-slot: ");
            Console.ResetColor();
            Console.WriteLine("Create a new storage slot");

            Console.ForegroundColor = color;
            Console.Write("list-storage-slots: ");
            Console.ResetColor();
            Console.WriteLine("List storage slots");

            Console.ForegroundColor = color;
            Console.Write("create-article-type: ");
            Console.ResetColor();
            Console.WriteLine("Create a new article type");

            Console.ForegroundColor = color;
            Console.Write("create-article: ");
            Console.ResetColor();
            Console.WriteLine("Create a new article");

            Console.ForegroundColor = color;
            Console.Write("sort-article: ");
            Console.ResetColor();
            Console.WriteLine("Sort an article");

            Console.ForegroundColor = color;
            Console.Write("display-inventory: ");
            Console.ResetColor();
            Console.WriteLine("Display inventory (List articles)");

            Console.ForegroundColor = color;
            Console.Write("create-order: ");
            Console.ResetColor();
            Console.WriteLine("Create a new order");

            Console.ForegroundColor = color;
            Console.Write("list-orders: ");
            Console.ResetColor();
            Console.WriteLine("List orders");

            Console.ForegroundColor = color;
            Console.Write("create-price-list: ");
            Console.ResetColor();
            Console.WriteLine("Create new price list");

            Console.ForegroundColor = color;
            Console.Write("list-price-lists: ");
            Console.ResetColor();
            Console.WriteLine("List price lists");

            Console.ForegroundColor = color;
            Console.Write("create-bill: ");
            Console.ResetColor();
            Console.WriteLine("Create a new bill from order");

            Console.ForegroundColor = color;
            Console.Write("list-bills: ");
            Console.ResetColor();
            Console.WriteLine("List bills");

            Console.ForegroundColor = color;
            Console.Write("exit: ");
            Console.ResetColor();
            Console.WriteLine("Exit the ERP");

            Console.WriteLine("");
            Console.WriteLine("<END-OF-OUTPUT>");

            // Dictionaries
            Dictionary<int, StorageSlot> storageSlots = new Dictionary<int, StorageSlot>();
            NameCollection<int, string, ArticleType> articleTypes = new NameCollection<int, string, ArticleType>();
            Dictionary<int, Article> articles = new Dictionary<int, Article>();
            Dictionary<int, Order> orders = new Dictionary<int, Order>();
            Dictionary<int, Bill> bills = new Dictionary<int, Bill>();
            Dictionary<int, Prices> prices = new Dictionary<int, Prices>();

            Program program = new Program();

            while (true)
            {
                string? inputLine = Console.ReadLine();
                if (inputLine == null) break;
                string[] inputParts = inputLine.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                string command = inputParts[0].ToLower();
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                if (inputParts.Length > 1)
                {
                    var paramPairs = inputParts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var pair in paramPairs)
                    {
                        var kv = pair.Split('=', 2);
                        if (kv.Length == 2)
                            parameters[kv[0].ToLower()] = kv[1];
                    }
                }

                if (command == "create-storage-slot")
                {
                    Console.WriteLine("You chose: Create a new storage slot");
                    StorageSlot newStorageSlot = program.NewStorageSlot();
                    storageSlots.Add(newStorageSlot.Id, newStorageSlot);
                    Console.WriteLine($"Your new Storage slot is accessible as S[{newStorageSlot.Id}]");
                    Console.WriteLine("<END-OF-OUTPUT>");
                }
                else if (command == "list-storage-slots")
                {
                    Console.WriteLine("You chose: List storage slots");
                    program.ListStorageSlots();
                    Console.WriteLine("<END-OF-OUTPUT>");
                }
                else if (command == "create-article-type")
                {
                    Console.WriteLine("You chose: Create a new article type");
                    string typeName;
                    if (!parameters.TryGetValue("name", out typeName) || string.IsNullOrWhiteSpace(typeName))
                    {
                        Console.Write("Enter the name of the new article type: ");
                        typeName = Console.ReadLine();
                    }
                    if (string.IsNullOrWhiteSpace(typeName))
                    {
                        Console.WriteLine("Invalid input. Please enter a valid article type name.");
                        Console.WriteLine("<END-OF-OUTPUT>");
                        continue;
                    }
                    ArticleType newArticleType = program.NewArticleType(typeName);
                    try
                    {
                        articleTypes.Add(newArticleType.Id, newArticleType.Name, newArticleType);
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        Console.WriteLine("<END-OF-OUTPUT>");
                        continue;
                    }
                    Console.WriteLine($"Your new Article type is accessible as AT[{newArticleType.Id}] or AT[{newArticleType.Name}]");
                    Console.WriteLine("<END-OF-OUTPUT>");
                }
                else if (command == "create-article")
                {
                    CreateNewArticle(program, articleTypes, articles, parameters);
                    Console.WriteLine("<END-OF-OUTPUT>");
                }
                else if (command == "sort-article")
                {
                    Console.WriteLine("You chose: Sort an article");
                    string articleInput;
                    if (!parameters.TryGetValue("article", out articleInput) || string.IsNullOrWhiteSpace(articleInput))
                    {
                        Console.Write("Enter the article to sort (A[id]): ");
                        articleInput = Console.ReadLine();
                    }
                    Match match = Regex.Match(articleInput, @"^A\[(.*)\]$");
                    if (!match.Success)
                    {
                        Console.WriteLine("Invalid article format. Please use A[id].");
                        Console.WriteLine("<END-OF-OUTPUT>");
                        continue;
                    }
                    string innerContent = match.Groups[1].Value;
                    Article articleToSort = null;
                    if (int.TryParse(innerContent, out int id))
                    {
                        if (!articles.ContainsKey(id))
                        {
                            Console.WriteLine("Invalid article id. Please try again.");
                            Console.WriteLine("<END-OF-OUTPUT>");
                            continue;
                        }
                        articleToSort = articles[id];
                    }
                    else
                    {
                        Console.WriteLine("Invalid input. Please try again.");
                        Console.WriteLine("<END-OF-OUTPUT>");
                        continue;
                    }
                    string slotInput;
                    if (!parameters.TryGetValue("slot", out slotInput) || string.IsNullOrWhiteSpace(slotInput))
                    {
                        Console.Write("Enter the storage slot to sort into (S[id]): ");
                        slotInput = Console.ReadLine();
                    }
                    Match matchX = Regex.Match(slotInput, @"^S\[(.*)\]$");
                    if (!matchX.Success)
                    {
                        Console.WriteLine("Invalid storage slot format. Please use S[id].");
                        Console.WriteLine("<END-OF-OUTPUT>");
                        continue;
                    }
                    string innerContentX = matchX.Groups[1].Value;
                    StorageSlot storageSlotToSortIn = null;
                    if (int.TryParse(innerContentX, out int idX))
                    {
                        if (!storageSlots.ContainsKey(idX))
                        {
                            Console.WriteLine("Invalid storage slot id. Please try again.");
                            Console.WriteLine("<END-OF-OUTPUT>");
                            continue;
                        }
                        storageSlotToSortIn = storageSlots[idX];
                    }
                    else
                    {
                        Console.WriteLine("Invalid input. Please try again.");
                        Console.WriteLine("<END-OF-OUTPUT>");
                        continue;
                    }
                    program.SortArticle(articleToSort.Id, storageSlotToSortIn.Id);
                    Console.WriteLine($"Article {articleToSort.Id} sorted into storage slot {storageSlotToSortIn.Id}.");
                    Console.WriteLine("<END-OF-OUTPUT>");
                }
                else if (command == "display-inventory")
                {
                    Console.WriteLine("You chose: Display inventory (List articles)");
                    program.DisplayInventory();
                    Console.WriteLine("<END-OF-OUTPUT>");
                }
                else if (command == "create-order")
                {
                    Console.WriteLine("You chose: Create a new order");
                    if (articleTypes.Count == 0)
                    {
                        Console.WriteLine("No article types available. Please create an article type first.");
                        Console.WriteLine("<END-OF-OUTPUT>");
                        continue;
                    }
                    string orderInput;
                    if (!parameters.TryGetValue("order", out orderInput) || string.IsNullOrWhiteSpace(orderInput))
                    {
                        Console.Write("Enter the articles to order (OI[AT[id], amount], semicolon separated): ");
                        orderInput = Console.ReadLine();
                    }
                    string[] articleIds = orderInput.Split(';');
                    List<OrderItem> articlesToOrder = new List<OrderItem>();
                    foreach (string articleId in articleIds)
                    {
                        Match match = Regex.Match(articleId.Trim(), @"^OI\[AT\[(\d+)\],\s*(\d+)\]$");
                        if (!match.Success)
                        {
                            Console.WriteLine($"Invalid order format: {articleId}. Please use OI[AT[id], amount].");
                        }
                        else
                        {
                            string typeIdStr = match.Groups[1].Value;
                            string amountStr = match.Groups[2].Value;
                            if (int.TryParse(typeIdStr, out int typeId) && int.TryParse(amountStr, out int amount))
                            {
                                ArticleType? articleType = articleTypes.GetById(typeId)?.Value;
                                if (articleType == null)
                                {
                                    Console.WriteLine($"ArticleType with id {typeId} does not exist.");
                                }
                                else
                                {
                                    OrderItem article = program.NewOrderItem(typeId, amount);
                                    articlesToOrder.Add(article);
                                    Console.WriteLine($"Added ArticleType {articleType.Name} (ID {typeId}) with amount {amount} to order.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Invalid numbers in input. Please try again.");
                            }
                        }
                    }
                    if (articlesToOrder.Count == 0)
                    {
                        Console.WriteLine("No valid articles to order. Please try again.");
                        Console.WriteLine("<END-OF-OUTPUT>");
                        continue;
                    }
                    Order newOrder = program.NewOrder(articlesToOrder, null);
                    orders.Add(newOrder.Id, newOrder);
                    Console.WriteLine($"Your new order is accessible as O[{newOrder.Id}]");
                    Console.WriteLine("<END-OF-OUTPUT>");
                }
                else if (command == "list-orders")
                {
                    Console.WriteLine("You chose: List orders");
                    program.ListOrders();
                    Console.WriteLine("<END-OF-OUTPUT>");
                }
                else if (command == "create-price-list")
                {
                    Console.WriteLine("You chose: Create new price list");
                    if (articleTypes.Count == 0)
                    {
                        Console.WriteLine("No article types available. Please create an article type first.");
                        Console.WriteLine("<END-OF-OUTPUT>");
                        continue;
                    }
                    Dictionary<ArticleType, double> pricesHere = new Dictionary<ArticleType, double>();
                    foreach (var articleType in articleTypes.GetAll())
                    {
                        string priceInput;
                        if (!parameters.TryGetValue($"price_{articleType.Id}", out priceInput) || string.IsNullOrWhiteSpace(priceInput))
                        {
                            Console.Write($"Enter the price for {articleType.Name} (AT[{articleType.Id}]): ");
                            priceInput = Console.ReadLine();
                        }
                        if (double.TryParse(priceInput, out double price) && price >= 0)
                        {
                            pricesHere.Add(articleType.Value, price);
                        }
                        else
                        {
                            Console.WriteLine("Invalid price. Please enter a non-negative number.");
                        }
                    }
                    Prices newPrices = program.NewPrices(pricesHere);
                    prices.Add(newPrices.Id, newPrices);
                    Console.WriteLine($"Your new price list is accessible as P[{newPrices.Id}]");
                    Console.WriteLine("<END-OF-OUTPUT>");
                }
                else if (command == "list-price-lists")
                {
                    Console.WriteLine("You chose: List price lists");
                    program.ListPrices();
                    Console.WriteLine("<END-OF-OUTPUT>");
                }
                else if (command == "create-bill")
                {
                    Console.WriteLine("You chose: Create a new bill from order");
                    if (orders.Count == 0)
                    {
                        Console.WriteLine("No orders available. Please create an order first.");
                        Console.WriteLine("<END-OF-OUTPUT>");
                        continue;
                    }
                    string orderInput;
                    if (!parameters.TryGetValue("order", out orderInput) || string.IsNullOrWhiteSpace(orderInput))
                    {
                        Console.Write("Enter the order id to create a bill from (O[id]): ");
                        orderInput = Console.ReadLine();
                    }
                    Match match = Regex.Match(orderInput, @"^O\[(.*)\]$");
                    if (!match.Success)
                    {
                        Console.WriteLine("Invalid order format. Please use O[id].");
                        Console.WriteLine("<END-OF-OUTPUT>");
                        continue;
                    }
                    Order orderToBill = null;
                    string innerContent = match.Groups[1].Value;
                    if (int.TryParse(innerContent, out int orderId))
                    {
                        if (!orders.ContainsKey(orderId))
                        {
                            Console.WriteLine("Invalid order id. Please try again.");
                            Console.WriteLine("<END-OF-OUTPUT>");
                            continue;
                        }
                        orderToBill = orders[orderId];
                    }
                    else
                    {
                        Console.WriteLine("Invalid input. Please try again.");
                        Console.WriteLine("<END-OF-OUTPUT>");
                        continue;
                    }
                    if (prices.Count == 0)
                    {
                        Console.WriteLine("No price lists available. Please create a price list first.");
                        Console.WriteLine("<END-OF-OUTPUT>");
                        continue;
                    }
                    string priceInput;
                    if (!parameters.TryGetValue("price", out priceInput) || string.IsNullOrWhiteSpace(priceInput))
                    {
                        Console.Write("Enter the price list to use (P[id]): ");
                        priceInput = Console.ReadLine();
                    }
                    Match priceMatch = Regex.Match(priceInput, @"^P\[(.*)\]$");
                    if (!priceMatch.Success)
                    {
                        Console.WriteLine("Invalid price list format. Please use P[id].");
                        Console.WriteLine("<END-OF-OUTPUT>");
                        continue;
                    }
                    Prices pricesToUse = null;
                    string innerContentPrice = priceMatch.Groups[1].Value;
                    if (int.TryParse(innerContentPrice, out int priceId))
                    {
                        if (!prices.ContainsKey(priceId))
                        {
                            Console.WriteLine("Invalid price list id. Please try again.");
                            Console.WriteLine("<END-OF-OUTPUT>");
                            continue;
                        }
                        pricesToUse = prices[priceId];
                    }
                    else
                    {
                        Console.WriteLine("Invalid input. Please try again.");
                        Console.WriteLine("<END-OF-OUTPUT>");
                        continue;
                    }
                    Bill newBill = program.NewBill(orderToBill, pricesToUse);
                    bills.Add(newBill.Id, newBill);
                    Console.WriteLine($"Bill created for order {orderId}.");
                    Console.WriteLine("<END-OF-OUTPUT>");
                }
                else if (command == "list-bills")
                {
                    Console.WriteLine("You chose: List bills");
                    program.ListBills();
                    Console.WriteLine("<END-OF-OUTPUT>");
                }
                else if (command == "exit")
                {
                    Console.WriteLine("Exiting the ERP...");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Goodbye");
                    Console.ResetColor();
                    Console.WriteLine("<END-OF-OUTPUT>");
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid input. Please try again.");
                    Console.WriteLine("<END-OF-OUTPUT>");
                }
            }
        }

        private void CreateNewArticle(
    Program program,
    NameCollection<int, string, ArticleType> articleTypes,
    Dictionary<int, Article> articles,
    Dictionary<string, string> parameters = null)
        {
            Console.WriteLine("You chose: Create a new article");
            if (articleTypes.Count == 0)
            {
                Console.WriteLine("No article types available. Please create an article type first.");
                Console.WriteLine("<END-OF-OUTPUT>");
                return;
            }

            string typeInput;
            if (parameters != null && parameters.TryGetValue("type", out typeInput) && !string.IsNullOrWhiteSpace(typeInput))
            {
                // nothing to do, typeInput is set
            }
            else
            {
                Console.Write("Enter the article type: ");
                typeInput = Console.ReadLine();
            }

            Match match = Regex.Match(typeInput, @"^AT\[(.*)\]$");
            if (!match.Success)
            {
                Console.WriteLine("Invalid article type. Please try again and use AT[id] or AT[name]");
                Console.WriteLine("<END-OF-OUTPUT>");
                return;
            }

            ArticleType wantedArticleType = null;
            string innerContent = match.Groups[1].Value;
            if (int.TryParse(innerContent, out int id))
            {
                if (!articleTypes.ContainsId(id))
                {
                    Console.WriteLine("Invalid article type. Please try again.");
                    Console.WriteLine("<END-OF-OUTPUT>");
                    return;
                }
                else
                {
                    wantedArticleType = articleTypes.GetById(id).Value;
                }
            }
            else
            {
                if (!articleTypes.ContainsName(innerContent))
                {
                    Console.WriteLine("Invalid article type. Please try again.");
                    Console.WriteLine("<END-OF-OUTPUT>");
                    return;
                }
                else
                {
                    wantedArticleType = articleTypes.GetByName(innerContent).Value;
                }
            }

            string amountInput;
            if (parameters != null && parameters.TryGetValue("amount", out amountInput) && !string.IsNullOrWhiteSpace(amountInput))
            {
                // nothing to do, amountInput is set
            }
            else
            {
                Console.Write("Enter the amount of the new article: ");
                amountInput = Console.ReadLine();
            }

            if (!int.TryParse(amountInput, out int amount) || amount < 0)
            {
                Console.WriteLine("Invalid amount. Please enter a positive integer.");
                Console.WriteLine("<END-OF-OUTPUT>");
                return;
            }
            Article newArticle = program.NewArticle(wantedArticleType.Id, amount);
            articles.Add(newArticle.Id, newArticle);
            Console.WriteLine($"Your new Article is accessible as A[{newArticle.Id}]");
            Console.WriteLine("<END-OF-OUTPUT>");
        }
    }
}
