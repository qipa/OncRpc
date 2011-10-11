using System;
using Rpc.MessageProtocol;

namespace Rpc
{
	/// <summary>
	/// data exchange interface
	/// </summary>
	public interface IConnector
	{
		/// <summary>
		/// synchronous or asynchronous execution of an RPC request
		/// </summary>
		/// <typeparam name="TReq"></typeparam>
		/// <typeparam name="TResp"></typeparam>
		/// <param name="header"></param>
		/// <param name="request"></param>
		/// <param name="completed"></param>
		/// <param name="excepted"></param>
		void Request<TReq, TResp>(rpc_msg header, TReq request, Action<TResp> completed, Action<Exception> excepted);
	}
}
