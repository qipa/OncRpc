﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rpc.MessageProtocol;

namespace Rpc
{
	public class RpcException: SystemException
	{
		public RpcException()
		{
		}

		public RpcException(string message)
			: base(message)
		{
		}

		public RpcException(string message, Exception innerEx)
			: base(message, innerEx)
		{
		}
	}
}
