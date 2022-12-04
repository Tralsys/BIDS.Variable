using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using BIDS.Parser.Variable;

using TR.BIDSSMemLib;

namespace BIDSSMemLib.Variable.RandomWriter;

partial class Program : IDisposable
{
	static void Main(string[] args)
	{
		string prompt = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;

		using Program program = new();
		program.Run(prompt);
	}

	readonly RandomValueGenerator rand = new();
	readonly SortedDictionary<string, VariableSMem> VariableSMemDic = new();
	readonly VariableSMemNameManager NameManager = new();

	const string COMMAND_LIST = "COMMAND LIST : add / + / update [index1, index2...] / sleep [time_ms] / ls / help / ? / exit / quit";
	public void Run(string prompt)
	{
		Log("VariableSMem Random Writer Starting...");

		foreach (var v in NameManager)
		{
			if (VariableSMemDic.ContainsKey(v.Name))
				continue;

			VariableSMem smem = VariableSMem.CreateWithoutType(v.Name);

			// 初期値の読み込みを行う
			_ = smem.ReadFromSMem();
			VariableSMemDic.Add(v.Name, smem);
		}

		Log($"Init Complete.  There are already {VariableSMemDic.Count} instance");

		string? s = null;
		do
		{
			string[]? sarr = s?.Split(' ');
			switch (sarr?[0])
			{
				case "exit":
				case "quit":
					return;

				case "ls":
					ShowSMemList();
					break;

				case "add":
				case "+":
					AddNewSMem();
					break;

				case "sleep":
					if (sarr.Length >= 2 && int.TryParse(sarr[1], out int time_ms))
						Thread.Sleep(time_ms);
					else
						Log("Sleep Time Input was Invalid", ConsoleColor.Red);
					break;

				case "update":
					UpdateSMemContent(sarr.AsSpan()[1..]);
					break;

				case "help":
				case "?":
				default:
					Log(COMMAND_LIST);
					break;
			}

			Console.Write($"{prompt} > ");
		} while ((s = Console.ReadLine()) is not null);
	}

	void ShowSMemList()
	{
		StringBuilder builder = new($"There are {VariableSMemDic.Count} Instances\n");

		int i = 0;
		foreach (VariableSMem smem in VariableSMemDic.Values)
		{
			AppendSMemInfoAndStructure(builder, i++, smem);
			builder.AppendLine();
		}

		Log(builder);
	}

	void AddNewSMem()
	{
		string id = rand.GetInt32().ToString();
		while (VariableSMemDic.ContainsKey(id))
			id = rand.GetInt32().ToString();


		VariableStructure structure = rand.GetRandomStructure(id);

		VariableSMem smem = new(id, 0x1000, structure);
		NameManager.AddName(id);
		VariableSMemDic.Add(id, smem);

		StringBuilder builder = new($"SMem `{id}` is successfully added to SMem with structure shown below:\n");
		AppendSMemStructure(builder, structure.Records);
		Log(builder);
	}

	void UpdateSMemContent(ReadOnlySpan<string> indexArr)
	{
		if (indexArr.IsEmpty)
		{
			Log($"No Instance was selected.", ConsoleColor.Red);
			return;
		}

		Dictionary<int, (VariableSMem smem, VariableStructurePayload payload)> payloads = new();

		foreach (var indexStr in indexArr)
		{
			if (!int.TryParse(indexStr, out int index))
			{
				Log($"Cannot Parse to int (str:`{indexStr}`)", ConsoleColor.Red);
				continue;
			}

			if (index < 0 || VariableSMemDic.Count <= index)
			{
				Log($"Index out of range (str:`{indexStr}` / Current SMem Instance Count: {VariableSMemDic.Count})", ConsoleColor.Red);
				continue;
			}

			if (payloads.ContainsKey(index))
			{
				Log($"Content Updating is already reserved (str:`{indexStr}`)", ConsoleColor.Red);
				continue;
			}

			var kvp = VariableSMemDic.ElementAt(index);
			VariableSMem smem = kvp.Value;

			VariableStructurePayload payload = rand.GetRandomPayload(smem.Structure);

			Log(AppendSMemInfoAndStructure(new("New Values are Generated\n"), index, smem.Structure.Name, payload.Values));
			payloads.Add(index, (smem, payload));
		}

		if (payloads.Count <= 0)
		{
			Log($"All Instance Selection had Error", ConsoleColor.Red);
			return;
		}

		Log("Content Update Committing...");
		foreach (var v in payloads.Values)
		{
			if (v.payload.Count > 0)
				v.smem.WriteToSMemFromPayload(v.payload);
		}
		Log("✅ Content Update Success!", ConsoleColor.Green);
	}

	public void Dispose()
	{
		NameManager.Dispose();
		foreach (var v in VariableSMemDic.Values)
			v.Dispose();
		VariableSMemDic.Clear();
		Log("Object Disposed");
	}

}
