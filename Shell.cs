using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using ERP_Fix;
using System.Data.Common;
using System.Drawing;

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

            ERPManager program = new ERPManager();

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
    ERPManager program,
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

    class NewShell
    {
        ERPManager erpManager = new();
        Dictionary<int, ArticleType> articleTypes = new();
        Dictionary<int, StorageSlot> storageSlots = new();
        Dictionary<int, Article> articles = new();
        Dictionary<int, Order> orders = new();
        Dictionary<int, Bill> bills = new();
        Dictionary<int, SelfOrder> selfOrders = new();
        Dictionary<int, Customer> customers = new();
        Dictionary<int, Prices> prices = new();

        public void Start()
        {
            // Print out "ERP Shell" text
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("  _____ ____  ____    ____  _          _ _ ");
            Console.WriteLine(" | ____|  _ \\|  _ \\  / ___|| |__   ___| | |");
            Console.WriteLine(" |  _| | |_) | |_) | \\___ \\| '_ \\ / _ \\ | |");
            Console.WriteLine(" | |___|  _ <|  __/   ___) | | | |  __/ | |");
            Console.WriteLine(" |_____|_| \\_\\_|     |____/|_| |_|\\___|_|_|");
            Console.ResetColor();
            Console.WriteLine("");
            // credits
            if (!Code.HideCredits)
            {
                Console.WriteLine("© 2025 Lukas Grambs");
            }
            // Auto help menu
            HelpMenu();
            // main loop
            while (true)
            {
                // Show input indicator
                Console.Write(">> ");
                string? command = Console.ReadLine();
                if (command == null || command == "")
                {
                    ShellAssistance.Error("Enter a valid command");
                }
                else if (command.StartsWith("help"))
                {
                    HelpMenu();
                }
                else if (command.StartsWith("create"))
                {
                    bool preChoice = false;
                    string? choiceStr = "";

                    string[] commandSplit = command.Split(' ');
                    if (commandSplit.Length > 1)
                    {
                        if (int.TryParse(commandSplit[1], out _))
                        {
                            preChoice = true;
                            choiceStr = commandSplit[1];
                        }
                    }

                    if (!preChoice)
                    {
                        Console.WriteLine("Which type of element do you want to create?");
                        Console.WriteLine("Possible element types:\n   1) article-type\n   2) storage-slot\n   3) article\n   4) order\n   5) self-order\n   6) bill\n   7) price-list\n   8) payment-terms\n   9) section\n   10) employee\n   11) customer");
                        Console.Write("create >> ");
                        choiceStr = Console.ReadLine();
                    }

                    if (int.TryParse(choiceStr, out int choice))
                    {
                        if (choice == 1) // article-type
                        {
                            Console.WriteLine("Enter the name for the article type");
                            Console.Write("create >> ");
                            string? name = Console.ReadLine();
                            if (name == null || name == "")
                            {
                                ShellAssistance.Error("Enter a valid name");
                                continue;
                            }

                            ArticleType newArticleType = erpManager.NewArticleType(name);
                            ShellAssistance.Success($"Your new article type ({newArticleType.Name}) is accessible as AT-{newArticleType.Id}");

                            articleTypes.Add(newArticleType.Id, newArticleType);
                        }
                        else if (choice == 2) // storage-slot
                        {
                            StorageSlot newStorageSlot = erpManager.NewStorageSlot();
                            ShellAssistance.Success($"Your new storage slot is accessible as S-{newStorageSlot.Id}");

                            storageSlots.Add(newStorageSlot.Id, newStorageSlot);
                        }
                        else if (choice == 3) // article
                        {
                            if (erpManager.GetArticleTypeCount() == 0)
                            {
                                ShellAssistance.Error("Create an article type first");
                                continue;
                            }
                            Console.WriteLine("Enter the article type (e.g. AT-0)");
                            Console.Write("create >> ");
                            string? atChoiceStr = Console.ReadLine();
                            if (!atChoiceStr.StartsWith("AT-"))
                            {
                                ShellAssistance.Error("Enter a valid article type");
                                continue;
                            }
                            string? isolatedAtChoiceStr = Regex.Replace(atChoiceStr, @"^AT-", "");

                            if (int.TryParse(isolatedAtChoiceStr, out int atChoice))
                            {
                                Console.WriteLine("Enter the stock of the article");
                                Console.Write("create >> ");
                                string? stockChoiceStr = Console.ReadLine();

                                if (int.TryParse(stockChoiceStr, out int stockChoice))
                                {
                                    Article newArticle = erpManager.NewArticle(atChoice, stockChoice);
                                    ShellAssistance.Success($"Your new article is accessible as A-{newArticle.Id}");
                                    articles.Add(newArticle.Id, newArticle);
                                }
                                else
                                {
                                    ShellAssistance.Error("Enter a valid stock as number");
                                }
                            }
                            else
                            {
                                ShellAssistance.Error("Enter a valid article type");
                            }
                        }
                        else if (choice == 4) // order
                        {
                            if (erpManager.GetArticleTypeCount() == 0)
                            {
                                ShellAssistance.Error("Create an article type first");
                                continue;
                            }

                            if (erpManager.GetCustomerCount() == 0)
                            {
                                ShellAssistance.Error("Create a customer first");
                                continue;
                            }

                            Console.WriteLine("Enter the Items you want to add to the order");
                            Console.WriteLine("Enter in format AT-0;AT-1...");
                            Console.Write("create >> ");
                            string? atChoiceStr = Console.ReadLine();
                            string[] atChoices = atChoiceStr.Split(';');

                            List<OrderItem> orderItems = new();
                            foreach (string atChoice in atChoices)
                            {
                                if (!atChoice.StartsWith("AT-"))
                                {
                                    ShellAssistance.Error($"Enter a valid article type instead of {atChoice}");
                                    continue;
                                }
                                string? isolatedAtChoiceStr = Regex.Replace(atChoice, @"^AT-", "");

                                if (int.TryParse(isolatedAtChoiceStr, out int atChoiceX))
                                {
                                    if (!articleTypes.TryGetValue(atChoiceX, out _))
                                    {
                                        ShellAssistance.Error($"Article type {atChoice} not found");
                                        continue;
                                    }

                                    Console.WriteLine($"Enter stock for order item with article type {atChoice}");
                                    Console.Write("create >> ");
                                    string? stockChoiceStr = Console.ReadLine();

                                    if (int.TryParse(stockChoiceStr, out int stockChoice))
                                    {
                                        orderItems.Add(erpManager.NewOrderItem(atChoiceX, stockChoice));
                                        ShellAssistance.Success($"Order item ({atChoice}, stock {stockChoice}) added to order.");
                                    }
                                }
                                else
                                {
                                    ShellAssistance.Error($"Invalid: Enter e.g. AT-0");
                                }
                            }

                            Console.WriteLine("Enter customer for order");
                            Console.Write("create >> ");
                            string? customerChoiceStr = Console.ReadLine();
                            if (!customerChoiceStr.StartsWith("C-"))
                            {
                                ShellAssistance.Error($"Enter a valid customer");
                                continue;
                            }
                            string isolatedCustomerChoiceStr = Regex.Replace(customerChoiceStr, @"^C-", "");
                            if (int.TryParse(isolatedCustomerChoiceStr, out int customerChoice))
                            {
                                if (customers.TryGetValue(customerChoice, out Customer customer))
                                {
                                    Order newOrder = erpManager.NewOrder(orderItems, customer);
                                    orders.Add(newOrder.Id, newOrder);
                                    ShellAssistance.Success($"Your new order is accessible as O-{newOrder.Id}");
                                }
                            }
                        }
                        else if (choice == 5) // self order
                        {
                            if (erpManager.GetArticleTypeCount() == 0)
                            {
                                ShellAssistance.Error("Create an article type first");
                                continue;
                            }

                            Console.WriteLine("Enter the Items you want to add to the order");
                            Console.WriteLine("Enter in format AT-0;AT-1...");
                            Console.Write("create >> ");
                            string? atChoiceStr = Console.ReadLine();
                            string[] atChoices = atChoiceStr.Split(';');

                            List<OrderItem> orderItems = new();
                            foreach (string atChoice in atChoices)
                            {
                                if (!atChoice.StartsWith("AT-"))
                                {
                                    ShellAssistance.Error($"Enter a valid article type instead of {atChoice}");
                                    continue;
                                }
                                string? isolatedAtChoiceStr = Regex.Replace(atChoice, @"^AT-", "");

                                if (int.TryParse(isolatedAtChoiceStr, out int atChoiceX))
                                {
                                    if (!articleTypes.TryGetValue(atChoiceX, out _))
                                    {
                                        ShellAssistance.Error($"Article type {atChoice} not found");
                                        continue;
                                    }

                                    Console.WriteLine($"Enter stock for order item with article type {atChoice}");
                                    Console.Write("create >> ");
                                    string? stockChoiceStr = Console.ReadLine();

                                    if (int.TryParse(stockChoiceStr, out int stockChoice))
                                    {
                                        orderItems.Add(erpManager.NewOrderItem(atChoiceX, stockChoice));
                                        ShellAssistance.Success($"Order item ({atChoice}, stock {stockChoice}) added to order.");
                                    }
                                }
                                else
                                {
                                    ShellAssistance.Error($"Invalid: Enter e.g. AT-0");
                                }
                            }

                            SelfOrder newSelfOrder = erpManager.NewSelfOrder(orderItems);
                            selfOrders.Add(newSelfOrder.Id, newSelfOrder);
                            ShellAssistance.Success($"Your new selforder is accessible as SO-{newSelfOrder.Id}");
                        }
                        else if (choice == 6) // bill
                        {
                            if (erpManager.GetOrderCount() == 0)
                            {
                                ShellAssistance.Error("Create an order first");
                                continue;
                            }

                            if (erpManager.GetPricesCount() == 0)
                            {
                                ShellAssistance.Error("Create a price list first");
                                continue;
                            }

                            Order orderToUse;
                            Console.WriteLine("Enter the order to use");
                            Console.Write("create >> ");
                            string? orderChoiceStr = Console.ReadLine();
                            if (!orderChoiceStr.StartsWith("O-"))
                            {
                                ShellAssistance.Error($"Enter a valid order");
                                continue;
                            }
                            string isolatedOrderChoiceStr = Regex.Replace(orderChoiceStr, @"^O-", "");
                            if (int.TryParse(isolatedOrderChoiceStr, out int orderChoice))
                            {
                                if (orders.TryGetValue(orderChoice, out Order order))
                                {
                                    orderToUse = order;
                                }
                                else
                                {
                                    orderToUse = null;
                                }
                            }
                            else
                            {
                                orderToUse = null;
                            }

                            Console.WriteLine("Enter the price list to use");
                            Console.Write("create >> ");
                            string? pricesChoiceStr = Console.ReadLine();
                            if (!pricesChoiceStr.StartsWith("O-"))
                            {
                                ShellAssistance.Error($"Enter a valid order");
                                continue;
                            }
                            string isolatedPricesChoiceStr = Regex.Replace(pricesChoiceStr, @"^O-", "");
                            if (int.TryParse(isolatedPricesChoiceStr, out int pricesChoice))
                            {
                                if (prices.TryGetValue(pricesChoice, out Prices priceList))
                                {
                                    Bill newBill = erpManager.NewBill(orderToUse, priceList);
                                    bills.Add(newBill.Id, newBill);
                                    ShellAssistance.Success($"Your new bill ist accessible as B-{newBill.Id}");
                                }
                            }
                        }
                        else if (choice > 6 && choice <= 11)
                        {
                            throw new NotImplementedException();
                        }
                        else
                        {
                            Console.WriteLine("Invalid choice");
                        }
                    }
                    else
                    {
                        ShellAssistance.Error("Enter a valid option as number");
                    }
                }
                else if (command.StartsWith("list"))
                {
                    bool preChoice = false;
                    string? choiceStr = "";

                    string[] commandSplit = command.Split(' ');
                    if (commandSplit.Length > 1)
                    {
                        if (int.TryParse(commandSplit[1], out _))
                        {
                            preChoice = true;
                            choiceStr = commandSplit[1];
                        }
                    }

                    if (!preChoice)
                    {
                        Console.WriteLine("What to you want to list?");
                        Console.WriteLine("Possible element types:\n   1) articles\n   2) storage slots\n   3) orders\n   4) self-orders\n   5) bills\n   6) price lists\n   7) payment terms\n   8) sections\n   9) employees\n   10) customers");
                        Console.Write("list >> ");
                        choiceStr = Console.ReadLine();
                    }

                    if (int.TryParse(choiceStr, out int choice))
                    {
                        if (choice == 1) // articles
                        {
                            erpManager.DisplayInventory();
                        }
                        else if (choice == 2) // storage slots
                        {
                            erpManager.ListStorageSlots();
                        }
                        else if (choice == 3) // orders
                        {
                            erpManager.ListOrders();
                        }
                        else if (choice == 4) // self-orders
                        {
                            erpManager.ListSelfOrders();
                        }
                        else if (choice == 5) // bills
                        {
                            erpManager.ListBills();
                        }
                        else if (choice == 6) // price lists
                        {
                            erpManager.ListPrices();
                        }
                        else if (choice == 7) // payment terms
                        {
                            erpManager.ListPaymentTerms();
                        }
                        else if (choice == 8) // sections
                        {
                            erpManager.ListSections();
                        }
                        else if (choice == 9) // employees
                        {
                            erpManager.ListEmployees();
                        }
                        else if (choice == 10) // customers
                        {
                            erpManager.ListCustomers();
                        }
                        else
                        {
                            ShellAssistance.Error("Invalid input");
                        }
                    }
                    else
                    {
                        ShellAssistance.Error("Enter a valid option as number");
                    }
                }
                else if (command.StartsWith("AT-"))
                {
                    string numStr = Regex.Replace(command, @"^AT-", "");
                    if (int.TryParse(numStr, out int num))
                    {
                        if (articleTypes.ContainsKey(num) && articleTypes.TryGetValue(num, out ArticleType? articleType))
                        {
                            Console.WriteLine($"Article type info for AT-{num}");
                            Console.WriteLine($"Name: {articleType.Name}\nId: {articleType.Id}");
                        }
                        else
                        {
                            ShellAssistance.Error("Unknown article type");
                        }
                    }
                }
                else if (command.StartsWith("exit") || command.StartsWith("quit"))
                {
                    Console.WriteLine("Goodbye!");
                    break;
                }
                else
                {
                    ShellAssistance.Error("Enter a valid command");
                }
            }
        }

        public void HelpMenu()
        {
            ShellAssistance.WIT("============ Help Menu ============");
            ShellAssistance.HelpOut("help", "Show this help menu");
            ShellAssistance.HelpOut("create", "Create an element");
            ShellAssistance.HelpOut("list", "List something");
            ShellAssistance.HelpOut("<element id>", "Show info about the element (e.g. AT-0)");
            ShellAssistance.HelpOut("exit/quit", "Exit the program");
        }
    }

    class ShellAssistance
    {
        public static void HelpOut(string cmd, string text)
        {
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write(cmd);
            Console.ResetColor();
            Console.Write(" ");
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static void WIT(string text)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static void Error(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("[ERROR]");
            Console.ResetColor();
            Console.Write(" ");
            Console.WriteLine(text);
            Console.ResetColor();
        }
        public static void Success(string text)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[SUCCESS]");
            Console.ResetColor();
            Console.Write(" ");
            Console.WriteLine(text);
            Console.ResetColor();
        }
    }
}
