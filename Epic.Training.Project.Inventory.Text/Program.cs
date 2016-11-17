using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Epic.Training.Project.Inventory;
using Epic.Training.Project.SuppliedCode.Exceptions;
using Epic.Training.Project.Inventory.Text.Exceptions;
using Epic.Training.Project.Inventory.Text.Persistence;
using Epic.Training.Project.Inventory.Text.Menus;
using System.Runtime.CompilerServices;


//FOUNDATIONS LAB B

namespace Epic.Training.Project.Inventory.Text
{
    /// <summary>
    /// The UI portion of your project
    /// </summary>
    class Program
    {
        private static string defaultBinDir = String.Format(@"{0}\{1}", Environment.GetEnvironmentVariable(Resource1.DefaultBinDirEnv), Resource1.DefaultBinDir);


        #region CUSTOM TESTS

        private static void TwoInventoriesSharedItem()
        {
            Inventory inv1 = new Inventory();
            Inventory inv2 = new Inventory();

            Item item1 = new Item("One", 1, 1, 1);
            Item item2 = new Item("Two", 1, 1, 1);
            Item item3 = new Item("Three", 1, 1, 1);

            inv1.Add(item1);
            inv2.Add(item1);


            inv2.Add(item2); //Changing item1's name to "2" should cause problems for inv2.

            inv2.Add(item3);
            Console.WriteLine("inv1");
            foreach (Item item in inv1)
            {
                Console.WriteLine(item.Name);
            }
            Separator();

            Console.WriteLine("inv2");
            foreach (Item item in inv2)
            {
                Console.WriteLine(item.Name);
            }
            Separator();

            try
            {
                item1.Name = "Four";
                item3.Name = "seben";
                item1.Name = "Three";
                item3.Name = "Two";
            }
            catch (DuplicateProductNameException ex)
            {
                Console.WriteLine("There exists an inventory with that product name already");
            }


            Separator();
            Console.WriteLine("inv1");
            foreach (Item item in inv1)
            {
                Console.WriteLine(item.Name);
            }
            Separator();

            Console.WriteLine("inv2");
            foreach (Item item in inv2)
            {
                Console.WriteLine(item.Name);
            }
        }

        private static void BetterInputTest()
        {
            bool repeat = true;
            while (repeat)
            {
                Console.WriteLine("Menu Options lol");
                string input;
                try
                {
                    input = betterInput("stuff> ");
                }
                catch (EscapeKeyPressedException ex)
                {
                    Console.WriteLine("\n\n[ESC] Pressed - back to menu");
                    System.Threading.Thread.Sleep(2500);
                    Console.Clear();
                    continue;
                }
                Console.WriteLine("Input was: {0}", input);
            }
        }

        #endregion

        /// <summary>
        /// Calls MainMenu and processes optional command-line arguments to pass into MainMenu. 
        /// </summary>
        /// <param name="args">Command-line arguments</param>
        [STAThread()]
        static void Main(string[] args)
        {
            Persist.createPersistentDir();
            TextMenu.MainMenu();


            //MainMenu();
            
    
            //TwoInventoriesSharedItem();
            //BetterInputTest();
            //Console.ReadKey();
        }

        #region MENU FORMATTING - Seperator;Version;TableHeader;DisplayRow;DisplayInventory

        /// <summary>
        /// Included in menus for clear separation between console output and user input.
        /// </summary>
        static void Separator()
        {
            Console.WriteLine(new String('-',25) + "\n");
        }

        /// <summary>
        /// Displays version of UI build
        /// </summary>
        static void Version()
        {
            Console.WriteLine("InventoryTracker v1");
            Separator();
        }

        /// <summary>
        /// Displays TableHeader used when displaying inventory. Columns = {Order, Name, Wholesale (USD), Retail (USD), Quantity, Weight (Lbs)
        /// </summary>
        static void TableHeader()
        {
            Console.WriteLine("  |-------------------------------------------------------------------------");
            Console.WriteLine("  | ORD |           NAME           | WHLSALE-USD | RTL-USD | QTY | WGT-LBS |");
            Console.WriteLine("  |-------------------------------------------------------------------------");
        }

