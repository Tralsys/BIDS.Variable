using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using BIDS.Parser.Variable;

namespace TR.VariableSMemMonitor.Core;

public class VariableSMemNameAddedEventArgs : EventArgs
{
	public string Name { get; }

	public VariableStructure Structure { get; }

	public VariableSMemNameAddedEventArgs(string name, VariableStructure structure)
	{
		Name = name;
		Structure = structure;
	}
}

public class VariableSMemAutoReader : IDisposable
{
	public event EventHandler<VariableSMemNameAddedEventArgs>? NameAdded;
	public event EventHandler<VariableSMemWatcher.ChangedValues>? ValueChanged;

	NameSMemWatcher SMemNameWatcher { get; }

	Dictionary<string, VariableSMemWatcher> VariableSMemWatcherDic { get; }
	readonly int Interval_ms;

	Task? AutoReadTask = null;

	public VariableSMemAutoReader(int Interval_ms)
	{
		VariableSMemWatcherDic = new();

		SMemNameWatcher = new();

		this.Interval_ms = Interval_ms;
	}

	public Task Run()
	{
		AutoReadTask ??= Task.Run(AutoReader);

		return AutoReadTask;
	}

	async Task AutoReader()
	{
		while (!disposingValue)
		{
			IReadOnlyList<string> newNames = SMemNameWatcher.CheckNewName();

			foreach (var newName in newNames)
			{
				VariableSMemWatcher vsmemWatcher = new(newName);
				VariableSMemWatcherDic.Add(newName, vsmemWatcher);

				NameAdded?.Invoke(this, new(newName, vsmemWatcher.Structure));
			}

			foreach (var watcher in VariableSMemWatcherDic.Values)
			{
				var v = watcher.CheckForValueChange();

				if (v.ChangedValuesDic.Count > 0)
					ValueChanged?.Invoke(this, v);
			}

			await Task.Delay(Interval_ms);
		}
	}

	public VariableSMemWatcher.ChangedValues? ApplyNewValue(VariableStructure structure, VariableStructurePayload payload)
		=> VariableSMemWatcherDic.TryGetValue(structure.Name, out VariableSMemWatcher? watcher)
			? watcher.CheckForValueChange(payload)
			: AddNewStructure(structure, payload);

	public VariableSMemWatcher.ChangedValues? AddNewStructure(VariableStructure structure)
	{
		VariableStructurePayload payload = new(structure.DataTypeId);

		foreach (var v in structure.Records)
			payload.Add(v.Name, v);

		return AddNewStructure(structure, payload);
	}

	public VariableSMemWatcher.ChangedValues? AddNewStructure(VariableStructure structure, VariableStructurePayload payload)
	{
		VariableSMemWatcher watcher = new(structure);
		SMemNameWatcher.AddNewName(structure.Name);
		VariableSMemWatcherDic.Add(structure.Name, watcher);

		// 新規追加は「全ての値が更新された」扱い
		return new(
			watcher.SMemName,
			structure,
			new VariableStructurePayload(structure.DataTypeId),
			// `CurrentValue`はWatcher内部で更新されるため、コピーを作成して返す
#if NET6_0_OR_GREATER
			new Dictionary<string, object?>(watcher.CurrentValues)
#else
			watcher.CurrentValues.ToDictionary(v => v.Key, v => v.Value)
#endif
		);
	}

	#region IDisposable
	private bool disposingValue = false;
	private bool disposedValue;

	protected virtual void Dispose(bool disposing)
	{
		disposingValue = true;

		if (!disposedValue)
		{
			if (disposing)
			{
				AutoReadTask?.Wait();

				SMemNameWatcher.Dispose();

				foreach (var v in VariableSMemWatcherDic.Values)
					v.Dispose();

				VariableSMemWatcherDic.Clear();
			}

			disposedValue = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
	#endregion

	// TODO: intをKeyとしたVariableStructureのDicって要る?
	// BIDSsv側では、Structure Nameをkeyとして管理する。
	// 通信でIntを使うのは、データ量を削減するため。
	// 一定間隔で更新をチェックし、更新があったらそれをイベントで通知
	// 外部から受信したデータは、ここを経由せずに、BIDSsv側から直接別のMODに転送する
}
