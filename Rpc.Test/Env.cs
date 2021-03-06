using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rpc.Connectors;

namespace Rpc
{
	public class Env
	{
		static Env()
		{
			LoggingConfiguration config = new LoggingConfiguration();
			ConsoleTarget consoleTarget = new ConsoleTarget();
			config.AddTarget("console", consoleTarget);
			consoleTarget.Layout = "${date:format=HH\\:MM\\:ss.fff}, ${logger}, ${level}, Th:${threadid}, ${message}";
			LoggingRule rule1 = new LoggingRule("*", LogLevel.Trace, consoleTarget);
			config.LoggingRules.Add(rule1);
			LogManager.Configuration = config;

			PortMapper = new IPEndPoint(IPAddress.Loopback, 111);
		}

		public readonly static IPEndPoint PortMapper;
		
		public static TResp WaitTask<TResp>(Func<CancellationToken, Task<TResp>> taskCreater)
		{
			CancellationTokenSource cts = new CancellationTokenSource();
			var t = taskCreater(cts.Token);

			if(!t.Wait(2000))
				cts.Cancel(false);

			return t.Result;
		}

		public static TResp WaitResult<TResp>(Task<TResp> task)
		{
			Assert.IsTrue(task.Wait(2000), "task is hung");
			return task.Result;
		}
	}
}
