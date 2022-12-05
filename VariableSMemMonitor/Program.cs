using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using BIDS.Parser.Variable;

using TR.VariableSMemMonitor.Core;

namespace VariableSMemMonitor;

class Program : IDisposable
{
	static readonly TimeSpan DefaultTimeSpan = new(0, 0, 0, 0, 200);

	static async Task Main(string[] args)
	{
		using Program program = new();

		await program.Run();
	}

	VariableSMemAutoReader SMemReader { get; }

	public Program()
	{
		Log($"{typeof(Program).Name} Starting ...");

		SMemReader = new(10);

		SMemReader.NameAdded += SMemReader_NameAdded;
		SMemReader.ValueChanged += SMemReader_ValueChanged;

		Log($"{typeof(Program).Name} Initialize Complete");
	}

	public async Task Run()
	{
		_ = SMemReader.Run();

		while (true)
		{
			await Task.Delay(DefaultTimeSpan);

			if (Console.KeyAvailable && (Console.ReadLine() is "exit" or "quit"))
			{
				Log("`exit` or `quit` detected.  Program will terminate soon.");
				SMemReader.Dispose();
				return;
			}
		}
	}

	private static void SMemReader_NameAdded(object? sender, VariableSMemNameAddedEventArgs e)
	{
		Log(AppendSMemStructure(
				new($"NewName `{e.Name}` detected\n"),
				e.Structure.Records
			).ToString()
		);
	}

	private static void SMemReader_ValueChanged(object? sender, VariableSMemWatcher.ChangedValues e)
	{
		Log(AppendSMemStructure(
				new($"Data Update Detected! SMemName: `{e.SMemName}`\n"),
				e.RawPayload.Values.Where(v => e.ChangedValuesDic.ContainsKey(v.Name))
			).ToString()
		);
	}

	static StringBuilder AppendSMemStructure(StringBuilder builder, IEnumerable<VariableStructure.IDataRecord> list)
		=> builder.AppendJoin('\n', list.Select((v, i) => $"\t[{i:D2}]: {v}"));

	static void Log(
		string text,
		[CallerMemberName]
		string callerMemberName = ""
	)
		=> Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ({callerMemberName}) {text}");

	bool isDisposed = false;
	public void Dispose()
	{
		if (isDisposed)
			return;

		SMemReader.Dispose();

		isDisposed = true;
	}
}

