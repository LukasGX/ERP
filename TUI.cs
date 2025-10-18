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
        private Window? mainWindow;
        Dictionary<string, Window> windows = new();
        // Store default layouts for extra windows so we can restore after expanding one
        private readonly Dictionary<string, (int X, int Y, int W, int H)> defaultLayouts = new();
        // Navigation: cycle focus across secondary windows to access their controls via keyboard
        private bool secondaryNavRegistered = false;
        public static bool ShowCompletedOrders = false;
        public static bool ShowCancelledOrders = false;

        private readonly string[] secondaryOrder = new[] { "articleType", "storageSlot", "article", "customer", "section", "employee", "order", "prices", "paymentTerms", "bill", "selfOrder" };

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
            // keep a reference to main window for focus cycling
            mainWindow = win;

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

        // Global key handler to cycle focus into secondary windows so their buttons are reachable with keyboard
        private void OnGlobalKeyDown(View.KeyEventEventArgs e)
        {
            if (mainWindow == null) return;

            var k = e.KeyEvent.Key;
            if (k == Key.F7)
            {
                FocusNextSecondaryWindow(+1);
                e.Handled = true;
            }
            else if (k == Key.F6)
            {
                FocusNextSecondaryWindow(-1);
                e.Handled = true;
            }
        }

        private void RegisterSecondaryWindowNavigation()
        {
            if (secondaryNavRegistered) return;
            Application.Top.KeyDown += OnGlobalKeyDown;
            secondaryNavRegistered = true;
        }

        private void UnregisterSecondaryWindowNavigation()
        {
            if (!secondaryNavRegistered) return;
            Application.Top.KeyDown -= OnGlobalKeyDown;
            secondaryNavRegistered = false;
        }

        // Move focus to next/previous secondary window and focus its first focusable child (typically "Alle anzeigen")
        private void FocusNextSecondaryWindow(int direction)
        {
            // Build list in desired cycle order: main, then visible secondary windows in fixed order
            var existing = new List<Window>();
            if (mainWindow != null)
            {
                existing.Add(mainWindow);
            }
            foreach (var key in secondaryOrder)
            {
                if (windows.TryGetValue(key, out var w) && w.Visible)
                {
                    existing.Add(w);
                }
            }
            if (existing.Count == 0) return;

            // Find which secondary window (if any) currently has focus (or contains the focus)
            int currentIndex = existing.FindIndex(w => w.HasFocus || (w.MostFocused != null && (w.MostFocused == w || w.MostFocused.SuperView == w)));

            int nextIndex = (currentIndex + direction + existing.Count) % existing.Count;
            var target = existing[nextIndex];

            // Ensure window can take focus, then focus its first focusable control
            target.CanFocus = true;
            target.SetFocus();
            target.FocusFirst();
            Application.Top.SetNeedsDisplay();
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
            var rootDir = "./";

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
            win.Width = Dim.Sized(40);

            var inputText = newInstanceNameTextField.Text.ToString();
            if (erpManager == null)
            {
                erpManager = new ERPManager(inputText ?? "unnamed");
            }

            // main window
            win.RemoveAll();
            var instanceTitle = erpManager!.InstanceName ?? inputText ?? "unnamed";
            win.Title = $"ERP - {instanceTitle}";

            // Removed header label showing "ERP - <instance name>"

            var buttonCreate = new Button("Element erstellen")
            {
                X = 2,
                Y = 1,
                ColorScheme = schemes[2]
            };

            var buttonArticleOps = new Button("Artikeloperationen")
            {
                X = 2,
                Y = 3,
                ColorScheme = schemes[2]
            };

            var buttonArticleScan = new Button("Artikel scannen")
            {
                X = 2,
                Y = 5,
                ColorScheme = schemes[2]
            };

            var buttonOwnCapital = new Button("Eigenkapital verwalten")
            {
                X = 2,
                Y = 7,
                ColorScheme = schemes[2]
            };

            var buttonSave = new Button("Speichern")
            {
                X = 2,
                Y = 9,
                ColorScheme = schemes[2]
            };

            var buttonClose = new Button("Schließen")
            {
                X = 2,
                Y = 11,
                ColorScheme = schemes[2]
            };

            var labelOwnCapital = new Label($"Eigenkapital: {erpManager.ownCapital:F2} €")
            {
                X = 2,
                Y = 13,
                ColorScheme = schemes[4]
            };

            win.Add(labelOwnCapital);

            var labelWindowSwitch = new Label("Fenster wechseln mit F6/F7")
            {
                X = 2,
                Y = 14,
                ColorScheme = schemes[4]
            };
            win.Add(labelWindowSwitch);

            var labelExitProgram = new Label("Programm beenden mit Esc")
            {
                X = 2,
                Y = 15,
                ColorScheme = schemes[4]
            };
            win.Add(labelExitProgram);

            Action? buttonCreateClick = null;
            Action? buttonArticleOpsClick = null;
            Action? buttonArticleScanClick = null;
            Action? buttonOwnCapitalClick = null;
            Action? buttonSaveClick = null;
            Action? buttonCloseClick = null;

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

                var buttonArticleOps = new Button("Artikeloperationen")
                {
                    X = 2,
                    Y = 3,
                    ColorScheme = schemes[2]
                };
                buttonArticleOps.Clicked += buttonArticleOpsClick;
                win.Add(buttonArticleOps);

                var buttonArticleScan = new Button("Artikel scannen")
                {
                    X = 2,
                    Y = 5,
                    ColorScheme = schemes[2]
                };
                buttonArticleScan.Clicked += buttonArticleScanClick;
                win.Add(buttonArticleScan);

                var buttonOwnCapital = new Button("Eigenkapital verwalten")
                {
                    X = 2,
                    Y = 7,
                    ColorScheme = schemes[2]
                };
                buttonOwnCapital.Clicked += buttonOwnCapitalClick;
                win.Add(buttonOwnCapital);

                var buttonSave = new Button("Speichern")
                {
                    X = 2,
                    Y = 9,
                    ColorScheme = schemes[2]
                };
                buttonSave.Clicked += buttonSaveClick;
                win.Add(buttonSave);

                var buttonClose = new Button("Schließen")
                {
                    X = 2,
                    Y = 11,
                    ColorScheme = schemes[2]
                };
                buttonClose.Clicked += buttonCloseClick;
                win.Add(buttonClose);

                var labelOwnCapital = new Label($"Eigenkapital: {erpManager.ownCapital:F2} €")
                {
                    X = 2,
                    Y = 13,
                    ColorScheme = schemes[4]
                };
                win.Add(labelOwnCapital);

                var labelWindowSwitch = new Label("Fenster wechseln mit F6/F7")
                {
                    X = 2,
                    Y = 14,
                    ColorScheme = schemes[4]
                };
                win.Add(labelWindowSwitch);

                var labelExitProgram = new Label("Programm beenden mit Esc")
                {
                    X = 2,
                    Y = 15,
                    ColorScheme = schemes[4]
                };
                win.Add(labelExitProgram);

                fillAllWindows();

                win.SetFocus();

                Application.Top.SetNeedsDisplay();
            };

            buttonCreateClick = () => { CreatingElementMenu(win, schemes, DoAfter); };
            buttonArticleOpsClick = () => { ArticleOperationsMenu(win, schemes, DoAfter); };
            buttonArticleScanClick = () => { ScanArticle(win, schemes, DoAfter); };
            buttonOwnCapitalClick = () => { ManageOwnCapitalMenu(win, schemes, DoAfter); };
            buttonCloseClick = () => { CloseInstance(win, schemes); };
            buttonSaveClick = () => { SaveInstance(win, schemes, DoAfter); };

            buttonCreate.Clicked += buttonCreateClick;
            win.Add(buttonCreate);

            buttonArticleOps.Clicked += buttonArticleOpsClick;
            win.Add(buttonArticleOps);

            buttonArticleScan.Clicked += buttonArticleScanClick;
            win.Add(buttonArticleScan);

            buttonOwnCapital.Clicked += buttonOwnCapitalClick;
            win.Add(buttonOwnCapital);

            buttonSave.Clicked += buttonSaveClick;
            win.Add(buttonSave);

            buttonClose.Clicked += buttonCloseClick;
            win.Add(buttonClose);

            win.SetFocus();

            // articleType window
            var articleTypeWin = new Window("Artikeltypen")
            {
                X = 42,
                Y = 1,
                Width = 35,
                Height = 7,
                ColorScheme = schemes[0]
            };
            Application.Top.Add(articleTypeWin);
            windows["articleType"] = articleTypeWin;
            defaultLayouts["articleType"] = (42, 1, 35, 7);

            // storageSlot window
            var storageSlotWin = new Window("Lagerplätze")
            {
                X = 79,
                Y = 1,
                Width = 30,
                Height = 7
            };
            storageSlotWin.ColorScheme = schemes[0];
            Application.Top.Add(storageSlotWin);
            windows["storageSlot"] = storageSlotWin;
            defaultLayouts["storageSlot"] = (79, 1, 30, 7);

            // article window
            var articleWin = new Window("Artikel")
            {
                X = 111,
                Y = 1,
                Width = 85,
                Height = 7
            };
            articleWin.ColorScheme = schemes[0];
            Application.Top.Add(articleWin);
            windows["article"] = articleWin;
            defaultLayouts["article"] = (111, 1, 85, 7);

            // customer window
            var customerWin = new Window("Kunden")
            {
                X = 42,
                Y = 12,
                Width = 45,
                Height = 7
            };
            customerWin.ColorScheme = schemes[0];
            Application.Top.Add(customerWin);
            windows["customer"] = customerWin;
            defaultLayouts["customer"] = (42, 12, 45, 7);

            // section window
            var sectionWin = new Window("Abteilungen")
            {
                X = 89,
                Y = 12,
                Width = 60,
                Height = 7
            };
            sectionWin.ColorScheme = schemes[0];
            Application.Top.Add(sectionWin);
            windows["section"] = sectionWin;
            defaultLayouts["section"] = (89, 12, 60, 7);

            // employee window
            var employeeWin = new Window("Mitarbeiter")
            {
                X = 151,
                Y = 12,
                Width = 45,
                Height = 7
            };
            employeeWin.ColorScheme = schemes[0];
            Application.Top.Add(employeeWin);
            windows["employee"] = employeeWin;
            defaultLayouts["employee"] = (151, 12, 45, 7);

            // order window
            var orderWin = new Window("Bestellungen")
            {
                X = 42,
                Y = 23,
                Width = 70,
                Height = 7
            };
            orderWin.ColorScheme = schemes[0];
            Application.Top.Add(orderWin);
            windows["order"] = orderWin;
            defaultLayouts["order"] = (42, 23, 70, 7);

            // prices window
            var pricesWin = new Window("Preislisten")
            {
                X = 115,
                Y = 23,
                Width = 60,
                Height = 7
            };
            pricesWin.ColorScheme = schemes[0];
            Application.Top.Add(pricesWin);
            windows["prices"] = pricesWin;
            defaultLayouts["prices"] = (115, 23, 60, 7);

            // payment terms window
            var paymentTermsWin = new Window("Zahlungsbedingungen")
            {
                X = 42,
                Y = 34,
                Width = 55,
                Height = 7
            };
            paymentTermsWin.ColorScheme = schemes[0];
            Application.Top.Add(paymentTermsWin);
            windows["paymentTerms"] = paymentTermsWin;
            defaultLayouts["paymentTerms"] = (42, 34, 55, 7);

            // bill window
            var billWin = new Window("Rechnungen")
            {
                X = 100,
                Y = 34,
                Width = 90,
                Height = 7
            };
            billWin.ColorScheme = schemes[0];
            Application.Top.Add(billWin);
            windows["bill"] = billWin;
            defaultLayouts["bill"] = (100, 34, 90, 7);

            // self order window
            var selfOrderWin = new Window("Eigene Bestellungen")
            {
                X = 42,
                Y = 45,
                Width = 80,
                Height = 7
            };
            selfOrderWin.ColorScheme = schemes[0];
            Application.Top.Add(selfOrderWin);
            windows["selfOrder"] = selfOrderWin;
            defaultLayouts["selfOrder"] = (42, 45, 80, 7);

            // Enable keyboard navigation across secondary windows (F6/Shift+F6)
            RegisterSecondaryWindowNavigation();

            fillAllWindows();
            Application.Top.SetNeedsDisplay();
        }

        private void fillAllWindows()
        {
            FillArticleTypeWindow(windows["articleType"], Schemes(), false);
            FillStorageSlotWindow(windows["storageSlot"], Schemes(), false);
            FillArticleWindow(windows["article"], Schemes(), false);
            FillCustomerWindow(windows["customer"], Schemes(), false);
            FillSectionWindow(windows["section"], Schemes(), false);
            FillEmployeeWindow(windows["employee"], Schemes(), false);
            FillOrderWindow(windows["order"], Schemes());
            FillPricesWindow(windows["prices"], Schemes(), false);
            FillPaymentTermsWindow(windows["paymentTerms"], Schemes(), false);
            FillBillWindow(windows["bill"], Schemes(), false);
            FillSelfOrderWindow(windows["selfOrder"], Schemes(), false);
        }

        private void FillArticleTypeWindow(Window articleTypeWin, List<ColorScheme> schemes, bool showAll)
        {
            articleTypeWin.RemoveAll();

            var articleTypes = erpManager!.GetAllArticleTypes();
            int total = articleTypes.Count;
            bool useShowAllButton = !showAll && total >= 7;
            int toShow = showAll ? total : Math.Min(6, total);
            if (useShowAllButton) toShow = 5;

            for (int i = 0; i < toShow; i++)
            {
                var at = articleTypes[i];
                var nameLabel = new Label(at.Name)
                {
                    X = 2,
                    Y = 1 + i,
                    ColorScheme = schemes[1]
                };
                articleTypeWin.Add(nameLabel);

                int intend = nameLabel.Frame.Width + 3;

                var extraInfoLabel = new Label($"(ID: {at.Id})")
                {
                    X = intend,
                    Y = 1 + i,
                    ColorScheme = schemes[4]
                };
                articleTypeWin.Add(extraInfoLabel);
            }

            if (useShowAllButton)
            {
                var showAllBtn = new Button("Alle anzeigen")
                {
                    X = 2,
                    Y = 1 + toShow,
                    ColorScheme = schemes[2]
                };
                showAllBtn.Clicked += () => ExpandExtraWindow("articleType", schemes);
                articleTypeWin.Add(showAllBtn);
            }

            articleTypeWin.Height = 4 + (useShowAllButton ? (toShow + 1) : toShow);

            Application.Top.SetNeedsDisplay();
        }

        private void FillStorageSlotWindow(Window storageSlotWin, List<ColorScheme> schemes, bool showAll)
        {
            storageSlotWin.RemoveAll();

            var storageSlots = erpManager!.GetAllStorageSlots();
            int total = storageSlots.Count;
            bool useShowAllButton = !showAll && total >= 7;
            int toShow = showAll ? total : Math.Min(6, total);
            if (useShowAllButton) toShow = 5;

            for (int i = 0; i < toShow; i++)
            {
                var ss = storageSlots[i];
                var nameLabel = new Label($"Lagerplatz")
                {
                    X = 2,
                    Y = 1 + i,
                    ColorScheme = schemes[1]
                };
                storageSlotWin.Add(nameLabel);

                int intend = nameLabel.Frame.Width + 3;

                var extraInfoLabel = new Label($"(ID: {ss.Id})")
                {
                    X = intend,
                    Y = 1 + i,
                    ColorScheme = schemes[4]
                };
                storageSlotWin.Add(extraInfoLabel);
            }

            if (useShowAllButton)
            {
                var showAllBtn = new Button("Alle anzeigen")
                {
                    X = 2,
                    Y = 1 + toShow,
                    ColorScheme = schemes[2]
                };
                showAllBtn.Clicked += () => ExpandExtraWindow("storageSlot", schemes);
                storageSlotWin.Add(showAllBtn);
            }

            storageSlotWin.Height = 4 + (useShowAllButton ? (toShow + 1) : toShow);

            Application.Top.SetNeedsDisplay();
        }

        private void FillArticleWindow(Window articleWin, List<ColorScheme> schemes, bool showAll)
        {
            articleWin.RemoveAll();

            var articles = erpManager!.GetAllArticles();
            int total = articles.Count;
            bool useShowAllButton = !showAll && total >= 7;
            int toShow = showAll ? total : Math.Min(6, total);
            if (useShowAllButton) toShow = 5;

            for (int i = 0; i < toShow; i++)
            {
                var a = articles[i];
                var nameLabel = new Label($"{a.Type.Name}")
                {
                    X = 2,
                    Y = 1 + i,
                    ColorScheme = schemes[1]
                };
                articleWin.Add(nameLabel);

                int intend = nameLabel.Frame.Width + 3;

                var slot = erpManager.FindStorageSlot(a);
                var extraInfoLabel = new Label($"(ID: {a.Id}, Anz. {a.Stock}, Lagerplatz: {(slot == null ? "-" : slot.Id.ToString())}, ScannerId: {(string.IsNullOrEmpty(a.ScannerId.ToString()) ? "-" : a.ScannerId)})")
                {
                    X = intend,
                    Y = 1 + i,
                    ColorScheme = schemes[4]
                };
                articleWin.Add(extraInfoLabel);
            }

            if (useShowAllButton)
            {
                var showAllBtn = new Button("Alle anzeigen")
                {
                    X = 2,
                    Y = 1 + toShow,
                    ColorScheme = schemes[2]
                };
                showAllBtn.Clicked += () => ExpandExtraWindow("article", schemes);
                articleWin.Add(showAllBtn);
            }

            articleWin.Height = 4 + (useShowAllButton ? (toShow + 1) : toShow);

            Application.Top.SetNeedsDisplay();
        }

        private void FillCustomerWindow(Window customerWin, List<ColorScheme> schemes, bool showAll)
        {
            customerWin.RemoveAll();

            var customers = erpManager!.GetAllCustomers();
            int total = customers.Count;
            bool useShowAllButton = !showAll && total >= 7;
            int toShow = showAll ? total : Math.Min(6, total);
            if (useShowAllButton) toShow = 5;

            for (int i = 0; i < toShow; i++)
            {
                var c = customers[i];
                var nameLabel = new Label(c.Name)
                {
                    X = 2,
                    Y = 1 + i,
                    ColorScheme = schemes[1]
                };
                customerWin.Add(nameLabel);

                int intend = nameLabel.Frame.Width + 3;

                var extraInfoLabel = new Label($"(ID: {c.Id})")
                {
                    X = intend,
                    Y = 1 + i,
                    ColorScheme = schemes[4]
                };
                customerWin.Add(extraInfoLabel);
            }

            if (useShowAllButton)
            {
                var showAllBtn = new Button("Alle anzeigen")
                {
                    X = 2,
                    Y = 1 + toShow,
                    ColorScheme = schemes[2]
                };
                showAllBtn.Clicked += () => ExpandExtraWindow("customer", schemes);
                customerWin.Add(showAllBtn);
            }

            customerWin.Height = 4 + (useShowAllButton ? (toShow + 1) : toShow);

            Application.Top.SetNeedsDisplay();
        }

        private void FillSectionWindow(Window sectionWin, List<ColorScheme> schemes, bool showAll)
        {
            sectionWin.RemoveAll();

            var sections = erpManager!.GetAllSections();
            int total = sections.Count;
            bool useShowAllButton = !showAll && total >= 7;
            int toShow = showAll ? total : Math.Min(6, total);
            if (useShowAllButton) toShow = 5;

            for (int i = 0; i < toShow; i++)
            {
                var s = sections[i];
                var nameLabel = new Label(s.Name)
                {
                    X = 2,
                    Y = 1 + i,
                    ColorScheme = schemes[1]
                };
                sectionWin.Add(nameLabel);

                int intend = nameLabel.Frame.Width + 3;

                var extraInfoLabel = new Label($"(ID: {s.Id})")
                {
                    X = intend,
                    Y = 1 + i,
                    ColorScheme = schemes[4]
                };
                sectionWin.Add(extraInfoLabel);
            }

            if (useShowAllButton)
            {
                var showAllBtn = new Button("Alle anzeigen")
                {
                    X = 2,
                    Y = 1 + toShow,
                    ColorScheme = schemes[2]
                };
                showAllBtn.Clicked += () => ExpandExtraWindow("section", schemes);
                sectionWin.Add(showAllBtn);
            }

            sectionWin.Height = 4 + (useShowAllButton ? (toShow + 1) : toShow);

            Application.Top.SetNeedsDisplay();
        }

        private void FillEmployeeWindow(Window employeeWin, List<ColorScheme> schemes, bool showAll)
        {
            employeeWin.RemoveAll();

            var employees = erpManager!.GetAllEmployees();
            int total = employees.Count;
            bool useShowAllButton = !showAll && total >= 7;
            int toShow = showAll ? total : Math.Min(6, total);
            if (useShowAllButton) toShow = 5;

            for (int i = 0; i < toShow; i++)
            {
                var e = employees[i];
                var nameLabel = new Label(e.Name)
                {
                    X = 2,
                    Y = 1 + i,
                    ColorScheme = schemes[1]
                };
                employeeWin.Add(nameLabel);

                int intend = nameLabel.Frame.Width + 3;

                var extraInfoLabel = new Label($"(ID: {e.Id}, Abt. {(e.worksIn == null ? "-" : e.worksIn.Id)})")
                {
                    X = intend,
                    Y = 1 + i,
                    ColorScheme = schemes[4]
                };
                employeeWin.Add(extraInfoLabel);
            }

            if (useShowAllButton)
            {
                var showAllBtn = new Button("Alle anzeigen")
                {
                    X = 2,
                    Y = 1 + toShow,
                    ColorScheme = schemes[2]
                };
                showAllBtn.Clicked += () => ExpandExtraWindow("employee", schemes);
                employeeWin.Add(showAllBtn);
            }

            employeeWin.Height = 4 + (useShowAllButton ? (toShow + 1) : toShow);

            Application.Top.SetNeedsDisplay();
        }

        private void FillOrderWindow(Window orderWin, List<ColorScheme> schemes)
        {
            orderWin.RemoveAll();

            // Filter out completed orders

            var visibleOrders = erpManager!.GetAllOrders()
                .Where(o => o.Status == OrderStatus.Pending || (ShowCompletedOrders && o.Status == OrderStatus.Completed) || (ShowCancelledOrders && o.Status == OrderStatus.Cancelled))
                .OrderBy(o => o.Id)
                .ToList();

            int total = visibleOrders.Count;
            bool useShowAllButton = total >= 7;
            int toShow = Math.Min(6, total);
            if (useShowAllButton) toShow = 5;

            for (int i = 0; i < toShow; i++)
            {
                var o = visibleOrders[i];
                var baseLabel = new Label("Bestellung")
                {
                    X = 2,
                    Y = 1 + i,
                    ColorScheme = schemes[1]
                };
                orderWin.Add(baseLabel);

                int nextX = baseLabel.Frame.Width + 3;

                (string statusText, Color statusColor) = o.Status switch
                {
                    OrderStatus.Pending => ("Ausstehend", Color.BrightBlue),
                    OrderStatus.Cancelled => ("Abgebrochen", Color.BrightRed),
                    OrderStatus.Completed => ("Abgeschlossen", Color.Green),
                    _ => (o.Status.ToString(), Color.Gray)
                };
                var statusAttr = Application.Driver.MakeAttribute(statusColor, Color.Black);
                var statusLabel = new Label($"[{statusText}]")
                {
                    X = nextX,
                    Y = 1 + i,
                    ColorScheme = new ColorScheme { Normal = statusAttr, Focus = statusAttr, HotNormal = statusAttr, HotFocus = statusAttr }
                };
                orderWin.Add(statusLabel);
                nextX += statusLabel.Frame.Width + 1;

                var infoLabel = new Label($"(ID: {o.Id}, Kunde: {(o.Customer == null ? "-" : o.Customer.Id)}, {o.Articles.Count} Artikel)")
                {
                    X = nextX,
                    Y = 1 + i,
                    ColorScheme = schemes[4]
                };
                orderWin.Add(infoLabel);
                nextX += infoLabel.Frame.Width + 1;

                var detailButton = new Button("Details")
                {
                    X = nextX,
                    Y = 1 + i,
                    ColorScheme = schemes[2]
                };
                var localOrder = o;
                detailButton.Clicked += () => ShowOrderDetails(localOrder, schemes);
                orderWin.Add(detailButton);
            }

            if (useShowAllButton)
            {
                var showAllBtn = new Button("Alle anzeigen")
                {
                    X = 2,
                    Y = 1 + toShow,
                    ColorScheme = schemes[2]
                };
                showAllBtn.Clicked += () => ExpandExtraWindow("order", schemes);
                orderWin.Add(showAllBtn);
            }

            orderWin.Height = 4 + (useShowAllButton ? (toShow + 1) : toShow);
            Application.Top.SetNeedsDisplay();
        }

        private void FillPricesWindow(Window pricesWin, List<ColorScheme> schemes, bool showAll)
        {
            pricesWin.RemoveAll();

            var priceLists = erpManager!.GetAllPrices();
            int total = priceLists.Count;
            bool useShowAllButton = !showAll && total >= 7;
            int toShow = showAll ? total : Math.Min(6, total);
            if (useShowAllButton) toShow = 5;

            for (int i = 0; i < toShow; i++)
            {
                var pl = priceLists[i];
                var nameLabel = new Label("Preisliste")
                {
                    X = 2,
                    Y = 1 + i,
                    ColorScheme = schemes[1]
                };
                pricesWin.Add(nameLabel);

                int intend = nameLabel.Frame.Width + 3;

                var extraInfoLabel = new Label($"(ID: {pl.Id}, Artikelanzahl: {pl.PriceList.Count})")
                {
                    X = intend,
                    Y = 1 + i,
                    ColorScheme = schemes[4]
                };
                pricesWin.Add(extraInfoLabel);

                // Details button to inspect this price list
                var plLocal = pl;
                var detailBtn = new Button("Details")
                {
                    X = Pos.Right(extraInfoLabel) + 2,
                    Y = 1 + i,
                    ColorScheme = schemes[2]
                };
                detailBtn.Clicked += () => ShowPricesDetails(plLocal, schemes);
                pricesWin.Add(detailBtn);
            }

            if (useShowAllButton)
            {
                var showAllBtn = new Button("Alle anzeigen")
                {
                    X = 2,
                    Y = 1 + toShow,
                    ColorScheme = schemes[2]
                };
                showAllBtn.Clicked += () => ExpandExtraWindow("prices", schemes);
                pricesWin.Add(showAllBtn);
            }

            pricesWin.Height = 4 + (useShowAllButton ? (toShow + 1) : toShow);

            Application.Top.SetNeedsDisplay();
        }

        private void FillPaymentTermsWindow(Window paymentTermsWin, List<ColorScheme> schemes, bool showAll)
        {
            paymentTermsWin.RemoveAll();

            var paymentTerms = erpManager!.GetAllPaymentTerms();
            int total = paymentTerms.Count;
            bool useShowAllButton = !showAll && total >= 7;
            int toShow = showAll ? total : Math.Min(6, total);
            if (useShowAllButton) toShow = 5;

            for (int i = 0; i < toShow; i++)
            {
                var pt = paymentTerms[i];
                var nameLabel = new Label(pt.Name)
                {
                    X = 2,
                    Y = 1 + i,
                    ColorScheme = schemes[1]
                };
                paymentTermsWin.Add(nameLabel);

                int intend = nameLabel.Frame.Width + 3;

                var extraInfoLabel = new Label($"(ID: {pt.Id}, Tage: {pt.DaysUntilDue})")
                {
                    X = intend,
                    Y = 1 + i,
                    ColorScheme = schemes[4]
                };
                paymentTermsWin.Add(extraInfoLabel);

                // Details button to inspect this payment terms entry
                var ptLocal = pt;
                var detailBtn = new Button("Details")
                {
                    X = Pos.Right(extraInfoLabel) + 2,
                    Y = 1 + i,
                    ColorScheme = schemes[2]
                };
                detailBtn.Clicked += () => ShowPaymentTermsDetails(ptLocal, schemes);
                paymentTermsWin.Add(detailBtn);
            }

            if (useShowAllButton)
            {
                var showAllBtn = new Button("Alle anzeigen")
                {
                    X = 2,
                    Y = 1 + toShow,
                    ColorScheme = schemes[2]
                };
                showAllBtn.Clicked += () => ExpandExtraWindow("paymentTerms", schemes);
                paymentTermsWin.Add(showAllBtn);
            }

            paymentTermsWin.Height = 4 + (useShowAllButton ? (toShow + 1) : toShow);

            Application.Top.SetNeedsDisplay();
        }

        private void FillBillWindow(Window billWin, List<ColorScheme> schemes, bool showAll)
        {
            billWin.RemoveAll();

            var bills = erpManager!.GetAllBills();
            int total = bills.Count;
            bool useShowAllButton = !showAll && total >= 7;
            int toShow = showAll ? total : Math.Min(6, total);
            if (useShowAllButton) toShow = 5;

            for (int i = 0; i < toShow; i++)
            {
                var b = bills[i];
                var nameLabel = new Label("Rechnung")
                {
                    X = 2,
                    Y = 1 + i,
                    ColorScheme = schemes[1]
                };
                billWin.Add(nameLabel);

                int intend = nameLabel.Frame.Width + 3;

                var due = PaymentTerms.GetDueDate(DateOnly.FromDateTime(DateTime.Now.Date), b.PaymentTerms.DaysUntilDue).ToString("dd.MM.yyyy");
                var extraInfoLabel = new Label($"(ID: {b.Id}, Best: {(b.Order == null ? "-" : b.Order.Id.ToString())}, Zahlungsbed: {(b.PaymentTerms == null ? "-" : b.PaymentTerms.Id.ToString())}, Fällig: {due}, Gesamt: {erpManager!.FormatAmount(b.TotalPrice)})")
                {
                    X = intend,
                    Y = 1 + i,
                    ColorScheme = schemes[4]
                };
                billWin.Add(extraInfoLabel);
            }

            if (useShowAllButton)
            {
                var showAllBtn = new Button("Alle anzeigen")
                {
                    X = 2,
                    Y = 1 + toShow,
                    ColorScheme = schemes[2]
                };
                showAllBtn.Clicked += () => ExpandExtraWindow("bill", schemes);
                billWin.Add(showAllBtn);
            }

            billWin.Height = 4 + (useShowAllButton ? (toShow + 1) : toShow);

            Application.Top.SetNeedsDisplay();
        }
        
        private void FillSelfOrderWindow(Window selfOrderWin, List<ColorScheme> schemes, bool showAll)
        {
            selfOrderWin.RemoveAll();

            var selfOrders = erpManager!.GetAllSelfOrders();
            int total = selfOrders.Count;
            bool useShowAllButton = !showAll && total >= 7;
            int toShow = showAll ? total : Math.Min(6, total);
            if (useShowAllButton) toShow = 5;

            for (int i = 0; i < toShow; i++)
            {
                var so = selfOrders[i];

                // Base label
                var baseLabel = new Label("Eigene Bestellung")
                {
                    X = 2,
                    Y = 1 + i,
                    ColorScheme = schemes[1]
                };
                selfOrderWin.Add(baseLabel);

                int nextX = baseLabel.Frame.Width + 3;

                // Progress arrived/total (count of lines)
                int arrivedCount = so.Arrived.Count;
                int totalCount = so.Arrived.Count + so.Articles.Count;

                // Status label (German names, colored)
                (string statusText, Color statusColor) = so.Status switch
                {
                    OrderStatus.Pending => ("Ausstehend", Color.BrightBlue),
                    OrderStatus.Cancelled => ("Abgebrochen", Color.BrightRed),
                    OrderStatus.Completed => ("Abgeschlossen", Color.Green),
                    _ => (so.Status.ToString(), Color.Gray)
                };
                var statusAttr = Application.Driver.MakeAttribute(statusColor, Color.Black);
                var statusLabel = new Label($"[{statusText} {arrivedCount}/{totalCount}]")
                {
                    X = nextX,
                    Y = 1 + i,
                    ColorScheme = new ColorScheme { Normal = statusAttr, Focus = statusAttr, HotNormal = statusAttr, HotFocus = statusAttr }
                };
                selfOrderWin.Add(statusLabel);
                nextX += statusLabel.Frame.Width + 1;

                // Extra info (ID and remaining line count)
                var extraInfoLabel = new Label($"(ID: {so.Id}, Artikelanzahl: {so.Articles.Count})")
                {
                    X = nextX,
                    Y = 1 + i,
                    ColorScheme = schemes[4]
                };
                selfOrderWin.Add(extraInfoLabel);
                nextX += extraInfoLabel.Frame.Width + 2;

                // Details button to inspect this self order
                var soLocal = so;
                var detailBtn = new Button("Details")
                {
                    X = nextX,
                    Y = 1 + i,
                    ColorScheme = schemes[2]
                };
                detailBtn.Clicked += () => ShowSelfOrderDetails(soLocal, schemes);
                selfOrderWin.Add(detailBtn);
            }

            if (useShowAllButton)
            {
                var showAllBtn = new Button("Alle anzeigen")
                {
                    X = 2,
                    Y = 1 + toShow,
                    ColorScheme = schemes[2]
                };
                showAllBtn.Clicked += () => ExpandExtraWindow("selfOrder", schemes);
                selfOrderWin.Add(showAllBtn);
            }

            selfOrderWin.Height = 4 + (useShowAllButton ? (toShow + 1) : toShow);

            Application.Top.SetNeedsDisplay();
        }

        private void ShowPaymentTermsDetails(PaymentTerms paymentTerms, List<ColorScheme> schemes)
        {
            if (!windows.ContainsKey("paymentTerms")) return;

            // Hide other secondary windows
            foreach (var kv in windows)
            {
                try { kv.Value.Visible = false; } catch { }
            }

            var detailWin = new Window($"Zahlungsbedingung {paymentTerms.Id} - Details")
            {
                X = 42,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = schemes[0]
            };

            int y = 1;
            // Basic info
            detailWin.Add(new Label($"ID: {paymentTerms.Id}") { X = 2, Y = y++, ColorScheme = schemes[1] });
            detailWin.Add(new Label($"Name: {paymentTerms.Name}") { X = 2, Y = y++, ColorScheme = schemes[1] });
            detailWin.Add(new Label($"Tage bis Fälligkeit: {paymentTerms.DaysUntilDue}") { X = 2, Y = y++, ColorScheme = schemes[1] });
            detailWin.Add(new Label($"Skonto: {paymentTerms.DiscountDays} Tage") { X = 2, Y = y++, ColorScheme = schemes[1] });
            detailWin.Add(new Label($"Skonto: {paymentTerms.DiscountPercent}%") { X = 2, Y = y++, ColorScheme = schemes[1] });
            detailWin.Add(new Label($"Verzugszins: {paymentTerms.PenaltyRate}%") { X = 2, Y = y++, ColorScheme = schemes[1] });
            detailWin.Add(new Label($"Absolute Mahngebühr: {paymentTerms.AbsolutePenalty}") { X = 2, Y = y++, ColorScheme = schemes[1] });

            
            y++;
            var closeBtn = new Button("Schließen")
            {
                X = 2,
                Y = y,
                ColorScheme = schemes[2]
            };
            closeBtn.Clicked += () =>
            {
                Application.Top.Remove(detailWin);
                try { detailWin.Dispose(); } catch { }
                foreach (var kv in windows)
                {
                    try { kv.Value.Visible = true; } catch { }
                }
                if (windows.TryGetValue("paymentTerms", out var pw))
                {
                    FillPaymentTermsWindow(pw, Schemes(), false);
                }
                Application.Top.SetNeedsDisplay();
            };
            detailWin.Add(closeBtn);

            Application.Top.Add(detailWin);
            detailWin.SetFocus();
            Application.Top.SetNeedsDisplay();
        }

        private void ShowPricesDetails(Prices prices, List<ColorScheme> schemes)
        {
            if (!windows.ContainsKey("prices")) return;

            // Hide other secondary windows
            foreach (var kv in windows)
            {
                try { kv.Value.Visible = false; } catch { }
            }

            var detailWin = new Window($"Preisliste {prices.Id} - Details")
            {
                X = 42,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = schemes[0]
            };

            int y = 1;
            // Basic info
            detailWin.Add(new Label($"ID: {prices.Id}") { X = 2, Y = y++, ColorScheme = schemes[1] });
            detailWin.Add(new Label($"Artikelanzahl: {prices.PriceList.Count}") { X = 2, Y = y++, ColorScheme = schemes[1] });
            y++;

            detailWin.Add(new Label("Einträge:") { X = 2, Y = y++, ColorScheme = schemes[1] });

            if (prices.PriceList.Count == 0)
            {
                detailWin.Add(new Label("(Keine Einträge)") { X = 4, Y = y++, ColorScheme = schemes[4] });
            }
            else
            {
                foreach (var kv in prices.PriceList.OrderBy(k => k.Key.Id))
                {
                    var at = kv.Key;
                    var price = kv.Value;
                    string priceTxt = erpManager!.FormatAmount(price);
                    string line = $"{at.Name} (ID: {at.Id}, Preis: {priceTxt})";
                    detailWin.Add(new Label(line) { X = 4, Y = y++, ColorScheme = schemes[4] });
                }
            }

            y++;
            var closeBtn = new Button("Schließen")
            {
                X = 2,
                Y = y,
                ColorScheme = schemes[2]
            };
            closeBtn.Clicked += () =>
            {
                Application.Top.Remove(detailWin);
                try { detailWin.Dispose(); } catch { }
                foreach (var kv in windows)
                {
                    try { kv.Value.Visible = true; } catch { }
                }
                if (windows.TryGetValue("prices", out var pw))
                {
                    FillPricesWindow(pw, Schemes(), false);
                }
                Application.Top.SetNeedsDisplay();
            };
            detailWin.Add(closeBtn);

            Application.Top.Add(detailWin);
            detailWin.SetFocus();
            Application.Top.SetNeedsDisplay();
        }

        private void ShowOrderDetails(Order order, List<ColorScheme> schemes)
        {
            if (!windows.ContainsKey("order")) return; // Safety

            // Hide other secondary windows (keep main)
            foreach (var kv in windows)
            {
                try { kv.Value.Visible = false; } catch { }
            }

            var detailWin = new Window($"Bestellung {order.Id} - Details")
            {
                X = 42,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = schemes[0]
            };

            var germanStatus = order.Status switch
            {
                OrderStatus.Pending => "Ausstehend",
                OrderStatus.Completed => "Abgeschlossen",
                OrderStatus.Cancelled => "Abgebrochen",
                _ => "Unbekannt"
            };

            // Basic order info
            string customerInfo = order.Customer == null ? "-" : $"{order.Customer.Name} (ID {order.Customer.Id})";
            var infoLines = new List<string>
            {
                $"ID: {order.Id}",
                $"Kunde: {customerInfo}",
                $"Status: {germanStatus}",
                $"Artikelanzahl: {order.Articles.Count}"
            };

            int y = 1;
            foreach (var line in infoLines)
            {
                detailWin.Add(new Label(line)
                {
                    X = 2,
                    Y = y,
                    ColorScheme = schemes[1]
                });
                y++;
            }

            y++; // spacer

            detailWin.Add(new Label("Artikel:")
            {
                X = 2,
                Y = y,
                ColorScheme = schemes[1]
            });
            y++;

            if (order.Articles.Count == 0)
            {
                detailWin.Add(new Label("(Keine Artikel)") { X = 4, Y = y, ColorScheme = schemes[4] });
                y++;
            }
            else
            {
                // List each article item
                foreach (var item in order.Articles)
                {
                    var slot = erpManager!.FindStorageSlot(item);
                    string slotTxt = slot == null ? "-" : slot.Id.ToString();
                    string line = $"{item.Type.Name} (ID: {item.Type.Id}, PosID: {item.Id}, Menge: {item.Stock}, Lagerplatz: {slotTxt})";
                    detailWin.Add(new Label(line)
                    {
                        X = 4,
                        Y = y,
                        ColorScheme = schemes[4]
                    });
                    y++;
                }
            }

            y += 1;

            // Action buttons
            var closeBtn = new Button("Schließen")
            {
                X = 2,
                Y = y,
                ColorScheme = schemes[2]
            };
            closeBtn.Clicked += () =>
            {
                Application.Top.Remove(detailWin);
                try { detailWin.Dispose(); } catch { }
                // Restore all windows
                foreach (var kv in windows)
                {
                    try { kv.Value.Visible = true; } catch { }
                }
                // Refresh orders (status might have changed)
                if (windows.TryGetValue("order", out var ow))
                {
                    FillOrderWindow(ow, Schemes());
                }
                Application.Top.SetNeedsDisplay();
            };
            detailWin.Add(closeBtn);

            if (order.Status == OrderStatus.Pending)
            {
                var completeBtn = new Button("Abschließen")
                {
                    X = Pos.Right(closeBtn) + 2,
                    Y = y,
                    ColorScheme = schemes[2]
                };
                completeBtn.Clicked += () =>
                {
                    erpManager!.FinishOrder(order);
                    FillOrderWindow(windows["order"], Schemes());
                    Application.Top.Remove(detailWin);
                    ShowOrderDetails(order, schemes); // refresh details
                    Application.Top.SetNeedsDisplay();
                };
                detailWin.Add(completeBtn);

                var cancelBtn = new Button("Abbrechen")
                {
                    X = Pos.Right(completeBtn) + 2,
                    Y = y,
                    ColorScheme = schemes[2]
                };
                cancelBtn.Clicked += () =>
                {
                    erpManager!.CancelOrder(order);
                    FillOrderWindow(windows["order"], Schemes());
                    Application.Top.Remove(detailWin);
                    ShowOrderDetails(order, schemes); // refresh details
                    Application.Top.SetNeedsDisplay();
                };
                detailWin.Add(cancelBtn);
            }

            Application.Top.Add(detailWin);
            detailWin.SetFocus();
            Application.Top.SetNeedsDisplay();
        }

        private void ShowSelfOrderDetails(SelfOrder selfOrder, List<ColorScheme> schemes)
        {
            if (!windows.ContainsKey("selfOrder")) return; // Safety

            // Hide other secondary windows
            foreach (var kv in windows)
            {
                try { kv.Value.Visible = false; } catch { }
            }

            var detailWin = new Window($"Eigenbestellung {selfOrder.Id} - Details")
            {
                X = 42,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = schemes[0]
            };

            var germanStatus = selfOrder.Status switch
            {
                OrderStatus.Pending => "Ausstehend",
                OrderStatus.Completed => "Abgeschlossen",
                OrderStatus.Cancelled => "Abgebrochen",
                _ => "Unbekannt"
            };

            int y = 1;
            detailWin.Add(new Label($"ID: {selfOrder.Id}") { X = 2, Y = y++, ColorScheme = schemes[1] });
            detailWin.Add(new Label($"Status: {germanStatus}") { X = 2, Y = y++, ColorScheme = schemes[1] });
            detailWin.Add(new Label($"Artikelanzahl: {selfOrder.Articles.Count}") { X = 2, Y = y++, ColorScheme = schemes[1] });
            y++;

            detailWin.Add(new Label("Bestellte Artikel:") { X = 2, Y = y++, ColorScheme = schemes[1] });
            if (selfOrder.Articles.Count == 0)
            {
                detailWin.Add(new Label("(Keine offenen Artikel)") { X = 4, Y = y++, ColorScheme = schemes[4] });
            }
            else
            {
                foreach (var item in selfOrder.Articles)
                {
                    var slot = erpManager!.FindStorageSlot(item);
                    string slotTxt = slot == null ? "-" : slot.Id.ToString();
                    string line = $"{item.Type.Name} (ID: {item.Type.Id}, PosID: {item.Id}, Menge offen: {item.Stock}, Lagerplatz: {slotTxt})";
                    detailWin.Add(new Label(line) { X = 4, Y = y++, ColorScheme = schemes[4] });
                }
            }

            y++;
            detailWin.Add(new Label("Bereits eingetroffen:") { X = 2, Y = y++, ColorScheme = schemes[1] });
            if (selfOrder.Arrived.Count == 0)
            {
                detailWin.Add(new Label("(Noch nichts eingetroffen)") { X = 4, Y = y++, ColorScheme = schemes[4] });
            }
            else
            {
                foreach (var item in selfOrder.Arrived)
                {
                    string line = $"{item.Type.Name} (ID: {item.Type.Id}, PosID: {item.Id}, Menge: {item.Stock})";
                    detailWin.Add(new Label(line) { X = 4, Y = y++, ColorScheme = schemes[4] });
                }
            }

            y += 1;

            var closeBtn = new Button("Schließen") { X = 2, Y = y, ColorScheme = schemes[2] };
            closeBtn.Clicked += () =>
            {
                Application.Top.Remove(detailWin);
                try { detailWin.Dispose(); } catch { }
                foreach (var kv in windows)
                {
                    try { kv.Value.Visible = true; } catch { }
                }
                if (windows.TryGetValue("selfOrder", out var sw))
                {
                    FillSelfOrderWindow(sw, Schemes(), false);
                }
                Application.Top.SetNeedsDisplay();
            };
            detailWin.Add(closeBtn);

            if (selfOrder.Status == OrderStatus.Pending)
            {
                var completeBtn = new Button("Abschließen")
                {
                    X = Pos.Right(closeBtn) + 2,
                    Y = y,
                    ColorScheme = schemes[2]
                };
                completeBtn.Clicked += () =>
                {
                    erpManager!.FinishSelfOrder(selfOrder);
                    FillSelfOrderWindow(windows["selfOrder"], Schemes(), false);
                    Application.Top.Remove(detailWin);
                    ShowSelfOrderDetails(selfOrder, schemes);
                    Application.Top.SetNeedsDisplay();
                };
                detailWin.Add(completeBtn);

                var cancelBtn = new Button("Abbrechen")
                {
                    X = Pos.Right(completeBtn) + 2,
                    Y = y,
                    ColorScheme = schemes[2]
                };
                cancelBtn.Clicked += () =>
                {
                    erpManager!.CancelSelfOrder(selfOrder);
                    FillSelfOrderWindow(windows["selfOrder"], Schemes(), false);
                    Application.Top.Remove(detailWin);
                    ShowSelfOrderDetails(selfOrder, schemes);
                    Application.Top.SetNeedsDisplay();
                };
                detailWin.Add(cancelBtn);
            }

            Application.Top.Add(detailWin);
            detailWin.SetFocus();
            Application.Top.SetNeedsDisplay();
        }

        // Expand a specific extra window to full available space next to the main window,
        // hide all other extra windows, and render all entries plus a "Zurück" button.
        private void ExpandExtraWindow(string key, List<ColorScheme> schemes)
        {
            if (!windows.ContainsKey(key)) return;

            // Hide others
            foreach (var kv in windows)
            {
                if (kv.Key == key) continue;
                try { kv.Value.Visible = false; } catch { }
            }

            var w = windows[key];
            // Maximize selected window to the right of main window
            w.X = 42;
            w.Y = 1;
            w.Width = Dim.Fill();
            w.Height = Dim.Fill();

            // Paginated render with a back button
            int page = 0;
            const int pageSize = 20;

            void RenderPageArticleTypes()
            {
                w.RemoveAll();
                var backBtn = new Button("Zurück") { X = 2, Y = 1, ColorScheme = schemes[2] };
                backBtn.Clicked += () => RestoreExtraWindowsLayout(schemes);
                w.Add(backBtn);
                var data = erpManager!.GetAllArticleTypes();
                int totalPages = Math.Max(1, (int)Math.Ceiling(data.Count / (double)pageSize));
                page = Math.Min(Math.Max(0, page), totalPages - 1);
                int start = page * pageSize;
                int end = Math.Min(start + pageSize, data.Count);
                int y = 3;
                for (int i = start; i < end; i++)
                {
                    var at = data[i];
                    var nameLabel = new Label(at.Name) { X = 2, Y = y, ColorScheme = schemes[1] };
                    w.Add(nameLabel);
                    var info = new Label($"(ID: {at.Id})") { X = Pos.Right(nameLabel) + 1, Y = y, ColorScheme = schemes[4] };
                    w.Add(info);
                    y++;
                }
                AddPagerControls(w, schemes, page, totalPages, () => { page--; RenderPageArticleTypes(); }, () => { page++; RenderPageArticleTypes(); });
            }

            void RenderPageStorageSlots()
            {
                w.RemoveAll();
                var backBtn = new Button("Zurück") { X = 2, Y = 1, ColorScheme = schemes[2] };
                backBtn.Clicked += () => RestoreExtraWindowsLayout(schemes);
                w.Add(backBtn);
                var data = erpManager!.GetAllStorageSlots();
                int totalPages = Math.Max(1, (int)Math.Ceiling(data.Count / (double)pageSize));
                page = Math.Min(Math.Max(0, page), totalPages - 1);
                int start = page * pageSize;
                int end = Math.Min(start + pageSize, data.Count);
                int y = 3;
                for (int i = start; i < end; i++)
                {
                    var ss = data[i];
                    var nameLabel = new Label("Lagerplatz") { X = 2, Y = y, ColorScheme = schemes[1] };
                    w.Add(nameLabel);
                    var info = new Label($"(ID: {ss.Id})") { X = Pos.Right(nameLabel) + 1, Y = y, ColorScheme = schemes[4] };
                    w.Add(info);
                    y++;
                }
                AddPagerControls(w, schemes, page, totalPages, () => { page--; RenderPageStorageSlots(); }, () => { page++; RenderPageStorageSlots(); });
            }

            void RenderPageArticles()
            {
                w.RemoveAll();
                var backBtn = new Button("Zurück") { X = 2, Y = 1, ColorScheme = schemes[2] };
                backBtn.Clicked += () => RestoreExtraWindowsLayout(schemes);
                w.Add(backBtn);
                var data = erpManager!.GetAllArticles();
                int totalPages = Math.Max(1, (int)Math.Ceiling(data.Count / (double)pageSize));
                page = Math.Min(Math.Max(0, page), totalPages - 1);
                int start = page * pageSize;
                int end = Math.Min(start + pageSize, data.Count);
                int y = 3;
                for (int i = start; i < end; i++)
                {
                    var a = data[i];
                    var nameLabel = new Label(a.Type.Name) { X = 2, Y = y, ColorScheme = schemes[1] };
                    w.Add(nameLabel);
                    var slot = erpManager.FindStorageSlot(a);
                    var info = new Label($"(ID: {a.Id}, Anz. {a.Stock}, Lagerplatz: {(slot == null ? '-' : (char)0)})")
                    {
                        X = Pos.Right(nameLabel) + 1, Y = y, ColorScheme = schemes[4]
                    };
                    info.Text = $"(ID: {a.Id}, Anz. {a.Stock}, Lagerplatz: {(slot == null ? "-" : slot.Id.ToString())})";
                    w.Add(info);
                    y++;
                }
                AddPagerControls(w, schemes, page, totalPages, () => { page--; RenderPageArticles(); }, () => { page++; RenderPageArticles(); });
            }

            void RenderPageCustomers()
            {
                w.RemoveAll();
                var backBtn = new Button("Zurück") { X = 2, Y = 1, ColorScheme = schemes[2] };
                backBtn.Clicked += () => RestoreExtraWindowsLayout(schemes);
                w.Add(backBtn);
                var data = erpManager!.GetAllCustomers();
                int totalPages = Math.Max(1, (int)Math.Ceiling(data.Count / (double)pageSize));
                page = Math.Min(Math.Max(0, page), totalPages - 1);
                int start = page * pageSize;
                int end = Math.Min(start + pageSize, data.Count);
                int y = 3;
                for (int i = start; i < end; i++)
                {
                    var c = data[i];
                    var nameLabel = new Label(c.Name) { X = 2, Y = y, ColorScheme = schemes[1] };
                    w.Add(nameLabel);
                    var info = new Label($"(ID: {c.Id})") { X = Pos.Right(nameLabel) + 1, Y = y, ColorScheme = schemes[4] };
                    w.Add(info);
                    y++;
                }
                AddPagerControls(w, schemes, page, totalPages, () => { page--; RenderPageCustomers(); }, () => { page++; RenderPageCustomers(); });
            }

            void RenderPageSections()
            {
                w.RemoveAll();
                var backBtn = new Button("Zurück") { X = 2, Y = 1, ColorScheme = schemes[2] };
                backBtn.Clicked += () => RestoreExtraWindowsLayout(schemes);
                w.Add(backBtn);
                var data = erpManager!.GetAllSections();
                int totalPages = Math.Max(1, (int)Math.Ceiling(data.Count / (double)pageSize));
                page = Math.Min(Math.Max(0, page), totalPages - 1);
                int start = page * pageSize;
                int end = Math.Min(start + pageSize, data.Count);
                int y = 3;
                for (int i = start; i < end; i++)
                {
                    var s = data[i];
                    var nameLabel = new Label(s.Name) { X = 2, Y = y, ColorScheme = schemes[1] };
                    w.Add(nameLabel);
                    var info = new Label($"(ID: {s.Id})") { X = Pos.Right(nameLabel) + 1, Y = y, ColorScheme = schemes[4] };
                    w.Add(info);
                    y++;
                }
                AddPagerControls(w, schemes, page, totalPages, () => { page--; RenderPageSections(); }, () => { page++; RenderPageSections(); });
            }

            void RenderPageEmployees()
            {
                w.RemoveAll();
                var backBtn = new Button("Zurück") { X = 2, Y = 1, ColorScheme = schemes[2] };
                backBtn.Clicked += () => RestoreExtraWindowsLayout(schemes);
                w.Add(backBtn);
                var data = erpManager!.GetAllEmployees();
                int totalPages = Math.Max(1, (int)Math.Ceiling(data.Count / (double)pageSize));
                page = Math.Min(Math.Max(0, page), totalPages - 1);
                int start = page * pageSize;
                int end = Math.Min(start + pageSize, data.Count);
                int y = 3;
                for (int i = start; i < end; i++)
                {
                    var e = data[i];
                    var nameLabel = new Label(e.Name) { X = 2, Y = y, ColorScheme = schemes[1] };
                    w.Add(nameLabel);
                    var info = new Label($"(ID: {e.Id}, Abt. {(e.worksIn == null ? "-" : e.worksIn.Id.ToString())})") { X = Pos.Right(nameLabel) + 1, Y = y, ColorScheme = schemes[4] };
                    w.Add(info);
                    y++;
                }
                AddPagerControls(w, schemes, page, totalPages, () => { page--; RenderPageEmployees(); }, () => { page++; RenderPageEmployees(); });
            }

            switch (key)
            {
                case "articleType": RenderPageArticleTypes(); break;
                case "storageSlot": RenderPageStorageSlots(); break;
                case "article": RenderPageArticles(); break;
                case "customer": RenderPageCustomers(); break;
                case "section": RenderPageSections(); break;
                case "employee": RenderPageEmployees(); break;
                case "order":
                    w.RemoveAll();
                    var backBtnO = new Button("Zurück") { X = 2, Y = 1, ColorScheme = schemes[2] };
                    backBtnO.Clicked += () => RestoreExtraWindowsLayout(schemes);
                    w.Add(backBtnO);
                    var orders = erpManager!.GetAllOrders().Where(o => o.Status == OrderStatus.Pending || (ShowCompletedOrders && o.Status == OrderStatus.Completed) || (ShowCancelledOrders && o.Status == OrderStatus.Cancelled)).OrderBy(o => o.Id).ToList();
                    int totalPagesO = Math.Max(1, (int)Math.Ceiling(orders.Count / (double)pageSize));
                    page = Math.Min(Math.Max(0, page), totalPagesO - 1);
                    int startO = page * pageSize;
                    int endO = Math.Min(startO + pageSize, orders.Count);
                    int yO = 3;
                    for (int i = startO; i < endO; i++)
                    {
                        var o = orders[i];
                        var baseLabel = new Label($"Bestellung (ID: {o.Id})") { X = 2, Y = yO, ColorScheme = schemes[1] };
                        w.Add(baseLabel);
                        var btn = new Button("Details") { X = Pos.Right(baseLabel) + 2, Y = yO, ColorScheme = schemes[2] };
                        btn.Clicked += () => ShowOrderDetails(o, schemes);
                        w.Add(btn);
                        yO++;
                    }
                    AddPagerControls(w, schemes, page, totalPagesO, () => { page--; windows[key] = w; }, () => { page++; windows[key] = w; });
                    break;
                case "prices":
                    w.RemoveAll();
                    var backBtnP = new Button("Zurück") { X = 2, Y = 1, ColorScheme = schemes[2] };
                    backBtnP.Clicked += () => RestoreExtraWindowsLayout(schemes);
                    w.Add(backBtnP);
                    var priceData = erpManager!.GetAllPrices();
                    int totalPagesP = Math.Max(1, (int)Math.Ceiling(priceData.Count / (double)pageSize));
                    page = Math.Min(Math.Max(0, page), totalPagesP - 1);
                    int startP = page * pageSize;
                    int endP = Math.Min(startP + pageSize, priceData.Count);
                    int yP = 3;
                    for (int i = startP; i < endP; i++)
                    {
                        var p = priceData[i];
                        var lbl = new Label($"Preisliste (ID: {p.Id}, Artikel: {p.PriceList.Count})") { X = 2, Y = yP, ColorScheme = schemes[1] };
                        w.Add(lbl);
                        var db = new Button("Details") { X = Pos.Right(lbl) + 2, Y = yP, ColorScheme = schemes[2] };
                        db.Clicked += () => ShowPricesDetails(p, schemes);
                        w.Add(db);
                        yP++;
                    }
                    AddPagerControls(w, schemes, page, totalPagesP, () => { page--; windows[key] = w; }, () => { page++; windows[key] = w; });
                    break;
                case "paymentTerms":
                    w.RemoveAll();
                    var backBtnT = new Button("Zurück") { X = 2, Y = 1, ColorScheme = schemes[2] };
                    backBtnT.Clicked += () => RestoreExtraWindowsLayout(schemes);
                    w.Add(backBtnT);
                    var termsData = erpManager!.GetAllPaymentTerms();
                    int totalPagesT = Math.Max(1, (int)Math.Ceiling(termsData.Count / (double)pageSize));
                    page = Math.Min(Math.Max(0, page), totalPagesT - 1);
                    int startT = page * pageSize;
                    int endT = Math.Min(startT + pageSize, termsData.Count);
                    int yT = 3;
                    for (int i = startT; i < endT; i++)
                    {
                        var pt = termsData[i];
                        var lbl = new Label($"{pt.Name} (ID: {pt.Id}, Tage: {pt.DaysUntilDue})") { X = 2, Y = yT, ColorScheme = schemes[1] };
                        w.Add(lbl);
                        var db = new Button("Details") { X = Pos.Right(lbl) + 2, Y = yT, ColorScheme = schemes[2] };
                        db.Clicked += () => ShowPaymentTermsDetails(pt, schemes);
                        w.Add(db);
                        yT++;
                    }
                    AddPagerControls(w, schemes, page, totalPagesT, () => { page--; windows[key] = w; }, () => { page++; windows[key] = w; });
                    break;
                case "bill":
                    w.RemoveAll();
                    var backBtnB = new Button("Zurück") { X = 2, Y = 1, ColorScheme = schemes[2] };
                    backBtnB.Clicked += () => RestoreExtraWindowsLayout(schemes);
                    w.Add(backBtnB);
                    var billData = erpManager!.GetAllBills();
                    int totalPagesB = Math.Max(1, (int)Math.Ceiling(billData.Count / (double)pageSize));
                    page = Math.Min(Math.Max(0, page), totalPagesB - 1);
                    int startB = page * pageSize;
                    int endB = Math.Min(startB + pageSize, billData.Count);
                    int yB = 3;
                    for (int i = startB; i < endB; i++)
                    {
                        var b = billData[i];
                        var lbl = new Label($"Rechnung (ID: {b.Id}, Gesamt: {erpManager!.FormatAmount(b.TotalPrice)})") { X = 2, Y = yB, ColorScheme = schemes[1] };
                        w.Add(lbl);
                        yB++;
                    }
                    AddPagerControls(w, schemes, page, totalPagesB, () => { page--; windows[key] = w; }, () => { page++; windows[key] = w; });
                    break;
            }

            Application.Top?.SetNeedsDisplay();
        }

        // Helper to add pagination controls to a window
        private void AddPagerControls(Window w, List<ColorScheme> schemes, int page, int totalPages, Action prev, Action next)
        {
            int y = 2; // place near top-right; adapt if needed
            var pageInfo = new Label($"Seite {page + 1}/{totalPages}")
            {
                X = Pos.AnchorEnd(20),
                Y = y,
                ColorScheme = schemes[4]
            };
            w.Add(pageInfo);

            var prevBtn = new Button("< Vorherige")
            {
                X = Pos.AnchorEnd(38),
                Y = y,
                ColorScheme = schemes[2]
            };
            prevBtn.Clicked += () => { if (page > 0) prev(); };
            w.Add(prevBtn);

            var nextBtn = new Button("Nächste >")
            {
                X = Pos.AnchorEnd(12),
                Y = y,
                ColorScheme = schemes[2]
            };
            nextBtn.Clicked += () => { if (page < totalPages - 1) next(); };
            w.Add(nextBtn);
        }

        private void RestoreExtraWindowsLayout(List<ColorScheme> schemes)
        {
            // Restore sizes/positions and show all windows, then refill in capped mode
            foreach (var kv in windows)
            {
                if (defaultLayouts.TryGetValue(kv.Key, out var layout))
                {
                    kv.Value.X = layout.X;
                    kv.Value.Y = layout.Y;
                    kv.Value.Width = layout.W;
                    kv.Value.Height = layout.H;
                }
                try { kv.Value.Visible = true; } catch { }
            }
            fillAllWindows();
            Application.Top?.SetNeedsDisplay();
        }

        // Full render helpers (all entries, no cap), starting at specified Y
        private void RenderArticleTypesFull(Window win, List<ColorScheme> schemes, int startY)
        {
            var articleTypes = erpManager!.GetAllArticleTypes();
            for (int i = 0; i < articleTypes.Count; i++)
            {
                var at = articleTypes[i];
                var nameLabel = new Label(at.Name)
                {
                    X = 2,
                    Y = startY + i,
                    ColorScheme = schemes[1]
                };
                win.Add(nameLabel);
                int intend = nameLabel.Frame.Width + 3;
                var extraInfoLabel = new Label($"(ID: {at.Id})")
                {
                    X = intend,
                    Y = startY + i,
                    ColorScheme = schemes[4]
                };
                win.Add(extraInfoLabel);
            }
        }

        private void RenderStorageSlotsFull(Window win, List<ColorScheme> schemes, int startY)
        {
            var storageSlots = erpManager!.GetAllStorageSlots();
            for (int i = 0; i < storageSlots.Count; i++)
            {
                var ss = storageSlots[i];
                var nameLabel = new Label("Lagerplatz")
                {
                    X = 2,
                    Y = startY + i,
                    ColorScheme = schemes[1]
                };
                win.Add(nameLabel);
                int intend = nameLabel.Frame.Width + 3;
                var extraInfoLabel = new Label($"(ID: {ss.Id})")
                {
                    X = intend,
                    Y = startY + i,
                    ColorScheme = schemes[4]
                };
                win.Add(extraInfoLabel);
            }
        }

        private void RenderArticlesFull(Window win, List<ColorScheme> schemes, int startY)
        {
            var articles = erpManager!.GetAllArticles();
            for (int i = 0; i < articles.Count; i++)
            {
                var a = articles[i];
                var nameLabel = new Label($"{a.Type.Name}")
                {
                    X = 2,
                    Y = startY + i,
                    ColorScheme = schemes[1]
                };
                win.Add(nameLabel);
                int intend = nameLabel.Frame.Width + 3;
                var slot = erpManager.FindStorageSlot(a);
                var extraInfoLabel = new Label($"(ID: {a.Id}, Anz. {a.Stock}, Lagerplatz: {(slot == null ? "-" : slot.Id.ToString())})")
                {
                    X = intend,
                    Y = startY + i,
                    ColorScheme = schemes[4]
                };
                win.Add(extraInfoLabel);
            }
        }

        private void RenderCustomersFull(Window win, List<ColorScheme> schemes, int startY)
        {
            var customers = erpManager!.GetAllCustomers();
            for (int i = 0; i < customers.Count; i++)
            {
                var c = customers[i];
                var nameLabel = new Label(c.Name)
                {
                    X = 2,
                    Y = startY + i,
                    ColorScheme = schemes[1]
                };
                win.Add(nameLabel);
                int intend = nameLabel.Frame.Width + 3;
                var extraInfoLabel = new Label($"(ID: {c.Id})")
                {
                    X = intend,
                    Y = startY + i,
                    ColorScheme = schemes[4]
                };
                win.Add(extraInfoLabel);
            }
        }

        private void RenderSectionsFull(Window win, List<ColorScheme> schemes, int startY)
        {
            var sections = erpManager!.GetAllSections();
            for (int i = 0; i < sections.Count; i++)
            {
                var s = sections[i];
                var nameLabel = new Label(s.Name)
                {
                    X = 2,
                    Y = startY + i,
                    ColorScheme = schemes[1]
                };
                win.Add(nameLabel);
                int intend = nameLabel.Frame.Width + 3;
                var extraInfoLabel = new Label($"(ID: {s.Id})")
                {
                    X = intend,
                    Y = startY + i,
                    ColorScheme = schemes[4]
                };
                win.Add(extraInfoLabel);
            }
        }

        private void RenderEmployeesFull(Window win, List<ColorScheme> schemes, int startY)
        {
            var employees = erpManager!.GetAllEmployees();
            for (int i = 0; i < employees.Count; i++)
            {
                var e = employees[i];
                var nameLabel = new Label(e.Name)
                {
                    X = 2,
                    Y = startY + i,
                    ColorScheme = schemes[1]
                };
                win.Add(nameLabel);
                int intend = nameLabel.Frame.Width + 3;
                var extraInfoLabel = new Label($"(ID: {e.Id}, Abt. {(e.worksIn == null ? "-" : e.worksIn.Id)})")
                {
                    X = intend,
                    Y = startY + i,
                    ColorScheme = schemes[4]
                };
                win.Add(extraInfoLabel);
            }
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

            Application.Top?.SetNeedsDisplay();
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

            Application.Top?.SetNeedsDisplay();
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
            Application.Top?.SetNeedsDisplay();
        }

        private void CreateCustomer(Window win, List<ColorScheme> schemes, Action doAfter)
        {
            win.RemoveAll();

            var appellLabel = new Label("Name des Kunden:")
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

            // Additional questions for person information
            var streetLabel = new Label("Straße:") { X = 2, Y = 4, ColorScheme = schemes[1] };
            var streetInput = new TextField() { X = 2, Y = 5, Width = 40, ColorScheme = schemes[1] };
            win.Add(streetLabel);
            win.Add(streetInput);

            var cityLabel = new Label("Stadt:") { X = 2, Y = 7, ColorScheme = schemes[1] };
            var cityInput = new TextField() { X = 2, Y = 8, Width = 40, ColorScheme = schemes[1] };
            win.Add(cityLabel);
            win.Add(cityInput);

            var postalLabel = new Label("PLZ:") { X = 2, Y = 10, ColorScheme = schemes[1] };
            var postalInput = new TextField() { X = 2, Y = 11, Width = 40, ColorScheme = schemes[1] };
            win.Add(postalLabel);
            win.Add(postalInput);

            var countryLabel = new Label("Land:") { X = 2, Y = 13, ColorScheme = schemes[1] };
            var countryInput = new TextField() { X = 2, Y = 14, Width = 40, ColorScheme = schemes[1] };
            win.Add(countryLabel);
            win.Add(countryInput);

            var emailLabel = new Label("E-Mail:") { X = 2, Y = 16, ColorScheme = schemes[1] };
            var emailInput = new TextField() { X = 2, Y = 17, Width = 40, ColorScheme = schemes[1] };
            win.Add(emailLabel);
            win.Add(emailInput);

            var phoneLabel = new Label("Telefon:") { X = 2, Y = 19, ColorScheme = schemes[1] };
            var phoneInput = new TextField() { X = 2, Y = 20, Width = 40, ColorScheme = schemes[1] };
            win.Add(phoneLabel);
            win.Add(phoneInput);

            var sendButton = new Button("Ok")
            {
                X = 2,
                Y = 22,
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
                win.Remove(streetLabel); win.Remove(streetInput);
                win.Remove(cityLabel); win.Remove(cityInput);
                win.Remove(postalLabel); win.Remove(postalInput);
                win.Remove(countryLabel); win.Remove(countryInput);
                win.Remove(emailLabel); win.Remove(emailInput);
                win.Remove(phoneLabel); win.Remove(phoneInput);
                win.Remove(sendButton);

                string street = streetInput.Text?.ToString() ?? string.Empty;
                string city = cityInput.Text?.ToString() ?? string.Empty;
                string postal = postalInput.Text?.ToString() ?? string.Empty;
                string country = countryInput.Text?.ToString() ?? string.Empty;
                string email = emailInput.Text?.ToString() ?? string.Empty;
                string phone = phoneInput.Text?.ToString() ?? string.Empty;

                Customer customer = erpManager!.NewCustomer(text, street, city, postal, country, email, phone);

                var finishedText = new Label("Der Kunde wurde erstellt.")
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

            Application.Top?.SetNeedsDisplay();
        }

        private void CreateSection(Window win, List<ColorScheme> schemes, Action doAfter)
        {
            win.RemoveAll();

            var appellLabel = new Label("Name der Abteilung:")
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

                Section section = erpManager!.NewSection(text);

                var finishedText = new Label("Die Abteilung wurde erstellt.")
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

            Application.Top?.SetNeedsDisplay();
        }

        private void CreateEmployee(Window win, List<ColorScheme> schemes, Action doAfter)
        {
            win.RemoveAll();

            var appellLabel = new Label("Name des Mitarbeiters:")
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

            var sectionIdLabel = new Label("Abteilungs ID:")
            {
                X = 2,
                Y = 4,
                ColorScheme = schemes[1]
            };
            win.Add(sectionIdLabel);

            var sectionIdInput = new TextField()
            {
                X = 2,
                Y = 5,
                Width = 40,
                ColorScheme = schemes[1]
            };
            win.Add(sectionIdInput);

            // Additional person information
            var streetLabel = new Label("Straße:") { X = 2, Y = 7, ColorScheme = schemes[1] };
            var streetInput = new TextField() { X = 2, Y = 8, Width = 40, ColorScheme = schemes[1] };
            win.Add(streetLabel); win.Add(streetInput);

            var cityLabel = new Label("Stadt:") { X = 2, Y = 10, ColorScheme = schemes[1] };
            var cityInput = new TextField() { X = 2, Y = 11, Width = 40, ColorScheme = schemes[1] };
            win.Add(cityLabel); win.Add(cityInput);

            var postalLabel = new Label("PLZ:") { X = 2, Y = 13, ColorScheme = schemes[1] };
            var postalInput = new TextField() { X = 2, Y = 14, Width = 40, ColorScheme = schemes[1] };
            win.Add(postalLabel); win.Add(postalInput);

            var countryLabel = new Label("Land:") { X = 2, Y = 16, ColorScheme = schemes[1] };
            var countryInput = new TextField() { X = 2, Y = 17, Width = 40, ColorScheme = schemes[1] };
            win.Add(countryLabel); win.Add(countryInput);

            var emailLabel = new Label("E-Mail:") { X = 2, Y = 19, ColorScheme = schemes[1] };
            var emailInput = new TextField() { X = 2, Y = 20, Width = 40, ColorScheme = schemes[1] };
            win.Add(emailLabel); win.Add(emailInput);

            var phoneLabel = new Label("Telefon:") { X = 2, Y = 22, ColorScheme = schemes[1] };
            var phoneInput = new TextField() { X = 2, Y = 23, Width = 40, ColorScheme = schemes[1] };
            win.Add(phoneLabel); win.Add(phoneInput);

            var sendButton = new Button("Ok")
            {
                X = 2,
                Y = 25,
                ColorScheme = schemes[2]
            };
            sendButton.Clicked += () =>
            {
                var nameText = nameInput.Text?.ToString() ?? string.Empty;
                var sectionIdText = sectionIdInput.Text?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(nameText))
                {
                    doAfter.Invoke();
                    return;
                }

                int? sectionId = null;
                if (!string.IsNullOrWhiteSpace(sectionIdText))
                {
                    if (!int.TryParse(sectionIdText, out int parsed) || parsed < 0)
                    {
                        doAfter.Invoke();
                        return;
                    }
                    sectionId = parsed;
                    if (erpManager!.FindSection(parsed) == null)
                    {
                        doAfter.Invoke();
                        return;
                    }
                }

                win.Remove(appellLabel);
                win.Remove(nameInput);
                win.Remove(sectionIdLabel);
                win.Remove(sectionIdInput);
                win.Remove(streetLabel); win.Remove(streetInput);
                win.Remove(cityLabel); win.Remove(cityInput);
                win.Remove(postalLabel); win.Remove(postalInput);
                win.Remove(countryLabel); win.Remove(countryInput);
                win.Remove(emailLabel); win.Remove(emailInput);
                win.Remove(phoneLabel); win.Remove(phoneInput);
                win.Remove(sendButton);

                // Ensure we pass a non-null Section to NewEmployee
                Section worksInSection;
                if (sectionId.HasValue)
                {
                    worksInSection = erpManager!.FindSection(sectionId.Value)!; // validated above
                }
                else
                {
                    var unassigned = erpManager!.GetAllSections().FirstOrDefault(s => string.Equals(s.Name, "Unassigned", StringComparison.OrdinalIgnoreCase));
                    if (unassigned == null)
                    {
                        unassigned = erpManager.NewSection("Unassigned");
                    }
                    worksInSection = unassigned;
                }

                string street = streetInput.Text?.ToString() ?? string.Empty;
                string city = cityInput.Text?.ToString() ?? string.Empty;
                string postal = postalInput.Text?.ToString() ?? string.Empty;
                string country = countryInput.Text?.ToString() ?? string.Empty;
                string email = emailInput.Text?.ToString() ?? string.Empty;
                string phone = phoneInput.Text?.ToString() ?? string.Empty;

                Employee employee = erpManager!.NewEmployee(nameText, worksInSection, street, city, postal, country, email, phone);

                var finishedText = new Label("Der Mitarbeiter wurde erstellt.")
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

        private void CreateOrder(Window win, List<ColorScheme> schemes, Action doAfter)
        {
            win.RemoveAll();

            var customerIdLabel = new Label("Kunden ID:")
            {
                X = 2,
                Y = 1,
                ColorScheme = schemes[1]
            };
            win.Add(customerIdLabel);

            var customerIdInput = new TextField()
            {
                X = 2,
                Y = 2,
                Width = 40,
                ColorScheme = schemes[1]
            };
            win.Add(customerIdInput);

            var sendButton = new Button("Ok")
            {
                X = 2,
                Y = 4,
                ColorScheme = schemes[2]
            };
            sendButton.Clicked += () =>
            {
                var customerIdText = customerIdInput.Text?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(customerIdText))
                {
                    doAfter.Invoke();
                    return;
                }

                if (!int.TryParse(customerIdText, out int customerId) || customerId < 0)
                {
                    doAfter.Invoke();
                    return;
                }

                var customer = erpManager!.FindCustomer(customerId);
                if (customer == null)
                {
                    doAfter.Invoke();
                    return;
                }

                win.Remove(customerIdLabel);
                win.Remove(customerIdInput);
                win.Remove(sendButton);

                // Step 2: collect multiple OrderItems by Article ID and desired quantity
                var orderItems = new List<OrderItem>();
                var displayItems = new List<string>();

                var artIdLabel = new Label("Artikel ID:")
                {
                    X = 2,
                    Y = 1,
                    ColorScheme = schemes[1]
                };
                win.Add(artIdLabel);

                var artIdInput = new TextField()
                {
                    X = 2,
                    Y = 2,
                    Width = 16,
                    ColorScheme = schemes[1]
                };
                win.Add(artIdInput);

                var qtyLabel = new Label("Menge:")
                {
                    X = 20,
                    Y = 1,
                    ColorScheme = schemes[1]
                };
                win.Add(qtyLabel);

                var qtyInput = new TextField()
                {
                    X = 20,
                    Y = 2,
                    Width = 10,
                    ColorScheme = schemes[1]
                };
                win.Add(qtyInput);

                var addBtn = new Button("Hinzufügen")
                {
                    X = 2,
                    Y = 4,
                    ColorScheme = schemes[2]
                };
                win.Add(addBtn);

                var doneBtn = new Button("Fertig")
                {
                    X = 18,
                    Y = 4,
                    ColorScheme = schemes[2]
                };
                win.Add(doneBtn);

                // ListView to show added items
                var listHeader = new Label("Hinzugefügt:")
                {
                    X = 2,
                    Y = 6,
                    ColorScheme = schemes[4]
                };
                win.Add(listHeader);

                var listView = new ListView(displayItems)
                {
                    X = 2,
                    Y = 7,
                    Width = Dim.Fill() - 2,
                    Height = Dim.Fill() - 8,
                    ColorScheme = schemes[4]
                };
                win.Add(listView);

                Label? errorLabel = null;
                void ShowError(string msg)
                {
                    if (errorLabel != null)
                    {
                        win.Remove(errorLabel);
                        errorLabel = null;
                    }
                    errorLabel = new Label(msg)
                    {
                        X = 2,
                        Y = 5,
                        ColorScheme = schemes[4]
                    };
                    win.Add(errorLabel);
                    Application.Top.SetNeedsDisplay();
                }

                addBtn.Clicked += () =>
                {
                    var artText = artIdInput.Text?.ToString() ?? string.Empty;
                    var qtyText = qtyInput.Text?.ToString() ?? string.Empty;
                    if (!int.TryParse(artText, out var artId) || !int.TryParse(qtyText, out var qty) || qty <= 0)
                    {
                        ShowError("Ungültige Eingabe.");
                        return;
                    }
                    var article = erpManager!.FindArticle(artId);
                    if (article == null)
                    {
                        ShowError($"Artikel mit ID {artId} nicht gefunden.");
                        return;
                    }
                    // Convert to OrderItem using the article's type and requested quantity
                    var orderItem = erpManager.NewOrderItem(article.Type.Id, qty, false);
                    orderItems.Add(orderItem);
                    displayItems.Add($"{article.Type.Name} (ID: {article.Type.Id}, Menge: {qty})");
                    listView.SetSource(displayItems);
                    if (errorLabel != null) { win.Remove(errorLabel); errorLabel = null; }
                    artIdInput.Text = string.Empty;
                    qtyInput.Text = string.Empty;
                    artIdInput.SetFocus();
                    Application.Top.SetNeedsDisplay();
                };

                doneBtn.Clicked += () =>
                {
                    if (orderItems.Count == 0)
                    {
                        ShowError("Bitte mindestens einen Artikel hinzufügen.");
                        return;
                    }
                    // Create order with collected items
                    var order = erpManager.NewOrder(orderItems, customer);

                    // Clean up and show confirmation
                    win.Remove(artIdLabel);
                    win.Remove(artIdInput);
                    win.Remove(qtyLabel);
                    win.Remove(qtyInput);
                    win.Remove(addBtn);
                    win.Remove(doneBtn);
                    win.Remove(listHeader);
                    win.Remove(listView);
                    if (errorLabel != null) { win.Remove(errorLabel); errorLabel = null; }

                    var finishedText = new Label("Die Bestellung wurde erstellt.")
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
                    okButton.Clicked += () => { doAfter.Invoke(); };
                    win.Add(okButton);
                    Application.Top.SetNeedsDisplay();
                };

                // Set initial focus to article id input
                artIdInput.SetFocus();
            };
            win.Add(sendButton);
            Application.Top.SetNeedsDisplay();
        }

        private void CreatePriceList(Window win, List<ColorScheme> schemes, Action doAfter)
        {
            win.RemoveAll();

            var infoLabel = new Label("Bitte Preis für jeden")
            {
                X = 2,
                Y = 1,
                ColorScheme = schemes[1]
            };
            win.Add(infoLabel);

            var infoLabel2 = new Label("Artikeltyp eingeben:")
            {
                X = 2,
                Y = 2,
                ColorScheme = schemes[1]
            };
            win.Add(infoLabel2);

            var types = erpManager!.GetAllArticleTypes().OrderBy(t => t.Id).ToList();
            if (types.Count == 0)
            {
                var noTypes = new Label("(Keine Artikeltypen vorhanden)")
                {
                    X = 2,
                    Y = 4,
                    ColorScheme = schemes[4]
                };
                win.Add(noTypes);

                var backBtn = new Button("Zurück")
                {
                    X = 2,
                    Y = 6,
                    ColorScheme = schemes[2]
                };
                backBtn.Clicked += () => doAfter();
                win.Add(backBtn);
                Application.Top?.SetNeedsDisplay();
                return;
            }

            var inputs = new Dictionary<ArticleType, TextField>();
            int y = 4;
            foreach (var t in types)
            {
                var lbl = new Label($"{t.Name} (ID {t.Id}):")
                {
                    X = 2,
                    Y = y,
                    ColorScheme = schemes[1]
                };
                win.Add(lbl);

                var tf = new TextField("")
                {
                    X = 2,
                    Y = y + 1,
                    Width = 20,
                    ColorScheme = schemes[1]
                };
                win.Add(tf);
                inputs[t] = tf;
                y += 3;
            }

            Label? errorLabel = null;
            void ShowError(string msg)
            {
                if (errorLabel != null)
                {
                    win.Remove(errorLabel);
                }
                errorLabel = new Label(msg)
                {
                    X = 2,
                    Y = y,
                    ColorScheme = schemes[4]
                };
                win.Add(errorLabel);
                Application.Top?.SetNeedsDisplay();
            }

            var okBtn = new Button("Ok")
            {
                X = 2,
                Y = y + 2,
                ColorScheme = schemes[2]
            };
            okBtn.Clicked += () =>
            {
                var priceList = new Dictionary<ArticleType, double>();
                foreach (var kv in inputs)
                {
                    var type = kv.Key;
                    var text = kv.Value.Text.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        ShowError($"Preis für '{type.Name}' darf nicht leer sein.");
                        return;
                    }
                    double value;
                    var styles = System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands;
                    if (!double.TryParse(text, styles, System.Globalization.CultureInfo.CurrentCulture, out value)
                        && !double.TryParse(text, styles, System.Globalization.CultureInfo.InvariantCulture, out value))
                    {
                        ShowError($"Ungültiger Preis für '{type.Name}'.");
                        return;
                    }
                    if (value < 0)
                    {
                        ShowError($"Preis für '{type.Name}' darf nicht negativ sein.");
                        return;
                    }
                    priceList[type] = Math.Round(value, 2);
                }

                erpManager!.NewPrices(priceList);

                win.RemoveAll();
                var finishedText = new Label("Die Preisliste wurde erstellt.")
                {
                    X = 2,
                    Y = 1,
                    ColorScheme = schemes[3]
                };
                win.Add(finishedText);

                var closeBtn = new Button("Fertig")
                {
                    X = 2,
                    Y = 3,
                    ColorScheme = schemes[2]
                };
                closeBtn.Clicked += () => doAfter();
                win.Add(closeBtn);

                Application.Top?.SetNeedsDisplay();
            };
            win.Add(okBtn);

            Application.Top?.SetNeedsDisplay();
        }

        private void CreatePaymentTerms(Window win, List<ColorScheme> schemes, Action doAfter)
        {
            win.RemoveAll();

            // Name/description
            var nameLabel = new Label("Name:") { X = 2, Y = 1, ColorScheme = schemes[1] };
            var nameInput = new TextField() { X = 2, Y = 2, Width = 40, ColorScheme = schemes[1] };
            win.Add(nameLabel);
            win.Add(nameInput);

            // Days until due (integer)
            var daysLabel = new Label("Tage bis fällig (z. B. 30):") { X = 2, Y = 4, ColorScheme = schemes[1] };
            var daysInput = new TextField() { X = 2, Y = 5, Width = 40, ColorScheme = schemes[1] };
            win.Add(daysLabel);
            win.Add(daysInput);

            // Absolute penalty (fixed fee)
            var absPenaltyLabel = new Label("Absolute Mahngebühr (z. B. 5,00):") { X = 2, Y = 7, ColorScheme = schemes[1] };
            var absPenaltyInput = new TextField() { X = 2, Y = 8, Width = 40, ColorScheme = schemes[1] };
            win.Add(absPenaltyLabel);
            win.Add(absPenaltyInput);

            // Optional: Discount days and percent
            var discountDaysLabel = new Label("Skontotage (optional):") { X = 2, Y = 10, ColorScheme = schemes[1] };
            var discountDaysInput = new TextField() { X = 2, Y = 11, Width = 40, ColorScheme = schemes[1] };
            win.Add(discountDaysLabel);
            win.Add(discountDaysInput);

            var discountPercentLabel = new Label("Skonto in % (optional, z. B. 2,0):") { X = 2, Y = 13, ColorScheme = schemes[1] };
            var discountPercentInput = new TextField() { X = 2, Y = 14, Width = 40, ColorScheme = schemes[1] };
            win.Add(discountPercentLabel);
            win.Add(discountPercentInput);

            // Optional: Penalty interest rate
            var penaltyRateLabel = new Label("Verzugszins in % (optional):") { X = 2, Y = 16, ColorScheme = schemes[1] };
            var penaltyRateInput = new TextField() { X = 2, Y = 17, Width = 40, ColorScheme = schemes[1] };
            win.Add(penaltyRateLabel);
            win.Add(penaltyRateInput);

            // Error label placeholder
            Label? errorLabel = null;
            void ShowError(string msg)
            {
                if (errorLabel != null)
                {
                    win.Remove(errorLabel);
                }
                errorLabel = new Label(msg) { X = 2, Y = 19, ColorScheme = schemes[4] };
                win.Add(errorLabel);
                Application.Top?.SetNeedsDisplay();
            }

            var sendButton = new Button("Ok") { X = 2, Y = 21, ColorScheme = schemes[2] };
            sendButton.Clicked += () =>
            {
                var name = nameInput.Text?.ToString()?.Trim() ?? string.Empty;
                var daysStr = daysInput.Text?.ToString()?.Trim() ?? string.Empty;
                var absPenaltyStr = absPenaltyInput.Text?.ToString()?.Trim() ?? string.Empty;
                var discDaysStr = discountDaysInput.Text?.ToString()?.Trim() ?? string.Empty;
                var discPercentStr = discountPercentInput.Text?.ToString()?.Trim() ?? string.Empty;
                var penaltyRateStr = penaltyRateInput.Text?.ToString()?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(name)) { ShowError("Bitte einen Namen angeben."); return; }
                if (!int.TryParse(daysStr, out int days) || days < 0) { ShowError("Bitte gültige Tage bis Fälligkeit angeben."); return; }
                if (!double.TryParse(absPenaltyStr, out double absolutePenalty) || absolutePenalty < 0) { ShowError("Bitte gültige absolute Mahngebühr angeben."); return; }

                int? discountDays = null;
                if (!string.IsNullOrWhiteSpace(discDaysStr))
                {
                    if (int.TryParse(discDaysStr, out int dd) && dd >= 0) discountDays = dd; else { ShowError("Ungültige Skontotage."); return; }
                }

                double? discountPercent = null;
                if (!string.IsNullOrWhiteSpace(discPercentStr))
                {
                    if (double.TryParse(discPercentStr, out double dp) && dp >= 0) discountPercent = dp; else { ShowError("Ungültiger Skonto-Prozentsatz."); return; }
                }

                double? penaltyRate = null;
                if (!string.IsNullOrWhiteSpace(penaltyRateStr))
                {
                    if (double.TryParse(penaltyRateStr, out double pr) && pr >= 0) penaltyRate = pr; else { ShowError("Ungültiger Verzugszins."); return; }
                }

                // Apply defaults if left empty to match method signature optional parameters
                int? ddFinal = discountDays ?? 0;
                double? dpFinal = discountPercent ?? 0.0;
                double? prFinal = penaltyRate ?? 0.0;

                var created = erpManager!.NewPaymentTerms(name, days, absolutePenalty, ddFinal, dpFinal, prFinal);
                if (created == null)
                {
                    ShowError("Zahlungsbedingung konnte nicht erstellt werden.");
                    return;
                }

                win.RemoveAll();
                var finishedText = new Label("Die Zahlungsbedingung wurde erstellt.") { X = 2, Y = 1, ColorScheme = schemes[3] };
                win.Add(finishedText);
                var closeBtn = new Button("Fertig") { X = 2, Y = 3, ColorScheme = schemes[2] };
                closeBtn.Clicked += () => doAfter();
                win.Add(closeBtn);
                Application.Top?.SetNeedsDisplay();
            };
            win.Add(sendButton);

            Application.Top?.SetNeedsDisplay();
        }

        private void CreateBill(Window win, List<ColorScheme> schemes, Action doAfter)
        {
            win.RemoveAll();

            // Inputs: Order ID, Price List ID, Payment Terms ID (optional)
            var orderIdLabel = new Label("Bestellungs ID:") { X = 2, Y = 1, ColorScheme = schemes[1] };
            var orderIdInput = new TextField() { X = 2, Y = 2, Width = 40, ColorScheme = schemes[1] };
            win.Add(orderIdLabel);
            win.Add(orderIdInput);

            var pricesIdLabel = new Label("Preisliste ID:") { X = 2, Y = 4, ColorScheme = schemes[1] };
            var pricesIdInput = new TextField() { X = 2, Y = 5, Width = 40, ColorScheme = schemes[1] };
            win.Add(pricesIdLabel);
            win.Add(pricesIdInput);

            var termsIdLabel = new Label("Zahlungsbed. ID:") { X = 2, Y = 7, ColorScheme = schemes[1] };
            var termsIdInput = new TextField() { X = 2, Y = 8, Width = 40, ColorScheme = schemes[1] };
            win.Add(termsIdLabel);
            win.Add(termsIdInput);

            Label? errorLabel = null;
            void ShowError(string msg)
            {
                if (errorLabel != null)
                {
                    try { win.Remove(errorLabel); } catch { }
                }
                errorLabel = new Label(msg) { X = 2, Y = 10, ColorScheme = schemes[4] };
                win.Add(errorLabel);
                Application.Top?.SetNeedsDisplay();
            }

            var sendButton = new Button("Ok") { X = 2, Y = 12, ColorScheme = schemes[2] };
            sendButton.Clicked += () =>
            {
                // Order validation
                var orderIdText = orderIdInput.Text?.ToString()?.Trim() ?? string.Empty;
                if (!int.TryParse(orderIdText, out int orderId) || orderId < 0)
                {
                    ShowError("Bitte eine gültige Bestellungs-ID angeben.");
                    return;
                }
                var order = erpManager!.GetAllOrders().FirstOrDefault(o => o.Id == orderId);
                if (order == null)
                {
                    ShowError($"Keine Bestellung mit ID {orderId} gefunden.");
                    return;
                }

                // Price list selection
                Prices? priceList = null;
                var pricesIdText = pricesIdInput.Text?.ToString()?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(pricesIdText))
                {
                    // If exactly one exists, pick it; otherwise ask for explicit ID
                    var allPrices = erpManager!.GetAllPrices();
                    if (allPrices.Count == 1)
                    {
                        priceList = allPrices[0];
                    }
                    else
                    {
                        ShowError("Bitte eine Preisliste-ID angeben.");
                        return;
                    }
                }
                else if (int.TryParse(pricesIdText, out int pricesId))
                {
                    priceList = erpManager!.GetAllPrices().FirstOrDefault(p => p.Id == pricesId);
                    if (priceList == null)
                    {
                        ShowError($"Keine Preisliste mit ID {pricesId} gefunden.");
                        return;
                    }
                }
                else
                {
                    ShowError("Bitte eine gültige Preisliste-ID angeben.");
                    return;
                }

                // Validate that price list covers all order items
                foreach (var item in order.Articles)
                {
                    if (!priceList.PriceList.ContainsKey(item.Type))
                    {
                        ShowError($"Preis fehlt für Artikeltp. {item.Type.Name} in Preisliste {priceList.Id}.");
                        return;
                    }
                }

                // Payment terms selection (optional, create default if none exist)
                PaymentTerms? terms = null;
                var termsIdText = termsIdInput.Text?.ToString()?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(termsIdText))
                {
                    ShowError("Bitte eine Zahlungsbedingungs-ID angeben.");
                }
                else if (int.TryParse(termsIdText, out int termsId))
                {
                    terms = erpManager!.GetAllPaymentTerms().FirstOrDefault(t => t.Id == termsId);
                    if (terms == null)
                    {
                        ShowError($"Keine Zahlungsbedingung mit ID {termsId} gefunden.");
                        return;
                    }
                }
                else
                {
                    ShowError("Bitte eine gültige Zahlungsbedingungs-ID angeben.");
                    return;
                }

                // Compute total and create bill
                double total = erpManager!.CalculateOrderTotal(order, priceList);
                if (terms == null)
                {
                    ShowError("Zahlungsbedingungen sind erforderlich.");
                    return;
                }
                var bill = erpManager!.NewBill(order, priceList, terms);
                if (bill == null)
                {
                    ShowError("Rechnung konnte nicht erstellt werden.");
                    return;
                }

                win.RemoveAll();
                var finishedText = new Label($"Die Rechnung wurde erstellt. (B-{bill.Id}, Gesamt: {erpManager!.FormatAmount(total)})")
                {
                    X = 2,
                    Y = 1,
                    ColorScheme = schemes[3]
                };
                win.Add(finishedText);

                // Optional: show due date
                var dueDate = PaymentTerms.GetDueDate(DateOnly.FromDateTime(DateTime.Now), terms.DaysUntilDue).ToString("dd.MM.yyyy");
                win.Add(new Label($"Fällig am: {dueDate}") { X = 2, Y = 2, ColorScheme = schemes[1] });

                var okButton = new Button("Ok") { X = 2, Y = 4, ColorScheme = schemes[2] };
                okButton.Clicked += () => { doAfter.Invoke(); };
                win.Add(okButton);
                Application.Top?.SetNeedsDisplay();
            };
            win.Add(sendButton);
            Application.Top.SetNeedsDisplay();
        }

        private void CreateSelfOrder(Window win, List<ColorScheme> schemes, Action doAfter)
        {
            win.RemoveAll();

            // If there are no article types yet, we can't create a self order
            var types = erpManager!.GetAllArticleTypes();
            if (types.Count == 0)
            {
                win.Add(new Label("Keine Artikeltypen vorhanden. Bitte zuerst Artikeltypen anlegen.")
                {
                    X = 2,
                    Y = 1,
                    ColorScheme = schemes[4]
                });
                var backBtn = new Button("Zurück") { X = 2, Y = 3, ColorScheme = schemes[2] };
                backBtn.Clicked += () => doAfter.Invoke();
                win.Add(backBtn);
                Application.Top.SetNeedsDisplay();
                return;
            }

            // UI: collect order items by ArticleType ID and quantity
            var typeIdLabel = new Label("Artikeltyp ID:") { X = 2, Y = 1, ColorScheme = schemes[1] };
            var typeIdInput = new TextField() { X = 2, Y = 2, Width = 16, ColorScheme = schemes[1] };
            var qtyLabel = new Label("Menge:") { X = 20, Y = 1, ColorScheme = schemes[1] };
            var qtyInput = new TextField() { X = 20, Y = 2, Width = 10, ColorScheme = schemes[1] };
            var addBtn = new Button("Hinzufügen") { X = 2, Y = 4, ColorScheme = schemes[2] };
            var suggestBtn = new Button("Vorschläge übernehmen") { X = 2, Y = 5, ColorScheme = schemes[2] };
            var doneBtn = new Button("Fertig") { X = 2, Y = 6, ColorScheme = schemes[2] };

            win.Add(typeIdLabel);
            win.Add(typeIdInput);
            win.Add(qtyLabel);
            win.Add(qtyInput);
            win.Add(addBtn);
            win.Add(suggestBtn);
            win.Add(doneBtn);

            // List of items (merge duplicates by type)
            var orderItems = new List<OrderItem>();
            var displayItems = new List<string>();

            var listHeader = new Label("Hinzugefügt:") { X = 2, Y = 8, ColorScheme = schemes[4] };
            var listView = new ListView(displayItems)
            {
                X = 2,
                Y = 9,
                Width = Dim.Fill() - 2,
                Height = Dim.Fill() - 8,
                ColorScheme = schemes[4]
            };
            win.Add(listHeader);
            win.Add(listView);

            Label? errorLabel = null;
            void ShowError(string msg)
            {
                if (errorLabel != null)
                {
                    win.Remove(errorLabel);
                    errorLabel = null;
                }
                errorLabel = new Label(msg) { X = 2, Y = 5, ColorScheme = schemes[4] };
                win.Add(errorLabel);
                Application.Top.SetNeedsDisplay();
            }

            void RefreshList()
            {
                displayItems.Clear();
                foreach (var it in orderItems)
                {
                    displayItems.Add($"{it.Type.Name} (Typ-ID: {it.Type.Id}, Menge: {it.Stock})");
                }
                listView.SetSource(displayItems);
            }

            void AddOrMerge(ArticleType type, int qty)
            {
                var existing = orderItems.FirstOrDefault(i => i.Type.Id == type.Id);
                if (existing != null)
                {
                    existing.Stock += qty;
                }
                else
                {
                    var oi = erpManager.NewOrderItem(type.Id, qty, false);
                    orderItems.Add(oi);
                }
                if (errorLabel != null) { win.Remove(errorLabel); errorLabel = null; }
                RefreshList();
            }

            addBtn.Clicked += () =>
            {
                var typeText = typeIdInput.Text?.ToString() ?? string.Empty;
                var qtyText = qtyInput.Text?.ToString() ?? string.Empty;
                if (!int.TryParse(typeText, out var typeId) || !int.TryParse(qtyText, out var qty) || qty <= 0)
                {
                    ShowError("Ungültige Eingabe.");
                    return;
                }
                var type = erpManager.FindArticleType(typeId);
                if (type == null)
                {
                    ShowError($"Artikeltyp mit ID {typeId} nicht gefunden.");
                    return;
                }
                AddOrMerge(type, qty);
                typeIdInput.Text = string.Empty;
                qtyInput.Text = string.Empty;
                typeIdInput.SetFocus();
                Application.Top.SetNeedsDisplay();
            };

            suggestBtn.Clicked += () =>
            {
                var suggestions = erpManager.SuggestOrders();
                if (suggestions.Count == 0)
                {
                    ShowError("Keine Vorschläge vorhanden.");
                    return;
                }
                foreach (var kv in suggestions)
                {
                    if (kv.Value > 0)
                    {
                        AddOrMerge(kv.Key, kv.Value);
                    }
                }
                Application.Top.SetNeedsDisplay();
            };

            doneBtn.Clicked += () =>
            {
                if (orderItems.Count == 0)
                {
                    ShowError("Bitte mindestens einen Artikel hinzufügen.");
                    return;
                }

                var selfOrder = erpManager.NewSelfOrder(orderItems);

                // Clean up UI
                win.Remove(typeIdLabel);
                win.Remove(typeIdInput);
                win.Remove(qtyLabel);
                win.Remove(qtyInput);
                win.Remove(addBtn);
                win.Remove(suggestBtn);
                win.Remove(doneBtn);
                win.Remove(listHeader);
                win.Remove(listView);
                if (errorLabel != null) { win.Remove(errorLabel); errorLabel = null; }

                // Confirmation
                win.Add(new Label($"Die Eigenbestellung wurde erstellt. ID: {selfOrder.Id}")
                { X = 2, Y = 1, ColorScheme = schemes[3] });
                var okButton = new Button("Ok") { X = 2, Y = 3, ColorScheme = schemes[2] };
                okButton.Clicked += () => { doAfter.Invoke(); };
                win.Add(okButton);
                Application.Top.SetNeedsDisplay();
            };

            // Initial focus
            typeIdInput.SetFocus();
            Application.Top.SetNeedsDisplay();
        }

        private void CreatingElementMenu(Window win, List<ColorScheme> schemes, Action DoAfter)
        {
            win.RemoveAll();
            // Removed header label showing "ERP - <instance name>"

            Dictionary<string, Action> buttons = new()
            {
                { "Zurück", () => { DoAfter(); } },
                { "Artikeltyp", () => { CreateArticleType(win, schemes, DoAfter); } },
                { "Lagerplatz", () => { CreateStorageSlot(win, schemes, DoAfter); } },
                { "Artikel", () => { CreateArticle(win, schemes, DoAfter); } },
                { "Kunde", () => { CreateCustomer(win, schemes, DoAfter); } },
                { "Abteilung", () => { CreateSection(win, schemes, DoAfter); } },
                { "Mitarbeiter", () => { CreateEmployee(win, schemes, DoAfter); } },
                { "Bestellung", () => { CreateOrder(win, schemes, DoAfter); } },
                { "Preisliste", () => { CreatePriceList(win, schemes, DoAfter); } },
                { "Zahlungsbedingungen", () => { CreatePaymentTerms(win, schemes, DoAfter); } },
                { "Rechnung", () => { CreateBill(win, schemes, DoAfter); } },
                { "Eigenbestellung", () => { CreateSelfOrder(win, schemes, DoAfter); } }
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
                button.Clicked += kvp.Value.Invoke;
                win.Add(button);

                posY += 2;
            }

            Application.Top.SetNeedsDisplay();
        }

        private void ArticleOperationsMenu(Window win, List<ColorScheme> schemes, Action DoAfter)
        {
            win.RemoveAll();

            var buttons = new Dictionary<string, Action>
            {
                { "Zurück", () => { DoAfter(); } },
                { "Artikel auffüllen", () => { RestockArticle(win, schemes, DoAfter); } },
                { "Artikel entnehmen", () => { WithdrawArticle(win, schemes, DoAfter); } },
                { "Artikel einsortieren", () => { SortArticle(win, schemes, DoAfter); } },
                { "Barcode generieren", () => { GenerateBarcode(win, schemes, DoAfter); } }
            };

            int posY = 1;
            foreach (var kv in buttons)
            {
                var btn = new Button(kv.Key)
                {
                    X = 2,
                    Y = posY,
                    ColorScheme = schemes[2]
                };
                btn.Clicked += () => kv.Value.Invoke();
                win.Add(btn);
                posY += 2;
            }

            Application.Top.SetNeedsDisplay();
        }

        private void ScanArticle(Window mainWin, List<ColorScheme> schemes, Action DoAfter)
        {
            // Hide other secondary windows (keep main)
            foreach (var kv in windows)
            {
                try { kv.Value.Visible = false; } catch { }
            }

            // Create or show a full-size window to the right of the main window
            var scanWin = new Window("Artikel scannen")
            {
                X = 42,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = schemes[0]
            };

            void CloseScan()
            {
                try { Application.Top.Remove(scanWin); } catch { }
                try { scanWin.Dispose(); } catch { }
                // Restore all secondary windows
                foreach (var kv in windows)
                {
                    try { kv.Value.Visible = true; } catch { }
                }
                // Refresh quick lists
                fillAllWindows();
                Application.Top.SetNeedsDisplay();
            }

            void RenderSearch()
            {
                scanWin.RemoveAll();

                var info = new Label("Scanner-ID oder Artikel-ID eingeben und Enter drücken")
                {
                    X = 2,
                    Y = 1,
                    ColorScheme = schemes[1]
                };
                scanWin.Add(info);

                var input = new TextField("")
                {
                    X = 2,
                    Y = 3,
                    Width = 40,
                    ColorScheme = schemes[1]
                };
                scanWin.Add(input);

                Label? errorLabel = null;
                void ShowError(string msg)
                {
                    if (errorLabel != null)
                    {
                        scanWin.Remove(errorLabel);
                        errorLabel = null;
                    }
                    errorLabel = new Label(msg)
                    {
                        X = 2,
                        Y = 7,
                        ColorScheme = schemes[4]
                    };
                    scanWin.Add(errorLabel);
                }

                void ShowDetails(Article article)
                {
                    scanWin.RemoveAll();

                    var mockBtn = new Button("")
                    {
                        X = 2,
                        Y = 1,
                        Width = 1,
                        Height = 1
                    };
                    scanWin.Add(mockBtn);
                    mockBtn.SetFocus();

                    var backBtn = new Button("Zurück zur Suche")
                    {
                        X = 2,
                        Y = 1,
                        ColorScheme = schemes[2]
                    };
                    backBtn.Clicked += RenderSearch;
                    scanWin.Add(backBtn);

                    int y = 3;
                    var slot = erpManager!.FindStorageSlot(article);
                    var details = new List<string>
                    {
                        $"Artikel: {article.Type.Name}",
                        $"Artikel-ID: {article.Id}",
                        $"Typ-ID: {article.Type.Id}",
                        $"Bestand: {article.Stock}",
                        $"Lagerplatz: {(slot == null ? "-" : slot.Id.ToString())}",
                        $"Scanner-ID: {article.ScannerId}"
                    };
                    foreach (var line in details)
                    {
                        scanWin.Add(new Label(line)
                        {
                            X = 2,
                            Y = y++,
                            ColorScheme = schemes[1]
                        });
                    }

                    y += 1;

                    // Action buttons
                    var restockBtn = new Button("Auffüllen") { X = 2, Y = y, ColorScheme = schemes[2] };
                    var withdrawBtn = new Button("Entnehmen") { X = Pos.Right(restockBtn) + 2, Y = y, ColorScheme = schemes[2] };
                    var sortBtn = new Button("Einsortieren") { X = Pos.Right(withdrawBtn) + 2, Y = y, ColorScheme = schemes[2] };
                    var barcodeBtn = new Button("Barcode") { X = Pos.Right(sortBtn) + 2, Y = y, ColorScheme = schemes[2] };
                    var closeBtn = new Button("Schließen") { X = Pos.Right(barcodeBtn) + 2, Y = y, ColorScheme = schemes[2] };

                    // After each operation, refresh side lists and redisplay details for the same article
                    Action AfterOp = () =>
                    {
                        fillAllWindows();
                        ShowDetails(article);
                        Application.Top.SetNeedsDisplay();
                    };

                    restockBtn.Clicked += () =>
                    {
                        // Reuse existing form; provide our own DoAfter to return here
                        RestockArticle(scanWin, schemes, AfterOp, article.Id);
                    };
                    withdrawBtn.Clicked += () =>
                    {
                        WithdrawArticle(scanWin, schemes, AfterOp, article.Id);
                    };
                    sortBtn.Clicked += () =>
                    {
                        SortArticle(scanWin, schemes, AfterOp, article.Id);
                    };
                    barcodeBtn.Clicked += () =>
                    {
                        GenerateBarcode(scanWin, schemes, AfterOp, article.Id);
                    };
                    closeBtn.Clicked += CloseScan;

                    scanWin.Add(restockBtn);
                    scanWin.Add(withdrawBtn);
                    scanWin.Add(sortBtn);
                    scanWin.Add(barcodeBtn);
                    scanWin.Add(closeBtn);
                }

                void DoSearch()
                {
                    var text = input.Text?.ToString()?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(text)) { ShowError("Bitte eine ID eingeben."); return; }

                    Article? found = null;
                    // Try scanner id first (long), then article id (int)
                    if (long.TryParse(text, out var scannerId))
                    {
                        found = erpManager!.FindArticleByScannerId(scannerId);
                    }
                    if (found == null && int.TryParse(text, out var articleId))
                    {
                        found = erpManager!.FindArticle(articleId);
                    }

                    if (found == null)
                    {
                        ShowError("Kein Artikel gefunden.");
                        return;
                    }

                    ShowDetails(found);
                }

                var searchBtn = new Button("Suchen")
                {
                    X = 2,
                    Y = 5,
                    ColorScheme = schemes[2]
                };
                searchBtn.Clicked += DoSearch;
                scanWin.Add(searchBtn);

                var cancelBtn = new Button("Abbrechen")
                {
                    X = Pos.Right(searchBtn) + 2,
                    Y = 5,
                    ColorScheme = schemes[2]
                };
                cancelBtn.Clicked += CloseScan;
                scanWin.Add(cancelBtn);

                input.KeyDown += (args) =>
                {
                    if (args.KeyEvent.Key == Key.Enter)
                    {
                        args.Handled = true;
                        searchBtn.OnClicked();
                    }
                };

                input.SetFocus();
            }

            Application.Top.Add(scanWin);
            RenderSearch();
            Application.Top.SetNeedsDisplay();
        }

        private void ManageOwnCapitalMenu(Window win, List<ColorScheme> schemes, Action DoAfter)
        {
            win.RemoveAll();

            var buttons = new Dictionary<string, Action>
            {
                { "Zurück", () => { DoAfter(); } },
                { "Eigenkapital festlegen", () => { SetOwnCapital(win, schemes, DoAfter); } },
                { "Eigenkapital erhöhen", () => { IncreaseOwnCapital(win, schemes, DoAfter); } },
                { "Eigenkapital verringern", () => { DecreaseOwnCapital(win, schemes, DoAfter); } }
            };

            int posY = 1;
            foreach (var kv in buttons)
            {
                var btn = new Button(kv.Key)
                {
                    X = 2,
                    Y = posY,
                    ColorScheme = schemes[2]
                };
                btn.Clicked += () => kv.Value.Invoke();
                win.Add(btn);
                posY += 2;
            }

            Application.Top.SetNeedsDisplay();
        }

        private void SetOwnCapital(Window win, List<ColorScheme> schemes, Action DoAfter)
        {
            win.RemoveAll();

            var capitalLabel = new Label("Neues Eigenkapital:")
            {
                X = 2,
                Y = 1,
                ColorScheme = schemes[1]
            };
            win.Add(capitalLabel);

            var capitalInput = new TextField()
            {
                X = 2,
                Y = 2,
                Width = 40,
                ColorScheme = schemes[1]
            };
            win.Add(capitalInput);

            var sendButton = new Button("Ok")
            {
                X = 2,
                Y = 4,
                ColorScheme = schemes[2]
            };
            sendButton.Clicked += () =>
            {
                var capitalText = capitalInput.Text?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(capitalText))
                {
                    DoAfter.Invoke();
                    return;
                }

                if (!double.TryParse(capitalText, out double newCapital) || newCapital < 0)
                {
                    DoAfter.Invoke();
                    return;
                }

                erpManager!.SetOwnCapital(newCapital);

                var finishedText = new Label("Das Eigenkapital wurde festgelegt.")
                {
                    X = 2,
                    Y = 1,
                    ColorScheme = schemes[3]
                };
                win.RemoveAll();
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

        private void IncreaseOwnCapital(Window win, List<ColorScheme> schemes, Action DoAfter)
        {
            win.RemoveAll();

            var capitalLabel = new Label("Erhöhung des Eigenkapitals:")
            {
                X = 2,
                Y = 1,
                ColorScheme = schemes[1]
            };
            win.Add(capitalLabel);

            var capitalInput = new TextField()
            {
                X = 2,
                Y = 2,
                Width = 40,
                ColorScheme = schemes[1]
            };
            win.Add(capitalInput);

            var sendButton = new Button("Ok")
            {
                X = 2,
                Y = 4,
                ColorScheme = schemes[2]
            };
            sendButton.Clicked += () =>
            {
                var capitalText = capitalInput.Text?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(capitalText))
                {
                    DoAfter.Invoke();
                    return;
                }

                if (!double.TryParse(capitalText, out double increaseAmount) || increaseAmount <= 0)
                {
                    DoAfter.Invoke();
                    return;
                }

                erpManager!.AddOwnCapital(increaseAmount);

                var finishedText = new Label("Das Eigenkapital wurde erhöht.")
                {
                    X = 2,
                    Y = 1,
                    ColorScheme = schemes[3]
                };
                win.RemoveAll();
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

        private void DecreaseOwnCapital(Window win, List<ColorScheme> schemes, Action DoAfter)
        {
            win.RemoveAll();

            var capitalLabel = new Label("Verringerung des Eigenkapitals:")
            {
                X = 2,
                Y = 1,
                ColorScheme = schemes[1]
            };
            win.Add(capitalLabel);

            var capitalInput = new TextField()
            {
                X = 2,
                Y = 2,
                Width = 40,
                ColorScheme = schemes[1]
            };
            win.Add(capitalInput);

            var sendButton = new Button("Ok")
            {
                X = 2,
                Y = 4,
                ColorScheme = schemes[2]
            };
            sendButton.Clicked += () =>
            {
                var capitalText = capitalInput.Text?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(capitalText))
                {
                    DoAfter.Invoke();
                    return;
                }

                if (!double.TryParse(capitalText, out double decreaseAmount) || decreaseAmount <= 0)
                {
                    DoAfter.Invoke();
                    return;
                }

                erpManager!.RemoveOwnCapital(decreaseAmount);

                var finishedText = new Label("Das Eigenkapital wurde verringert.")
                {
                    X = 2,
                    Y = 1,
                    ColorScheme = schemes[3]
                };
                win.RemoveAll();
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
        
        private void RestockArticle(Window win, List<ColorScheme> schemes, Action DoAfter, int? presetId = null)
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
                ColorScheme = schemes[1],
                Text = presetId?.ToString() ?? string.Empty,
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

            if (presetId.HasValue)
            {
                amountInput.SetFocus();
            }

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

        private void WithdrawArticle(Window win, List<ColorScheme> schemes, Action DoAfter, int? presetId = null)
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
                ColorScheme = schemes[1],
                Text = presetId?.ToString() ?? string.Empty,
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

            if (presetId.HasValue)
            {
                amountInput.SetFocus();
            }

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

                // Validate stock before withdrawing
                if (article.Stock < amount)
                {
                    win.Remove(articleIdLabel);
                    win.Remove(articleIdInput);
                    win.Remove(amountLabel);
                    win.Remove(amountInput);
                    win.Remove(sendButton);

                    var errorText = new Label($"Nicht genügend Bestand. Verfügbar: {article.Stock}.")
                    {
                        X = 2,
                        Y = 1,
                        ColorScheme = schemes[4]
                    };
                    win.Add(errorText);

                    var okButtonErr = new Button("Ok")
                    {
                        X = 2,
                        Y = 3,
                        ColorScheme = schemes[2]
                    };
                    okButtonErr.Clicked += () => { DoAfter.Invoke(); };
                    win.Add(okButtonErr);
                    return;
                }

                win.Remove(articleIdLabel);
                win.Remove(articleIdInput);
                win.Remove(amountLabel);
                win.Remove(amountInput);
                win.Remove(sendButton);

                erpManager!.WithdrawArticle(article.Id, amount);

                var finishedText = new Label("Der Artikel wurde entnommen.")
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

        private void SortArticle(Window win, List<ColorScheme> schemes, Action DoAfter, int? presetId = null)
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
                ColorScheme = schemes[1],
                Text = presetId?.ToString() ?? string.Empty,
            };
            win.Add(articleIdInput);

            var slotIdLabel = new Label("Lagerplatz ID:")
            {
                X = 2,
                Y = 4,
                ColorScheme = schemes[1]
            };
            win.Add(slotIdLabel);

            var slotIdInput = new TextField()
            {
                X = 2,
                Y = 5,
                Width = 40,
                ColorScheme = schemes[1]
            };
            win.Add(slotIdInput);

            if (presetId.HasValue)
            {
                slotIdInput.SetFocus();   
            }

            var sendButton = new Button("Ok")
            {
                X = 2,
                Y = 7,
                ColorScheme = schemes[2]
            };
            sendButton.Clicked += () =>
            {
                var articleIdText = articleIdInput.Text?.ToString() ?? string.Empty;
                var slotIdText = slotIdInput.Text?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(articleIdText) || string.IsNullOrWhiteSpace(slotIdText))
                {
                    DoAfter.Invoke();
                    return;
                }

                if (!int.TryParse(articleIdText, out int articleId) || !int.TryParse(slotIdText, out int slotId))
                {
                    DoAfter.Invoke();
                    return;
                }

                var article = erpManager!.FindArticle(articleId);
                var slot = erpManager!.GetAllStorageSlots().FirstOrDefault(s => s.Id == slotId);
                if (article == null || slot == null)
                {
                    win.Remove(articleIdLabel);
                    win.Remove(articleIdInput);
                    win.Remove(slotIdLabel);
                    win.Remove(slotIdInput);
                    win.Remove(sendButton);

                    string msg = article == null ? $"Artikel mit ID {articleId} nicht gefunden." : $"Lagerplatz mit ID {slotId} nicht gefunden.";
                    var errorText = new Label(msg)
                    {
                        X = 2,
                        Y = 1,
                        ColorScheme = schemes[4]
                    };
                    win.Add(errorText);

                    var okButtonErr = new Button("Ok")
                    {
                        X = 2,
                        Y = 3,
                        ColorScheme = schemes[2]
                    };
                    okButtonErr.Clicked += () => { DoAfter.Invoke(); };
                    win.Add(okButtonErr);
                    return;
                }

                win.Remove(articleIdLabel);
                win.Remove(articleIdInput);
                win.Remove(slotIdLabel);
                win.Remove(slotIdInput);
                win.Remove(sendButton);

                erpManager!.SortArticle(article.Id, slot.Id);

                var finishedText = new Label("Der Artikel wurde einsortiert.")
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

        private void GenerateBarcode(Window win, List<ColorScheme> schemes, Action DoAfter, int? presetId = null)
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
                ColorScheme = schemes[1],
                Text = presetId?.ToString() ?? string.Empty,
            };
            win.Add(articleIdInput);

            var sendButton = new Button("Ok")
            {
                X = 2,
                Y = 4,
                ColorScheme = schemes[2]
            };
            sendButton.Clicked += () =>
            {
                var articleIdText = articleIdInput.Text?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(articleIdText))
                {
                    DoAfter.Invoke();
                    return;
                }

                if (!int.TryParse(articleIdText, out int articleId))
                {
                    DoAfter.Invoke();
                    return;
                }

                var article = erpManager!.FindArticle(articleId);
                if (article == null)
                {
                    win.Remove(articleIdLabel);
                    win.Remove(articleIdInput);
                    win.Remove(sendButton);

                    var errorText = new Label($"Artikel mit ID {articleId} nicht gefunden.")
                    {
                        X = 2,
                        Y = 1,
                        ColorScheme = schemes[4]
                    };
                    win.Add(errorText);

                    var okButtonErr = new Button("Ok")
                    {
                        X = 2,
                        Y = 3,
                        ColorScheme = schemes[2]
                    };
                    okButtonErr.Clicked += () => { DoAfter.Invoke(); };
                    win.Add(okButtonErr);
                    return;
                }

                win.Remove(articleIdLabel);
                win.Remove(articleIdInput);
                win.Remove(sendButton);

                var finishedText = new Label($"Der Barcode wurde generiert: {article.GenerateBarCode()}")
                {
                    X = 2,
                    Y = 1,
                    ColorScheme = schemes[3]
                };
                win.Add(finishedText);
                var okButton = new Button("Ok")
                {
                    X = 2,
                    Y = 4,
                    ColorScheme = schemes[2]
                };
                okButton.Clicked += () =>
                {
                    DoAfter.Invoke();
                };
                win.Add(okButton);
            };
            win.Add(sendButton);

            if (presetId.HasValue)
            {
                sendButton.OnClicked();
            }

            Application.Top.SetNeedsDisplay();
        }

        private void SaveInstance(Window win, List<ColorScheme> schemes, Action doAfter)
        {
            erpManager!.SaveInstance("saves");

            // Show confirmation message
            win.RemoveAll();

            var savedText = new Label("Die Instanz wurde gespeichert.")
            {
                X = 2,
                Y = 1,
                ColorScheme = schemes[3]
            };
            win.Add(savedText);

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

        private void CloseInstance(Window win, List<ColorScheme> schemes)
        {
            // Remove any secondary windows we created (side panels)
            foreach (var w in windows.Values)
            {
                try { Application.Top?.Remove(w); } catch { }
            }
            windows.Clear();
            defaultLayouts.Clear();

            // Disable secondary window navigation
            UnregisterSecondaryWindowNavigation();

            // Reset state
            erpManager = null;

            // Restore main window to the welcome screen
            win.Title = "ERP";
            win.Width = Dim.Fill();
            win.RemoveAll();

            var welcomeLabel = new Label("Willkommen bei meinem ERP-System")
            {
                X = 2,
                Y = 1,
                ColorScheme = schemes[1]
            };
            win.Add(welcomeLabel);

            var buttonNewLocal = new Button("Neue Instanz")
            {
                X = 2,
                Y = 3,
                ColorScheme = schemes[2]
            };
            buttonNewLocal.Clicked += () => { newInstanceMenu(win, schemes); };
            win.Add(buttonNewLocal);

            var buttonOpenLocal = new Button("Instanz öffnen")
            {
                X = 2,
                Y = 5,
                ColorScheme = schemes[2]
            };
            buttonOpenLocal.Clicked += () => { openInstanceMenu(win, schemes); };
            win.Add(buttonOpenLocal);

            win.SetFocus();
            Application.Top?.SetNeedsDisplay();
        }
    }
}