using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Epic.Training.Project.Inventory.Text;
using Epic.Training.Project.Inventory.Text.Formatting;
using Epic.Training.Project.Inventory.Text.UserInput;
using Epic.Training.Project.Inventory.Text.Exceptions;
using Epic.Training.Project.Inventory.Text.Persistence;

namespace Epic.Training.Project.Inventory.Text.Menus
{
    class TextMenu
    {
        #region MENU FUNCTIONS - MainMenu;InventoryMenu;CreateItem;EditItem;RemoveItem;DisplayInventory

        /// <summary>
        /// First Menu in the UI. Options - Load inventory from file; New inventory; Quit
        /// </summary>
        internal static void MainMenu()
        {
            while (true)
            {
                Console.Clear();
                Format.Version();
                Console.WriteLine("{0}\n", Resource1.MAIN_MENU_TITLE);


                Console.WriteLine("<<{0}>>", Resource1.SELECT_OPTIONS);
                Console.WriteLine("[1]Load inventory from file");
                Console.WriteLine("[2]New Inventory");
                Console.WriteLine("[3]Quit");
                Format.Separator();

                switch (Console.ReadKey(true).KeyChar)
                {
                    case '1':
                        Inventory inv;
                        string filepath = null;
                        

                        try
                        {
                            inv = Persist.LoadInventory(out filepath);
                            InventoryMenu(inv, filepath);
                        }
                        catch (Exception ex)
                        {
                            if (ex is EscapeKeyPressedException)
                            {
                                throw ex; //Mostly here for testing to make sure stray Escape exceptions arent bubbling up
                            }
                            Console.WriteLine("[! Problem loading inventory: {0} !]", ex.Message);
                            wait(2000);
                        }

                        break;
                    case '2':
                        InventoryMenu(new Inventory());

                        break;
                    case '3':
                        return;
                    default:
                        Console.WriteLine("\n[! {0} !]", Resource1.INVALID_SEL);
                        wait(2000);
                        break;
                }
            }
        }

        /// <summary>
        /// Menu that contains options to manipulate the currently loaded inventory.
        /// </summary>
        /// <param name="inventory">Currently loaded inventory passed in from MainMenu()</param>
        private static void InventoryMenu(Inventory inventory, string filepath = null)
        {
            while (true)
            {
                Console.Clear();
                Format.Version();

                if (filepath != null)
                {
                    Console.WriteLine("{0} - Loaded from {1}\n", Resource1.INV_MENU_TITLE, filepath);
                }
                else
                {
                    Console.WriteLine("{0} - New Inventory\n", Resource1.INV_MENU_TITLE);
                }

                Console.WriteLine("<<{0}>>", Resource1.SELECT_OPTIONS);
                Console.WriteLine("[1]View Inventory State");
                Console.WriteLine("[2]Create New Item(s)");
                Console.WriteLine("[3]Edit Item(s)");
                Console.WriteLine("[4]Save Inventory");
                Console.WriteLine("[5]Return to Main Menu");
                Format.Separator();

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
                        try
                        {
                            Persist.SaveInventory(inventory, ref filepath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("[! Problem saving inventory: {0} !]", ex.Message);
                            wait(2000);
                        }

                        break;
                    case '5':
                        return;
                    default:
                        Console.WriteLine("\n[! {0} !]", Resource1.INVALID_SEL);
                        wait(2000);
                        break;
                }
            }
        }

        /// <summary>
        /// Menu for creating Items to be stored in the currently loaded inventory.
        /// </summary>
        /// <param name="inv">Currently loaded inventory.</param>
        private static void CreateItem(Inventory inv)
        {
            while (true)
            {
                Console.Clear();
                Format.Version();
                Console.WriteLine("{0}\n", Resource1.CREATE_ITEM);

                Console.WriteLine("<<{0}>>\n", Resource1.ESC_PROMPT);
                bool repeat = true;
                int numOption = 0; //Keeps track of what the next prompt should be (0=Name, 1=Quantity, 2=Wholesale, 3=Weight)


                Item itemStage = new Item("", 1, 1, 1m);

                while (repeat)
                {
                    switch (numOption)
                    {
                        case 0: //Name
                            try
                            {
                                itemStage.Name = Input.GetNameFromUser(inv);
                                numOption += 1;
                            }
                            catch (EscapeKeyPressedException ex)
                            {
                                return;
                            }

                            break;
                        case 1: //Quantity
                            try
                            {
                                itemStage.QuantityOnHand = Input.GetQuantityFromUser(itemStage);
                                numOption += 1;
                            }
                            catch (EscapeKeyPressedException ex)
                            {
                                Console.WriteLine();
                                numOption -= 1;
                            }

                            break;
                        case 2: //Wholesale
                            try
                            {
                                itemStage.WholesalePrice = Input.GetWholesaleFromUser(itemStage);
                                numOption += 1;
                            }
                            catch (EscapeKeyPressedException ex)
                            {
                                Console.WriteLine();
                                numOption -= 1;
                            }

                            break;
                        case 3: //Weight
                            try
                            {
                                itemStage.Weight = Input.GetWeightFromUser(itemStage);
                                repeat = false;
                            }
                            catch (EscapeKeyPressedException ex)
                            {
                                Console.WriteLine();
                                numOption -= 1;
                            }

                            break;
                    }
                }

                Console.WriteLine();
                Format.TableHeader();
                Format.DisplayRow(itemStage, 0);
                Console.WriteLine("\n<<[ENTER] to add Item '{0}' to the inventory - any other key to discard it>>\n", itemStage.Name);
                if (Console.ReadKey().Key == ConsoleKey.Enter)
                {
                    inv.Add(itemStage);
                    Console.WriteLine("\n['{0}' added to Inventory]", itemStage.Name);
                }
                else
                {
                    Console.WriteLine("\n[Discarding staged Item]");
                }

                wait(2000);
            }

        }

