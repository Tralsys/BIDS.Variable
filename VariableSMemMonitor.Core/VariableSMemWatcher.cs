using System;
using System.Collections;
using System.Collections.Generic;

using BIDS.Parser.Variable;

using TR.BIDSSMemLib;

namespace TR.VariableSMemMonitor.Core;

public class VariableSMemWatcher : IDisposable
{
	public record ChangedValues(string SMemName, VariableStructure Structure, VariableStructurePayload RawPayload, IReadOnlyDictionary<string, object?> ChangedValuesDic);

	internal VariableSMem VSMem { get; }
	public VariableStructure Structure => VSMem.Structure;

	public string SMemName { get; }

	Dictionary<string, object?> _CurrentValues { get; }
	public IReadOnlyDictionary<string, object?> CurrentValues => _CurrentValues;

	public VariableSMemWatcher(string SMemName) : this(VariableSMem.CreateWithoutType(SMemName)) { }

	public VariableSMemWatcher(ISMemIF SMemIF) : this(VariableSMem.CreateWithoutType(SMemIF)) { }

	// TODO: 0x1000 = 4096byte以上の場合に対応する
	// 現状、どうやってCapacityの必要量を推定するかを決めれない
	public VariableSMemWatcher(VariableStructure structure) : this(new VariableSMem(structure.Name, 0x1000, structure)) { }

	public VariableSMemWatcher(VariableSMem vsMem)
	{
		this.SMemName = vsMem.Name;

		VSMem = vsMem;

		_CurrentValues = new();

		foreach (var v in VSMem.Structure.Records)
		{
			// Structure指定で初期化されて、かつValue / ValueArrayに値が保存されていた場合に限り初期値が設定される
			_CurrentValues.Add(v.Name, v switch
			{
				VariableStructure.IDataRecordWithValue dataRecord => dataRecord.Value,
				VariableStructure.IArrayDataRecordWithValue dataRecord => dataRecord.ValueArray,

				_ => null,
			});
		}
	}

	public ChangedValues CheckForValueChange()
		=> CheckForValueChange(VSMem.ReadFromSMem());

	public ChangedValues CheckForValueChange(VariableStructurePayload payload)
	{
		Dictionary<string, object?> ChangedValues = new();

		foreach (var v in payload.Values)
		{
			if (v is VariableStructure.IDataRecordWithValue data)
			{
				object? lastValue = _CurrentValues[v.Name];

				if (lastValue is null || !Equals(lastValue, data.Value))
				{
					if (data.Value is not null)
						ChangedValues[v.Name] = data.Value;

					_CurrentValues[v.Name] = data.Value;
				}
			}
			else if (v is VariableStructure.IArrayDataRecordWithValue arr)
			{
				Array? lastValue = _CurrentValues[v.Name] as Array;

				if (lastValue is null)
				{
					if (arr.ValueArray is not null)
						ChangedValues[v.Name] = arr.ValueArray;

					_CurrentValues[v.Name] = arr.ValueArray;
				}
				else if (arr.ValueArray is not null && AreTwoArrayNotSame(lastValue, arr.ValueArray))
				{
					ChangedValues[v.Name] = arr.ValueArray;

					_CurrentValues[v.Name] = arr.ValueArray;
				}
			}
		}

		return new(
			SMemName: this.SMemName,
			Structure: this.Structure,
			RawPayload: payload,
			ChangedValuesDic: ChangedValues
		);
	}

	static bool AreTwoArrayNotSame(Array arr1, Array arr2)
	{
		IEnumerator enumerator1 = arr1.GetEnumerator();
		IEnumerator enumerator2 = arr2.GetEnumerator();

		bool moveNext1 = enumerator1.MoveNext();
		bool moveNext2 = enumerator2.MoveNext();

		while (moveNext1 && moveNext2)
		{
			if (!Equals(enumerator1.Current, enumerator2.Current))
				return true;

			moveNext1 = enumerator1.MoveNext();
			moveNext2 = enumerator2.MoveNext();
		}

		// MoveNextに両方失敗したなら、両者は同じ長さ。 -> return false
		// MoveNextにどちらか片方失敗したなら、両者は違う長さ -> return true
		// `white`の条件指定から考えて、ここまで来るのに両方とも成功はあり得ない
		return (moveNext1 || moveNext2);
	}

	public void Dispose()
	{
		((IDisposable)VSMem).Dispose();
	}
}

