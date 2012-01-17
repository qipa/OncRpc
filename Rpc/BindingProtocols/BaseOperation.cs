﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rpc.MessageProtocol;
using System.Threading.Tasks;

namespace Rpc.BindingProtocols
{
	/// <summary>
	/// operations of binding protocols
	/// </summary>
	public abstract class BaseOperation
	{
		private const uint Program = 100000u;
		private IConnector _conn;
		private uint _version;

		internal BaseOperation(uint vers, IConnector conn)
		{
			_version = vers;
			_conn = conn;
		}

		private call_body CreateHeader(uint procNum)
		{
			return new call_body()
			{
				rpcvers = 2,
				prog = Program,
				proc = procNum,
				vers = _version,
				cred = opaque_auth.None,
				verf = opaque_auth.None
			};
		}

		/// <summary>
		/// generic request
		/// </summary>
		/// <typeparam name="TReq"></typeparam>
		/// <typeparam name="TResp"></typeparam>
		/// <param name="proc"></param>
		/// <param name="args"></param>
		/// <param name="completed"></param>
		/// <param name="excepted"></param>
		protected Task<TResp> CreateTask<TReq, TResp>(uint proc, TReq args)
		{
			return _conn.Request<TReq, TResp>(CreateHeader(proc), args);
		}

		/// <summary>
		/// generic request
		/// </summary>
		/// <typeparam name="TReq"></typeparam>
		/// <typeparam name="TResp"></typeparam>
		/// <param name="proc"></param>
		/// <param name="args"></param>
		/// <param name="completed"></param>
		/// <param name="excepted"></param>
		protected IRpcRequest<TResp> Request<TReq, TResp>(uint proc, TReq args, Action<TResp> completed, Action<Exception> excepted)
		{
			throw new NotImplementedException();
		}
	}
}
