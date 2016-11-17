using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Epic.Training.Project.SuppliedCode.Exceptions;
using Epic.Training.Project.SuppliedCode.Instrumentation;
using Epic.Training.Project.SuppliedCode.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using etpi = Epic.Training.Project.Inventory;

namespace Epic.Training.Project.UnitTest
{

	/// <summary>
	/// All inventory-specific tests
	/// </summary>
	[TestClass]
	public class InventoryTests
	{
		#region Helper members
		/// <summary>
		/// The default constructor of the Inventory class
		/// </summary>
		private static readonly ConstructorInfo s_defaultInventoryConstructor = TestUtilities.GetDefaultConstructor(typeof(etpi.Inventory));

		/// <summary>
		/// Creates an inventory using hte default constructor.
		/// </summary>
		/// <returns>The newly created inventory</returns>
		internal static etpi.Inventory CreateDefaultInventory()
		{
			if (s_defaultInventoryConstructor != null)
			{
				return (etpi.Inventory)s_defaultInventoryConstructor.Invoke(new object[] { });
			}

			Assert.Fail("Inventory must have a default constructor to run the inventory tests");
			return null;
		}

		/// <summary>
		/// Verify that the Inventory properties match a given set of expected values
		/// </summary>
		/// <param name="inventory">The inventory to test</param>
		/// <param name="totalProducts">The expected number of different products</param>
		/// <param name="itemsInStock">The expected items in stock</param>
		/// <param name="totalWholesale">The expected TotalWholesalePrice</param>
		/// <param name="totalRetail">The expected TotalRetailPrice</param>
		private static void TestProductList(etpi.Inventory inventory, int totalProducts, int itemsInStock, decimal totalWholesale, decimal totalRetail)
		{
			Assert.IsTrue(inventory.TotalProducts == totalProducts, "TotalProducts is {0} but it should be {1}", inventory.TotalProducts, totalProducts);
			Assert.IsTrue(inventory.ItemsInStock == itemsInStock, "ItemsInStock is {0} but it shold be {1}", inventory.ItemsInStock, itemsInStock);
			Assert.IsTrue(inventory.TotalWholesalePrice == totalWholesale, "TotalWholesalePrice is {0} but it should be {1}", inventory.TotalWholesalePrice, totalWholesale);
			Assert.IsTrue(inventory.TotalRetailPrice == totalRetail, "TotalRetailPrice is {0} but it should be {1}", inventory.TotalRetailPrice, totalRetail);
		}

		/// <summary>
		/// Verify that items in the given order match the expected order
		/// </summary>
		/// <param name="errorMessage">Error message to display if the order doesn't match.  Include the tokens {0}=index, {1}=given item name, {2}=expected item name</param>
		/// <param name="givenOrder">Order of the items in the inventory</param>
		/// <param name="expectedOrder">Expected order of items in the inventory</param>
		private static void TestOrder(string errorMessage, IEnumerable<etpi.Item> givenOrder, params etpi.Item[] expectedOrder)
		{
			int index = 0;
			foreach (etpi.Item givenItem in givenOrder)
			{
				etpi.Item expectedItem = expectedOrder[index++];
				Assert.IsTrue(object.Equals(givenItem, expectedItem), errorMessage, index, givenItem.Name, expectedItem.Name);
			}
		}

