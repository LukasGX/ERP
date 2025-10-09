using System;
using System.Linq.Expressions;
using ERP_Fix;
using Terminal.Gui;


namespace ERP_Fix {
    class TUI
    {
        public ERPManager erpManager;
        public Object WorkingWith;

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
            buttonNew.Clicked += () =>
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

                okButton.Clicked += () =>
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
                    Action buttonCreateClick = null;

                    buttonCreateClick = () => {
                        win.RemoveAll();
                        var headerLabel = new Label($"ERP - {erpManager.InstanceName}")
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
                            buttonCreate.Clicked += buttonCreateClick;
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
                    buttonCreate.Clicked += buttonCreateClick;
                    win.Add(buttonCreate);

                    win.SetFocus();
                    win.Height = 7;
                    win.Width = 80;

                    Application.Top.SetNeedsDisplay();
                };
                win.Add(okButton);

                Application.Top.SetNeedsDisplay();
            };
            win.Add(buttonNew);

            var buttonOpen = new Button("Instanz öffnen")
            {
                X = 2,
                Y = 5,
                ColorScheme = schemes[2]
            };
            win.Add(buttonOpen);

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

        // creation button click
        private void CreateArticleType(Window win, List<ColorScheme> schemes, Action doAfter)
        {
            win.RemoveAll();

            var headerLabel = new Label($"ERP - {erpManager.InstanceName}")
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
            nameInput.KeyDown += (e) =>
            {
                if (e.KeyEvent.Key == Key.Enter)
                {
                    if (string.IsNullOrEmpty(nameInput.Text.ToString()))
                    {
                        doAfter.Invoke();
                    }

                    win.Remove(appellLabel);
                    win.Remove(nameInput);

                    ArticleType? articleType = erpManager.NewArticleType(nameInput.Text.ToString());

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
                }
            };
            win.Add(nameInput);

            Application.Top.SetNeedsDisplay();
        }
    }
}