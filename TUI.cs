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
        private readonly string[] secondaryOrder = new[] { "articleType", "storageSlot", "article", "customer", "section", "employee" };

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

            var buttonSave = new Button("Speichern")
            {
                X = 2,
                Y = 5,
                ColorScheme = schemes[2]
            };

            var buttonClose = new Button("Schließen")
            {
                X = 2,
                Y = 7,
                ColorScheme = schemes[2]
            };

            var labelWindowSwitch = new Label("Fenster wechseln mit F6/F7")
            {
                X = 2,
                Y = 10,
                ColorScheme = schemes[4]
            };
            win.Add(labelWindowSwitch);

            var labelExitProgram = new Label("Programm beenden mit Esc")
            {
                X = 2,
                Y = 11,
                ColorScheme = schemes[4]
            };
            win.Add(labelExitProgram);

            Action? buttonCreateClick = null;
            Action? buttonArticleOpsClick = null;
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

                var buttonSave = new Button("Speichern")
                {
                    X = 2,
                    Y = 5,
                    ColorScheme = schemes[2]
                };
                buttonSave.Clicked += buttonSaveClick;
                win.Add(buttonSave);

                var buttonClose = new Button("Schließen")
                {
                    X = 2,
                    Y = 7,
                    ColorScheme = schemes[2]
                };
                buttonClose.Clicked += buttonCloseClick;
                win.Add(buttonClose);

                var labelWindowSwitch = new Label("Fenster wechseln mit F6/F7")
                {
                    X = 2,
                    Y = 10,
                    ColorScheme = schemes[4]
                };
                win.Add(labelWindowSwitch);

                var labelExitProgram = new Label("Programm beenden mit Esc")
                {
                    X = 2,
                    Y = 11,
                    ColorScheme = schemes[4]
                };
                win.Add(labelExitProgram);

                fillAllWindows();

                win.SetFocus();

                Application.Top.SetNeedsDisplay();
            };

            buttonCreateClick = () => { CreatingElementMenu(win, schemes, DoAfter); };
            buttonArticleOpsClick = () => { ArticleOperationsMenu(win, schemes, DoAfter); };
            buttonCloseClick = () => { CloseInstance(win, schemes); };
            buttonSaveClick = () => { SaveInstance(win, schemes, DoAfter); };

            buttonCreate.Clicked += buttonCreateClick;
            win.Add(buttonCreate);

            buttonArticleOps.Clicked += buttonArticleOpsClick;
            win.Add(buttonArticleOps);

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
                Width = 55,
                Height = 7
            };
            articleWin.ColorScheme = schemes[0];
            Application.Top.Add(articleWin);
            windows["article"] = articleWin;
            defaultLayouts["article"] = (111, 1, 55, 7);

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
                var extraInfoLabel = new Label($"(ID: {a.Id}, Anz. {a.Stock}, Lagerplatz: {(slot == null ? "-" : slot.Id.ToString())})")
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

            // Re-render full content with a back button
            w.RemoveAll();

            var backBtn = new Button("Zurück")
            {
                X = 2,
                Y = 1,
                ColorScheme = schemes[2]
            };
            backBtn.Clicked += () => RestoreExtraWindowsLayout(schemes);
            w.Add(backBtn);

            // Render all entries starting after the back button
            switch (key)
            {
                case "articleType":
                    RenderArticleTypesFull(w, schemes, startY: 3);
                    break;
                case "storageSlot":
                    RenderStorageSlotsFull(w, schemes, startY: 3);
                    break;
                case "article":
                    RenderArticlesFull(w, schemes, startY: 3);
                    break;
                case "customer":
                    RenderCustomersFull(w, schemes, startY: 3);
                    break;
                case "section":
                    RenderSectionsFull(w, schemes, startY: 3);
                    break;
                case "employee":
                    RenderEmployeesFull(w, schemes, startY: 3);
                    break;
            }

            Application.Top?.SetNeedsDisplay();
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

                Customer customer = erpManager!.NewCustomer(text);

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

            var sendButton = new Button("Ok")
            {
                X = 2,
                Y = 7,
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

                Employee employee = erpManager!.NewEmployee(nameText, worksInSection);

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
                { "Artikel einsortieren", () => { SortArticle(win, schemes, DoAfter); } }
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

        private void WithdrawArticle(Window win, List<ColorScheme> schemes, Action DoAfter)
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

        private void SortArticle(Window win, List<ColorScheme> schemes, Action DoAfter)
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