		/// <summary>
		/// Ensures that property values before and after serialization of an Inventory and all containing items are the same.  Also ensures that the inventory properly 
		/// subscribes to the PropertyChanging and PropertyChanged events of each item.
		/// </summary>
		/// <param name="stream">The memory stream to use during the serialization test</param>
		/// <param name="inventoryBefore">The inventory before serialization</param>
		/// <param name="inventoryAfter">The inventory after serialization</param>
		private static void TestInventorySerialization(MemoryStream stream, etpi.Inventory inventoryBefore, etpi.Inventory inventoryAfter)
		{
			bool doTotalsMatch = true;
			var itemsBefore = new List<etpi.Item>(inventoryBefore.GetSortedProductsByName());
			var itemsAfter = new List<etpi.Item>(inventoryAfter.GetSortedProductsByName());

			string message = "The inventory property {0} does not match before and after serialization. Before: {1}, After: {2}";
			doTotalsMatch = TestUtilities.CompareProperty<etpi.Inventory>(inventoryBefore, inventoryAfter, "TotalProducts", message);
			TestUtilities.CompareProperty<etpi.Inventory>(inventoryBefore, inventoryAfter, "TotalWholesalePrice", message);
			TestUtilities.CompareProperty<etpi.Inventory>(inventoryBefore, inventoryAfter, "TotalRetailPrice", message);
			TestUtilities.CompareProperty<etpi.Inventory>(inventoryBefore, inventoryAfter, "ItemsInStock", message);

			bool nullItemsAfter = false;
			if (doTotalsMatch)
			{
				for (int i = 0; i < itemsBefore.Count; i++)
				{
					etpi.Item itemBefore = itemsBefore[i];
					etpi.Item itemAfter = itemsAfter[i];

					if (itemAfter == null)
					{
						nullItemsAfter = true;
					}

					message = "The {0} of item " + i + " doesn't match before and after serializing the inventory. Before: {1}, After: {2}";
					TestUtilities.CompareProperty<etpi.Item>(itemBefore, itemAfter, "Name", message);
					TestUtilities.CompareProperty<etpi.Item>(itemBefore, itemAfter, "QuantityOnHand", message);
					TestUtilities.CompareProperty<etpi.Item>(itemBefore, itemAfter, "WholesalePrice", message);
					TestUtilities.CompareProperty<etpi.Item>(itemBefore, itemAfter, "RetailPrice", message);
					TestUtilities.CompareProperty<etpi.Item>(itemBefore, itemAfter, "ShippingCost", message);
				}
			}

			Assert.IsFalse(nullItemsAfter, "After deserializing your inventory, one or more of the items in the deserialized inventory is null.");

			foreach (etpi.Item item in (IEnumerable<etpi.Item>) inventoryBefore)
			{
				item.QuantityOnHand += 1;
				item.WholesalePrice += 1;
				item.Weight += 1;
			}

			message = "After altering items in the original inventory, {0} for the inventories still match (Original: {1}, Deserialized: {2}).  Did you unhook events from the original inventory when it was serialized?";
			TestUtilities.CompareProperty<etpi.Inventory>(inventoryBefore, inventoryAfter, "TotalWholesalePrice", message, false);
			TestUtilities.CompareProperty<etpi.Inventory>(inventoryBefore, inventoryAfter, "TotalRetailPrice", message, false);
			TestUtilities.CompareProperty<etpi.Inventory>(inventoryBefore, inventoryAfter, "ItemsInStock", message, false);

			foreach (etpi.Item t in (IEnumerable<etpi.Item>) inventoryAfter)
			{
				t.QuantityOnHand += 1;
				t.WholesalePrice += 1;
				t.Weight += 1;
			}

			message = "After altering items in the original and deserialized inventory, {0} for the inventories no longer matches.  Before: {1}, After: {2}.  Did you forget to hook events back up after deserializing?";
			TestUtilities.CompareProperty<etpi.Inventory>(inventoryBefore, inventoryAfter, "TotalWholesalePrice", message);
			TestUtilities.CompareProperty<etpi.Inventory>(inventoryBefore, inventoryAfter, "TotalRetailPrice", message);
			TestUtilities.CompareProperty<etpi.Inventory>(inventoryBefore, inventoryAfter, "ItemsInStock", message);

		}

		private void TestNoInventoryChanges(etpi.Inventory inv, etpi.Item item)
		{
			try { item.Name = "B"; }
			catch { }


			etpi.Item testItem = null;
			try { testItem = inv["A"]; }
			catch { }

			Assert.IsTrue(testItem == item, "Item should still be indexed under \"A\" if the name change is canceled");


			int oldItemsInStock = inv.ItemsInStock;
			decimal oldTotalWholesalePrice = inv.TotalWholesalePrice;
			decimal oldTotalRetailPrice = inv.TotalRetailPrice;

			try { item.QuantityOnHand = 2; }
			catch { }
			Assert.IsTrue(inv.ItemsInStock == oldItemsInStock, "Since the QuantityOnHand change was canceled, ItemsInStock should not have changed");
			Assert.IsTrue(inv.TotalWholesalePrice == oldTotalWholesalePrice, "Since the QuantityOnHand change was canceled, TotalWholesalePrice should not have changed");
			Assert.IsTrue(inv.TotalRetailPrice == oldTotalRetailPrice, "Since the QuantityOnHand change was canceled, TotalRetailPrice should not have changed");

			try { item.WholesalePrice = 2; }
			catch { }

			Assert.IsTrue(inv.TotalWholesalePrice == oldTotalWholesalePrice, "Since the WholesalePrice change was canceled, TotalWholesalePrice should not have changed");
			Assert.IsTrue(inv.TotalRetailPrice == oldTotalRetailPrice, "Since the WholesalePrice change was canceled, TotalRetailPrice should not have changed");

			try { item.Weight = 2; }
			catch { }

			Assert.IsTrue(inv.TotalRetailPrice == oldTotalRetailPrice, "Since the Weight change was canceled, TotalRetailPrice should not have changed");
		}
		#endregion

