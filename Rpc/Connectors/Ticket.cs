using System;
using System.Threading;
using System.Threading.Tasks;
using Rpc.MessageProtocol;
using Xdr;

namespace Rpc.Connectors
{
	internal class Ticket<TReq, TResp> : ITicket
	{
		private ITicketOwner _owner;
		private call_body _callBody;
		private TReq _reqArgs;
		private TaskCompletionSource<TResp> _taskSrc;
		private CancellationTokenRegistration _ctr;

		public uint Xid { get; set; }

		public Ticket(ITicketOwner owner, call_body callBody, TReq reqArgs, TaskCreationOptions options, CancellationToken token)
		{
			_owner = owner;
			_callBody = callBody;
			_reqArgs = reqArgs;
			_taskSrc = new TaskCompletionSource<TResp>(options);
			if(token.CanBeCanceled)
				_ctr = token.Register(Cancel);
			else
				_ctr = new CancellationTokenRegistration();
		}

		public Task<TResp> Task
		{
			get
			{
				return _taskSrc.Task;
			}
		}
		
		public void BuildRpcMessage(IByteWriter bw)
		{
			rpc_msg reqHeader = new rpc_msg()
			{
				xid = Xid,
				body = new body()
				{
					mtype = msg_type.CALL,
					cbody = _callBody
				}
			};

			Writer xw = Toolkit.CreateWriter(bw);
			xw.Write(reqHeader);
			xw.Write(_reqArgs);
			
			_callBody = null;
			_reqArgs = default(TReq);
		}
		
		public void ReadResult(IMsgReader mr, Reader r, rpc_msg respMsg)
		{
			_ctr.Dispose();
			try
			{
				Toolkit.ReplyMessageValidate(respMsg);

				TResp respArgs = r.Read<TResp>();
				mr.CheckEmpty();

				_taskSrc.TrySetResult(respArgs);
			}
			catch(Exception ex)
			{
				_taskSrc.TrySetException(ex);
			}
		}

		public void Except(Exception ex)
		{
			_ctr.Dispose();
			_taskSrc.TrySetException(ex);
		}

		private void Cancel()
		{
			if (_taskSrc.TrySetCanceled())
				_owner.RemoveTicket(this);
		}
	}
}
