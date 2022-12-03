using System;
using System.Collections.Generic;

using TR.BIDSSMemLib;

namespace TR.VariableSMemMonitor.Core;

public class NameSMemWatcher : IDisposable
{
	VariableSMemNameManager NameManager { get; }

	HashSet<string> KnownNames { get; }

	public NameSMemWatcher() : this(new VariableSMemNameManager()) { }

	public NameSMemWatcher(ISMemIF SMemIF) : this(new VariableSMemNameManager(SMemIF)) { }

	public NameSMemWatcher(VariableSMemNameManager NameManager)
	{
		KnownNames = new();
		this.NameManager = NameManager;
	}

	public IReadOnlyList<string> CheckNewName()
	{
		List<string> addedName = new();

		foreach (var v in NameManager)
		{
			if (!KnownNames.Contains(v.Name))
			{
				KnownNames.Add(v.Name);
				addedName.Add(v.Name);
			}
		}

		return addedName;
	}

	public void Dispose()
	{
		((IDisposable)NameManager).Dispose();
	}
}

