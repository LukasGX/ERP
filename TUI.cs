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
    public Object? WorkingWith;

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

            return [scheme0, scheme1, scheme2, scheme3];
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
            erpManager = new ERPManager(inputText ?? "unnamed");
            win.RemoveAll();

            var headerLabel = new Label($"ERP - {inputText ?? "unnamed"}")
            {
                X = 2,
                Y = 1,
                ColorScheme = schemes[1]
            };
            win.Add(headerLabel);

            var buttonCreate = new Button("Element erstellen")
            {
                X = 2,
                Y = 3,
                ColorScheme = schemes[2]
            };
            Action? buttonCreateClick = null;

            buttonCreateClick = () =>
            {
                win.RemoveAll();
                var headerLabel = new Label($"ERP - {erpManager!.InstanceName}")
                {
                    X = 2,
                    Y = 1,
                    ColorScheme = schemes[1]
                };
                win.Add(headerLabel);

                Action DoAfter = () =>
                {
                    win.RemoveAll();
                    var headerLabel = new Label($"ERP - {inputText ?? "unnamed"}")
                    {
                        X = 2,
                        Y = 1,
                        ColorScheme = schemes[1]
                    };
                    win.Add(headerLabel);

                    var buttonCreate = new Button("Element erstellen")
                    {
                        X = 2,
                        Y = 3,
                        ColorScheme = schemes[2]
                    };
                    buttonCreate.Clicked += buttonCreateClick!;
                    win.Add(buttonCreate);

                    win.SetFocus();
                    win.Height = 7;
                    win.Width = 80;

                    Application.Top.SetNeedsDisplay();
                };

                Dictionary<string, Action> buttons = new()
                    {
                        { "Artikeltyp", () => { CreateArticleType(win, schemes, DoAfter); } }
                    };
                int posY = 3;

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

                win.Height = 17;

                Application.Top.SetNeedsDisplay();
            };
            buttonCreate.Clicked += buttonCreateClick!;
            win.Add(buttonCreate);

            win.SetFocus();
            win.Height = 7;
            win.Width = 80;

            Application.Top.SetNeedsDisplay();
        }

        // creation button click
        private void CreateArticleType(Window win, List<ColorScheme> schemes, Action doAfter)
        {
            win.RemoveAll();

            var headerLabel = new Label($"ERP - {erpManager!.InstanceName}")
            {
                X = 2,
                Y = 1,
                ColorScheme = schemes[1]
            };
            win.Add(headerLabel);

            var appellLabel = new Label("Name des Artikeltypen:")
            {
                X = 2,
                Y = 3,
                ColorScheme = schemes[1]
            };
            win.Add(appellLabel);

            var nameInput = new TextField()
            {
                X = 2,
                Y = 4,
                Width = 40,
                ColorScheme = schemes[1]
            };
            win.Add(nameInput);

            var sendButton = new Button("Ok")
            {
                X = 2,
                Y = 6,
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
                    Y = 3,
                    ColorScheme = schemes[3]
                };
                win.Add(finishedText);

                var okButton = new Button("Ok")
                {
                    X = 2,
                    Y = 5,
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
    }
}