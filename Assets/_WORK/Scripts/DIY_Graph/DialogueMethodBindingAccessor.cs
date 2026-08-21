using System;
using System.Collections.Generic;

namespace UniversalGraph.Editor
{
	/// <summary>
	/// Supplies a node inspector with read/write access to one serialized dialogue method binding.
	/// It lives in the runtime assembly because node data is runtime data, while its consumers are editor-only.
	/// </summary>
	public readonly struct DialogueMethodBindingAccessor
	{
		/// <summary>Gets the selected method key.</summary>
		public Func<string> GetKey { get; }

		/// <summary>Sets the selected method key.</summary>
		public Action<string> SetKey { get; }

		/// <summary>Gets the legacy single-string argument value.</summary>
		public Func<string> GetLegacyParameter { get; }

		/// <summary>Sets the legacy single-string argument value.</summary>
		public Action<string> SetLegacyParameter { get; }

		/// <summary>Gets the current serialized arguments.</summary>
		public Func<List<DialogueArgumentData>> GetArguments { get; }

		/// <summary>Replaces the current serialized arguments.</summary>
		public Action<List<DialogueArgumentData>> SetArguments { get; }

		/// <summary>Creates an accessor from the owning node data's get/set delegates.</summary>
		public DialogueMethodBindingAccessor(Func<string> getKey, Action<string> setKey, Func<string> getLegacyParameter, Action<string> setLegacyParameter, Func<List<DialogueArgumentData>> getArguments, Action<List<DialogueArgumentData>> setArguments)
		{
			GetKey = getKey;
			SetKey = setKey;
			GetLegacyParameter = getLegacyParameter;
			SetLegacyParameter = setLegacyParameter;
			GetArguments = getArguments;
			SetArguments = setArguments;
		}
	}
}
