using System;
using System.ComponentModel;
using System.Collections.Generic;
using Epic.Training.Project.SuppliedCode.Exceptions;
using Epic.Training.Project.SuppliedCode.Collections;
using Epic.Training.Project.SuppliedCode.Interfaces;
using Epic.Training.Project.Inventory.EventArgs;
using System.Runtime.Serialization;

namespace Epic.Training.Project.Inventory
{
    /// <summary>
    /// Just something I'm playing around with for the SortedOrder methods.
    /// </summary>
    public enum SortOption { ByName, ByAdded };

    /// <summary>
    /// The inventory class for the C# project.  
    /// </summary>
    [Serializable]
    public class Inventory : IProductList<Item>
    {
        #region INVENTORY - PRIVATE BACKING COLLECTIONS; _inventory; _index 

        /// <summary>
        /// Primary private backing collection. Maintains reversed order of items added to the Inventory.
        /// </summary>
        private InstrumentedList<Item> _inventory = new InstrumentedList<Item>();

        /// <summary>
        /// Secondary private backing collection. Used for decreased lookup times of Items in Inventory instance.
        /// </summary>
        private InstrumentedDictionary<string, Item> _index = new InstrumentedDictionary<string, Item>();

        #endregion

        #region INVENTORY STATISTICS - ItemsInStock; TotalProducts;TotalRetailPrice;TotalWholesalePrice

        private int _itemsInStock;
        /// <summary>
        /// [Int] Total number of non-unique items in Inventory - A sum of all Item objects' QuantityOnHand property
        /// </summary>
        public int ItemsInStock
        {
            get { return _itemsInStock; }
        }

        /// <summary>
        /// [Int] Total number of unique items in Inventory
        /// </summary>
        public int TotalProducts
        {
            get { return _index.Keys.Count; }
        }

        private decimal _totalRetail;
        /// <summary>
        /// [Decimal] Retail Price (USD) of the entire Inventory - A sum of all Item objects' RetailPrice * QuantityOnHand
        /// </summary>
        public decimal TotalRetailPrice
        {
            get { return _totalRetail; }
        }

        private decimal _totalWholesale;
        /// <summary>
        /// [Decimal] Wholesale Price (USD) of the entire Inventory - A sum of all Item objects' WholesalePrice * QuantityOnHand
        /// </summary>
        public decimal TotalWholesalePrice
        {
            get { return _totalWholesale; }
        }

        #endregion

        #region CONSTRUCTORS
        /// <summary>
        /// Simple constructor for Inventory class. Fulfills empty constructor requirement for serialization
        /// </summary>
        /// <remarks>
        /// At the moment, there is no need for any other constructor. Would eventually like to add one that takes another Inventory as an argument and either merges or clones.
        /// </remarks>
        public Inventory()
        { }

        #endregion

        #region Methods to subscribe to Item PropertyChanged and PropertyChanging event handlers

        /// <summary>
        /// [Inventory Method:Add] subscribes this to the PropertyChanging event handler of the added Item object.
        /// Used to prepare changes to an Inventory's statistics based off individual Item objects' changes.
        /// </summary>
        /// <param name="sender">Specific Item object that raised its PropertyChanging event.</param>
        /// <param name="e">Used to determine which property of Item is changing.</param>
        private void ChangingTotals(object sender, PropertyChangingEventArgs e)
        {
            Item item = sender as Item;
            ExtendedChangingEventArgs eventArgs = e as ExtendedChangingEventArgs;
            //I can't just use 'ExtendedChangingEventArgs' as a subscriber parameter, because
            //it doesn't match the parameter signature of the PropertyChanging delegate...
            //but I know that it is what is being passed into PropertyChanging in 'Item' class.

            switch (eventArgs.PropertyName)
            {
                case "QuantityOnHand":
                    item.OldQuantity = item.QuantityOnHand;
                    break;
                case "WholesalePrice":
                    item.OldWholesale = item.WholesalePrice;
                    break;
                case "Name":
                    if (this.Contains(eventArgs.ProposedName))
                    {
                        throw new DuplicateProductNameException(eventArgs.ProposedName);
                    }
                    else //prepare for name change and adjusting _index
                    {
                        item.OldName = item.Name;
                    }
                    
                    break;
                case "Weight":
                    item.OldWeight = item.Weight;
                    break;
            }
        }

