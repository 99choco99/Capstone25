using System;
using System.Collections.Generic;

namespace UniversalDialogue.Editor
{
	internal readonly struct DialogueMethodBindingAccessor
	{
		public Func<string> GetKey { get; }

		public Action<string> SetKey { get; }

		public Func<string> GetLegacyParameter { get; }

		public Action<string> SetLegacyParameter { get; }

		public Func<List<DialogueArgumentData>> GetArguments { get; }

		public Action<List<DialogueArgumentData>> SetArguments { get; }

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
