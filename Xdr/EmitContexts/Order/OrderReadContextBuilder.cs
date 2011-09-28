using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace Xdr.EmitContexts
{
	public class OrderReadContextBuilder
	{
		private OrderModel _model;

		private Type _targetType;
		
		private TypeBuilder _typeBuilder;
		private FieldBuilder _targetField;
		private FieldBuilder _readerField;
		private FieldBuilder _completedField;
		private FieldBuilder _exceptedField;
		private ConstructorBuilder _constructor;

		public OrderReadContextBuilder(ModuleBuilder mb, Type targetType, OrderModel model)
		{
			_model = model;
			_targetType = targetType;
			_typeBuilder = mb.DefineType(_targetType.FullName + "_ReadContext", TypeAttributes.Public | TypeAttributes.Class);
			CreateFields();
		}
		
		public Type Build()
		{
			ILGenerator il;
			MethodBuilder nextMethod;

			FieldDesc lastField = _model.Fields[_model.Fields.Count - 1];
			nextMethod = lastField.CreateReaded(_typeBuilder, _targetField, out il);
			AppendReturn(il);

			for (int i = _model.Fields.Count - 2; i >= 0; i--)
			{
				var curField = _model.Fields[i];
				var nextField = _model.Fields[i + 1];
				
				MethodBuilder curMethod = curField.CreateReaded(_typeBuilder, _targetField, out il);
				nextField.AppendCall(il, _readerField, nextMethod, _exceptedField);
				nextMethod = curMethod;
			}

			FieldDesc firstField = _model.Fields[0];
			il = CreateConstructor();
			firstField.AppendCall(il, _readerField, nextMethod, _exceptedField);
			
			CreateStaticReader();
			return _typeBuilder.CreateType();
		}
		
		public void CreateStaticReader()
		{
			MethodBuilder mb = _typeBuilder.DefineMethod("Read", MethodAttributes.Public | MethodAttributes.Static, null,
				new Type[] { typeof(Reader), typeof(Action<>).MakeGenericType(_targetType), typeof(Action<Exception>)});
			ILGenerator il = mb.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Newobj, _constructor);
			il.Emit(OpCodes.Pop);
			il.Emit(OpCodes.Ret);
		}
		
		private ILGenerator CreateConstructor()
		{
			_constructor = _typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
						 new Type[] { typeof(Reader), typeof(Action<>).MakeGenericType(_targetType), typeof(Action<Exception>) });

			ILGenerator il = _constructor.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[] { }));
			
			if(_targetType.IsValueType)
			{
//IL_001B: ldarg.0
//IL_001C: ldloca.s V_0
//IL_001E: initobj Xdr.TestDtos.StructInt
//IL_0024: ldloc.0
//IL_0025: stfld Xdr.TestDtos.StructInt Xdr.TestDtos.StructInt/ReadContext._target
				LocalBuilder v0 = il.DeclareLocal(_targetType);
				
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldloca_S, v0);
				il.Emit(OpCodes.Initobj,_targetType );
				il.Emit(OpCodes.Ldloc_0);
				il.Emit(OpCodes.Stfld, _targetField);
			}
			else
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Newobj, _targetType.GetConstructor(new Type[] { }));
				il.Emit(OpCodes.Stfld, _targetField);
			}
			
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Stfld, _readerField);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Stfld, _completedField);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_3);
			il.Emit(OpCodes.Stfld, _exceptedField);
			return il;
		}
		
		private void AppendReturn(ILGenerator il)
		{
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, _completedField);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, _targetField);
			il.Emit(OpCodes.Callvirt, typeof(Action<>).MakeGenericType(_targetType).GetMethod("Invoke", new Type[] { _targetType }));
			il.Emit(OpCodes.Ret);
		}

		private void CreateFields()
		{
			_targetField = _typeBuilder.DefineField("_target", _targetType, FieldAttributes.Private);
			_readerField = _typeBuilder.DefineField("_reader", typeof(Reader), FieldAttributes.Private);
			_completedField = _typeBuilder.DefineField("_completed", typeof(Action<>).MakeGenericType(_targetType), FieldAttributes.Private);
			_exceptedField = _typeBuilder.DefineField("_excepted", typeof(Action<Exception>), FieldAttributes.Private);
		}
	}
}

