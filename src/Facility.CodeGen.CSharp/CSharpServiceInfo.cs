using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Facility.Definition;
using Facility.Definition.CodeGen;
using Facility.Definition.Http;

namespace Facility.CodeGen.CSharp
{
	/// <summary>
	/// Service information used when generating C#.
	/// </summary>
	public sealed class CSharpServiceInfo
	{
		/// <summary>
		/// Creates C# info for a service.
		/// </summary>
		/// <exception cref="ServiceDefinitionException">Thrown if there are errors.</exception>
		public static CSharpServiceInfo Create(ServiceInfo serviceInfo) =>
			TryCreate(serviceInfo, out var service, out var errors) ? service : throw new ServiceDefinitionException(errors);

		/// <summary>
		/// Attempts to create C# info for a service.
		/// </summary>
		/// <returns>True if there are no errors.</returns>
		/// <remarks>Even if there are errors, an invalid HTTP mapping will be returned.</remarks>
		public static bool TryCreate(ServiceInfo serviceInfo, out CSharpServiceInfo csharpServiceInfo, out IReadOnlyList<ServiceDefinitionError> errors)
		{
			csharpServiceInfo = new CSharpServiceInfo(serviceInfo, out errors);
			return errors.Count == 0;
		}

		/// <summary>
		/// The service.
		/// </summary>
		public ServiceInfo Service { get; }

		/// <summary>
		/// The namespace.
		/// </summary>
		public string Namespace => m_namespace ?? CodeGenUtility.Capitalize(Service.Name);

		/// <summary>
		/// Gets the property name for the specified field.
		/// </summary>
		public string GetFieldPropertyName(ServiceFieldInfo field) =>
			m_fieldPropertyNames.TryGetValue(field, out var value) ? value : CodeGenUtility.Capitalize(field.Name);

		private CSharpServiceInfo(ServiceInfo serviceInfo, out IReadOnlyList<ServiceDefinitionError> errors)
		{
			Service = serviceInfo;
			m_fieldPropertyNames = new Dictionary<ServiceFieldInfo, string>();

			var validationErrors = new List<ServiceDefinitionError>();

			foreach (var descendant in serviceInfo.GetElementAndDescendants().OfType<ServiceElementWithAttributesInfo>())
			{
				var csharpAttributes = descendant.GetAttributes("csharp");
				if (csharpAttributes.Count == 1)
				{
					var csharpAttribute = csharpAttributes[0];
					if (descendant is ServiceInfo || descendant is ServiceFieldInfo)
					{
						foreach (var parameter in csharpAttribute.Parameters)
						{
							if (parameter.Name == "namespace" && descendant is ServiceInfo)
								m_namespace = parameter.Value;
							else if (parameter.Name == "name" && descendant is ServiceFieldInfo field)
								m_fieldPropertyNames[field] = parameter.Value;
							else
								validationErrors.Add(ServiceDefinitionUtility.CreateUnexpectedAttributeParameterError(csharpAttribute.Name, parameter));
						}
					}
					else
					{
						validationErrors.Add(ServiceDefinitionUtility.CreateUnexpectedAttributeError(csharpAttribute));
					}
				}
				else if (csharpAttributes.Count > 1)
				{
					validationErrors.Add(ServiceDefinitionUtility.CreateDuplicateAttributeError(csharpAttributes[1]));
				}
			}

			var typeName = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { CSharpUtility.GetInterfaceName(serviceInfo) };

			void checkTypeName(string name, ServiceDefinitionPosition position)
			{
				if (!typeName.Add(name))
					validationErrors.Add(new ServiceDefinitionError($"Element generates duplicate C# type '{name}'.", position));
			}

			foreach (var member in serviceInfo.Members)
			{
				if (member is ServiceMethodInfo method)
				{
					checkTypeName(CSharpUtility.GetRequestDtoName(method), method.Position);
					checkTypeName(CSharpUtility.GetResponseDtoName(method), method.Position);
				}
				else if (member is ServiceDtoInfo dto)
				{
					checkTypeName(CSharpUtility.GetDtoName(dto), dto.Position);
				}
				else if (member is ServiceEnumInfo @enum)
				{
					checkTypeName(CSharpUtility.GetEnumName(@enum), @enum.Position);
				}
				else if (member is ServiceErrorSetInfo errorSet)
				{
					checkTypeName(CSharpUtility.GetErrorSetName(errorSet), errorSet.Position);
				}
				else
				{
					throw new InvalidOperationException($"Unknown member type {member.GetType().FullName}");
				}
			}

			errors = validationErrors;
		}

		private readonly string m_namespace;
		private readonly Dictionary<ServiceFieldInfo, string> m_fieldPropertyNames;
	}
}

