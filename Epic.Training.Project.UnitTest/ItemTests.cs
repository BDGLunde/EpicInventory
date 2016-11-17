using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using Epic.Training.Project.Inventory;
using Epic.Training.Project.SuppliedCode.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Epic.Training.Project.UnitTest
{
	/// <summary>
	/// All item-specific tests
	/// </summary>
	[TestClass]
	public class ItemTests
	{
		#region Helper/support members
		/// <summary>
		/// Pulls a property name from the text of an exception.
		/// 
		/// Property accessors follow the pattern: 
		///     Void set_PropertyName(PropertyDataType)
		///     or
		///     PropertyDataType get_PropertyName(Void)
		/// </summary>
		/// <param name="e">The exception</param>
		/// <param name="accessorType">The type of accessor.  "get" or "set"</param>
		/// <param name="propertyName">The name of hte property</param>
		private static void ExtractPropertyFromException(Exception e, out string accessorType, out string propertyName)
		{
			string propertyMethodName = e.TargetSite.ToString();
			string[] propertyMethodNameParts = propertyMethodName.Split('_');
			accessorType = propertyMethodNameParts[0].Split(' ')[1];
			propertyName = propertyMethodNameParts[1].Split('(')[0];
		}

		/// <summary>
		/// The default constructor of the item class
		/// </summary>
		private static ConstructorInfo s_defaultConstructor = TestUtilities.GetDefaultConstructor(typeof(Item));

		/// <summary>
		/// Use to create an Item and initialize values
		/// </summary>
		/// <param name="name">Name of the product</param>
		/// <param name="weight">Weight of the product</param>
		/// <param name="quatnityOnHand">Number of products on hand</param>
		/// <param name="wholesale">The wholesale price</param>
		/// <param name="defaultConstructor">The default co</param>
		/// <returns>A new item</returns>
		internal static Item CreateProduct(string name, double weight, int quatnityOnHand, decimal wholesale)
		{
			Item item = null;
			try
			{
				item = (Item)s_defaultConstructor.Invoke(new object[] { });
			}
			catch
			{
				Assert.Fail("Could not call default constructor of Item");
			}
			if (item != null)
			{
				try
				{
					item.Name = name;
					item.Weight = weight;
					item.QuantityOnHand = quatnityOnHand;
					item.WholesalePrice = wholesale;
				}
				catch (Exception e)
				{
					string accessorType;
					string propertyName;
					ExtractPropertyFromException(e, out accessorType, out propertyName);
					Assert.Fail("There was a problem calling the {0} accessor from property: {1}: 2", accessorType, propertyName, e.Message);
				}
			}
			return item;
		}

		/// <summary>
		/// Runs all event tests on one particular property of item
		/// </summary>
		/// <typeparam name="T">The data type of hte property</typeparam>
		/// <param name="product">The product used for testing</param>
		/// <param name="newValue">The new value to assign to the property</param>
		/// <param name="mainPropertyName">The name of the main/root property</param>
		/// <param name="otherPropertyNames">Names of any dependent properties that change when mainProperty changes</param>
		private void RunPropertyEventTests<T>(IProduct product, T newValue, string mainPropertyName, params string[] otherPropertyNames)
		{
			Type t = product.GetType();
			HashSet<string> propertiesToTest = new HashSet<string>();
			propertiesToTest.Add(mainPropertyName);
			if (otherPropertyNames != null)
			{
				foreach (string propName in otherPropertyNames)
				{
					if (!propertiesToTest.Contains(propName))
					{
						propertiesToTest.Add(propName);
					}
				}
			}

			PropertyInfo p = t.GetProperty(mainPropertyName);
			if (p != null)
			{
				PropertyChangingEventHandler changingDelegate = delegate(object sender, PropertyChangingEventArgs e)
				{
					Assert.IsFalse(e.PropertyName == mainPropertyName, "Don't raise PropertyChanging event if new {0} = old {0}", e.PropertyName);
				};

				PropertyChangedEventHandler changedDelegate = delegate(object sender, PropertyChangedEventArgs e)
				{
					Assert.IsFalse(e.PropertyName == mainPropertyName, "Don't raise PropertyChanged event if new {0} = old {0}", e.PropertyName);
				};

				product.PropertyChanged += changedDelegate;
				product.PropertyChanging += changingDelegate;

				// Test setting to the old value
				T oldValue = (T)p.GetGetMethod().Invoke(product, new object[] { });


				TestUtilities.InvokePropertySetter(p, product, oldValue);

				product.PropertyChanged -= changedDelegate;
				product.PropertyChanging -= changingDelegate;

				HashSet<string> changing = new HashSet<string>();
				HashSet<string> changed = new HashSet<string>();

				changingDelegate = delegate(object sender, PropertyChangingEventArgs e)
				{
					Assert.IsTrue(sender == product, "Sender is not correct in PropertyChangingEvent");

					object currentValue = p.GetGetMethod().Invoke(product, new object[] { });
					Assert.IsFalse(e.PropertyName == mainPropertyName && !object.Equals(currentValue, oldValue),
						"PropertyChainging should be called BEFORE changing the backing field of {0}", e.PropertyName);

					Assert.IsTrue(propertiesToTest.Contains(e.PropertyName), "PropertyChanging raised for {0}, an unexpected property.  Is this a misspelling?", e.PropertyName);

					Assert.IsFalse(changing.Contains(e.PropertyName), "PropertyChanging raised multiple times for {0} when changing {1}", e.PropertyName, mainPropertyName);

					Assert.IsFalse(changed.Contains(e.PropertyName), "When setting {1}, PropertyChanging({0}) should happen before PropertyChanged({0})", e.PropertyName, mainPropertyName);

					changing.Add(e.PropertyName);
				};

				changedDelegate = delegate(object sender, PropertyChangedEventArgs e)
				{
					Assert.IsTrue(sender == product, "Sender is not correct in PropertyChangingEvent");

					object currentValue = p.GetGetMethod().Invoke(product, new object[] { });
					Assert.IsFalse(e.PropertyName == mainPropertyName && !object.Equals(currentValue, newValue),
						"When the PropertyChanged event was raised, the value of the {0} property is different than what the {0} property was set to. Are you setting the backing field before raising PropertyChanged? It should be set after.", e.PropertyName);


					Assert.IsTrue(propertiesToTest.Contains(e.PropertyName), "PropertyChanged raised for {0}, an unexpected property.  Is this a misspelling?", e.PropertyName);

					Assert.IsFalse(changed.Contains(e.PropertyName), "PropertyChanged raised multiple times for {0} when changing {1}", e.PropertyName, mainPropertyName);

					changed.Add(e.PropertyName);
				};


				product.PropertyChanged += changedDelegate;
				product.PropertyChanging += changingDelegate;

				// Test setting to the new value
				TestUtilities.InvokePropertySetter(p, product, newValue);

				product.PropertyChanged -= changedDelegate;
				product.PropertyChanging -= changingDelegate;

				foreach (string propName in propertiesToTest)
				{
					Assert.IsTrue(changing.Contains(propName), "PropertyChanging should have been called for {0} when changing {1}, but it wasn't", propName, mainPropertyName);
					Assert.IsTrue(changed.Contains(propName), "PropertyChanged should have been called for {0} when changing {1}, but it wasn't", propName, mainPropertyName);
				}
			}

		}

		/// <summary>
		/// Verify that setting illegal values results in the proper exception
		/// </summary>
		/// <typeparam name="TPropType">The data type of the property to test</typeparam>
		/// <param name="product">The product used for testing</param>
		/// <param name="propertyName">Name of the property</param>
		/// <param name="illegalValue">The illegal value to set</param>
		/// <param name="errorMessage">The error message to display in the assertion</param>
		private void RunPropertyCornerCaseTest<TPropType>(IProduct product, string propertyName, TPropType illegalValue, string errorMessage)
		{
			PropertyInfo property = product.GetType().GetProperty(propertyName);

			bool threwException = false;

			try
			{
				property.GetSetMethod().Invoke(product, new object[] { illegalValue });
			}
			catch (Exception e)
			{
				Assert.IsFalse(e.InnerException.GetType().Name != typeof(ArgumentException).Name,
					"An exception was thrown, but it was not a System.ArgumentException.  Message: \"{0}\"",
					e.InnerException.Message);

				threwException = true;
			}

			Assert.IsTrue(threwException, errorMessage);
		}

		/// <summary>
		/// Verifies that the item's values match those provided
		/// </summary>
		/// <param name="product">The product to test</param>
		/// <param name="name">expected Name</param>
		/// <param name="weight">expectedWeight</param>
		/// <param name="quatnityOnHand">expected QuantityOnHand</param>
		/// <param name="wholesale">expected WholesalePrice</param>
		/// <param name="shippingCost">expected ShippingCost</param>
		/// <param name="retailPrice">expected RetailPrice</param>
		private void TestPropertyValues(IProduct product, string name, double weight, int quatnityOnHand, decimal wholesale, decimal shippingCost, decimal retailPrice)
		{
			TestUtilities.TestProperty(product, "Name", name);
			TestUtilities.TestProperty(product, "Weight", weight);
			TestUtilities.TestProperty(product, "QuantityOnHand", quatnityOnHand);
			TestUtilities.TestProperty(product, "WholesalePrice", wholesale);
			TestUtilities.TestProperty(product, "ShippingCost", shippingCost);
			TestUtilities.TestProperty(product, "RetailPrice", retailPrice);
		}

		/// <summary>
		/// Indicates the maximum expected size of an item that has been serialized
		/// </summary>
		private const long ExpectedSerializedItemSizeInBytes = 510L;

		/// <summary>
		/// Ensures that property values before and after serialization of an Item (which implements IProduct) are the same.
		/// </summary>
		/// <param name="stream">The memory stream to use during the serialization test</param>
		/// <param name="beforeSerialization">The item before serialization</param>
		/// <param name="afterSerialization">The item after serialization</param>
		private static void TestItemSerialization(MemoryStream stream, IProduct beforeSerialization, IProduct afterSerialization)
		{
			Assert.IsFalse(stream.Length > ExpectedSerializedItemSizeInBytes, "The serialized item is larger than expected.  Did you serialize the calculated fields?");
			Assert.IsTrue(afterSerialization.Name == beforeSerialization.Name, "Item Name did not deserialzie correctly");
			Assert.IsTrue(afterSerialization.QuantityOnHand == beforeSerialization.QuantityOnHand, "Item QuantityOnHand did not deserialzie correctly");
			Assert.IsTrue(afterSerialization.Weight == beforeSerialization.Weight, "Item Weight did not deserialzie correctly");
			Assert.IsTrue(afterSerialization.WholesalePrice == beforeSerialization.WholesalePrice, "Item WholesalePrice did not deserialzie correctly");
			Assert.IsTrue(afterSerialization.RetailPrice == beforeSerialization.RetailPrice, "Item RetailPrice did not deserialzie correctly");
			Assert.IsTrue(afterSerialization.ShippingCost == beforeSerialization.ShippingCost, "Item ShippingCost did not deserialzie correctly");
		}

		#endregion

		#region Tests
		/// <summary>
		/// Verify that Item has a default constructor, and that calling it doesn't result in an exception.
		/// </summary>
		[TestMethod]
		public void TestConstructors()
		{
			Assert.IsFalse(s_defaultConstructor == null, "Item must have a default constructor for the autograder tests.");
			if (s_defaultConstructor != null)
			{
				try
				{
					IProduct p1 = (IProduct)s_defaultConstructor.Invoke(new object[] { });
				}
				catch (Exception e)
				{
					Assert.Fail(string.Format("Creation of a new Item failed: {0}", e.InnerException.Message));
				}
			}
		}

		/// <summary>
		/// Verifies that item properties function as expected
		/// </summary>
		[TestMethod]
		public void TestPropertyAccessors()
		{
			string name = "A";
			double weight = 0.05;
			int quatnityOnHand = 5;
			decimal wholesale = 5.95m;
			decimal shippingCost = 0.1625m;
			decimal retailPrice = 10.2775m;

			IProduct p1 = CreateProduct(name, weight, quatnityOnHand, wholesale);
			TestPropertyValues(p1, name, weight, quatnityOnHand, wholesale, shippingCost, retailPrice);

			name = "B";
			weight = 1.5;
			quatnityOnHand = 10;
			wholesale = 10.99m;
			shippingCost = 4.875m;
			retailPrice = 23.558m;

			IProduct p2 = CreateProduct(name, weight, quatnityOnHand, wholesale);
			TestPropertyValues(p2, name, weight, quatnityOnHand, wholesale, shippingCost, retailPrice);

		}

		/// <summary>
		/// Verifies that PropertyChanging and PropertyChanged events happen when they should, do not happen when they
		/// should not, and are passed the correct information.
		/// </summary>
		[TestMethod]
		public void TestPropertyEvents()
		{
			string name = "C";
			double weight = 2;
			int quatnityOnHand = 2;
			decimal wholesale = 2m;

			IProduct product = CreateProduct(name, weight, quatnityOnHand, wholesale);

			RunPropertyEventTests<string>(product, "a", "Name");
			RunPropertyEventTests<int>(product, 3, "QuantityOnHand");
			RunPropertyEventTests<decimal>(product, 3m, "WholesalePrice", "RetailPrice");
			RunPropertyEventTests<double>(product, 3.0, "Weight", "RetailPrice", "ShippingCost");

		}

		/// <summary>
		/// Attempts to assign illegal values to properties
		/// </summary>
		[TestMethod]
		public void TestPropertyCornerCases()
		{
			string name = "C";
			double weight = 2;
			int quatnityOnHand = 2;
			decimal wholesale = 2m;

			IProduct product = CreateProduct(name, weight, quatnityOnHand, wholesale);

			RunPropertyCornerCaseTest<double>(product, "Weight", -1, "Weight cannot be < 0.  You should check for this and throw an ArgumentException in set");
			RunPropertyCornerCaseTest<double>(product, "Weight", 0, "Weight cannot be 0.  You should check for this and throw an ArgumentException in set");
			RunPropertyCornerCaseTest<decimal>(product, "WholesalePrice", -1m, "WholesalePrice cannot be < 0.  You should check for this and throw an ArgumentException in set");
			RunPropertyCornerCaseTest<int>(product, "QuantityOnHand", -1, "QuantityOnHand cannot be < 0.  You should check for this and throw an ArgumentException in set");
		}

		/// <summary>
		/// Ensures that item serialization functions as expected
		/// </summary>
		[TestMethod]
		public void TestItemSerialization()
		{
			string name = "A";
			double weight = 0.05;
			int quatnityOnHand = 5;
			decimal wholesale = 5.95m;

			Item product = CreateProduct(name, weight, quatnityOnHand, wholesale);

			TestUtilities.TestSerialization<Item>(product, TestItemSerialization);
		}
		#endregion
	}
}
