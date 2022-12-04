using TR;
using TR.BIDSSMemLib;
using TR.VariableSMemMonitor.Core;

namespace VariableSMemMonitor.Core.Tests;

public class NameSMemWatcherTests
{
	const string TEST_NAME_1 = "test01";
	const string TEST_NAME_2 = "test02";
	const string TEST_NAME_3 = "test03";

	[Test]
	public void EmptySMemTest()
	{
		SMemIFMock memory = new("test", 0x1000);

		NameSMemWatcher watcher = new();

		IReadOnlyList<string> newNames = watcher.CheckNewName();

		Assert.That(newNames, Has.Count.Zero);
	}

	[Test]
	public void TwoTimeAddTest()
	{
		SMemIFMock memory = new("test", 0x1000);
		VariableSMemNameManager manager = new(memory);

		manager.AddName(TEST_NAME_1);

		NameSMemWatcher watcher = new(manager);

		IReadOnlyList<string> newNames = watcher.CheckNewName();

		Assert.That(newNames, Has.Count.EqualTo(1));
		Assert.That(newNames[0], Is.EqualTo(TEST_NAME_1));

		manager.AddName(TEST_NAME_2);
		manager.AddName(TEST_NAME_3);

		IReadOnlyList<string> newNames2 = watcher.CheckNewName();
		Assert.That(newNames2, Has.Count.EqualTo(2));
		Assert.Multiple(() =>
		{
			Assert.That(newNames2[0], Is.EqualTo(TEST_NAME_2));
			Assert.That(newNames2[1], Is.EqualTo(TEST_NAME_3));
		});
	}

	[Test]
	public void DeleteAndAddTest()
	{
		SMemIFMock memory = new("test", 0x1000);
		VariableSMemNameManager manager = new(memory);

		var name1 = manager.AddName(TEST_NAME_1);
		Assert.That(name1, Is.Not.Null);

		NameSMemWatcher watcher = new(manager);

		IReadOnlyList<string> newNames = watcher.CheckNewName();
		Assert.That(newNames, Has.Count.EqualTo(1));
		Assert.That(newNames[0], Is.EqualTo(TEST_NAME_1));

		manager.AddName(TEST_NAME_2);
		IReadOnlyList<string> newNames2 = watcher.CheckNewName();
		Assert.That(newNames2, Has.Count.EqualTo(1));
		Assert.That(newNames2[0], Is.EqualTo(TEST_NAME_2));

		manager.DeleteName(name1);

		IReadOnlyList<string> newNames3 = watcher.CheckNewName();
		Assert.That(newNames3, Has.Count.Zero);

		manager.AddName(TEST_NAME_3);
		IReadOnlyList<string> newNames4 = watcher.CheckNewName();
		Assert.That(newNames4, Has.Count.EqualTo(1));
		Assert.That(newNames4[0], Is.EqualTo(TEST_NAME_3));
	}
}
