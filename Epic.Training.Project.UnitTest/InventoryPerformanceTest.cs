using System;
using System.Collections.Generic;
using Epic.Training.Project.SuppliedCode.Instrumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using etpi = Epic.Training.Project.Inventory;

namespace Epic.Training.Project.UnitTest
{
	[TestClass]
	public class InventoryPerformanceTest
	{
		private etpi.Inventory _inventory;
		private List<etpi.Item> _items;

		private void AddProducts()
		{
			_inventory = InventoryTests.CreateDefaultInventory();
			_items = new List<etpi.Item>();
			for (int i = 0; i < 100; i++)
			{
				int val = i + 1;
				_items.Add(ItemTests.CreateProduct("p" + val, val, val, val));
				_inventory.Add(_items[i]);
			}
		}

		[TestInitialize]
		public void Initialize()
		{
			AddProducts();
			StepTracker.ClearRegions();
		}
		[TestCleanup]
		public void Report()
		{
			StepTracker.EndRegion();
			TestUtilities.ReportRegions();
		}
		/// <summary>
		/// Measures how many steps it takes to add items to the inventory
		/// </summary>
		[TestMethod]
		public void AddItems()
		{
			StepTracker.StartRegion("Adding 100 products to inventory", 300);
			AddProducts();
		}

		/// <summary>
		/// Measures how many steps it takes to look an item up by name
		/// </summary>
		[TestMethod]
		public void ItemLookup()
		{
			StepTracker.StartRegion("Looking up a product by name", 2);
			etpi.Item p = _inventory["p30"];
		}

		/// <summary>
		/// Measures how many steps it takes to iterate over item names
		/// </summary>
		[TestMethod]
		public void IterateByName()
		{
			StepTracker.StartRegion("Iterating by name", 100);
			foreach (etpi.Item product in _inventory.GetSortedProductsByName()) { }
		}

		/// <summary>
		/// Measures how many steps it takes to iterate by the order items were added to the inventory
		/// </summary>
		[TestMethod]
		public void IterateByOrderEntered()
		{
			StepTracker.StartRegion("Iterating over order enterd", 100);
			foreach (etpi.Item product in (IEnumerable<etpi.Item>) _inventory) { }
		}

		/// <summary>
		/// Measures how many steps it takes to remove a product from the inventory
		/// </summary>
		[TestMethod]
		public void RemoveProducts()
		{
			StepTracker.StartRegion("Removing products", 4);
			_inventory.Remove(_items[50]);
		}

		[TestMethod]
		public void GetTotalProducts()
		{
			StepTracker.StartRegion("Getting TotalProducts", 0);
			object value = _inventory.TotalProducts;
		}

		[TestMethod]
		public void GetTotalRetailPrice()
		{
			StepTracker.StartRegion("Getting TotalRetailPrice", 0);
			object value = _inventory.TotalRetailPrice;
		}

		[TestMethod]
		public void GetTotalWholesalePrice()
		{
			StepTracker.StartRegion("Getting TotalWholesalePrice", 0);
			object value = _inventory.TotalWholesalePrice;
		}

		[TestMethod]
		public void GetItemsInStock()
		{
			StepTracker.StartRegion("Getting ItemsInStock", 0);
			object value = _inventory.ItemsInStock;
		}

		[TestMethod]
		public void ChangeItemName()
		{
			StepTracker.StartRegion("Changing Name of an item", 4);
			_items[0].Name = "The new name";
		}

		[TestMethod]
		public void ChangeQuantityOnHand()
		{
			StepTracker.StartRegion("Changing QuantityOnHand", 0);
			_items[0].QuantityOnHand = 10;
		}

		[TestMethod]
		public void ChangeWholesalePrice()
		{
			StepTracker.StartRegion("Changing WholesalePrice", 0);
			_items[0].WholesalePrice = 10m;
		}

		[TestMethod]
		public void ChangeWeight()
		{
			StepTracker.StartRegion("Changing Weight", 0);
			_items[0].Weight = 10.0;
		}

	}
}
