using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using ERP_Fix;
using Terminal.Gui;


namespace ERP_Fix {
    class TUI
    {
        public ERPManager? erpManager;
        Dictionary<string, Window> windows = new();

        public void Start()
        {
            Application.Init();
            var top = Application.Top;
            var win = new Window("ERP")
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            // define color schemes
            List<ColorScheme> schemes = Schemes();
            win.ColorScheme = schemes[0];
            top.Add(win);

            var welcomeLabel = new Label("Willkommen bei meinem ERP-System")
            {
                X = 2,
                Y = 1,
                ColorScheme = schemes[1]
            };
            win.Add(welcomeLabel);

            var buttonNew = new Button("Neue Instanz")
            {
                X = 2,
                Y = 3,
                ColorScheme = schemes[2]
            };
            buttonNew.Clicked += () => { newInstanceMenu(win, schemes); };
            win.Add(buttonNew);

            var buttonOpen = new Button("Instanz öffnen")
            {
                X = 2,
                Y = 5,
                ColorScheme = schemes[2]
            };
            buttonOpen.Clicked += () => { openInstanceMenu(win, schemes); };
            win.Add(buttonOpen);

            // Exit on Esc
            top.KeyDown += (e) =>
            {
                if (e.KeyEvent.Key == Key.Esc)
                {
                    Application.RequestStop();
                    e.Handled = true;
                }
            };

            Application.Run();
        }

