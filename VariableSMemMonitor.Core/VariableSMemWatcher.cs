using System;
using System.Collections;
using System.Collections.Generic;

using BIDS.Parser.Variable;

using TR;
using TR.BIDSSMemLib;

namespace VariableSMemMonitor.Core;

public class VariableSMemWatcher : IDisposable
{
	public record ChangedValues(string SMemName, VariableStructurePayload RawPayload, Dictionary<string, object> ChangedValuesDic);

	VariableSMem VSMem { get; }

	public string SMemName { get; }

	Dictionary<string, object?> CurrentValues { get; }

	public VariableSMemWatcher(string SMemName) : this(SMemName, null) { }

	public VariableSMemWatcher(ISMemIF SMemIF) : this(SMemIF.SMemName, SMemIF) { }

	private VariableSMemWatcher(string SMemName, ISMemIF? SMemIF)
	{
		if (string.IsNullOrEmpty(SMemName))
			throw new ArgumentNullException(nameof(SMemName), $"{nameof(SMemName)} cannot be NULL or Empty");

		this.SMemName = SMemName;

		VSMem = SMemIF is null
			? VariableSMem.CreateWithoutType(SMemName)
			: VariableSMem.CreateWithoutType(SMemIF);

		CurrentValues = new();

		foreach (var v in VSMem.Structure.Records)
		{
			CurrentValues.Add(v.Name, null);
		}
	}

	public ChangedValues CheckForValueChange()
	{
		VariableStructurePayload payload = VSMem.ReadFromSMem();

		Dictionary<string, object> ChangedValues = new();

		foreach (var v in payload.Values)
		{
			if (v is VariableStructure.DataRecord data)
			{
				object? lastValue = CurrentValues[v.Name];

				if (lastValue is null || !Equals(lastValue, data.Value))
				{
					if (data.Value is not null)
						ChangedValues[v.Name] = data.Value;

					CurrentValues[v.Name] = data.Value;
				}
			}
			else if (v is VariableStructure.ArrayStructure arr)
			{
				Array? lastValue = CurrentValues[v.Name] as Array;

				if (lastValue is null)
				{
					if (arr.ValueArray is not null)
						ChangedValues[v.Name] = arr.ValueArray;

					CurrentValues[v.Name] = arr.ValueArray;
				}
				else if (arr.ValueArray is not null && AreTwoArrayNotSame(lastValue, arr.ValueArray))
				{
					ChangedValues[v.Name] = arr.ValueArray;

					CurrentValues[v.Name] = arr.ValueArray;
				}
			}
		}

		return new(
			SMemName: this.SMemName,
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