        /// <summary>
        /// Menu for editing pre-existing Items in the currently loaded inventory.
        /// </summary>
        /// <param name="inv">Currently loaded inventory.</param>
        private static void EditItem(Inventory inv)
        {
            bool continueEditingCurrent = false; //Whether or not to continue editing currently selected Item

            while (true)
            {
                Console.Clear();
                Format.Version();
                Console.WriteLine("{0}\n", Resource1.EDIT_ITEM);

                #region CHECK INVENTORY IS NOT EMPTY; DISPLAY ITEMS

                if (inv.TotalProducts < 1)
                {
                    Console.WriteLine("[! There are no items in this Inventory yet. Returning to Inventory Menu !]");
                    wait(2000);
                    return;
                }

                Format.DisplayInventory(inv, SortOption.ByName);

                Console.WriteLine("\n<<{0}>>\n", Resource1.ESC_PROMPT);

                #endregion

                #region CHECK IF USER CHOSEN ITEM EXISTS; INSTANTIATE ITEMSTAGE

                Item itemStage = null;
                string itemName;

                do
                {
                    try
                    {
                        itemName = Input.betterInput("\n<<Enter the name of the Item you wish to edit>> ");

                        if (inv.Contains(itemName))
                        {
                            itemStage = inv[itemName];
                            continueEditingCurrent = true;
                        }
                        else
                        {
                            Console.WriteLine("\n[! No Item with name '{0}' exists in this inventory !]", itemName);
                        }
                    }
                    catch (EscapeKeyPressedException ex)
                    {
                        return;
                    }
                }
                while (itemStage == null);

                #endregion

                
                while (continueEditingCurrent)
                {
                    Console.Clear();
                    Console.WriteLine("\n<<{0}>>\n", Resource1.ESC_PROMPT);

                    Format.TableHeader();
                    Format.DisplayRow(itemStage, 0);
       
                    Console.WriteLine("\n<<Item '{0}' found. Select which property to edit>>", itemStage.Name);
                    Console.WriteLine("[1]Name");
                    Console.WriteLine("[2]Quantity On Hand");
                    Console.WriteLine("[3]Wholesale Price (USD)");
                    Console.WriteLine("[4]Weight (LBS)");
                    Console.WriteLine("[5]Remove Item from Inventory");
                    Console.WriteLine("[6]Select another Item");
                    Console.WriteLine("[7]Return to Inventory Menu");
                    Format.Separator();

                    switch (Console.ReadKey(true).KeyChar)
                    {
                        case '1':
                            try
                            {
                                itemStage.Name = Input.GetNameFromUser(inv);
                            }
                            catch (EscapeKeyPressedException ex)
                            {
                                Console.WriteLine();
                            }

                            break;
                        case '2':
                            try
                            {
                                itemStage.QuantityOnHand = Input.GetQuantityFromUser(itemStage);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine();
                            }

                            break;
                        case '3':
                            try
                            {
                                itemStage.WholesalePrice = Input.GetWholesaleFromUser(itemStage);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine();
                            }

                            break;
                        case '4':
                            try
                            {
                                itemStage.Weight = Input.GetWeightFromUser(itemStage);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine();
                            }

                            break;
                        case '5':
                            Console.WriteLine("<<[ENTER] to confirm Item removal or any other key to abort>>");
                            if (Console.ReadKey().Key == ConsoleKey.Enter)
                            {
                                inv.Remove(itemStage);
                                Console.WriteLine("\n[Item '{0}' has been removed from Inventory]\n", itemStage.Name);
                                continueEditingCurrent = false;
                            }

                            break;
                        case '6':
                            continueEditingCurrent = false;

                            break;
                        case '7':
                            return;
                        default:
                            Console.WriteLine("[! {0} !]\n", Resource1.INVALID_SEL);
                            wait(2000);
                            continue;
                    }
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
            Format.InventoryStatistics(inv);

            while (true)
            {
                Console.WriteLine("\n<<Select a sorted view for the Inventory>>\n");
                Console.WriteLine("[1]Sort Items alphabetically by name");
                Console.WriteLine("[2]Sort Items by order entered into Inventory");
                //Console.WriteLine("[3]Display Inventory Statistics");
                Console.WriteLine("[3]Return to Inventory Menu");
                Format.Separator();

                switch (Console.ReadKey(true).KeyChar)
                {
                    case '1':
                        Console.Clear();
                        Format.InventoryStatistics(inv);
                        Format.DisplayInventory(inv, SortOption.ByName);

                        break;
                    case '2':
                        Console.Clear();
                        Format.InventoryStatistics(inv);
                        Format.DisplayInventory(inv, SortOption.ByAdded);

                        break;
                    case '3':
                        return;
                    default:
                        Console.WriteLine("\n[! {0} !]", Resource1.INVALID_SEL);
                        wait(2000);
                        Console.Clear();
                        Format.InventoryStatistics(inv);
                        
                        break;
                }
            }
        }

        private static void wait(int milliseconds)
        {
            System.Threading.Thread.Sleep(milliseconds);
        }


        #endregion
    }
}