        /// <summary>
        /// [Inventory Method:Add] Subscribes this to the PropertyChanged event handler of the added Item object.
        /// Used to reflect finalized changes to an Inventory's statistics based off individual Item objects' changes. 
        /// </summary>
        /// <param name="sender">Specific Item object that raised its PropertyChanged event.</param>
        /// <param name="e">Used to determine which property of Item has changed.</param>
        private void ChangedTotals(object sender, PropertyChangedEventArgs e)
        {
            Item item = (Item)sender;
           
            switch (e.PropertyName)
            {
                case "QuantityOnHand":
                    int diffQuant = item.QuantityOnHand - item.OldQuantity;

                    this._itemsInStock += diffQuant;
                    this._totalWholesale += diffQuant * item.WholesalePrice;
                    this._totalRetail += diffQuant * item.RetailPrice;
                   
                    break;
                case "WholesalePrice":
                    decimal diffPrice = item.WholesalePrice - item.OldWholesale;

                    this._totalWholesale += diffPrice * item.QuantityOnHand;
                    this._totalRetail += Item.MarkupFactor * (diffPrice * item.QuantityOnHand);

                    break;
                case "Weight":
                    double diffWeight = item.Weight - item.OldWeight;

                    this._totalRetail += Item.ShippingFactor * ((decimal)diffWeight * item.QuantityOnHand);

                    break;
                case "Name":
                    //Because ChangingTotals is responsible for throwing the exception,
                    //if we get to this point, the name has already been changed. 
                    _index.Remove(item.OldName);
                    _index.Add(item.Name, item);

                    break;
            }
            
        }

        #endregion

        #region (DE)SERIALIZATION PREPARATION - OnDeserialized; OnSerializing; PruneInventory

        [OnDeserialized()]
        private void OnDeserialized(StreamingContext context)
        {
            _index.Clear();
			//foreach (string key in _index.Keys)
			//{
			//	Console.WriteLine("{0}, {1}", key, _index[key] == null ? "null" : "not null");
			//}
            foreach (Item item in _inventory)
            {
				try
				{	
					_index.Add(item.Name, item);
					item.PropertyChanging += ChangingTotals;
					item.PropertyChanged += ChangedTotals;
				}
				catch (ArgumentException e)
				{
					Console.WriteLine("Nope {0} : {1}", item.Name, _index[item.Name] == null ? "null" : "not null");
					System.Threading.Thread.Sleep(1000);
				}
                
            }
			
        }

        /// <summary>
        /// Forcibly ensures integrity between an Inventory's private backing collections
        /// </summary>
        private void PruneInventory() //I had trouble implementing this logic directly in the OnSerialized method.
        {
            for (int i = this._inventory.Count - 1; i >= 0; i--)
            {
                if (!this.Contains(this._inventory[i]))
                {
                    this._inventory.RemoveAt(i);
                }
            }
        }

        [OnSerializing()]
        private void OnSerializing(StreamingContext context)
        {
            this.PruneInventory(); //
        }

        #endregion

        #region SORTED INVENTORY VIEWS - GetSortedProductsByName; GetSortedProductsByAdded; GetSortedProducts

        /// <summary>
        /// Returns a generic 'Item'-IEnumerable of Item objects contained in Inventory sorted alphanumerically by Name.
        /// Private collection _index is an InstrumentedDictionary, which already contains values in sorted order by keys. 
        /// </summary>
        /// <remarks>
        /// Would have liked to make this private, since I liked being able to access different sorted views through GetSortedProducts(SortOption option).
        /// </remarks>
        /// <returns>IEnumerable of type Item</returns>
        public IEnumerable<Item> GetSortedProductsByName()
        {
            foreach (Item item in this._index.Values)
            {
                yield return item;
            }
        }

        /// <summary>
        /// Returns a generic 'Item'-IEnumerable of Item objects contained in Inventory sorted by order added.
        /// Private collection _inventory is an InstrumentedList, which already preserves the order of items by order added.
        /// </summary>
        /// <returns>IEnumerable of type Item</returns>
        private IEnumerable<Item> GetSortedProductsByAdded()
        {
            //foreach (Item item in this._inventory)
            //{
            //    if (_index.ContainsKey(item.Name))
            //        yield return item;
            //} This had weird bugs for some reason. Figured it was easier to just prune and return _inventory since it implements IEnumerable. 
            // The commented segment was already iterating through the whole instrumented list, so there shouldn't be a large difference in performance. 
            this.PruneInventory();
            return _inventory;
        }

        /// <summary>
        /// Returns a generic 'Item'-IEnumerable of Item objects contained in Inventory sorted by the enum SortOption.
        /// Private collection _index is an InstrumentedDictionary, which contains values in sorted order by keys.
        /// </summary>
        /// <param name="option">Enum SortOption - really just wanted to play with enums</param>
        /// <returns>IEnumerable of type Item</returns>
        public IEnumerable<Item> GetSortedProducts(SortOption option)
        {
            PruneInventory(); //Necessary here as well as before serializing an inventory. What happens when someone updates _index and then views _inventory in the same session?
            switch (option)
            {
                case SortOption.ByName:
                    return GetSortedProductsByName();
                case SortOption.ByAdded:
                    return GetSortedProductsByAdded();
                default:
                    return null;
            }
        }

