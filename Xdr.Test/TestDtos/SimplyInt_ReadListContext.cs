using System;
using System.Collections.Generic;

namespace Xdr.TestDtos
{
	public class SimplyInt_ReadListContext
	{
		private List<SimplyInt> _target;
		private IReader _reader;
		private uint _length = 0;
		private uint _maxlength = 0;
		private Action<List<SimplyInt>> _completed;
		private Action<Exception> _excepted;

		private SimplyInt_ReadListContext(IReader reader, uint len, bool fix, Action<List<SimplyInt>> completed, Action<Exception> excepted)
		{
			_reader = reader;
			_completed = completed;
			_excepted = excepted;

			_target = new List<SimplyInt>();
			
			if (fix)
			{
				_length = len;
				ReadNextItem();
			}
			else
			{
				_maxlength = len;
				_reader.ReadUInt32(Lenght_Readed, _excepted);
			}
		}

		private void Lenght_Readed(uint val)
		{
			_length = val;
			if (_length > _maxlength)
				_excepted(new InvalidOperationException(string.Format("unexpected length {0} items", _length)));
			else
				ReadNextItem();
		}

		private void ReadNextItem()
		{
			if (_length > 0)
				_reader.Read<SimplyInt>(Item_Readed, _excepted);
			else
				_completed(_target);
		}

		private void Item_Readed(SimplyInt val)
		{
			_target.Add(val);
			_length--;
			ReadNextItem();
		}

		public static void ReadList(IReader reader, uint len, bool fix, Action<List<SimplyInt>> completed, Action<Exception> excepted)
		{
			new SimplyInt_ReadListContext(reader, len, fix, completed, excepted);
		}
	}
}

