using System;

namespace Epic.Training.Project.Inventory.Text.Formatting
{
    internal static class Format
    {
        private static String SEPARATOR = new String('-', 25);

        /// <summary>
        /// Included in menus for clear separation between console output and user input.
        /// </summary>
        internal static void Separator()
        {
            Console.WriteLine(SEPARATOR);
        }

        /// <summary>
        /// Displays version of UI build
        /// </summary>
        internal static void Version()
        {
            Console.WriteLine("{0}", Resource1.UI_VERSION);
            Separator();
        }

        /// <summary>
        /// Displays TableHeader used when displaying inventory. Columns = {Order, Name, Wholesale (USD), Retail (USD), Quantity, Weight (Lbs)
        /// </summary>
        internal static void TableHeader()
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
        internal static void DisplayRow(Item item, int order)
        {
            Console.WriteLine("  |{0,5}|{1,26}|{2,13}|{3,9}|{4,5}|{5,9}|", order, item.Name, item.WholesalePrice, item.RetailPrice, item.QuantityOnHand, item.Weight);
            Console.WriteLine("  |-------------------------------------------------------------------------");
        }

        /// <summary>
        /// Prints the contents of the currently loaded Inventory
        /// </summary>
        /// <param name="inv">Inventory to display</param>
        /// <param name="option">How to sort the Items in the Inventory before viewing</param>
        internal static void DisplayInventory(Inventory inv, SortOption option)
        {
            TableHeader();
            int counter = 0;
            foreach (Item item in inv.GetSortedProducts(option))
            {
                DisplayRow(item, counter++);
            }
        }

        /// <summary>
        /// Prints relevant public Inventory statistics to the screen
        /// </summary>
        /// <param name="inv">Currently loaded inventory</param>
        internal static void InventoryStatistics(Inventory inv)
        {
            //Console.Clear();
            Format.Version();

            Console.WriteLine("Total number of unique products: {0}", inv.TotalProducts);
            Console.WriteLine("Total number of products in stock: {0}", inv.ItemsInStock);
            Console.WriteLine("Total Wholesale-USD of Inventory: {0}", inv.TotalWholesalePrice);
            Console.WriteLine("Total Retail-USD of Inventory: {0}", inv.TotalRetailPrice);

            Format.Separator();
        }

    }
}