		#region Tests
		/// <summary>
		/// Verify that Inventory has a default constructor
		/// </summary>
		[TestMethod]
		public void TestConstructor()
		{
			etpi.Inventory inventory = CreateDefaultInventory();
		}

		/// <summary>
		/// Ensure that the calculated totals are correct 
		/// </summary>
		[TestMethod]
		public void TestTotals()
		{
			etpi.Item p1 = ItemTests.CreateProduct("A", 0.05, 5, 5.95m);
			etpi.Item p2 = ItemTests.CreateProduct("B", 1.5, 10, 10.99m);

			etpi.Inventory inventory = CreateDefaultInventory();

			inventory.Add(p2);
			TestProductList(inventory, 1, 10, 109.9m, 235.58m);

			inventory.Add(p1);
			TestProductList(inventory, 2, 15, 139.65m, 286.9675m);

			p1.QuantityOnHand = 1;
			p1.WholesalePrice = 1;
			TestProductList(inventory, 2, 11, 110.9m, 237.4425m);

			p1.Weight = 1;
			TestProductList(inventory, 2, 11, 110.9m, 240.53m);
		}

		/// <summary>
		/// Verify that GetSortedProductsByName doesn't directly expose a private field of the inventory
		/// </summary>
		[TestMethod]
		public void TestInventoryEncapsulation()
		{
			etpi.Inventory inventory = CreateDefaultInventory();
			etpi.Item p1 = ItemTests.CreateProduct("A", 0.05, 1, 1m);
			etpi.Item p2 = ItemTests.CreateProduct("B", 1, 10, 10.99m);
			inventory.Add(p1);
			inventory.Add(p2);

			ICollection<etpi.Item> castTest = inventory.GetSortedProductsByName() as ICollection<etpi.Item>;

			if (castTest != null)
			{
				if (!castTest.IsReadOnly)
				{
					castTest.Clear();
				}
			}

			string errorMessage = "There was an encapsulation problem with your inventory.  Ensure GetSortedProductsByName doesn't expose an internal collection.";
			Assert.IsTrue(inventory.Contains(p1), errorMessage);
			Assert.IsTrue(inventory.Contains(p2), errorMessage);
			Assert.IsTrue(inventory.TotalProducts == 2, errorMessage);
		}

		/// <summary>
		/// Verifies that records can be accessed from an inventory in order added as well as in name order
		/// </summary>
		[TestMethod]
		public void TestProductSortOrder()
		{
			etpi.Inventory inventory = CreateDefaultInventory();
			etpi.Item p1 = ItemTests.CreateProduct("A", 0.05, 1, 1m);
			etpi.Item p2 = ItemTests.CreateProduct("B", 1, 10, 10.99m);

			inventory.Add(p2);
			inventory.Add(p1);

			TestOrder("The items should be in the order entered when iterating through the inventory using forech.  The item at position {0} should be {2}, but it is {1}", inventory, p2, p1);
			TestOrder("The items should be in name order when returned by GetSortedProductsByName().  The item at position {0} should be {2}, but it is {1}", inventory.GetSortedProductsByName(), p1, p2);
		}

		/// <summary>
		/// Verifies that the inventory is properly notified whenever an item's name changes
		/// </summary>
		[TestMethod]
		public void TestRenamingItem()
		{
			etpi.Inventory inventory = CreateDefaultInventory();
			etpi.Item p1 = ItemTests.CreateProduct("A", 0.05, 1, 1m);
			etpi.Item p2 = ItemTests.CreateProduct("B", 1, 10, 10.99m);

			inventory.Add(p2);
			inventory.Add(p1);

			p1.Name = "C";
			bool hasItemNamedC = true;
			IProduct tmp = null;

			try
			{
				tmp = inventory["C"];
			}
			catch
			{
				hasItemNamedC = false;
			}

			Assert.IsTrue(p1.Equals(tmp) && hasItemNamedC, "The inventory was not informed when the name of an item it contains was cahnged.");
			TestOrder("The item order returned by GetSortedProductsByName() is not correct after an item name change.  The item at position {0} should be {2}, but it is {1}", inventory.GetSortedProductsByName(), p2, p1);
		}

