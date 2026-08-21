using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace UniversalGraph.Editor
{
	public static class GraphNodeEditorRegistry
	{
		public sealed class Registration
		{
			public Type ViewType { get; }

			public Type DataType { get; }

			public Type ContainerType { get; }

			public string MenuPath { get; }

			internal Registration(Type viewType, Type dataType, Type containerType, string menuPath)
			{
				ViewType = viewType;
				DataType = dataType;
				ContainerType = containerType;
				MenuPath = menuPath;
			}
		}

		private static readonly Dictionary<Type, Registration> registrationsByDataType = new Dictionary<Type, Registration>();

		private static readonly List<Registration> registrations = new List<Registration>();

		private static readonly ReadOnlyCollection<Registration> readOnlyRegistrations = registrations.AsReadOnly();

		private static bool isInitialized;

		public static IReadOnlyList<Registration> Registrations
		{
			get
			{
				EnsureInitialized();
				return readOnlyRegistrations;
			}
		}

		public static IEnumerable<Registration> GetRegistrations(GraphContainer container)
		{
			EnsureInitialized();
			if ((object)container == (object)null)
			{
				return Array.Empty<Registration>();
			}
			Type actualContainerType = ((object)container).GetType();
			return registrations.Where((Registration registration) => registration.ContainerType.IsAssignableFrom(actualContainerType));
		}

		[InitializeOnLoadMethod]
		private static void Initialize()
		{
			registrationsByDataType.Clear();
			registrations.Clear();
			isInitialized = false;
			List<Registration> list = new List<Registration>();
			IEnumerable<Type> enumerable = ((IEnumerable<Type>)(object)TypeCache.GetTypesWithAttribute<GraphNodeEditorAttribute>()).OrderBy((Type type) => type.AssemblyQualifiedName, StringComparer.Ordinal);
			foreach (Type item3 in enumerable)
			{
				if (TryCreateRegistration(item3, out var registration, out var error))
				{
					list.Add(registration);
				}
				else
				{
					Debug.LogError((object)("[Dialogue Graph] Node Editor ?깅줉 ?ㅽ뙣: " + error));
				}
			}
			HashSet<Registration> invalidRegistrations = new HashSet<Registration>();
			foreach (IGrouping<Type, Registration> item4 in from candidate in list
				group candidate by candidate.DataType into @group
				where @group.Count() > 1
				select @group)
			{
				Registration[] array = item4.ToArray();
				Registration[] array2 = array;
				Registration[] array3 = array2;
				foreach (Registration item in array3)
				{
					invalidRegistrations.Add(item);
				}
				Debug.LogError((object)("[Dialogue Graph] Data ?\u0080??'" + item4.Key.FullName + "'??GraphNode View媛\u0080 以묐났 ?깅줉?먯뒿?덈떎: " + FormatViewNames(array)));
			}
			foreach (IGrouping<string, Registration> item5 in from @group in list.GroupBy((Registration candidate) => candidate.MenuPath, StringComparer.OrdinalIgnoreCase)
				where @group.Count() > 1
				select @group)
			{
				Registration[] array4 = item5.ToArray();
				Registration[] array5 = array4;
				Registration[] array6 = array5;
				foreach (Registration item2 in array6)
				{
					invalidRegistrations.Add(item2);
				}
				Debug.LogError((object)("[Dialogue Graph] 硫붾돱 寃쎈줈 '" + item5.Key + "'媛\u0080 以묐났 ?깅줉?먯뒿?덈떎: " + FormatViewNames(array4)));
			}
			foreach (Registration item6 in list.Where((Registration candidate) => !invalidRegistrations.Contains(candidate)).OrderBy((Registration candidate) => candidate.MenuPath, StringComparer.OrdinalIgnoreCase))
			{
				registrations.Add(item6);
				registrationsByDataType.Add(item6.DataType, item6);
			}
			isInitialized = true;
		}

		public static GraphNode CreateNode(GraphContainer container, NodeBaseData data)
		{
			if ((object)container == (object)null)
			{
				throw new ArgumentNullException("container");
			}
			if (data == null)
			{
				throw new ArgumentNullException("data");
			}
			EnsureInitialized();
			if (!registrationsByDataType.TryGetValue(data.GetType(), out var value))
			{
				throw new InvalidOperationException("'" + data.GetType().FullName + "'???깅줉??GraphNode View媛\u0080 ?놁뒿?덈떎.");
			}
			EnsureContainerCompatibility(container, value);
			GraphNode graphNode = CreateView(value);
			try
			{
				graphNode.BindNodeData(data);
			}
			catch (Exception innerException)
			{
				throw new InvalidOperationException("'" + value.ViewType.FullName + "'??湲곗〈 Data '" + value.DataType.FullName + "'瑜?Bind?섏? 紐삵뻽?듬땲??", innerException);
			}
			return graphNode;
		}

		public static GraphNode CreateNewNode(GraphContainer container, Registration registration, GraphNodeCreationContext context)
		{
			if ((object)container == (object)null)
			{
				throw new ArgumentNullException("container");
			}
			if (registration == null)
			{
				throw new ArgumentNullException("registration");
			}
			EnsureInitialized();
			if (!registrationsByDataType.TryGetValue(registration.DataType, out var value) || value != registration)
			{
				throw new InvalidOperationException("?꾩옱 Registry???녿뒗 Node ?깅줉 ?뺣낫?낅땲??");
			}
			EnsureContainerCompatibility(container, registration);
			GraphNode graphNode = CreateView(registration);
			NodeBaseData nodeBaseData;
			try
			{
				nodeBaseData = graphNode.CreateNewData(context);
			}
			catch (Exception innerException)
			{
				throw new InvalidOperationException("'" + registration.ViewType.FullName + "'??湲곕낯 Data瑜??앹꽦?섏? 紐삵뻽?듬땲??", innerException);
			}
			if (nodeBaseData == null || nodeBaseData.GetType() != registration.DataType)
			{
				throw new InvalidOperationException("'" + registration.ViewType.FullName + "'???깅줉 ?\u0080??'" + registration.DataType.FullName + "'怨??ㅻⅨ Data瑜??앹꽦?덉뒿?덈떎.");
			}
			try
			{
				graphNode.BindNodeData(nodeBaseData);
			}
			catch (Exception innerException2)
			{
				throw new InvalidOperationException("'" + registration.ViewType.FullName + "'????Data瑜?Bind?섏? 紐삵뻽?듬땲??", innerException2);
			}
			((GraphElement)graphNode).SetPosition(new Rect(context.Position, graphNode.DefaultSize));
			return graphNode;
		}

		private static GraphNode CreateView(Registration registration)
		{
			try
			{
				if (Activator.CreateInstance(registration.ViewType) is GraphNode result)
				{
					return result;
				}
			}
			catch (Exception innerException)
			{
				throw new InvalidOperationException("GraphNode View '" + registration.ViewType.FullName + "'瑜??앹꽦?섏? 紐삵뻽?듬땲??", innerException);
			}
			throw new InvalidOperationException("'" + registration.ViewType.FullName + "' ?몄뒪?댁뒪媛\u0080 GraphNode媛\u0080 ?꾨떃?덈떎.");
		}

		private static bool TryCreateRegistration(Type viewType, out Registration registration, out string error)
		{
			registration = null;
			if (viewType == null || !viewType.IsClass || viewType.IsAbstract || viewType.ContainsGenericParameters || !typeof(GraphNode).IsAssignableFrom(viewType))
			{
				error = "'" + (viewType?.FullName ?? "null") + "'?\u0080 concrete GraphNode ?대옒?ㅼ뿬???⑸땲??";
				return false;
			}
			if (viewType.GetConstructor(Type.EmptyTypes) == null)
			{
				error = "'" + viewType.FullName + "'??public 湲곕낯 ?앹꽦?먭? ?놁뒿?덈떎.";
				return false;
			}
			GraphNodeEditorAttribute customAttribute = viewType.GetCustomAttribute<GraphNodeEditorAttribute>(inherit: false);
			Type type = customAttribute?.ContainerType;
			if (type == null || !typeof(GraphContainer).IsAssignableFrom(type))
			{
				error = "'" + viewType.FullName + "'??Container ?\u0080?낆? GraphContainer?ъ빞 ?⑸땲??";
				return false;
			}
			string text = customAttribute?.MenuPath?.Trim();
			if (!IsValidMenuPath(text))
			{
				error = "'" + viewType.FullName + "'??硫붾돱 寃쎈줈 '" + (text ?? "null") + "'媛\u0080 ?щ컮瑜댁? ?딆뒿?덈떎.";
				return false;
			}
			Type type2 = FindDataType(viewType);
			if (type2 == null || !typeof(NodeBaseData).IsAssignableFrom(type2) || type2.IsAbstract || type2.ContainsGenericParameters)
			{
				error = "'" + viewType.FullName + "'?먯꽌 concrete NodeBaseData ?\u0080?낆쓣 異붾줎?섏? 紐삵뻽?듬땲??";
				return false;
			}
			if (type2.GetConstructor(Type.EmptyTypes) == null)
			{
				error = "?좉퇋 ?앹꽦???ъ슜??'" + type2.FullName + "'??public 湲곕낯 ?앹꽦?먭? ?놁뒿?덈떎.";
				return false;
			}
			registration = new Registration(viewType, type2, type, text);
			error = null;
			return true;
		}

		private static void EnsureContainerCompatibility(GraphContainer container, Registration registration)
		{
			if (registration.ContainerType.IsAssignableFrom(((object)container).GetType()))
			{
				return;
			}
			throw new InvalidOperationException("Node '" + registration.ViewType.FullName + "'?\u0080 '" + registration.ContainerType.FullName + "' Graph?먯꽌留??ъ슜?????덉?留?'" + ((object)container).GetType().FullName + "'???붿껌?먯뒿?덈떎.");
		}

		private static Type FindDataType(Type viewType)
		{
			Type type = viewType;
			while (type != null)
			{
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(GraphNode<>))
				{
					return type.GetGenericArguments()[0];
				}
				type = type.BaseType;
			}
			return null;
		}

		private static bool IsValidMenuPath(string menuPath)
		{
			if (string.IsNullOrWhiteSpace(menuPath))
			{
				return false;
			}
			string[] source = menuPath.Split('/', StringSplitOptions.None);
			return source.All((string segment) => !string.IsNullOrWhiteSpace(segment));
		}

		private static string FormatViewNames(IEnumerable<Registration> registrationsToFormat)
		{
			return string.Join(", ", registrationsToFormat.Select((Registration item) => item.ViewType.FullName));
		}

		private static void EnsureInitialized()
		{
			if (!isInitialized)
			{
				Initialize();
			}
		}
	}
}