        /// <summary>
        /// Displays a row containing columns of an Item's publicly facing properties.
        /// </summary>
        /// <param name="item">Item object to display row for.</param>
        /// <param name="order">Int - used for first column to track order of objects.</param>
        static void DisplayRow(Item item, int order)
        {
            Console.WriteLine("  |{0,5}|{1,26}|{2,13}|{3,9}|{4,5}|{5,9}|", order, item.Name, item.WholesalePrice, item.RetailPrice, item.QuantityOnHand, item.Weight);
            Console.WriteLine("  |-------------------------------------------------------------------------");
        }

        /// <summary>
        /// Prints the contents of the currently loaded Inventory
        /// </summary>
        /// <param name="inv">Inventory to display</param>
        /// <param name="option">How to sort the Items in the Inventory before viewing</param>
        static void DisplayInventory(Inventory inv, SortOption option)
        {
            TableHeader();
            int counter = 0;
            foreach (Item item in inv.GetSortedProducts(option))
            {
                DisplayRow(item, counter++);
            }
        }

        #endregion

        #region ITEM PROPERTY VALIDATION - [GetName|GetQuantity|GetWholesale|GetWeight]FromUser

        /// <summary>
        /// Leave it up to calling function/menu to decide outcome.
        /// </summary>
        /// <param name="inv">Currently loaded inventory</param>
        /// <param name="proposedName">Proposed name of Item</param>
        /// <returns>{False, Invalid Name}; {True, Valid Name}</returns>
        static bool TestName(Inventory inv, out string proposedName)
        {
            Console.Write("\b<<Product name (MAX-26 chars)>> ");
            proposedName = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(proposedName))
            {
                Console.WriteLine("\n[!Product name cannot be blank!]\n");
                proposedName = null;
                return false;
            }
            else if (proposedName.Count() > 26)
            {
                Console.WriteLine("\n[!'{0}' too long of a name (MAXLENGTH = 26)!]\n", proposedName);
                proposedName = null;
                return false;
            }
            else if (inv.Contains(proposedName))
            {
                Console.WriteLine("\n[!Product with name '{0}' already exists in the inventory!]\n", proposedName);
                proposedName = null;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Retrieves and validates user input for item names. 
        /// </summary>
        /// <param name="inv">Currently loaded Inventory</param>
        /// <param name="callerName">Name of calling Menu/method</param>
        /// <exception cref="Epic.Training.Project.Inventory.Text.Exceptions">Thrown when user presses [ESC] key</exception>
        /// <returns type="String">Name of Item</returns>
        private static string GetNameFromUser(Inventory inv, [CallerMemberName] string callerName="")
        {
            string proposedName;

            while (true)
            {
                try
                {
                    proposedName = betterInput("\b<<Product NAME (MAX-26 chars)>> ");
                }
                catch (EscapeKeyPressedException ex)
                {
                    throw ex; //Let the caller of this method handle it. 
                }
                

                if (string.IsNullOrWhiteSpace(proposedName))
                {
                    Console.WriteLine("\n[! Product name cannot be blank !]\n");
                    continue;
                }
                else if (proposedName.Count() > 26)
                {
                    Console.WriteLine("\n[! '{0}' too long of a name (MAXLENGTH = 26) !]\n", proposedName);
                    continue;
                }
                else if (inv.Contains(proposedName))
                {
                    if (callerName == "CreateItem")
                    {
                        Console.WriteLine("Hi from CreateItem");
                    }
                    Console.WriteLine("\n[! Product with name '{0}' already exists in the inventory !]\n", proposedName);
                    continue;
                }

                Console.WriteLine("\n['{0}' is acceptable. Press Enter to continue or any other key to discard changes]\n", proposedName);
                if (Console.ReadKey().Key == ConsoleKey.Enter)
                {
                    return proposedName;
                }
                else
                {
                    continue;
                }
            }
        }

        private static int GetQuantityFromUser(Item itemStage)
        {
            bool result;
            int proposedQuantity;

            while (true)
            {
                //Console.Write("\b<<Quantity of {0}s on hand>> ", itemStage.Name);
                //string proposedAsString = Console.ReadLine();
                string proposedAsString = null;
                try
                {
                    proposedAsString = betterInput(String.Format("\b<<Quantity of {0}s on hand>> ", itemStage.Name));
                }
                catch (EscapeKeyPressedException ex)
                {
                    throw ex;
                }
                result = Int32.TryParse(proposedAsString, out proposedQuantity);

                if (result)
                {
                    if (proposedQuantity < 0)
                    {
                        Console.WriteLine("\n[!'{0}' is negative. Enter a more suitable integer!]\n", proposedQuantity);
                        continue;
                    }

                    Console.WriteLine("\n['{0}' is acceptable. Press 'Enter' to confirm or any other key to discard the change]\n", proposedQuantity);
                    if (Console.ReadKey().Key == ConsoleKey.Enter)
                    {
                        return proposedQuantity;   
                    }
                }
                else
                {
                    Console.WriteLine("\n[!Could not parse '{0}'. Entry too large, contains alpha chars, or is not an integer!]\n", proposedAsString); //TryParse is already going to fail an entry larger than type's MaxValue
                }
                    
            }
        }

        private static decimal GetWholesaleFromUser(Item itemStage)
        {
            bool result;
            decimal proposedWholesale;

            while (true)
            {
                //Console.Write("\b<<Wholesale Price (USD) of one unit of {0}>> ", itemStage.Name);
                string proposedAsString = null;
                try
                {
                    proposedAsString = betterInput(String.Format("\b<<Wholesale Price (USD) of one unit of {0}>> ", itemStage.Name));
                }
                catch (EscapeKeyPressedException ex)
                {
                    throw ex;
                }
                result = Decimal.TryParse(proposedAsString, out proposedWholesale);

                if (result)
                {
                    if (proposedWholesale <= 0)
                    {
                        Console.WriteLine("\n[!'{0}' is negative, or zero. Please enter a more suitable number!]\n", proposedWholesale);
                        continue;
                    }

                    Console.WriteLine("\n['{0}' is acceptable. Press 'Enter' to confirm or any other key to discard the change]\n", proposedWholesale);
                    if (Console.ReadKey().Key == ConsoleKey.Enter)
                    {
                        return proposedWholesale;
                    }
                }
                else
                {
                    Console.WriteLine("\n[!Could not parse '{0}'. Entry too large or contains alpha chars!]\n", proposedWholesale); //TryParse is already going to fail an entry larger than type's MaxValue
                }
                    
            }
        }

        private static double GetWeightFromUser(Item itemStage)
        {
            bool result;
            double proposedWeight;

            while (true)
            {
                //Console.Write("\b<<Weight (lb) of one unit of {0}>> ", itemStage.Name);
                string proposedAsString = null;
                try
                {
                    proposedAsString = betterInput(String.Format("\b<<Weight (lb) of one unit of {0}>> ", itemStage.Name));
                }
                catch (EscapeKeyPressedException ex)
                {
                    throw ex;
                }
                result = Double.TryParse(proposedAsString, out proposedWeight);

                if (result)
                {
                    if (proposedWeight <= 0)
                    {
                        Console.WriteLine("\n[!'{0}' is negative or zero. Please enter a more suitable number!]\n", proposedWeight);
                        continue;
                    }

                    Console.WriteLine("\n['{0}' is acceptable. Press 'Enter' to confirm or any other key to discard the change]\n", proposedWeight);
                    if (Console.ReadKey().Key == ConsoleKey.Enter)
                    {
                        return proposedWeight;    
                    }
                }
                else
                {
                    Console.WriteLine("\n[!Could not parse '{0}'. Entry too large or contains alpha chars!]\n", proposedWeight); //TryParse is already going to fail an entry larger than type's MaxValue
                }       
            }
        }

        #endregion

        #region MENU FUNCTIONS - MainMenu;InventoryMenu;CreateItem;EditItem;RemoveItem;DisplayInventory

        /// <summary>
        /// First Menu in the UI. Options - Load inventory from file; New inventory, Quit
        /// </summary>
        private static void MainMenu()
        {
            while (true)
            {
                Console.Clear();
                Version();
                Console.WriteLine("<<MAIN MENU>>\n");

                
                Console.WriteLine("<<Please select a numbered option from below>>");
                Console.WriteLine("[1]Load inventory from file");
                Console.WriteLine("[2]New Inventory");
                Console.WriteLine("[3]Quit");
                Separator();

                switch (Console.ReadKey(true).KeyChar)
                {
                    case '1':
                        string filepath; 
                        Inventory inv = LoadInventory(out filepath);

                        if (inv != null)
                        {
                            InventoryMenu(inv, filepath);
                        }
                        else
                        {
                            Console.WriteLine("[!Problem loading inventory from {0} - Creating New Inventory!]", filepath);
                            System.Threading.Thread.Sleep(2500);
                            inv = new Inventory();
                            InventoryMenu(inv, null);
                        }
                        
                        break;
                    case '2':
                        inv = new Inventory();
                        InventoryMenu(inv);

                        break;
                    case '3':
                        return;
                    default:
                        Console.WriteLine("\n[!Invalid selection - returning to Main Menu!]");
                        System.Threading.Thread.Sleep(1000);
                        break;
                }  
            }
        }

        /// <summary>
        /// Menu that contains options to manipulate the currently loaded inventory.
        /// </summary>
        /// <param name="inventory">Currently loaded inventory passed in from MainMenu()</param>
        private static void InventoryMenu(Inventory inventory, string filepath=null)
        {
            while (true)
            {
                Console.Clear();
                Version();

                if (filepath != null)
                {
                    Console.WriteLine("<<INVENTORY MENU>> - Loaded from {0}\n", filepath);
                }
                else
                {
                    Console.WriteLine("<<INVENTORY MENU>> - New Inventory\n");
                }                

                Console.WriteLine("<<Please select a numbered option from below>>");
                Console.WriteLine("[1]View Inventory State");
                Console.WriteLine("[2]Create New Item");
                Console.WriteLine("[3]Edit Item");
                Console.WriteLine("[4]Save Inventory");
                Console.WriteLine("[5]Return to Main Menu");
                Separator();

                switch (Console.ReadKey(true).KeyChar)
                {
                    case '1':
                        InventoryState(inventory);
                        break;
                    case '2':
                        CreateItem(inventory);
                        break;
                    case '3':
                        EditItem(inventory);
                        break;
                    case '4':
                        SaveInventory(inventory, ref filepath);
                        break;
                    case '5':
                        return;
                    default:
                        Console.WriteLine("\n[!Invalid selection - returning to Inventory menu!]");
                        System.Threading.Thread.Sleep(1000);
                        break;
                }   
            }
        }

        /// <summary>
        /// Menu for creating Items to be stored in the currently loaded inventory.
        /// </summary>
        /// <param name="inv">Currently loaded inventory.</param>
        static private void CreateItem(Inventory inv)
        {
            Console.Clear();
            Version();
            Console.WriteLine("<<ITEM CREATION>>\n");

            Console.WriteLine("<<Press [ESC] at any time to return to previous Prompt or Menu>>\n");
            bool repeat = true;
            int numOption = 0; //Keeps track of what the next prompt should be (Name, Quantity, Wholesale, Weight)


            Item itemStage = new Item("", 1, 1, 1m);

            while (repeat)
            {
                switch (numOption)
                {
                    case 0: //Name
                        try
                        {
                            itemStage.Name = GetNameFromUser(inv);
                            numOption += 1;
                        }
                        catch (EscapeKeyPressedException ex)
                        {
                            Console.WriteLine("\n\n[Key [ESC] Pressed - Going back to Main Menu]\n");
                            System.Threading.Thread.Sleep(2000);
                            return;
                        }

                        break;
                    case 1: //Quantity
                        try
                        {
                            itemStage.QuantityOnHand = GetQuantityFromUser(itemStage);
                            numOption += 1;
                        }
                        catch (EscapeKeyPressedException ex)
                        {
                            Console.WriteLine("\n\n[Key [ESC] Pressed - Back to Name entry]\n");
                            numOption -= 1;
                        }

                        break;
                    case 2: //Wholesale
                        try
                        {
                            itemStage.WholesalePrice = GetWholesaleFromUser(itemStage);
                            numOption += 1;
                        }
                        catch (EscapeKeyPressedException ex)
                        {
                            Console.WriteLine("\n\n[Key [ESC] Pressed - Back to Quantity entry]\n");
                            numOption -= 1;
                        }

                        break;
                    case 3: //Weight
                        try
                        {
                            itemStage.Weight = GetWeightFromUser(itemStage);
                            repeat = false;
                        }
                        catch (EscapeKeyPressedException ex)
                        {
                            Console.WriteLine("\n\n[Key [ESC] Pressed - Back to Wholesale entry]");
                            numOption -= 1;
                        }

                        break;
                }
            }

            TableHeader();
            DisplayRow(itemStage, 0);
            Console.WriteLine("\n<<Press 'Enter' to add Item '{0}' to the inventory or any other key to discard and return to Inventory Menu>>\n", itemStage.Name);
            if (Console.ReadKey().Key == ConsoleKey.Enter)
            {
                inv.Add(itemStage);
            }
            else
            {
                Console.WriteLine("\n[Discarding staged Item - Returning to Inventory Menu]");
                System.Threading.Thread.Sleep(2000);
            }

        }

        /// <summary>
        /// Menu for editing pre-existing Items in the currently loaded inventory.
        /// </summary>
        /// <param name="inv">Currently loaded inventory.</param>
        private static void EditItem(Inventory inv)
        {
            while (true)
            {
                Console.Clear();
                Version();
                Console.WriteLine("<<Item Editor>>\n");

                #region CHECK INVENTORY IS NOT EMPTY; DISPLAY ITEMS

                if (inv.TotalProducts < 1)
                {
                    Console.WriteLine("[! There are no items in this Inventory yet. Returning to Inventory Menu !]");
                    System.Threading.Thread.Sleep(2000);
                    return;
                }
                
                DisplayInventory(inv, SortOption.ByName);

                Console.WriteLine("\n<<Press [ESC] at any time to return to previous Prompt or Menu>>\n");

                #endregion

                #region INSTANTIATE ITEMSTAGE; CHECK IF USER CHOSEN ITEM EXISTS

                Item itemStage = null;
                string itemName;

                do
                {
                    

                    try
                    {
                        itemName = betterInput("\n<<Enter the name of the Item you wish to edit>> ");

                        if (inv.Contains(itemName))
                        {
                            itemStage = inv[itemName];
                        }
                        else
                        {
                            Console.WriteLine("\n[! No Item with name '{0}' exists in this inventory !]", itemName);
                        }
                    }
                    catch (EscapeKeyPressedException ex)
                    {
                        Console.WriteLine("\n\n[Key [ESC] Pressed - Back to Inventory Menu]");
                        System.Threading.Thread.Sleep(2000);
                        return;
                    }
                    
                    
                }
                while (itemStage == null);

                #endregion


                Console.WriteLine("\n[Item '{0}' found. Select which property to edit]\n", itemStage.Name);
                TableHeader();
                DisplayRow(itemStage, 0);
                Console.WriteLine();
                Console.WriteLine("[1]Name");
                Console.WriteLine("[2]Quantity On Hand");
                Console.WriteLine("[3]Wholesale Price (USD)");
                Console.WriteLine("[4]Weight (LBS)");
                Console.WriteLine("[5]Remove Item from Inventory");
                Console.WriteLine("[6]Cancel and return to Inventory Menu");
                Separator();

                switch (Console.ReadKey(true).KeyChar)
                {
                    case '1':
                        string proposedName = null;
                        if (TestName(inv, out proposedName) && proposedName != null)
                        {
                            itemStage.Name = proposedName;
                        }
                        else
                        {
                            System.Threading.Thread.Sleep(2500);
                        }

                        break;
                    case '2':
                        itemStage.QuantityOnHand = GetQuantityFromUser(itemStage);
                        break;
                    case '3':
                        itemStage.WholesalePrice = GetWholesaleFromUser(itemStage);
                        break;
                    case '4':
                        itemStage.Weight = GetWeightFromUser(itemStage);
                        break;
                    case '5':
                        Console.WriteLine("<<Press 'Enter' to confirm Item removal or any other key to abort and return to Inventory Menu>>");
                        if (Console.ReadKey().Key == ConsoleKey.Enter)
                        {
                            inv.Remove(itemStage);
                            Console.WriteLine("\n[Item '{0}' has been removed from Inventory]\n", itemStage.Name);
                        }
                        break;                      
                    case '6':
                        return;
                    default:
                        Console.WriteLine("\n[! Invalid selection - returning to Inventory menu !]");
                        System.Threading.Thread.Sleep(1000);
                        return;
                }
            }
        }

        /// <summary>
        /// Menu for displaying inventory statistics of the currently loaded inventory, including its Item members.
        /// </summary>
        /// <param name="inv">Currently loaded inventory.</param>
        private static void InventoryState(Inventory inv)
        {
            Console.Clear();
            Version();

            Console.WriteLine("Total number of unique products: {0}", inv.TotalProducts);
            Console.WriteLine("Total number of products in stock: {0}", inv.ItemsInStock);
            Console.WriteLine("Total Wholesale-USD of Inventory: {0}", inv.TotalWholesalePrice);
            Console.WriteLine("Total Retail-USD of Inventory: {0}", inv.TotalRetailPrice);

            Separator();

            while(true)
            {
                Console.WriteLine("\n<<Select a sorted view for the Inventory>>\n");
                Console.WriteLine("[1]Sort Items alphabetically by name");
                Console.WriteLine("[2]Sort Items by order entered into Inventory");
                Console.WriteLine("[3]Display Inventory Statistics");
                Console.WriteLine("[4]Return to Inventory Menu");
                Separator();

                switch (Console.ReadKey(true).KeyChar)
                {
                    case '1':
                        Console.WriteLine("\n");
                        DisplayInventory(inv, SortOption.ByName);
                        break;
                    case '2':
                        Console.WriteLine("\n");
                        DisplayInventory(inv, SortOption.ByAdded);
                        break;
                    case '3':
                        Console.Clear();
                        Console.WriteLine("Total number of unique products: {0}", inv.TotalProducts);
                        Console.WriteLine("Total number of products in stock: {0}", inv.ItemsInStock);
                        Console.WriteLine("Total Wholesale-USD of Inventory: {0}", inv.TotalWholesalePrice);
                        Console.WriteLine("Total Retail-USD of Inventory: {0}", inv.TotalRetailPrice);
                        Separator();
                        break;
                    case '4':
                        return;
                    default:
                        Console.WriteLine("\n[Invalid selection - returning to Inventory menu]");
                        System.Threading.Thread.Sleep(1000);
                        break;               
                }
            }
        }

        #endregion

        #region INVENTORY (DE)SERIALIZATION - GetFileName;SaveInventory;LoadInventory

        /// <summary>
        /// Creates a [Save|Open]FileDialog for *.bin files located in %APPDATA%\EpicInventory and [Saves|Opens] the selected filename.
        /// </summary>
        /// <param name="load">True: Loading Inventory. False: Saving Inventory</param>
        /// <returns>String - Absolute path of file</returns>
        private static string GetFilename(bool load, string currentPath = null)
        {

            string filePath = null;

            if (load)
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Title = "INVENTORY-TRACKER-LOADING";
                dlg.Filter = "Binary files (*.bin)|*.bin";

                dlg.InitialDirectory = defaultBinDir;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    filePath = dlg.FileName;
                }
            }
            else
            {
                SaveFileDialog slg = new SaveFileDialog();
                slg.Title = "INVENTORY-TRACKER-SAVING";
                slg.Filter = "Binary files (*.bin)|*.bin";
                slg.InitialDirectory = defaultBinDir;

                slg.FileName = currentPath; //Defaults as the previously/currently loaded *.bin file. Empty if saving a new inventory
                if (slg.ShowDialog() == DialogResult.OK)
                {
                    filePath = slg.FileName;
                }
            }
      
