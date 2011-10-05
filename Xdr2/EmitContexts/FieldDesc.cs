using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Reflection.Emit;
using System.Linq.Expressions;

namespace Xdr2.EmitContexts
{
	public class FieldDesc
	{
		public readonly Type FieldType;
		public readonly MemberInfo MInfo;

		protected bool _isOption = false;
		
		protected bool _isMany = false;
		protected bool _isFix = false;
		protected uint _len = 0;

		public FieldDesc(MemberInfo mi)
		{
			MInfo = mi;

			FieldInfo fi = mi as FieldInfo;
			if (fi != null)
			{
				FieldType = fi.FieldType;
				ExtractAttributes();
			}

			PropertyInfo pi = mi as PropertyInfo;
			if (pi != null)
			{
				FieldType = pi.PropertyType;
				ExtractAttributes();
			}

			throw new NotImplementedException("only PropertyInfo or FieldInfo");
		}
		
		private void ExtractAttributes()
		{
			var optAttr = MInfo.GetAttr<OptionAttribute>();
			if(optAttr != null)
			{
				if(FieldType.IsValueType)
					throw new InvalidOperationException("ValueType not supported Option attribute (use Nullable<> type)");
				_isOption = true;
			}
			
			var fixAttr = MInfo.GetAttr<FixAttribute>();
			var varAttr = MInfo.GetAttr<VarAttribute>();
			
			if(fixAttr != null && varAttr != null)
				throw new InvalidOperationException("can not use Fix and Var attributes both");
			
			if(fixAttr != null)
			{
				_isMany = true;
				_isFix = true;
				_len = fixAttr.Length;
			}
			
			if(varAttr != null)
			{
				_isMany = true;
				_isFix = false;
				_len = varAttr.MaxLength;
			}

			if (_isOption && _isMany)
				throw new InvalidOperationException("can not use Fix and Option attributes both or Var and Option attributes both");
		}
		
		internal Expression BuildRead(Expression pReader)
		{
			if(_isMany)
				return Expression.Call(pReader, typeof(Reader).GetMethod(_isFix ? "ReadFix" : "ReadVar").MakeGenericMethod(FieldType), Expression.Constant(_len));
			else
				return Expression.Call(pReader, typeof(Reader).GetMethod(_isOption ? "ReadOption" : "Read").MakeGenericMethod(FieldType));
		}

		internal Expression BuildWrite(Expression pWriter, Expression pItem)
		{
			Expression field = Expression.PropertyOrField(pItem, MInfo.Name);
			if (_isMany)
				return Expression.Call(pWriter, typeof(Writer).GetMethod(_isFix ? "WriteFix" : "WriteVar").MakeGenericMethod(FieldType), Expression.Constant(_len), field);
			else
				return Expression.Call(pWriter, typeof(Writer).GetMethod(_isOption ? "WriteOption" : "Write").MakeGenericMethod(FieldType), field);
		}
	}
}

