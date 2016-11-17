using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Epic.Training.Project.SuppliedCode.Instrumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Epic.Training.Project.UnitTest
{
	/// <summary>
	/// Shared methods used by various test classes
	/// </summary>
	internal static class TestUtilities
	{
		/// <summary>
		/// Invokes the set method of the given property on the given object. Looks for an inner exception if an exception is thrown.
		/// </summary>
		/// <param name="p">The property to invoke the set method from</param>
		/// <param name="target">The target object</param>
		/// <param name="value">The value to set</param>
		public static void InvokePropertySetter(PropertyInfo p, object target, object value)
		{
			try
			{
				p.GetSetMethod().Invoke(target, new object[] { value });
			}
			catch (Exception e)
			{
				if (e.InnerException != null)
				{
					throw e.InnerException;
				}
				else
				{
					throw;
				}
			}
		}

		/// <summary>
		/// Returns the default constructor for the given type, if one exists
		/// </summary>
		/// <param name="t">The type to examine</param>
		/// <returns>The default constructor, or null if one does not exist</returns>
		public static ConstructorInfo GetDefaultConstructor(Type t)
		{
			ConstructorInfo[] itemConstructors = t.GetConstructors();
			ConstructorInfo defaultConstructor = null;

			foreach (ConstructorInfo c in itemConstructors)
			{
				if (c.GetParameters().Length == 0)
				{
					defaultConstructor = c;
					break;
				}
			}

			return defaultConstructor;
		}

		/// <summary>
		/// Runs the following tests:
		///  (1) A property propName exists in toTest
		///  (2) toTest.propName has a public get method
		///  (3) The Get method of toTest.propName returns the expected value
		/// </summary>
		/// <param name="toTest"></param>
		/// <param name="propName"></param>
		/// <param name="expectedValue"></param>
		public static void TestProperty(object toTest, string propName, object expectedValue)
		{
			Type type = toTest.GetType();
			PropertyInfo propInfo = type.GetProperty(propName);
			Assert.IsFalse(propInfo == null, string.Format("{0} has no property named {1}", type.Name, propName));

			if (propInfo != null)
			{
				object value = null;
				MethodInfo getmethod = propInfo.GetGetMethod();
				Assert.IsFalse(getmethod == null, string.Format("Property {0}.{1} has no public get method.", type.Name, propName));
				if (getmethod != null)
				{
					try
					{
						value = getmethod.Invoke(toTest, new object[] { });
					}
					catch (Exception e)
					{
						Assert.Fail(string.Format("There was a problem getting the value of property {0}.{1}: {2}", type.Name, propName, e.InnerException.Message));
					}

					if (value != null)
					{
						Assert.IsFalse(!object.Equals(value, expectedValue), string.Format("Property {0}.{1} = \"{2}\", but it should be \"{3}\"", type.Name, propName, value, expectedValue));
					}
				}
			}
		}

		/// <summary>
		/// Method type used to test the result of deserialzing an object of type T
		/// </summary>
		/// <typeparam name="T">The type being deserialized</typeparam>
		/// <param name="obj">The object of type T to test</param>
		public delegate void SerializationTestDelegate<T>(MemoryStream s, T beforeSerialization, T afterSerialization);

		/// <summary>
		/// Test serialization of type T by doing the following:
		/// 1. Determine if the type has [Serializable]
		/// 2. Serialize to a MemoryStream using a Binary formatter
		/// 3. Deserialize from a MemoryStream using a Binary formatter
		/// 4. Run the provided testResult method to compare the serialized and deserialized objects.
		/// </summary>
		/// <typeparam name="T">The type being tested</typeparam>
		/// <param name="beforeSerialization">The instance being tested</param>
		/// <param name="testResult">The method to determine if the deserialized object is correct</param>
		public static void TestSerialization<T>(T beforeSerialization, SerializationTestDelegate<T> testResult)
		{
			Type t = typeof(T);

			bool isSerializable = t.GetCustomAttribute<SerializableAttribute>() != null;

			Assert.IsTrue(isSerializable, "Even though you are implementing ISerialiable, you should also mark {0} as [Serializable], for documentation purposes.", typeof(T).Name);

			if (isSerializable)
			{
				using (MemoryStream stream = new MemoryStream())
				{
					BinaryFormatter b = new BinaryFormatter();
					b.Serialize(stream, beforeSerialization);

					stream.Flush();
					stream.Seek(0, SeekOrigin.Begin);

					T afterSerialization = (T)b.Deserialize(stream);
					testResult(stream, beforeSerialization, afterSerialization);
				}
			}

		}

		/// <summary>
		/// Compare a property in two given objects to determine if they match
		/// </summary>
		/// <typeparam name="T">The type of the objects</typeparam>
		/// <param name="obj1">The first object</param>
		/// <param name="obj2">The second object</param>
		/// <param name="propName">Name of the property to compare</param>
		/// <param name="errorString">Error strign to use if the properties don't match</param>
		/// <returns>True if the properties match</returns>
		public static bool CompareProperty<T>(T obj1, T obj2, string propName, string errorString)
		{
			if (object.Equals(obj1, null) || object.Equals(obj2, null))
			{
				return false;
			}
			return CompareProperty<T>(obj1, obj2, propName, errorString, true);
		}

		/// <summary>
		/// Compare a property in two given objects to determine if they match, or if they don't match, whichever is expected
		/// </summary>
		/// <typeparam name="T">The type of the objects</typeparam>
		/// <param name="obj1">The first object</param>
		/// <param name="obj2">The second object</param>
		/// <param name="propName">Name of the property to compare</param>
		/// <param name="errorString">Error strign to use if the properties don't match</param>
		/// <param name="shouldMatch">Indicates if the property values should be the same</param>
		/// <returns>True if the properties match</returns>
		public static bool CompareProperty<T>(T obj1, T obj2, string propName, string errorString, bool shouldMatch)
		{
			bool matches = true;
			PropertyInfo info = typeof(T).GetProperty(propName);
			MethodInfo getPropInfo = info.GetGetMethod();
			if (!getPropInfo.Invoke(obj1, new object[] { }).Equals(getPropInfo.Invoke(obj2, new object[] { })))
			{
				matches = false;
			}

			Assert.IsTrue(matches == shouldMatch, errorString, propName, getPropInfo.Invoke(obj1, new object[] { }), getPropInfo.Invoke(obj2, new object[] { }));

			return shouldMatch == matches;
		}

		public static void ReportRegions()
		{
			foreach (MeasuredRegion m in StepTracker.Regions)
			{
				string steps = string.Format("Measured:{0}, Reference:{1} ---> ", m.MeasuredSteps, m.ReferenceSteps);
				if (m.MeasuredSteps == 0 && m.ReferenceSteps > 0)
				{
					Assert.Fail(steps + "The measurement was zero for this test.  Did you forget to use an instrumented collection?");
				}
				else if (m.MeasuredSteps > 0 && m.ReferenceSteps == 0)
				{
					Assert.Fail(steps + "You are iterating over an instrumented list when no iteration is necessary. Possible causes include:\n\t1. You are recalculating your totals every time they are requested.\n\t2. When an item's QuantityOnHand, RetailPrice or Weight changes, you are recomputing the totals from scratch.");
				}
				else if (m.MeasuredSteps < m.ReferenceSteps)
				{
					Assert.Inconclusive(steps + "Your solution is faster than the reference.  Be sure that all collection processing takes place within an instrumented collection");
				}
				else if (m.MeasuredSteps > m.ReferenceSteps)
				{
					Assert.Inconclusive(steps + "Your solution is slower than the reference.  Can you find a more efficient way to implement your inventory?");
				}
			}
		}

	}
}