            return filePath;
        }

        private static void SaveInventory(Inventory inv, ref string currentName)
        {       
            if (!String.IsNullOrWhiteSpace(currentName))
            {
                Console.WriteLine("[File path of currently loaded inventory - {0}]\n", currentName);
            }

            string filePath = GetFilename(false, currentName);
            FileStream s;

            try
            {
                //s = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                s = File.Create(filePath);
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter b = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                
                b.Serialize(s, inv);
                Console.WriteLine(@"Formatted inventory saved to {0}", filePath);
                s.Close();
                currentName = filePath;
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("\n[! No valid file selection was made. Returning to Inventory Menu !]\n");
            }
            catch (System.Runtime.Serialization.SerializationException ex)
            {
                Console.WriteLine("\n[! IOError: {0}\n Returning to Inventory Menu !]\n", ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n[! Error: {0}\n Returning to Inventory Menu !]\n", ex.Message);         
            }

            System.Threading.Thread.Sleep(2000);
        }

        public static Inventory LoadInventory(out string filePath)
        {
            filePath = GetFilename(true);
            FileStream s;

            try
            {
                s = File.OpenRead(filePath);
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter b = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                Inventory inventory = (Inventory)b.Deserialize(s);
                s.Close();
                return inventory;
            }
            catch (ArgumentNullException ex)
            {
                Console.WriteLine("\n[! No valid file selection was made. Returning to Main Menu !]\n");
            }
            catch (System.Runtime.Serialization.SerializationException ex)
            {
                Console.WriteLine("\n[! IOError: {0}\n Returning to Main Menu !]\n", ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n[! Error: {0}\n Returning to Main Menu !]\n", ex.Message);
            }

			System.Threading.Thread.Sleep(2000);
            return null;
        }

        #endregion

        #region BETTER INPUT - ESCAPE PROMPTS AT ANY TIME

        /// <summary>
        /// Allows escape from any user prompt, at any time. 
        /// </summary>
        /// <param name="prompt">[String] Prompt displayed to user for input.</param>
        /// <returns>[String] if the Enter key is pressed. [null] if Escape key is pressed.</returns>
        private static string betterInput(string prompt)
        {
            StringBuilder input = new StringBuilder();
            ConsoleKeyInfo cki = new ConsoleKeyInfo();

            string inputAsStr = null; //Returned as input.ToString()
            bool inLoop = true; //Set to false when user hits [ENTER]
            
            Console.Write(prompt);
            while (inLoop)
            {
                cki = Console.ReadKey();

                switch (cki.Key)
                {
                    case ConsoleKey.Escape:
                        throw new EscapeKeyPressedException();
                    case ConsoleKey.Backspace:
                        if (input.Length > 0)
                        {
                            input.Remove(input.Length - 1, 1);
                            int currentLineCursor = Console.CursorTop;
                            Console.SetCursorPosition(input.Length + prompt.Length - 1, Console.CursorTop);
                            Console.Write(new string(' ', Console.WindowWidth)); //Clears/Overwrites character removed from input StringBuilder
                            Console.SetCursorPosition(input.Length + prompt.Length - 1, currentLineCursor); //Brings cursor back to same position 
                        }
                        else
                        {
                            ClearCurrentConsoleLine();
                            Console.Write(prompt);
                        }

                        break;
                    case ConsoleKey.Enter:
                        inputAsStr = input.ToString();
                        inLoop = false;

                        break;
                    default:
                        if (Char.IsLetterOrDigit(cki.KeyChar) || Char.IsPunctuation(cki.KeyChar) || Char.IsSymbol(cki.KeyChar) || Char.IsSeparator(cki.KeyChar) || cki.KeyChar == ' ')
                        {
                            input.Append(cki.KeyChar);
                        }

                        break;
                }
            }

            Console.WriteLine();
            return inputAsStr;
        }

        private static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        #endregion

    }




}
