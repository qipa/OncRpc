using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq.Expressions;

namespace Xdr2.EmitContexts
{
	public class SwitchModel
	{
		public FieldDesc SwitchField {get; private set;}
		public Dictionary<object, FieldDesc> Branches {get; private set;}

		public Delegate BuildWriter(Type targetType)
		{
			ParameterExpression pWriter = Expression.Parameter(typeof(Writer));
			ParameterExpression pItem = Expression.Parameter(targetType);

			List<ParameterExpression> variables = new List<ParameterExpression>();
			List<Expression> body = new List<Expression>();

			LabelTarget exit = Expression.Label();


			List<SwitchCase> cases = new List<SwitchCase>();
			foreach (var branch in Branches)
				cases.Add(BuildWriteBranch(branch.Key, branch.Value, pWriter, pItem, exit));

			body.Add(
			Expression.Switch(
				Expression.PropertyOrField(pItem, SwitchField.MInfo.Name),
				Expression.Block(
					Expression.Throw(Expression.New(typeof(InvalidOperationException).GetConstructor(new Type[] { typeof(string) }), Expression.Constant("unexpected value")))
					),
				cases.ToArray())
			);

			body.Add(Expression.Label(exit));

			BlockExpression block = Expression.Block(variables, body);

			return Expression
				.Lambda(typeof(WriteOneDelegate<>).MakeGenericType(targetType), block, pWriter, pItem)
				.Compile();
		}

		private SwitchCase BuildWriteBranch(object key, FieldDesc fieldDesc, Expression pWriter, Expression pItem, LabelTarget exit)
		{
			List<Expression> body = new List<Expression>();
			body.Add(Expression.Call(pWriter, typeof(Writer).GetMethod("Write").MakeGenericMethod(SwitchField.FieldType), Expression.Constant(key)));

			if (fieldDesc != null)
				body.Add(fieldDesc.BuildWrite(pWriter, pItem));

			body.Add(Expression.Return(exit));
			return Expression.SwitchCase(Expression.Block(body), Expression.Constant(key));
		}

		public Delegate BuildReader(Type targetType)
		{
			ParameterExpression pReader = Expression.Parameter(typeof(Reader));
			
			List<ParameterExpression> variables = new List<ParameterExpression>();
			List<Expression> body = new List<Expression>();

			ParameterExpression resultVar = Expression.Variable(targetType, "result");
			variables.Add(resultVar);

			BinaryExpression assign = Expression.Assign(resultVar, Expression.New(targetType));
			body.Add(assign);

			body.Add(Expression.Assign(
				Expression.PropertyOrField(resultVar, SwitchField.MInfo.Name),
				Expression.Call(pReader, typeof(Reader).GetMethod("Read").MakeGenericMethod(SwitchField.FieldType))));

			LabelTarget exit = Expression.Label();

			List<SwitchCase> cases = new List<SwitchCase>();
			foreach (var branch in Branches)
				cases.Add(BuildReadBranch(branch.Key, branch.Value, resultVar, pReader, exit));

			body.Add(
			Expression.Switch(
				Expression.PropertyOrField(resultVar, SwitchField.MInfo.Name),
				Expression.Block(
					Expression.Throw(Expression.New(typeof(InvalidOperationException).GetConstructor(new Type[] { typeof(string)}), Expression.Constant("unexpected value")))
					),
				cases.ToArray())
			);

			body.Add(Expression.Label(exit));
			body.Add(resultVar);

			BlockExpression block = Expression.Block(variables, body);

			return Expression
				.Lambda(typeof(ReadOneDelegate<>).MakeGenericType(targetType), block, pReader)
				.Compile();
		}

		private static SwitchCase BuildReadBranch(object key, FieldDesc fieldDesc, Expression resultVar, Expression pReader, LabelTarget exit)
		{
			List<Expression> body = new List<Expression>();
			if (fieldDesc != null)
				body.Add(Expression.Assign(
					Expression.PropertyOrField(resultVar, fieldDesc.MInfo.Name),
					fieldDesc.BuildRead(pReader)));
			body.Add(Expression.Break(exit));
			return Expression.SwitchCase(Expression.Block(body), Expression.Constant(key));
		}
		
		public static SwitchModel Create(Type t)
		{
			SwitchModel model = new SwitchModel();
			model.Branches = new Dictionary<object, FieldDesc>();
			
			foreach(var fi in t.GetFields().Where((fi) => fi.IsPublic && !fi.IsStatic))
				AppendField(model, fi);

			foreach (var pi in t.GetProperties().Where((pi) => pi.CanWrite && pi.CanRead))
				AppendField(model, pi);
			
			if(model.SwitchField == null && model.Branches.Count == 0)
				return null;
			
			if(model.SwitchField == null)
				throw new InvalidOperationException("switch attribute not found");
			
			if(model.Branches.Count <= 1)
				throw new InvalidOperationException("requires more than two case attributes");
			
			if(model.Branches.Values.All((f) => f == null))
				throw new InvalidOperationException("required no void case attribute");
			
			return model;
		}
		
		private static void AppendField(SwitchModel model, MemberInfo mi)
		{
			if (mi.GetAttr<SwitchAttribute>() != null)
			{ // switch field
				if(model.SwitchField != null)
					throw new InvalidOperationException("duplicate switch attribute");
				
				model.SwitchField = new FieldDesc(mi);
				
				foreach(var cAttr in mi.GetAttrs<CaseAttribute>())
				{
					if(model.Branches.ContainsKey(cAttr.Value))
						throw new InvalidOperationException("duplicate case value " + cAttr.Value.ToString());
					model.Branches.Add(cAttr.Value, null);
				}
			}
			else
			{ // case field
				foreach(var cAttr in mi.GetAttrs<CaseAttribute>())
				{
					if(model.Branches.ContainsKey(cAttr.Value))
						throw new InvalidOperationException("duplicate case value " + cAttr.Value.ToString());
					model.Branches.Add(cAttr.Value, new FieldDesc(mi));
				}
			}
		}
	}
}