        private List<ColorScheme> Schemes()
        {
            // define color schemes
            var scheme0 = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black),
                Focus = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black),
                HotNormal = Application.Driver.MakeAttribute(Color.BrightGreen, Color.Black),
                HotFocus = Application.Driver.MakeAttribute(Color.BrightGreen, Color.Black)
            };
            var scheme1 = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.White, Color.Black),
                Focus = Application.Driver.MakeAttribute(Color.White, Color.Black),
                HotNormal = Application.Driver.MakeAttribute(Color.BrightGreen, Color.Black),
                HotFocus = Application.Driver.MakeAttribute(Color.BrightGreen, Color.Black)
            };
            var scheme2 = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.White, Color.Black),
                Focus = Application.Driver.MakeAttribute(Color.BrightGreen, Color.Black),
                HotNormal = Application.Driver.MakeAttribute(Color.White, Color.Black),
                HotFocus = Application.Driver.MakeAttribute(Color.BrightGreen, Color.Black)
            };
            var scheme3 = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.Green, Color.Black),
                Focus = Application.Driver.MakeAttribute(Color.Green, Color.Black),
                HotNormal = Application.Driver.MakeAttribute(Color.Green, Color.Black),
                HotFocus = Application.Driver.MakeAttribute(Color.Green, Color.Black)
            };

            var scheme4 = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black),
                Focus = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black),
                HotNormal = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black),
                HotFocus = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black)
            };

            return [scheme0, scheme1, scheme2, scheme3, scheme4];
        }

        private void newInstanceMenu(Window win, List<ColorScheme> schemes)
        {
            win.RemoveAll();
            var creatingLabel = new Label("Neue Instanz erstellen...")
            {
                X = 2,
                Y = 1,
                ColorScheme = schemes[1]
            };
            win.Add(creatingLabel);

            var inputAppellLabel = new Label("Gib den Namen der neuen Instanz ein:")
            {
                X = 2,
                Y = 3,
                ColorScheme = schemes[1]
            };
            win.Add(inputAppellLabel);

            var newInstanceNameTextField = new TextField("")
            {
                X = 2,
                Y = 4,
                Width = 40,
                ColorScheme = schemes[1]
            };
            win.Add(newInstanceNameTextField);

            var okButton = new Button("OK")
            {
                X = 2,
                Y = 6,
                ColorScheme = schemes[2]
            };

            okButton.Clicked += () => { InInstanceMenu(win, schemes, newInstanceNameTextField); };
            win.Add(okButton);

            Application.Top.SetNeedsDisplay();
        }

        private void openInstanceMenu(Window win, List<ColorScheme> schemes)
        {
            // add code to list existing instances (recursively search this and all subdirs for .erp files)
            win.RemoveAll();
            var openingLabel = new Label("Instanz öffnen...")
            {
                X = 2,
                Y = 1,
                ColorScheme = schemes[1]
            };
            win.Add(openingLabel);
            var rootDir = "./"; // @Copilot do not change this

            int posY = 3;

            // Show root files header and files
            var rootFiles = Enumerable.Empty<string>();
            try
            {
                rootFiles = Directory.EnumerateFiles(rootDir, "*.erp", SearchOption.TopDirectoryOnly)
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch { }

            if (rootFiles.Any())
            {
                foreach (var f in rootFiles)
                {
                    var rel = Path.GetFileName(f);
                    var btn = new Button(rel)
                    {
                        X = 2,
                        Y = posY,
                        ColorScheme = schemes[2]
                    };
                    btn.Clicked += () =>
                    {
                        var fullPath = Path.Combine(rootDir, rel);
                        erpManager = ERPManager.OpenInstance(fullPath.Replace('\\', '/'));
                        InInstanceMenu(win, schemes, new TextField(rel));
                    };
                    win.Add(btn);
                    posY += 2;
                }
            }

            // Subdirectories with .erp files, recursively with headers per subdir
            const int maxDepth = 30;
            void RenderDir(string dirPath, int depth)
            {
                bool hasAnyErp;
                try
                {
                    hasAnyErp = Directory.EnumerateFiles(dirPath, "*.erp", SearchOption.AllDirectories).Any();
                }
                catch { return; }
                if (!hasAnyErp) return;

                var dirName = Path.GetFileName(dirPath).Replace('\\', '/');
                var header = new Label($"{dirName}/")
                {
                    X = 2 + (depth * 2),
                    Y = posY,
                    ColorScheme = schemes[1]
                };
                win.Add(header);
                posY += 1;

                IEnumerable<string> filesHere;
                try
                {
                    filesHere = Directory.EnumerateFiles(dirPath, "*.erp", SearchOption.TopDirectoryOnly)
                        .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
                catch { filesHere = Enumerable.Empty<string>(); }

                foreach (var f in filesHere)
                {
                    var relToRoot = Path.GetRelativePath(rootDir, f).Replace('\\', '/');
                    var relToDir = Path.GetFileName(f);
                    var btn = new Button(relToDir)
                    {
                        X = 4 + (depth * 2),
                        Y = posY,
                        ColorScheme = schemes[2]
                    };
                    btn.Clicked += () =>
                    {
                        var fullPath = Path.Combine(rootDir, relToRoot);
                        erpManager = ERPManager.OpenInstance(fullPath.Replace('\\', '/'));
                        InInstanceMenu(win, schemes, new TextField(relToDir));
                    };
                    win.Add(btn);
                    posY += 2;
                }

                if (depth >= maxDepth) return;

                IEnumerable<string> children;
                try
                {
                    children = Directory.EnumerateDirectories(dirPath)
                        .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
                catch { return; }

                foreach (var child in children)
                {
                    RenderDir(child, depth + 1);
                }

                // spacer after a directory group
                posY += 1;
            }

            IEnumerable<string> topDirs;
            try
            {
                topDirs = Directory.EnumerateDirectories(rootDir)
                    .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch { topDirs = Enumerable.Empty<string>(); }

            foreach (var d in topDirs)
            {
                RenderDir(d, 0);
            }
            Application.Top.SetNeedsDisplay();
        }

        private sealed class InstanceEntry
        {
            public string Display { get; init; } = string.Empty;
            public string RelativePath { get; init; } = string.Empty; // only for files
            public int Indent { get; init; } // 0 for root files; N for nested depth
        }

        // List only files (.erp) up to a maximum depth of 30 folders. Indentation reflects depth.
        // Display strings use the file name only; RelativePath is the path relative to rootDir.
        private IEnumerable<InstanceEntry> FindInstances(string rootDir)
        {
            try
            {
                if (!Directory.Exists(rootDir))
                {
                    return Enumerable.Empty<InstanceEntry>();
                }

                const int maxDepth = 30;
                var entries = new List<InstanceEntry>();

                void Recurse(string currentDir, int depth)
                {
                    if (depth > maxDepth) return;

                    IEnumerable<string> filesInThisDir;
                    try
                    {
                        filesInThisDir = Directory.EnumerateFiles(currentDir, "*.erp", SearchOption.TopDirectoryOnly)
                            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                            .ToList();
                    }
                    catch
                    {
                        return; // cannot access this directory
                    }

                    foreach (var f in filesInThisDir)
                    {
                        var relToRoot = Path.GetRelativePath(rootDir, f);
                        entries.Add(new InstanceEntry
                        {
                            Display = relToRoot.Replace('\\', '/'),
                            RelativePath = relToRoot,
                            Indent = depth
                        });
                    }

                    IEnumerable<string> subDirs;
                    try
                    {
                        subDirs = Directory.EnumerateDirectories(currentDir)
                            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
                            .ToList();
                    }
                    catch
                    {
                        return;
                    }

                    foreach (var dir in subDirs)
                    {
                        bool hasAnyErp;
                        try
                        {
                            hasAnyErp = Directory.EnumerateFiles(dir, "*.erp", SearchOption.AllDirectories).Any();
                        }
                        catch
                        {
                            continue;
                        }
                        if (!hasAnyErp) continue;

                        Recurse(dir, depth + 1);
                    }
                }

                Recurse(rootDir, 0);
                return entries;
            }
            catch
            {
                return Enumerable.Empty<InstanceEntry>();
            }
        }

        private void InInstanceMenu(Window win, List<ColorScheme> schemes, TextField newInstanceNameTextField)
        {
            var inputText = newInstanceNameTextField.Text.ToString();
            // If we're creating a new instance, erpManager will be null; opening existing sets it beforehand
            if (erpManager == null)
            {
                erpManager = new ERPManager(inputText ?? "unnamed");
            }

            // main window
            win.RemoveAll();
            var instanceTitle = erpManager!.InstanceName ?? (inputText ?? "unnamed");
            win.Title = $"ERP - {instanceTitle}";

            // Removed header label showing "ERP - <instance name>"

            var buttonCreate = new Button("Element erstellen")
            {
                X = 2,
                Y = 1,
                ColorScheme = schemes[2]
            };

            var buttonRestockArticle = new Button("Artikel auffüllen")
            {
                X = 2,
                Y = 3,
                ColorScheme = schemes[2]
            };
            Action? buttonCreateClick = null;
            Action? buttonRestockArticleClick = null;

            Action DoAfter = () =>
            {
                win.RemoveAll();
                win.Title = $"ERP - {erpManager!.InstanceName}";
                // Removed header label showing "ERP - <instance name>"

                var buttonCreate = new Button("Element erstellen")
                {
                    X = 2,
                    Y = 1,
                    ColorScheme = schemes[2]
                };
                buttonCreate.Clicked += buttonCreateClick!;
                win.Add(buttonCreate);

                var buttonRestockArticle = new Button("Artikel auffüllen")
                {
                    X = 2,
                    Y = 3,
                    ColorScheme = schemes[2]
                };
                buttonRestockArticle.Clicked += buttonRestockArticleClick;
                win.Add(buttonRestockArticle);

                fillAllWindows();
                resizeAllWindowsWidth();

                win.SetFocus();
                win.Width = 60;

                Application.Top.SetNeedsDisplay();
            };

            buttonCreateClick = () => { CreatingElementMenu(win, schemes, DoAfter); };
            buttonRestockArticleClick = () => { RestockArticle(win, schemes, DoAfter); };

            buttonCreate.Clicked += buttonCreateClick;
            win.Add(buttonCreate);

            buttonRestockArticle.Clicked += buttonRestockArticleClick;
            win.Add(buttonRestockArticle);

            win.SetFocus();
            win.Width = 60;

            // articleType window
            var articleTypeWin = new Window("Artikeltypen")
            {
                X = 62,
                Y = 1,
                Width = 40,
                Height = 7,
                ColorScheme = schemes[0]
            };
            Application.Top.Add(articleTypeWin);
            windows["articleType"] = articleTypeWin;

            // storageSlot window
            var storageSlotWin = new Window("Lagerplätze")
            {
                X = 104,
                Y = 1,
                Width = 40,
                Height = 7
            };
            storageSlotWin.ColorScheme = schemes[0];
            Application.Top.Add(storageSlotWin);
            windows["storageSlot"] = storageSlotWin;

            // article window
            var articleWin = new Window("Artikel")
            {
                X = 146,
                Y = 1,
                Width = 40,
                Height = 7
            };
            articleWin.ColorScheme = schemes[0];
            Application.Top.Add(articleWin);
            windows["article"] = articleWin;

            fillAllWindows();
            resizeAllWindowsWidth();
            Application.Top.SetNeedsDisplay();
        }

        private void resizeAllWindowsWidth()
        {
            if (windows == null || windows.Count == 0) return;

            static int CalcRequiredWidth(Window win)
            {
                int maxContentEndX = 0;

                var titleLen = (win.Title?.ToString() ?? string.Empty).Length;
                void Visit(View parent, int offsetX)
                {
                    foreach (var child in parent.Subviews)
                    {
                        var r = child.Frame;
                        int w = r.Width;

                        if (w <= 0 && child is Label lbl)
                        {
                            w = (lbl.Text?.ToString() ?? string.Empty).Length;
                        }

                        int endX = offsetX + r.X + w;
                        if (endX > maxContentEndX)
                            maxContentEndX = endX;

                        // Recurse
                        if (child.Subviews != null && child.Subviews.Count > 0)
                        {
                            Visit(child, offsetX + r.X);
                        }
                    }
                }

                Visit(win, 0);

                int contentNeeded = Math.Max(maxContentEndX + 2, titleLen + 2);
                int fullWidthNeeded = contentNeeded + 1;

                return fullWidthNeeded;
            }

            var ordered = windows
                .Select(kvp => kvp.Value)
                .OrderBy(w => w.Frame.X)
                .ToList();

            foreach (var w in ordered)
            {
                int required = CalcRequiredWidth(w);

                int consoleCols = Application.Driver?.Cols ?? int.MaxValue;
                int maxAvailable = consoleCols - w.Frame.X - 1;
                if (maxAvailable > 0)
                {
                    w.Width = Math.Min(required, maxAvailable);
                }
                else
                {
                    w.Width = required;
                }
            }

            if (ordered.Count > 0)
            {
                int nextX = ordered[0].Frame.X;
                foreach (var w in ordered)
                {
                    w.X = nextX;
                    nextX += w.Frame.Width + 2;
                }
            }

            Application.Top.SetNeedsDisplay();
        }

        private void fillAllWindows()
        {
            fillArticleTypeWindow(windows["articleType"], Schemes());
            fillStorageSlotWindow(windows["storageSlot"], Schemes());
            fillArticleWindow(windows["article"], Schemes());
        }

        private void fillArticleTypeWindow(Window articleTypeWin, List<ColorScheme> schemes)
        {
            articleTypeWin.RemoveAll();

            var articleTypes = erpManager!.GetAllArticleTypes();
            foreach (ArticleType at in articleTypes)
            {
                var nameLabel = new Label(at.Name)
                {
                    X = 2,
                    Y = 1 + articleTypes.IndexOf(at),
                    ColorScheme = schemes[1]
                };
                articleTypeWin.Add(nameLabel);

                int intend = nameLabel.Frame.Width + 3;

                var extraInfoLabel = new Label($"(ID: {at.Id})")
                {
                    X = intend,
                    Y = 1 + articleTypes.IndexOf(at),
                    ColorScheme = schemes[4]
                };
                articleTypeWin.Add(extraInfoLabel);
            }

            articleTypeWin.Height = 4 + articleTypes.Count;

            Application.Top.SetNeedsDisplay();
        }

        private void fillStorageSlotWindow(Window storageSlotWin, List<ColorScheme> schemes)
        {
            storageSlotWin.RemoveAll();

            var storageSlots = erpManager!.GetAllStorageSlots();
            foreach (StorageSlot ss in storageSlots)
            {
                var nameLabel = new Label($"Lagerplatz")
                {
                    X = 2,
                    Y = 1 + storageSlots.IndexOf(ss),
                    ColorScheme = schemes[1]
                };
                storageSlotWin.Add(nameLabel);

                int intend = nameLabel.Frame.Width + 3;

                var extraInfoLabel = new Label($"(ID: {ss.Id})")
                {
                    X = intend,
                    Y = 1 + storageSlots.IndexOf(ss),
                    ColorScheme = schemes[4]
                };
                storageSlotWin.Add(extraInfoLabel);
            }

            storageSlotWin.Height = 4 + storageSlots.Count;

            Application.Top.SetNeedsDisplay();
        }

        private void fillArticleWindow(Window articleWin, List<ColorScheme> schemes)
        {
            articleWin.RemoveAll();

            var articles = erpManager!.GetAllArticles();
            foreach (Article a in articles)
            {
                var nameLabel = new Label($"{a.Type.Name}")
                {
                    X = 2,
                    Y = 1 + articles.IndexOf(a),
                    ColorScheme = schemes[1]
                };
                articleWin.Add(nameLabel);

                int intend = nameLabel.Frame.Width + 3;

                var extraInfoLabel = new Label($"(ID: {a.Id}, Bestand: {a.Stock})")
                {
                    X = intend,
                    Y = 1 + articles.IndexOf(a),
                    ColorScheme = schemes[4]
                };
                articleWin.Add(extraInfoLabel);
            }

            articleWin.Height = 4 + articles.Count;

            Application.Top.SetNeedsDisplay();
        }

        // creation button click
        private void CreateArticleType(Window win, List<ColorScheme> schemes, Action doAfter)
        {
            win.RemoveAll();

            // Removed header label showing "ERP - <instance name>"

            var appellLabel = new Label("Name des Artikeltypen:")
            {
                X = 2,
                Y = 1,
                ColorScheme = schemes[1]
            };
            win.Add(appellLabel);

            var nameInput = new TextField()
            {
                X = 2,
                Y = 2,
                Width = 40,
                ColorScheme = schemes[1]
            };
            win.Add(nameInput);

            var sendButton = new Button("Ok")
            {
                X = 2,
                Y = 4,
                ColorScheme = schemes[2]
            };
            sendButton.Clicked += () =>
            {
                var text = nameInput.Text?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(text))
                {
                    doAfter.Invoke();
                    return;
                }

                win.Remove(appellLabel);
                win.Remove(nameInput);
                win.Remove(sendButton);

                ArticleType articleType = erpManager!.NewArticleType(text);

                var finishedText = new Label("Der Artikeltyp wurde erstellt.")
                {
                    X = 2,
                    Y = 1,
                    ColorScheme = schemes[3]
                };
                win.Add(finishedText);

                var okButton = new Button("Ok")
                {
                    X = 2,
                    Y = 3,
                    ColorScheme = schemes[2]
                };
                okButton.Clicked += () =>
                {
                    doAfter.Invoke();
                };
                win.Add(okButton);
            };
            win.Add(sendButton);

            Application.Top.SetNeedsDisplay();
        }
        private void CreateStorageSlot(Window win, List<ColorScheme> schemes, Action doAfter)
        {
            win.RemoveAll();

            // Removed header label showing "ERP - <instance name>"

            StorageSlot storageSlot = erpManager!.NewStorageSlot();

            var finishedText = new Label("Der Lagerplatz wurde erstellt.")
            {
                X = 2,
                Y = 1,
                ColorScheme = schemes[3]
            };
            win.Add(finishedText);

            var okButton = new Button("Ok")
            {
                X = 2,
                Y = 3,
                ColorScheme = schemes[2]
            };
            okButton.Clicked += () =>
            {
                doAfter.Invoke();
            };
            win.Add(okButton);

            Application.Top.SetNeedsDisplay();
        }

        private void CreateArticle(Window win, List<ColorScheme> schemes, Action doAfter)
        {
            win.RemoveAll();

            // Removed header label showing "ERP - <instance name>"

            var appellLabel = new Label("Artikeltyp ID:")
            {
                X = 2,
                Y = 1,
                ColorScheme = schemes[1]
            };
            win.Add(appellLabel);

            var typeIdInput = new TextField()
            {
                X = 2,
                Y = 2,
                Width = 40,
                ColorScheme = schemes[1]
            };
            win.Add(typeIdInput);

            var stockLabel = new Label("Anfangsbestand:")
            {
                X = 2,
                Y = 4,
                ColorScheme = schemes[1]
            };
            win.Add(stockLabel);

            var stockInput = new TextField()
            {
                X = 2,
                Y = 5,
                Width = 40,
                ColorScheme = schemes[1]
            };
            win.Add(stockInput);

            var sendButton = new Button("Ok")
            {
                X = 2,
                Y = 7,
                ColorScheme = schemes[2]
            };
            sendButton.Clicked += () =>
            {
                var typeIdText = typeIdInput.Text?.ToString() ?? string.Empty;
                var stockText = stockInput.Text?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(typeIdText) || string.IsNullOrWhiteSpace(stockText))
                {
                    doAfter.Invoke();
                    return;
                }

                if (!int.TryParse(typeIdText, out int typeId) || !int.TryParse(stockText, out int stock) || stock < 0)
                {
                    doAfter.Invoke();
                    return;
                }

                var at = erpManager!.FindArticleType(typeId);
                if (at == null)
                {
                    doAfter.Invoke();
                    return;
                }

                win.Remove(appellLabel);
                win.Remove(typeIdInput);
                win.Remove(stockLabel);
                win.Remove(stockInput);
                win.Remove(sendButton);

                Article article = erpManager!.NewArticle(at.Id, stock);

                var finishedText = new Label("Der Artikel wurde erstellt.")
                {
                    X = 2,
                    Y = 1,
                    ColorScheme = schemes[3]
                };
                win.Add(finishedText);
                var okButton = new Button("Ok")
                {
                    X = 2,
                    Y = 3,
                    ColorScheme = schemes[2]
                };
                okButton.Clicked += () =>
                {
                    doAfter.Invoke();
                };
                win.Add(okButton);
            };
            win.Add(sendButton);
            Application.Top.SetNeedsDisplay();
        }

        private void CreatingElementMenu(Window win, List<ColorScheme> schemes, Action DoAfter)
        {
            win.RemoveAll();
            // Removed header label showing "ERP - <instance name>"

            Dictionary<string, Action> buttons = new()
                {
                    { "Artikeltyp", () => { CreateArticleType(win, schemes, DoAfter); } },
                    { "Lagerplatz", () => { CreateStorageSlot(win, schemes, DoAfter); } },
                    { "Artikel", () => { CreateArticle(win, schemes, DoAfter); } }
                };
            int posY = 1;

            foreach (KeyValuePair<string, Action> kvp in buttons)
            {
                var button = new Button(kvp.Key)
                {
                    X = 2,
                    Y = posY,
                    ColorScheme = schemes[2]
                };
                button.Clicked += () => { kvp.Value.Invoke(); };
                win.Add(button);

                posY += 2;
            }

            Application.Top.SetNeedsDisplay();
        }

        private void RestockArticle(Window win, List<ColorScheme> schemes, Action DoAfter)
        {
            win.RemoveAll();

            var articleIdLabel = new Label("Artikel ID:")
            {
                X = 2,
                Y = 1,
                ColorScheme = schemes[1]
            };
            win.Add(articleIdLabel);

            var articleIdInput = new TextField()
            {
                X = 2,
                Y = 2,
                Width = 40,
                ColorScheme = schemes[1]
            };
            win.Add(articleIdInput);

            var amountLabel = new Label("Menge:")
            {
                X = 2,
                Y = 4,
                ColorScheme = schemes[1]
            };
            win.Add(amountLabel);

            var amountInput = new TextField()
            {
                X = 2,
                Y = 5,
                Width = 40,
                ColorScheme = schemes[1]
            };
            win.Add(amountInput);

            var sendButton = new Button("Ok")
            {
                X = 2,
                Y = 7,
                ColorScheme = schemes[2]
            };
            sendButton.Clicked += () =>
            {
                var articleIdText = articleIdInput.Text?.ToString() ?? string.Empty;
                var amountText = amountInput.Text?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(articleIdText) || string.IsNullOrWhiteSpace(amountText))
                {
                    DoAfter.Invoke();
                    return;
                }

                if (!int.TryParse(articleIdText, out int articleId) || !int.TryParse(amountText, out int amount) || amount <= 0)
                {
                    DoAfter.Invoke();
                    return;
                }

                var article = erpManager!.FindArticle(articleId);
                if (article == null)
                {
                    DoAfter.Invoke();
                    return;
                }

                win.Remove(articleIdLabel);
                win.Remove(articleIdInput);
                win.Remove(amountLabel);
                win.Remove(amountInput);
                win.Remove(sendButton);

                erpManager!.RestockArticle(article.Id, amount);

                var finishedText = new Label("Der Artikel wurde aufgefüllt.")
                {
                    X = 2,
                    Y = 1,
                    ColorScheme = schemes[3]
                };
                win.Add(finishedText);
                var okButton = new Button("Ok")
                {
                    X = 2,
                    Y = 3,
                    ColorScheme = schemes[2]
                };
                okButton.Clicked += () =>
                {
                    DoAfter.Invoke();
                };
                win.Add(okButton);
            };
            win.Add(sendButton);
        }
    }
}