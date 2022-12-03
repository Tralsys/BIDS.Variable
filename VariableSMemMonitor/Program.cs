using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

using VariableSMemMonitor.Core;

namespace VariableSMemMonitor;

class Program : IDisposable
{
	static readonly TimeSpan DefaultTimeSpan = new(0, 0, 0, 0, 200);
	static void Main(string[] args)
	{
		using Program program = new();

		program.Run();
	}

	NameSMemWatcher NameWatcher { get; }
	List<VariableSMemWatcher> VSMemWatchers { get; } = new();

	public Program()
	{
		Log($"{typeof(Program).Name} Starting ...");
		NameWatcher = new();
		Log($"{typeof(Program).Name} Initialize Complete");
	}

	public void Run()
	{
		bool isRunning = true;

		while (isRunning)
		{
			NameUpdateCheckAndCreateNewVSMem();

			DataUpdateCheck();

			Thread.Sleep(DefaultTimeSpan);

			if (Console.KeyAvailable && (Console.ReadLine() is "exit" or "quit"))
			{
				Log("`exit` or `quit` detected.  Program will terminate soon.");
				isRunning = false;
			}
		}
	}

	public void NameUpdateCheckAndCreateNewVSMem()
	{
		IReadOnlyList<string> newNames = NameWatcher.CheckNewName();

		foreach (var name in newNames)
		{
			Log($"NewName `{name}` detected");

			try
			{
				VSMemWatchers.Add(new(name));
				Log($"SMemName:`{name}` initialization Complete");
			}
			catch (Exception ex)
			{
				Log($"SMemName:`{name}` initialization failed ({ex.Message})");
				Console.WriteLine(ex);
			}
		}
	}

	public void DataUpdateCheck()
		=> VSMemWatchers.ForEach(DataUpdateCheck);

	public static void DataUpdateCheck(VariableSMemWatcher watcher)
	{
		var updatedValues = watcher.CheckForValueChange();

		if (updatedValues.ChangedValuesDic.Count <= 0)
			return;

		StringBuilder builder = new($"Data Update Detected! SMemName:`{updatedValues.SMemName}`");

		foreach (var v in updatedValues.ChangedValuesDic)
		{
			builder.AppendFormat("\t`{0}`=", v.Key);
			if (v.Value is Array array)
			{
				builder.Append('{');
				foreach (var elem in array)
					builder.Append(' ').Append(elem).Append(',');
				builder.Append('}');
			}
			else
			{
				builder.Append('`').Append(v.Value).Append('`');
			}
		}

		Log(builder.ToString());
	}

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

		((IDisposable)NameWatcher).Dispose();
		VSMemWatchers.ForEach(v => v.Dispose());
		VSMemWatchers.Clear();
		isDisposed = true;
	}
}

