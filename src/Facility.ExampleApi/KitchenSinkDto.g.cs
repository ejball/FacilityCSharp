// DO NOT EDIT: generated by fsdgencsharp
using System;
using System.Collections.Generic;
using Facility.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#pragma warning disable 612 // member is obsolete

namespace Facility.ExampleApi
{
	[System.CodeDom.Compiler.GeneratedCode("fsdgencsharp", "")]
	public sealed partial class KitchenSinkDto : ServiceDto<KitchenSinkDto>
	{
		[Obsolete]
		public string OldField { get; set; }

		/// <summary>
		/// Determines if two DTOs are equivalent.
		/// </summary>
		public override bool IsEquivalentTo(KitchenSinkDto other)
		{
			return other != null &&
				OldField == other.OldField;
		}
	}
}