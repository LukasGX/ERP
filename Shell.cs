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
        public void Main()
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
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("Please enter the command of your choice: ");
                Console.ResetColor();
                string? choice = Console.ReadLine()?.ToLower();

                if (choice == "create-storage-slot")
                {
                    Console.WriteLine("You chose: Create a new storage slot");
                    StorageSlot newStorageSlot = program.NewStorageSlot();
                    storageSlots.Add(newStorageSlot.Id, newStorageSlot);
                    Console.WriteLine($"Your new Storage slot is accessible as S[{newStorageSlot.Id}]");
                }
                else if (choice == "list-storage-slots")
                {
                    Console.WriteLine("You chose: List storage slots");
                    program.ListStorageSlots();
                }
                else if (choice == "create-article-type")
                {
                    Console.WriteLine("You chose: Create a new article type");
                    Console.Write("Enter the name of the new article type: ");
                    string? typeName = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(typeName)) // Ensure proper null checks
                    {
                        Console.WriteLine("Invalid input. Please enter a valid article type name.");
                        continue; // Fix return to continue for loop
                    }
                    ArticleType newArticleType = program.NewArticleType(typeName);
                    try
                    {
                        articleTypes.Add(newArticleType.Id, newArticleType.Name, newArticleType); // Validate dictionary operations
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        continue;
                    }
                    Console.WriteLine($"Your new Article type is accessible as AT[{newArticleType.Id}] or AT[{newArticleType.Name}]");
                }
                else if (choice == "create-article")
                {
                    CreateNewArticle(program, articleTypes, articles);
                }
                else if (choice == "sort-article")
                {
                    Console.WriteLine("You chose: Sort an article");
                    Console.Write("Enter the article to sort (A[id]): ");
                    string articleInput = Console.ReadLine();
                    Match match = Regex.Match(articleInput, @"^A\[(.*)\]$");
                    if (!match.Success)
                    {
                        Console.WriteLine("Invalid article format. Please use A[id].");
                        continue;
                    }
                    string innerContent = match.Groups[1].Value;
                    Article articleToSort = null;
                    if (int.TryParse(innerContent, out int id))
                    {
                        if (!articles.ContainsKey(id))
                        {
                            Console.WriteLine("Invalid article id. Please try again.");
                            continue;
                        }
                        articleToSort = articles[id];
                    }
                    else
                    {
                        Console.WriteLine("Invalid input. Please try again.");
                    }
                    Console.Write("Enter the storage slot to sort into (S[id]): ");
                    string slotInput = Console.ReadLine();
                    Match matchX = Regex.Match(slotInput, @"^S\[(.*)\]$");
                    if (!matchX.Success)
                    {
                        Console.WriteLine("Invalid storage slot format. Please use S[id].");
                        continue;
                    }
                    string innerContentX = matchX.Groups[1].Value; // Fix incorrect variable usage
                    StorageSlot storageSlotToSortIn = null;
                    if (int.TryParse(innerContentX, out int idX))
                    {
                        if (!storageSlots.ContainsKey(idX))
                        {
                            Console.WriteLine("Invalid storage slot id. Please try again.");
                            continue;
                        }
                        storageSlotToSortIn = storageSlots[idX]; // Fix incorrect dictionary key usage
                    }
                    else
                    {
                        Console.WriteLine("Invalid input. Please try again.");
                    }
                    program.SortArticle(articleToSort.Id, storageSlotToSortIn.Id);
                    Console.WriteLine($"Article {articleToSort.Id} sorted into storage slot {storageSlotToSortIn.Id}.");
                }
                else if (choice == "display-inventory")
                {
                    Console.WriteLine("You chose: Display inventory (List articles)");
                    program.DisplayInventory();
                }
                else if (choice == "create-order")
                {
                    Console.WriteLine("You chose: Create a new order");
                    if (articleTypes.Count == 0)
                    {
                        Console.WriteLine("No article types available. Please create an article type first.");
                        continue;
                    }
                    Console.Write("Enter the articles to order (OI[AT[id], amount], semicolon separated): ");
                    string orderInput = Console.ReadLine();
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
                            // Gruppen auslesen
                            string typeIdStr = match.Groups[1].Value;
                            string amountStr = match.Groups[2].Value;

                            if (int.TryParse(typeIdStr, out int typeId) && int.TryParse(amountStr, out int amount))
                            {
                                // Prüfen, ob der ArticleType existiert
                                //ArticleType? articleType = articleTypes.FirstOrDefault(at => at.Id == typeId);
                                ArticleType? articleType = articleTypes.GetById(typeId)?.Value;
                                if (articleType == null)
                                {
                                    Console.WriteLine($"ArticleType with id {typeId} does not exist.");
                                }
                                else
                                {
                                    // Artikel anlegen oder suchen, je nach Logik
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
                        continue;
                    }
                    Order newOrder = program.NewOrder(articlesToOrder);
                    orders.Add(newOrder.Id, newOrder);
                    Console.WriteLine($"Your new order is accessible as O[{newOrder.Id}]");
                }
                else if (choice == "list-orders")
                {
                    Console.WriteLine("You chose: List orders");
                    program.ListOrders();
                }
                else if (choice == "create-price-list")
                {
                    Console.WriteLine("You chose: Create new price list");
                    if (articleTypes.Count == 0)
                    {
                        Console.WriteLine("No article types available. Please create an article type first.");
                        continue;
                    }
                    Dictionary<ArticleType, double> pricesHere = new Dictionary<ArticleType, double>();
                    foreach (var articleType in articleTypes.GetAll())
                    {
                        Console.Write($"Enter the price for {articleType.Name} (AT[{articleType.Id}]): ");
                        string priceInput = Console.ReadLine();
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
                }
                else if (choice == "list-price-lists")
                {
                    Console.WriteLine("You chose: List price lists");
                    program.ListPrices();
                }
                else if (choice == "create-bill")
                {
                    Console.WriteLine("You chose: Create a new bill from order");
                    if (orders.Count == 0)
                    {
                        Console.WriteLine("No orders available. Please create an order first.");
                        continue;
                    }
                    Console.Write("Enter the order id to create a bill from (O[id]): ");
                    string orderInput = Console.ReadLine();
                    Match match = Regex.Match(orderInput, @"^O\[(.*)\]$");
                    if (!match.Success)
                    {
                        Console.WriteLine("Invalid order format. Please use O[id].");
                        continue;
                    }

                    Order orderToBill = null;
                    string innerContent = match.Groups[1].Value;
                    if (int.TryParse(innerContent, out int orderId))
                    {
                        if (!orders.ContainsKey(orderId))
                        {
                            Console.WriteLine("Invalid order id. Please try again.");
                            continue;
                        }
                        orderToBill = orders[orderId];
                    }
                    else
                    {
                        Console.WriteLine("Invalid input. Please try again.");
                    }

                    if (prices.Count == 0)
                    {
                        Console.WriteLine("No price lists available. Please create a price list first.");
                        continue;
                    }
                    Console.Write("Enter the price list to use (P[id]): ");
                    string priceInput = Console.ReadLine();
                    Match priceMatch = Regex.Match(priceInput, @"^P\[(.*)\]$");
                    if (!priceMatch.Success)
                    {
                        Console.WriteLine("Invalid price list format. Please use P[id].");
                        continue;
                    }
                    Prices pricesToUse = null;
                    string innerContentPrice = priceMatch.Groups[1].Value;
                    if (int.TryParse(innerContentPrice, out int priceId))
                    {
                        if (!prices.ContainsKey(priceId))
                        {
                            Console.WriteLine("Invalid price list id. Please try again.");
                            continue;
                        }
                        pricesToUse = prices[priceId];
                    }
                    else
                    {
                        Console.WriteLine("Invalid input. Please try again.");
                    }

                    Bill newBill = program.NewBill(orderToBill, pricesToUse);
                    bills.Add(newBill.Id, newBill);
                    Console.WriteLine($"Bill created for order {orderId}.");
                }
                else if (choice == "list-bills")
                {
                    Console.WriteLine("You chose: List bills");
                    program.ListBills();
                }
                else if (choice == "exit")
                {
                    Console.WriteLine("Exiting the ERP...");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Goodbye");
                    Console.ResetColor();
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid input. Please try again.");
                }

            }
        }

        private void CreateNewArticle(Program program, NameCollection<int, string, ArticleType> articleTypes, Dictionary<int, Article> articles)
        {
            Console.WriteLine("You chose: Create a new article");
            if (articleTypes.Count == 0)
            {
                Console.WriteLine("No article types available. Please create an article type first.");
                return;
            }
            Console.Write("Enter the article type: ");
            // Article type
            string typeInput = Console.ReadLine();
            Match match = Regex.Match(typeInput, @"^AT\[(.*)\]$");
            if (!match.Success)
            {
                Console.WriteLine("Invalid article type. Please try again and use AT[id] or AT[name]");
                return;
            }

            ArticleType wantedArticleType = null;

            string innerContent = match.Groups[1].Value;
            if (int.TryParse(innerContent, out int id))
            {
                if (!articleTypes.ContainsId(id))
                {
                    Console.WriteLine("Invalid article type. Please try again.");
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
                    return;
                }
                else
                {
                    wantedArticleType = articleTypes.GetByName(innerContent).Value;
                }
            }

            Console.Write("Enter the amount of the new article: ");
            string amountInput = Console.ReadLine();
            if (!int.TryParse(amountInput, out int amount) || amount < 0)
            {
                Console.WriteLine("Invalid amount. Please enter a positive integer.");
                return;
            }
            Article newArticle = program.NewArticle(wantedArticleType.Id, amount);
            articles.Add(newArticle.Id, newArticle);
            Console.WriteLine($"Your new Article is accessible as A[{newArticle.Id}]");
        }
    }
}
