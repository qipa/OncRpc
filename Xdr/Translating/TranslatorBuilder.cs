using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using Xdr.Translating;
using Xdr.Translating.Emit;

namespace Xdr
{
	public sealed class TranslatorBuilder
	{
		private Xdr.Examples.Translator _t;

		private ModuleBuilder _modBuilder;
		private DelegateCacheDescription _delegateCacheDescription;
		
		internal TranslatorBuilder(string name)
		{
			_t = new Xdr.Examples.Translator();

			AssemblyName asmName = new AssemblyName(name);
			AssemblyBuilder asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndCollect);
			_modBuilder = asmBuilder.DefineDynamicModule(name + ".dll", name + ".dll");

			_delegateCacheDescription = new DelegateCacheDescription(_modBuilder);
		}

		public TranslatorBuilder Map<T>(ReadOneDelegate<T> reader)
		{
			_t.AppendMethod(typeof(T), MethodType.ReadOne, reader);
			return this;
		}

		public TranslatorBuilder Map<T>(ReadManyDelegate<T> reader)
		{
			_t.AppendMethod(typeof(T), MethodType.ReadMany, reader);
			return this;
		}

		public ITranslator Build()
		{
			return _t;
		}
	}
}

