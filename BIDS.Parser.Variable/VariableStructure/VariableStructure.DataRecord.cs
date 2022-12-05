using System;
using System.Collections.Generic;
using System.Linq;

namespace BIDS.Parser.Variable;

public partial record VariableStructure
{
	public interface IDataRecordWithValue : IDataRecord
	{
		object? Value { get; }
	}
	public interface IDataRecordWithValue<T> : IDataRecordWithValue
	{
		new T? Value { get; }
	}


	public interface IDataRecordWithValue_HasWithNewValue : IDataRecord
	{
		IDataRecordWithValue_HasWithNewValue WithNewValue(object? NewValue);
	}
	public interface IDataRecordWithValue_HasWithNewValue<T> : IDataRecordWithValue_HasWithNewValue
	{
		IDataRecordWithValue_HasWithNewValue<T> WithNewValue(T? NewValue);
	}

	public interface IDataRecordWithValue_CanSet : IDataRecord
	{
		object? Value { set; }
	}
	public interface IDataRecordWithValue_CanSet<T> : IDataRecordWithValue_CanSet
	{
		new T? Value { set; }
	}

	public record DataRecord(VariableDataType Type, string Name, object? Value = null) : IDataRecordWithValue, IDataRecordWithValue_HasWithNewValue
	{
		public IDataRecord With(ref ReadOnlySpan<byte> bytes)
		{
			return this with
			{
				Value = this.Type.GetValueAndMoveNext(ref bytes)
			};
		}

		public IEnumerable<byte> GetStructureBytes()
			=> BitConverter.GetBytes((int)this.Type).Concat(System.Text.Encoding.UTF8.GetBytes(this.Name)).Append((byte)0);

		public IEnumerable<byte> GetBytes()
			=> this.Type.GetBytes(this.Value);

		public IDataRecordWithValue_HasWithNewValue WithNewValue(object? NewValue)
			=> this with
			{
				Value = NewValue
			};
	}
}