		/// <summary>
		/// Ensure that products with duplicate names are not permitted.
		/// </summary>
		[TestMethod]
		public void TestDuplicateProductNames()
		{
			etpi.Inventory inventory = CreateDefaultInventory();
			etpi.Item p1 = ItemTests.CreateProduct("C", 0.05, 1, 1m);
			etpi.Item p2 = ItemTests.CreateProduct("B", 1, 10, 10.99m);

			inventory.Add(p2);
			inventory.Add(p1);

			bool thrown = false;
			try
			{
				inventory.Add(p1);
			}
			catch (DuplicateProductNameException)
			{
				thrown = true;
			}
			catch
			{
				Assert.Fail("A System.Exception was thrown.  You should throw an Epic.Training.Project.SuppliedCode.Collections.DuplicateProductNameException instead");
				thrown = true;
			}

			Assert.IsTrue(thrown, "The collection allowed a product with a duplciate name to be added.  You should throw an Epic.Training.Project.SuppliedCode.Exceptions.DuplicateProductNameException");

			thrown = false;

			try
			{
				p1.Name = "B";
			}
			catch (DuplicateProductNameException)
			{
				thrown = true;
			}
			catch
			{
				Assert.Fail("A System.Exception was thrown.  You should throw an Epic.Training.Project.SuppliedCode.Collections.DuplicateProductNameException instead");
				thrown = true;
			}

			Assert.IsTrue(thrown, "The collection permitted an item name change that resulted in a duplicate name.  You should throw an Epic.Training.Project.SuppliedCode.Collections.DuplicateProductNameException");

			Assert.IsFalse(p1.Name == "B", "You threw a DuplicateProductNameException, but the name of p1 was still changed to \"B\".  You should prevent the name change from happening by throwing the exception in PropertyChanging, not in PropertyChanged");
		}

		/// <summary>
		/// Ensures that Inventory serialization functions as expected
		/// </summary>
		[TestMethod]
		public void TestSerialization()
		{
			etpi.Inventory inventory = CreateDefaultInventory();
			etpi.Item p1 = ItemTests.CreateProduct("C", 0.05, 1, 1m);
			etpi.Item p2 = ItemTests.CreateProduct("B", 1, 10, 10.99m);

			inventory.Add(p2);
			inventory.Add(p1);

			TestUtilities.TestSerialization<etpi.Inventory>(inventory, TestInventorySerialization);
		}

		/// <summary>
		/// Verify that item removal works as expected
		/// </summary>
		[TestMethod]
		public void TestRemoveItem()
		{
			etpi.Inventory inventory = CreateDefaultInventory();
			etpi.Item p1 = ItemTests.CreateProduct("C", 0.05, 1, 1m);
			etpi.Item p2 = ItemTests.CreateProduct("B", 1, 10, 10.99m);

			inventory.Add(p2);
			inventory.Add(p1);

			inventory.Remove(p2);
			try
			{
				p2.Name = p1.Name;
			}
			catch
			{
				Assert.Fail("An error was thrown changing the name of p2 to match p1 after p2 was removed from the inventory.  Did you remember to unsubscribe the inventory from the change events of an item when the item is removed?");
			}

			decimal totalWholesalePrice = inventory.TotalWholesalePrice;
			decimal totalRetailPrice = inventory.TotalRetailPrice;
			int totalProducts = inventory.TotalProducts;

			p2.Weight += 1;
			p2.WholesalePrice += 1;
			p2.QuantityOnHand += 1;

			Assert.IsFalse(
				totalWholesalePrice != inventory.TotalWholesalePrice || totalRetailPrice != inventory.TotalRetailPrice || totalProducts != inventory.TotalProducts,
				"Changing properties of p2 after it was removed from the inventory altered the inventory's totals.  Did you remember to unsubscribe the inventory from the change events of an item when the item is removed?");

		}

		/// <summary>
		/// Verfiies that inventory state is not altered if an exception is thrown by one of the delegates attached to 
		/// PropertyChanging.
		/// </summary>
		[TestMethod]
		public void TestChangingVsChanged()
		{
			//Inventory events come first
			etpi.Inventory earlyInv = CreateDefaultInventory();
			etpi.Item earlyItem = ItemTests.CreateProduct("A", 1, 1, 1m);

			earlyInv.Add(earlyItem);

			earlyItem.PropertyChanging +=
				delegate(object sender, System.ComponentModel.PropertyChangingEventArgs args)
				{
					throw new Exception("Cancel all changes");
				};

			TestNoInventoryChanges(earlyInv, earlyItem);


			//Inventory events come second (make sure events are order dependent)
			etpi.Inventory lateInv = CreateDefaultInventory();
			etpi.Item lateItem = ItemTests.CreateProduct("A", 1, 1, 1m);

			lateItem.PropertyChanging +=
				delegate(object sender, System.ComponentModel.PropertyChangingEventArgs args)
				{
					throw new Exception("Cancel all changes");
				};

			lateInv.Add(lateItem);

			TestNoInventoryChanges(lateInv, lateItem);
		}
		#endregion
	}
}