        #endregion

        #region IPRODUCTLIST<ITEM> INTERFACE METHODS

        /// <summary>
        /// Adds Item 'toAdd' to Inventory instance and updates Inventory statistics.
        /// Inventory methods 'ChangingTotals' and 'ChangedTotals' are subscribed to the added Item's event handlers.
        /// Throws 'DuplicateProductNameException' if a product with the same 'Name' already exists in Inventory instance.
        /// </summary>
        /// <param name="toAdd">Item to add to Inventory instance</param>
        public void Add(Item toAdd)
        {
            if (this.Contains(toAdd))
            {
                throw new DuplicateProductNameException(toAdd.Name);
            }       
            else
            {
                _inventory.Add(toAdd);
                _index.Add(toAdd.Name, toAdd);

                this._itemsInStock += toAdd.QuantityOnHand;
                this._totalWholesale += (toAdd.QuantityOnHand * toAdd.WholesalePrice);
                this._totalRetail += (toAdd.QuantityOnHand * toAdd.RetailPrice);
                
                toAdd.PropertyChanging += ChangingTotals;
                toAdd.PropertyChanged += ChangedTotals;
            }
        }

        /// <summary>
        /// Removes Item 'toRemove' from Inventory instance and updates Inventory statistics.
        /// Inventory methods 'ChangingTotals' and 'ChangedTotals' are unsubscribed from the removed Item's event handlers. 
        /// Throws 'KeyNotFoundException' if 'toRemove' cannot be found in Inventory instance.
        /// </summary>
        /// <param name="toRemove">Item to remove from Inventory instance</param>
        public void Remove(Item toRemove)
        {
            if (!this.Contains(toRemove))
            {
                throw new KeyNotFoundException(toRemove.Name);
            }
            else
            {
                //_inventory.Remove(toRemove); //!!!Presence or removal seems to make Test 'RemoveProducts' unhappy!!! Responsibility moved to 'OnSerializing'
                _index.Remove(toRemove.Name); //returns false if not found

                this._itemsInStock -= toRemove.QuantityOnHand;
                this._totalWholesale -= (toRemove.QuantityOnHand * toRemove.WholesalePrice);
                this._totalRetail -= (toRemove.QuantityOnHand * toRemove.RetailPrice);

                toRemove.PropertyChanging -= ChangingTotals;
                toRemove.PropertyChanged -= ChangedTotals;
            }

            //if (_index.Remove(toRemove.Name))
            //{
            //    this._itemsInStock -= toRemove.QuantityOnHand;
            //    this._totalWholesale -= (toRemove.QuantityOnHand * toRemove.WholesalePrice);
            //    this._totalRetail -= (toRemove.QuantityOnHand * toRemove.RetailPrice);

            //    toRemove.PropertyChanging -= ChangingTotals;
            //    toRemove.PropertyChanged -= ChangedTotals;
            //}
            //else
            //{
            //    throw new KeyNotFoundException(toRemove.Name);
            //}
        }
        
        /// <summary>
        /// Method 'Contains' with an 'Item' signature.
        /// </summary>
        /// <param name="toCheck">Checks if Item 'toCheck' already exists within _index</param>
        /// <returns>boolean</returns>
        public bool Contains(Item toCheck)
        {
            return _index.ContainsKey(toCheck.Name);
        }

        /// <summary>
        /// Method 'Contains' with a 'string' signature.
        /// </summary>
        /// <param name="toCheck">Checks if string 'toCheck' already exists as a key in _index</param>
        /// <returns>boolean</returns>
        public bool Contains(string toCheck)
        {
            return _index.ContainsKey(toCheck);
        }

        /// <summary>
        /// Allows an instance of Inventory to be indexed by the name of an item. Calls private _index[string name].
        /// </summary>
        /// <param name="name">Name of item to index into</param>
        /// <returns>If found in private _index, returns an Item instance with a matching name.</returns>
        public Item this[string name]
        {
            get 
            {
                if (this.Contains(name))
                {
                    return _index[name];
                }
                else
                {
                    throw new KeyNotFoundException();
                }
            }
        }

        /// <summary>
        /// Allows an instance of Inventory to be manipulated in a for/foreach loop
        /// </summary>
        /// <returns>Returns an IEnumerator of type Item</returns>
        public IEnumerator<Item> GetEnumerator()
        {
            return _inventory.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _inventory.GetEnumerator();
        }

        #endregion
    }
}
