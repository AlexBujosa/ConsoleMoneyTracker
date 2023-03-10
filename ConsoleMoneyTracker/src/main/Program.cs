using ConsoleMoneyTracker.src.main.controller;
using ConsoleMoneyTracker.src.main.model;
using ConsoleMoneyTracker.src.main.model.dbModel;
using ConsoleMoneyTracker.src.main.repository;
using Spectre.Console;
using System.Reflection;
using System.Xml.Linq;

namespace ConsoleMoneyTracker.src.main
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string userName = "Pedro";

            IRepository<Account, int> accountsRepository = new InMemoryRepository<Account, int>();
            IRepository<Category, int> categoryRepository = new InMemoryRepository<Category, int>();
            IRepository<Currency, string> currencyRepository = new InMemoryRepository<Currency, string>();
            IRepository<Transaction, int> transactionRepository = new InMemoryRepository<Transaction, int>();
            IRepository<ListItem, int> itemRepository = new InMemoryRepository<ListItem, int>();

            TransactionController transactionController = new TransactionController(transactionRepository, itemRepository, accountsRepository);
            AccountController accountController = new AccountController(accountsRepository, transactionRepository, itemRepository);
            CategoryController categoryController = new CategoryController(categoryRepository, itemRepository);
            CurrencyController currencyController = new CurrencyController(currencyRepository, new OnlineCurrencyInfoGetter());
            List<string> list = new List<string>()
                {
                    "Add Transaction", "Manage Information", "See a Report", "Exit",
                };


            while (true)
            {
                try
                {

                    int selection = SelectOption(list, $"Welcome [blue]{userName}[/]! What would you like to do?");
                    AnsiConsole.Clear();
                    if (selection == 0)
                    {
                        MakeTransactionPipeline(transactionController, accountController, categoryController, currencyController);
                    }
                    else if (selection == 1)
                    {
                        ManageInformationScreen(accountController, categoryController, currencyController, transactionController);
                    }
                    else if (selection == 2)
                    {
                        SeeReportScreen(transactionController);
                    }
                    else if (selection == list.Count() - 1)
                    {
                        break;
                    }
                }
                catch (NotImplementedException e)
                {
                    ShowErrorBox("This feature has not been implemented yet.");
                }
            }
        }

        // Make a transaction given the information in the database
        // TODO: update currencies before making transferences
        static void MakeTransactionPipeline(TransactionController transControl, AccountController actControl, CategoryController catControl, CurrencyController currencyController)
        {
            int accountCount = actControl.Count();
            // Validate if a transaction is possible
            if (accountCount <= 0)
            {
                ShowErrorBox("You need at least [red]one[/] [green]account[/] to make transactions.");
                return;
            }
            if (catControl.Count() <= 0)
            {
                ShowErrorBox("You need at least [red]one[/] [olive]category[/] to make transactions.");
                return;
            }

            // Select the kind of transaction
            int transactionType = SelectOption(new List<string>() { "Expense", "Income", "Movement" }, "What kind of transaction?");

            // Select the accounts
            Account? sourceAccount = null;
            Account? targetAccount = null;
            List<Account> selectableAccounts = actControl.GetAccounts().ToList();

            switch (transactionType)
            {
                case 0:
                    sourceAccount = SelectListable(selectableAccounts, "Select the source of the expense");
                    break;
                case 1:

                    targetAccount = SelectListable(selectableAccounts, "Select the target of the income.");
                    break;
                case 2:
                    if (accountCount < 2)
                    {
                        ShowErrorBox("You need at least [red]two[/] [olive]accounts[/] to make movements.");
                        return;
                    }
                    UpdateCurrenciesScreen(currencyController);
                    sourceAccount = SelectListable(selectableAccounts, "Select the source of the movement.");
                    selectableAccounts.Remove(sourceAccount);
                    targetAccount = SelectListable(selectableAccounts, "Select the target of the movement.");
                    // get both
                    break;
                default:
                    // error
                    break;
            }

            // Get the amount to transfer
            float amount = AnsiConsole.Ask<float>($"How much shall be transferred?{(sourceAccount != null ? "(Available: [blue]" + sourceAccount.amount + "[/]" : " ")}");
            if (sourceAccount != null && amount > sourceAccount.amount)
            {
                ShowErrorBox("There isn't enough balance on this account for this transference");
                return;
            }

            List<Category> selectableCategories = catControl.GetCategories().ToList();

            Category category = SelectListable(selectableCategories, "Select the category of this transaction.");
            string description = AnsiConsole.Ask<string>("Write a [green]description[/] for this transaction.");

            Transaction transaction = transControl.MakeTransaction(sourceAccount, targetAccount, amount, category, description);

            ShowTransactionSummary(transaction);

            if (AnsiConsole.Confirm("Are these OK?"))
            {
                transControl.CommitTransaction(transaction);
            };
        }

        #region AllCRUD
        // Manage Accounts
        // Manage Categories
        // Manage Currencies
        // See Transactions
        static void ManageInformationScreen(AccountController accountController, CategoryController categoryController, CurrencyController currencyController, TransactionController transactionController)
        {
            List<string> options = new List<string>()
            {
                "Manage Accounts",
                "Manage Categories",
                "Manage Currencies",
                "See Transactions",
                "Go Back"
            };

            while (true)
            {
                int selected = SelectOption(options, "What do you wish to do?");
                AnsiConsole.Clear();
                switch (selected)
                {
                    case 0:
                        ManageAccountsScreen(accountController, currencyController);
                        break;
                    case 1:
                        ManageCategoriesScreen(categoryController);
                        break;
                    case 2:
                        ManageCurrenciesScreen(currencyController);
                        break;
                    case 3:
                        SeeTransactionsScreen(transactionController);
                        break;
                    case 4:
                        return; // Goes back to the previous menu
                }
            }
        }

        #region AccountCRUD

        // CRUD Accounts
        static void ManageAccountsScreen(AccountController accountCtrl, CurrencyController currencyController)
        {
            List<string> options = new List<string>()
            {
                "Create Account",
                "Read Accounts",
                "Update Account",
                "Delete Account",
                "Go Back"
            };

            while (true)
            {
                int selected = SelectOption(options, "What do you wish to do?");
                AnsiConsole.Clear();

                switch (selected)
                {
                    case 0:
                        CreateAccountScreen(accountCtrl, currencyController);
                        break;
                    case 1:
                        ReadAccountsScreen(accountCtrl);
                        break;
                    case 2:
                        UpdateAccountScreen(accountCtrl, currencyController);
                        break;
                    case 3:
                        DeleteAccountScreen(accountCtrl);
                        break;
                    case 4:
                        return; // Goes back to the previous menu
                }
            }
        }

        static void CreateAccountScreen(AccountController accountCtrl, CurrencyController currencyController)
        {
            string name = AnsiConsole.Ask<string>("What's the account's name?");
            string shortName = AnsiConsole.Ask<string>("What's the account's short name?");
            string descripition = AnsiConsole.Ask<string>("What's the account's description?");
            Currency currency = SelectStrListable(currencyController.GetCurrencyList().ToList(), "Select the account's Currency");
            float startingMoney = AnsiConsole.Ask<float>("What's the account's starting money?", 0);
            if (AnsiConsole.Confirm("Create this account?"))
            {
                accountCtrl.InsertAccount(name, shortName, descripition, currency, startingMoney);
                ShowBox($"Account {name} created.");
            }
        }

        static void ReadAccountsScreen(AccountController accountCtrl)
        {
            var accounts = accountCtrl.GetAccounts().ToList();
            ShowListableTable(accounts, "Press Enter to Exit.");
        }

        static void UpdateAccountScreen(AccountController accountCtrl, CurrencyController currencyController)
        {
            var accounts = accountCtrl.GetAccounts().ToList();
            var selected = SelectListable(accounts, "Select an account to update");

            selected.item.name = AnsiConsole.Ask<string>("What's the account's name?", selected.item.name);
            selected.item.shortName = AnsiConsole.Ask<string>("What's the account's short name?", selected.item.shortName);
            selected.item.description = AnsiConsole.Ask<string>("What's the account's description?", selected.item.description);

            if (AnsiConsole.Confirm("Change the currency?"))
            {
                Currency newCurrency = SelectStrListable(currencyController.GetCurrencyList().ToList(), "Select the account's Currency");
                float newAmount = selected.amount * selected.currency.toDollar / newCurrency.toDollar;
                if (AnsiConsole.Confirm($"Convert old currency to new currency? New balance in {newCurrency.item.name}: {newCurrency.item.shortName} {newAmount}"))
                {
                    selected.amount = newAmount;
                }
                selected.currency = newCurrency;
            }

            accountCtrl.UpdateAccount(selected);
        }

        static void DeleteAccountScreen(AccountController accountCtrl)
        {
            var accounts = accountCtrl.GetAccounts().ToList();
            var selected = SelectListable(accounts, "Select an account to delete");
            if (AnsiConsole.Confirm($"Delete {selected.item.name}?"))
            {
                accountCtrl.DeleteAccount(selected);
            }
        }

        #endregion


        #region Categories
        // CRUD Categories
        static void ManageCategoriesScreen(CategoryController categoryController)
        {
            List<string> options = new List<string>()
            {
                "Create Category",
                "Read Categories",
                "Update Category",
                "Delete Category",
                "Go Back"
            };

            while (true)
            {
                int selected = SelectOption(options, "What do you wish to do?");
                AnsiConsole.Clear();

                switch (selected)
                {
                    case 0:
                        CreateCategoryScreen(categoryController);
                        break;
                    case 1:
                        ReadCategoriesScreen(categoryController);
                        break;
                    case 2:
                        UpdateCategoryScreen(categoryController);
                        break;
                    case 3:
                        DeleteCategoryScreen(categoryController);
                        break;
                    case 4:
                        return; // Goes back to the previous menu
                }
            }
        }

        static void CreateCategoryScreen(CategoryController categoryController)
        {
            string name = AnsiConsole.Ask<string>("What's the category's name?");
            string shortName = AnsiConsole.Ask<string>("What's the category's short name?");
            string descripition = AnsiConsole.Ask<string>("What's the category's description?");
            categoryController.InsertCategory(name, shortName, descripition);
        }

        static void ReadCategoriesScreen(CategoryController categoryController)
        {
            var categories = categoryController.GetCategories().ToList();
            ShowListableTable(categories, "Press Enter to Exit.");
        }

        static void UpdateCategoryScreen(CategoryController categoryController)
        {
            var categories = categoryController.GetCategories().ToList();
            var selected = SelectListable(categories, "Select a category to update");

            selected.item.name = AnsiConsole.Ask<string>("What's the category's name?", selected.item.name);
            selected.item.shortName = AnsiConsole.Ask<string>("What's the category's short name?", selected.item.shortName);
            selected.item.description = AnsiConsole.Ask<string>("What's the category's description?", selected.item.description);

            categoryController.UpdateCategory(selected);

        }

        static void DeleteCategoryScreen(CategoryController categoryController)
        {
            var categories = categoryController.GetCategories().ToList();
            var selected = SelectListable(categories, "Select a category to delete");
            if (AnsiConsole.Confirm($"Are you sure you want to delete {selected.item.name}?"))
            {
                categoryController.DeleteCategory(selected);
            };
        }

        #endregion

        #region Currencies
        // CRUD Currencies
        static void ManageCurrenciesScreen(CurrencyController currencyController)
        {
            List<string> options = new List<string>()
            {
                "Read Currencies",
                "Update Currencies from web",
                "Go Back"
            };

            while (true)
            {
                int selected = SelectOption(options, "What do you wish to do?");
                AnsiConsole.Clear();

                switch (selected)
                {
                    case 0:
                        ReadCurrenciesScreen(currencyController);
                        break;
                    case 1:
                        UpdateCurrenciesScreen(currencyController);
                        break;
                    case 2:
                        return; // Goes back to the previous menu
                }
            }
        }

        static void ReadCurrenciesScreen(CurrencyController currencyController)
        {
            var currencies = currencyController.GetCurrencyList().ToList();
            ShowStrListableTable(currencies, "Press Enter to Exit.");
        }

        static void UpdateCurrenciesScreen(CurrencyController currencyController)
        {
            var t = currencyController.updateCurrenciesFromInfoGetter();
            string waitingString = ".";
            int ticks = 0;
            while (!t.IsCompleted && ticks < 5 * 3) // 3 seconds
            {
                AnsiConsole.Clear();
                ShowBox($"Getting Currencies {waitingString}");
                Thread.Sleep(200);
                ticks++;
                waitingString += ".";
            }
            if (t.IsCompletedSuccessfully)
            {
                ShowBox("Updated Currency Database!");
            }
            else if (ticks >= 5 * 3)
            {
                ShowErrorBox("Could not get currencies: Timed out");
                t.Dispose();
            }
        }

        #endregion
        static void SeeTransactionsScreen(TransactionController transCtrl)
        {
            var transactions = transCtrl.GetTransactions().ToList();
            ShowListableTable(transactions, "Press Enter to Exit.");
        }

        // TODO: Use the power of listable so all items are listed custom
        static void ShowStrListableTable<T>(IList<T> listable, string prompt) where T : IListable, IIndexable<string>
        {
            var listing = listable.Select((it) => { return $"{it.ID} - {(it.item.shortName != null ? it.item.shortName : "").PadRight(3)} {it.item.name} {it.item.description}"; }).ToList();
            if (listing.Count == 0)
            {
                listing.Add("There are no items");
            }
            int selectedIndex = SelectOption(listing, prompt);
        }

        static void ShowListableTable<T>(IList<T> listable, string prompt) where T : IListable, IIndexable<int>
        {
            var listing = listable.Select((it) => { return $"{it.ID} {it.item.name}: {it.item.description}"; }).ToList();
            if (listing.Count == 0)
            {
                listing.Add("There are no items");
            }
            int selectedIndex = SelectOption(listing, prompt);
        }

        #endregion

        static void SeeReportScreen(TransactionController transactionCtrl)
        {
            var transactions = transactionCtrl.GetTransactions().ToList();
            int transactionCount = transactions.Count;
            if (transactionCount == 0)
            {
                ShowErrorBox("There are no transactions.");
                return;
            }
            List<string> options = new List<string>()
            {
                "By Category",
                "By Source Account",
                "By Target Account",
                "Go Back"
            };
            while (true)
            {
                int selected = SelectOption(options, "How should we group your report?");
                AnsiConsole.Clear();

                float totalExpenses = transactions.Sum((tr) => { return tr.targetAccount == null ? tr.amount * tr.rate : 0; });
                float totalIncome = transactions.Sum((tr) => { return tr.sourceAccount == null ? tr.amount * tr.rate : 0; });

                var table = new Table();

                switch (selected)
                {
                    case 0:
                        var transactionsByCategory = transactions.GroupBy((tr) =>
                        {
                            return tr.category.ID;
                        });

                        int categories = transactionsByCategory.Count();
                        // Add some columns
                        table.AddColumn("Category");
                        table.AddColumn("Count");
                        table.AddColumn("Density");

                        table.AddColumn("Expenses (US$)");
                        table.AddColumn("Expense Share");

                        table.AddColumn("Income (US$)");
                        table.AddColumn("Income Share");

                        foreach (var transactionByCategory in transactionsByCategory)
                        {
                            int catCount = transactionByCategory.Count();
                            float catDensity = (catCount * 1.0f / categories);

                            float catExpenses = transactionByCategory.Sum((tr) => { return tr.targetAccount == null ? tr.amount * tr.rate : 0; });
                            float catExpenseShare = catExpenses / totalExpenses;

                            float catIncome = transactionByCategory.Sum((tr) => { return tr.sourceAccount == null ? tr.amount * tr.rate : 0; });
                            float catIncomeShare = catIncome / totalIncome;


                            Category sampleCat = transactionByCategory.ElementAt(0).category;
                            table.AddRow(sampleCat.item.name,
                                catCount.ToString(),
                                catDensity.ToString(),

                                catExpenses.ToString(),
                                catExpenseShare.ToString(),

                                catIncome.ToString(),
                                catIncomeShare.ToString()
                                ); ;
                        }
                        AnsiConsole.Write(table);
                        break;
                    case 1:
                        var transactionsBySource = transactions.Where((tr) => tr.sourceAccount != null).GroupBy((tr) =>
                        {
                            return tr.sourceAccount.ID;
                        });

                        int sources = transactionsBySource.Count();
                        // Add some columns
                        table.AddColumn("Account");
                        table.AddColumn("Count");
                        table.AddColumn("Density");

                        table.AddColumn("Expenses (US$)");
                        table.AddColumn("Expense Share");

                        foreach (var transactionBySource in transactionsBySource)
                        {
                            int accCount = transactionBySource.Count();
                            float accDensity = (accCount * 1.0f / sources);

                            float accExpenses = transactionBySource.Sum((tr) => { return tr.targetAccount == null ? tr.amount * tr.rate : 0; });
                            float accExpenseShare = accExpenses / totalExpenses;

                            Account sampleAccount = transactionBySource.ElementAt(0).sourceAccount;
                            table.AddRow(sampleAccount.item.name,
                                accCount.ToString(),
                                accDensity.ToString(),

                                accExpenses.ToString(),
                                accExpenseShare.ToString()
                                );
                        }
                        AnsiConsole.Write(table);
                        break;
                    case 2:
                        var transactionsByTarget = transactions.Where((tr) => tr.targetAccount != null).GroupBy((tr) =>
                        {
                            return tr.targetAccount.ID;
                        });

                        int targets = transactionsByTarget.Count();
                        // Add some columns
                        table.AddColumn("Account");
                        table.AddColumn("Count");
                        table.AddColumn("Density");

                        table.AddColumn("Income (US$)");
                        table.AddColumn("Income Share");

                        foreach (var transactionByTarget in transactionsByTarget)
                        {
                            int accCount = transactionByTarget.Count();
                            float accDensity = (accCount * 1.0f / targets);

                            float accExpenses = transactionByTarget.Sum((tr) => { return tr.sourceAccount == null ? tr.amount * tr.rate : 0; });
                            float accExpenseShare = accExpenses / totalExpenses;

                            Account sampleAccount = transactionByTarget.ElementAt(0).targetAccount;
                            table.AddRow(sampleAccount.item.name,
                                accCount.ToString(),
                                accDensity.ToString(),

                                accExpenses.ToString(),
                                accExpenseShare.ToString()
                                );
                        }
                        break;
                    case 3:
                        return; // Goes back to the previous menu
                }
            }
        }

        #region Utilities
        static T SelectListable<T>(IList<T> listable, string prompt) where T : IListable, IIndexable<int>
        {
            var listing = listable.Select((it) => { return $"{it.ID}. {it.item.name}"; }).ToList();
            int selectedIndex = SelectOption(listing, prompt);

            return listable[selectedIndex];
        }

        static T SelectStrListable<T>(IList<T> listable, string prompt) where T : IListable, IIndexable<string>
        {
            var listing = listable.Select((it) => { return $"{it.ID}. {it.item.name}"; }).ToList();
            int selectedIndex = SelectOption(listing, prompt);

            return listable[selectedIndex];
        }

        static void ShowTransactionSummary(Transaction transaction)
        {
            var table = new Table();
            // Add some columns
            table.AddColumn("Item");
            table.AddColumn("Value");

            // Add some rows
            table.AddRow("Transaction Category", transaction.category.item.name);
            if (transaction.sourceAccount != null)
            {
                table.AddRow("Source Account", transaction.sourceAccount.item.name);
                table.AddRow("Outgoing Amount", transaction.amount.ToString());
                table.AddRow("New Source Balance", (transaction.sourceAccount.amount - transaction.amount).ToString());
            }
            if (transaction.rate != 1)
            {
                table.AddRow("Transaction Rate", transaction.rate.ToString());
            }
            if (transaction.targetAccount != null)
            {
                table.AddRow("Target Account", transaction.targetAccount.item.name);
                float incomingAmt = transaction.amount * transaction.rate;
                table.AddRow("Incoming Amount", incomingAmt.ToString());
                table.AddRow("New Target Balance", (transaction.targetAccount.amount + incomingAmt).ToString());
            }
            // Render the table to the console
            AnsiConsole.Write(table);
        }

        static int SelectOption(IList<string> options, string prompt)
        {
            var selectionText = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(prompt)
                .PageSize(10)
                .AddChoices(options));
            return options.IndexOf(selectionText);
        }

        static void ShowErrorBox(string prompt)
        {
            var panel = new Panel(prompt)
                    .Header("[red]Error[/]");
            AnsiConsole.Clear();
            AnsiConsole.Write(panel);
        }

        static void ShowBox(string prompt)
        {
            var panel = new Panel(prompt)
                    .Header("[blue]Processing[/]");
            AnsiConsole.Clear();
            AnsiConsole.Write(panel);
        }

        #endregion


    }
}