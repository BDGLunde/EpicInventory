using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Epic.Training.Project.Inventory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Epic.Training.Project.UnitTest
{
	/// <summary>
	/// Verifies Epic naming conventions
	/// </summary>
	[TestClass]
	public class NameConventionTests
	{
		#region Helper and support members
		/// <summary>
		/// Used to verify camel case
		/// </summary>
		private static readonly Regex CamelCase = new Regex("^[a-z]+(?:[A-Z][a-z]+)*$");

		/// <summary>
		/// Used to verify pascal case
		/// </summary>
		private static readonly Regex PascalCase = new Regex("^[A-Z][a-z]+(?:[A-Z][a-z]+)*$");


		/// <summary>
		/// Method used to look up members of a type
		/// </summary>
		/// <typeparam name="TMemberInfo">The type of member to look up</typeparam>
		/// <param name="flags">Flags used to indicate static vs. instance and public vs. non-public members</param>
		/// <returns></returns>
		private delegate TMemberInfo[] MemberLookup<TMemberInfo>(BindingFlags flags);

		/// <summary>
		/// Returns all members of every with every combination of Public/NonPublic and Instance/Static 
		/// </summary>
		/// <typeparam name="TMemberInfo">The type of member to look up</typeparam>
		/// <param name="lookupMethod">The method used to lookup the member information</param>
		/// <returns>A list of all members of the given type</returns>
		private static List<TMemberInfo> GetAllMembers<TMemberInfo>(MemberLookup<TMemberInfo> lookupMethod)
		{
			var allMembers = new List<TMemberInfo>();

			allMembers.AddRange(lookupMethod(BindingFlags.Instance | BindingFlags.Public));
			allMembers.AddRange(lookupMethod(BindingFlags.Instance | BindingFlags.NonPublic));
			allMembers.AddRange(lookupMethod(BindingFlags.Static | BindingFlags.Public));
			allMembers.AddRange(lookupMethod(BindingFlags.Static | BindingFlags.NonPublic));

			return allMembers;
		}

		/// <summary>
		/// Types that need to be examined
		/// </summary>
		private static List<Type> _typesInAssembly;

		/// <summary>
		/// Returns all relevent types, filtering out those that were auto-generated.
		/// </summary>
		/// <param name="context">From unit test framework</param>
		[ClassInitialize]
		public static void GetTypesInItemAssembly(TestContext context)
		{
			_typesInAssembly = new List<Type>();
			Assembly assemblyInfo = Assembly.GetAssembly(typeof(Epic.Training.Project.Inventory.Item));
			foreach (Type t in assemblyInfo.DefinedTypes)
			{
				// Skip the two classes already tested, along with auto-generated classes that come from IEnumerable results.
				if (t.Name[0] == '<' || t.Name.Contains("GetEnumerator>"))
				{
					continue;
				}
				_typesInAssembly.Add(t);
			}
		}
		#endregion

		#region Tests
		/// <summary>
		/// Verifies that class and struct names are in Pascal case, and interfaces are in IPascalCase
		/// </summary>
		[TestMethod]
		public void TestTypeNames()
		{
			foreach (Type t in _typesInAssembly)
			{
				// Skip the two classes already tested, along with auto-generated classes that come from IEnumerable results.
				string name = t.Name;
				if (t.IsGenericType)
				{
					name = name.Split('`')[0];
				}

				if (t.IsInterface)
				{
					Assert.IsFalse(name[0] != 'I' || !PascalCase.IsMatch(name.Substring(1)),
						string.Format("Your interface \"{0}\" does not match the pattern IPascalCase", t.Name));
				}
				else
				{
					Assert.IsFalse(!PascalCase.IsMatch(name),
						string.Format("Your class \"{0}\" is not in Pascal case", t.Name));
				}
			}
		}

		/// <summary>
		/// Verifies that all methods are in pascal case
		/// </summary>
		[TestMethod]
		public void TestMethodNames()
		{
			foreach (Type t in _typesInAssembly)
			{
				foreach (MethodInfo m in GetAllMembers<MethodInfo>(t.GetMethods))
				{
					string name = m.Name;

					// Exclude property getter/setter, event adder/remover and overloaded operators
					if (name.StartsWith("get_") || name.StartsWith("set_")) { continue; }

					// Exclude event adder/remover
					if (name.StartsWith("add_") || name.StartsWith("remove_")) { continue; }

					// Exclude overloaded operators
					if (name.StartsWith("op_")) { continue; }

					// Exclude explicitly implemented interfaces
					if (name.Contains(".")) { continue; }

					// Look for Linq
					Assert.IsFalse(name.Contains("AnonymousMethodDelegate") || name.Contains("<"),
						string.Format("The class \"{0}\" contains a method named {1}.  Are you using Linq?  Linq hides the implementation details and is not recommended except when used very carefully.  See: http://wiki.epic.com/main/Coding_Standards/.NET/3.0_Features#LINQ", t.Name, m.Name));


					Assert.IsFalse(!PascalCase.IsMatch(m.Name),
						string.Format("The method \"{0}\" of class \"{1}\" is not in Pascal case", m.Name, t.Name));
				}
			}
		}

		/// <summary>
		/// Verifies that all properties are in Pascal case
		/// </summary>
		[TestMethod]
		public void TestPropertyNames()
		{
			foreach (Type t in _typesInAssembly)
			{
				foreach (PropertyInfo p in GetAllMembers<PropertyInfo>(t.GetProperties))
				{
					// HResult is a property inherited from System.Exception.  If they create a
					// custom exception class, we may encounter it.
					if (p.Name == "HResult") { continue; }

					// Exclude explicitly implemented interfaces
					if (p.Name.Contains(".")) { continue; }

					Assert.IsFalse(!PascalCase.IsMatch(p.Name),
					  string.Format("The property \"{0}\" of class \"{1}\" is not in Pascal case", p.Name, t.Name));
				}
			}
		}

		/// <summary>
		/// Verifies that events follow Epic name conventions
		/// </summary>
		[TestMethod]
		public void TestEventNames()
		{
			foreach (Type t in _typesInAssembly)
			{
				foreach (EventInfo e in GetAllMembers<EventInfo>(t.GetEvents))
				{
					Assert.IsFalse(!PascalCase.IsMatch(e.Name),
						string.Format("The event \"{0}\" of class \"{1}\" is not in Pascal case", e.Name, t.Name));
				}
			}
		}

		/// <summary>
		/// Verifies that fields follow Epic name conventions
		/// </summary>
		[TestMethod]
		public void TestFieldNames()
		{
			foreach (Type t in _typesInAssembly)
			{
				if (t.IsSubclassOf(typeof(Delegate)))
				{
					return;
				}

				var eventNames = new HashSet<string>();
				foreach (EventInfo e in GetAllMembers<EventInfo>(t.GetEvents))
				{
					eventNames.Add(e.Name);
				}

				foreach (FieldInfo f in GetAllMembers<FieldInfo>(t.GetFields))
				{
					// Filter out backing field of enums
					if (f.Name == "value__") { continue; }

					// Filter out events
					if (eventNames.Contains(f.Name)) { continue; }

					// Filter out auto-implemented fields
					if (f.Name.Contains("__BackingField")) { continue; }

					// Filter out _HResult from Exception
					if (f.Name == "_HResult") { continue; }

					// Look for Linq
					Assert.IsFalse(f.Name.Contains("AnonymousMethodDelegate") || f.Name.Contains("<"),
						string.Format("The class \"{0}\" contains a field named {1}.  Are you using Linq?  Linq hides the implementation details and is not recommended except when used very carefully.  See: http://wiki.epic.com/main/Coding_Standards/.NET/3.0_Features#LINQ", t.Name, f.Name));

					if (f.IsLiteral || f.IsStatic && f.IsInitOnly)
					{
						if (t.IsEnum)
						{
							Assert.IsFalse(!PascalCase.IsMatch(f.Name),
								string.Format("The member \"{0}\" of enum \"{1}\" is not in Pascal case", f.Name, t.Name));
						}
						else
						{
							Assert.IsFalse(!PascalCase.IsMatch(f.Name),
								string.Format("The constant or static readonly field \"{0}\" of class \"{1}\" is not in Pascal case", f.Name, t.Name));
						}
					}
					else
					{
						if (f.IsPublic || f.IsAssembly || f.IsFamilyOrAssembly) // public, internal, or protected internal
						{
							Assert.IsFalse(!PascalCase.IsMatch(f.Name),
								string.Format("The field \"{0}\" of class \"{1}\" is public, internal or protected internal and is not in Pascal case", f.Name, t.Name));
						}
						else if (f.IsPrivate || f.IsFamily)  // protected or private
						{
							if (f.IsStatic)
							{
								Assert.IsFalse(f.IsPrivate && !f.Name.StartsWith("s_") && !CamelCase.IsMatch(f.Name.Substring(2)),
									string.Format("The static field \"{0}\" of class \"{1}\" is private or protected and doesn't match the pattern \"s_camelCase\"", f.Name, t.Name));
							}
							else
							{
								Assert.IsTrue(f.IsPrivate && !f.IsStatic && f.Name.StartsWith("_") && CamelCase.IsMatch(f.Name.Substring(1)),
									string.Format("The field \"{0}\" of class \"{1}\" is private or protected and doesn't match the pattern \"_camelCase\"", f.Name, t.Name));
							}

						}
					}
				}
			}
		}
		#endregion
	}
}
