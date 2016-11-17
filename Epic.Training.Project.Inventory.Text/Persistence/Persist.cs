using System;
using System.IO;
using System.Windows.Forms;

namespace Epic.Training.Project.Inventory.Text.Persistence
{
    class Persist
    {
        private static string defaultBinDir = String.Format(@"{0}\{1}", Environment.GetEnvironmentVariable(Resource1.DefaultBinDirEnv), Resource1.DefaultBinDir);

        internal static void createPersistentDir()
        {
            if (!Directory.Exists(defaultBinDir))
            {
                Directory.CreateDirectory(defaultBinDir);
            }
        }

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

        internal static void SaveInventory(Inventory inv, ref string currentName)
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
            catch (Exception ex)
            {
                throw ex; //Explicit reminder to let InventoryMenu deal with this.
            }
            //catch (ArgumentNullException)
            //{
            //    Console.WriteLine("\n[! No valid file selection was made. Returning to Inventory Menu !]\n");
            //}
            //catch (System.Runtime.Serialization.SerializationException ex)
            //{
            //    Console.WriteLine("\n[! IOError: {0}\n Returning to Inventory Menu !]\n", ex.Message);
            //}

        }

        /// <summary>
        /// Loads a *.bin file representing a serialized Inventory
        /// </summary>
        /// <param name="filePath">Absolute filepath of *.bin to load</param>
        /// <exception>Generic Exception thrown upon IO or Serialization exceptions</exception>
        /// <returns>Inventory object. Parameter 'filePath' is modified</returns>
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
            catch (Exception ex)
            {
                throw ex; //Explicit reminder to let MainMenu deal with this.
            }
            //catch (ArgumentNullException ex)
            //{
            //    Console.WriteLine("\n[! No valid file selection was made. Returning to Main Menu !]\n");
            //}
            //catch (System.Runtime.Serialization.SerializationException ex)
            //{
            //    Console.WriteLine("\n[! IOError: {0}\n Returning to Main Menu !]\n", ex.Message);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("\n[! Error: {0}\n Returning to Main Menu !]\n", ex.Message);
            //}

            //System.Threading.Thread.Sleep(2000);
            //return null;
        }

        #endregion

    }
}
