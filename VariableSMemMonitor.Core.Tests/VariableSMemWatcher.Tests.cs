using TR;
using TR.BIDSSMemLib;
using TR.VariableSMemMonitor.Core;

namespace VariableSMemMonitor.Core.Tests;

public class VariableSMemWatcherTests
{
	// Without Array
	class BasicSampleClass
	{
		public int IntValue { get; set; }

		public ushort UInt16Value { get; set; }
	}

	class ArraySampleClass : BasicSampleClass
	{
		public string SampleString = string.Empty;

		public double[] SampleDoubleArr { get; set; } = Array.Empty<double>();
	}

	[Test]
	public void BasicSampleClassInitTest()
	{
		SMemIFMock memory = new("test", 0x1000);

		VariableSMem<BasicSampleClass> VSMem = new(memory);
		BasicSampleClass data = new();

		VariableSMemWatcher monitor = new(new SMemIFMock(memory));

		VariableSMemWatcher.ChangedValues changedValues = monitor.CheckForValueChange();
		Assert.Multiple(() =>
		{
			Assert.That(changedValues.SMemName, Is.EqualTo(memory.SMemName));
			Assert.That(changedValues.ChangedValuesDic, Has.Count.EqualTo(2));

			Assert.That(changedValues.ChangedValuesDic[nameof(BasicSampleClass.IntValue)], Is.EqualTo((int)0));
			Assert.That(changedValues.ChangedValuesDic[nameof(BasicSampleClass.UInt16Value)], Is.EqualTo((ushort)0));
		});
	}

	[Test]
	public void ArraySampleClassInitTest()
	{
		SMemIFMock memory = new("test", 0x1000);

		VariableSMem<ArraySampleClass> VSMem = new(memory);

		VariableSMemWatcher monitor = new(new SMemIFMock(memory));

		VariableSMemWatcher.ChangedValues changedValues = monitor.CheckForValueChange();
		Assert.Multiple(() =>
		{
			Assert.That(changedValues.SMemName, Is.EqualTo(memory.SMemName));
			Assert.That(changedValues.ChangedValuesDic, Has.Count.EqualTo(4));

			Assert.That(changedValues.ChangedValuesDic[nameof(ArraySampleClass.IntValue)], Is.EqualTo((int)0));
			Assert.That(changedValues.ChangedValuesDic[nameof(ArraySampleClass.UInt16Value)], Is.EqualTo((ushort)0));

			Assert.That(changedValues.ChangedValuesDic[nameof(ArraySampleClass.SampleString)], Has.Length.Zero);
			Assert.That(changedValues.ChangedValuesDic[nameof(ArraySampleClass.SampleString)], Is.EqualTo(string.Empty));
			Assert.That(changedValues.ChangedValuesDic[nameof(ArraySampleClass.SampleDoubleArr)], Has.Length.Zero);
			Assert.That(changedValues.ChangedValuesDic[nameof(ArraySampleClass.SampleDoubleArr)].GetType(), Is.EqualTo(typeof(double[])));
		});
	}

	[Test]
	public void BasicSampleClassSetValueTest()
	{
		SMemIFMock memory = new("test", 0x1000);

		VariableSMem<BasicSampleClass> VSMem = new(memory);

		VariableSMemWatcher monitor = new(new SMemIFMock(memory));

		Assert.That(monitor.CheckForValueChange().ChangedValuesDic, Has.Count.EqualTo(2));

		BasicSampleClass data = new()
		{
			IntValue = 2,
			UInt16Value = 3
		};
		VSMem.Write(data);

		VariableSMemWatcher.ChangedValues changedValues = monitor.CheckForValueChange();
		Assert.Multiple(() =>
		{
			Assert.That(changedValues.ChangedValuesDic, Has.Count.EqualTo(2));

			Assert.That(changedValues.ChangedValuesDic[nameof(BasicSampleClass.IntValue)], Is.EqualTo(data.IntValue));
			Assert.That(changedValues.ChangedValuesDic[nameof(BasicSampleClass.UInt16Value)], Is.EqualTo(data.UInt16Value));
		});

		// 最後にチェックしてから何もWriteしていないため、データの更新は無い。
		Assert.That(monitor.CheckForValueChange().ChangedValuesDic, Has.Count.Zero);

		// 一つのメンバだけを更新すると、更新通知も一つだけになる
		data.IntValue = -2;
		VSMem.Write(data);

		VariableSMemWatcher.ChangedValues changedValues1 = monitor.CheckForValueChange();
		Assert.Multiple(() =>
		{
			Assert.That(changedValues1.ChangedValuesDic, Has.Count.EqualTo(1));

			Assert.That(changedValues1.ChangedValuesDic[nameof(BasicSampleClass.IntValue)], Is.EqualTo(data.IntValue));
		});
	}

	[Test]
	public void ArraySampleClassSetValueTest()
	{
		SMemIFMock memory = new("test", 0x1000);

		VariableSMem<ArraySampleClass> VSMem = new(memory);

		VariableSMemWatcher monitor = new(new SMemIFMock(memory));

		Assert.That(monitor.CheckForValueChange().ChangedValuesDic, Has.Count.EqualTo(4));

		ArraySampleClass data = new()
		{
			IntValue = 2,
			UInt16Value = 3,
			SampleString = "testTEST__Test String",
			SampleDoubleArr = new double[] { 1.2, 3.4 },
		};
		VSMem.Write(data);

		VariableSMemWatcher.ChangedValues changedValues = monitor.CheckForValueChange();
		Assert.Multiple(() =>
		{
			Assert.That(changedValues.ChangedValuesDic, Has.Count.EqualTo(4));

			Assert.That(changedValues.ChangedValuesDic[nameof(ArraySampleClass.IntValue)], Is.EqualTo(data.IntValue));
			Assert.That(changedValues.ChangedValuesDic[nameof(ArraySampleClass.UInt16Value)], Is.EqualTo(data.UInt16Value));
			Assert.That(changedValues.ChangedValuesDic[nameof(ArraySampleClass.SampleString)], Is.EqualTo(data.SampleString));
			Assert.That(changedValues.ChangedValuesDic[nameof(ArraySampleClass.SampleDoubleArr)], Is.EquivalentTo(data.SampleDoubleArr));
		});

		// 最後にチェックしてから何もWriteしていないため、データの更新は無い。
		Assert.That(monitor.CheckForValueChange().ChangedValuesDic, Has.Count.Zero);

		// 一部のメンバだけを更新
		data.IntValue = -2;
		data.SampleString = string.Empty;
		data.SampleDoubleArr = new double[]
		{
			9.8,
			7.6,
			5.4,
			3.2,
		};
		VSMem.Write(data);

		VariableSMemWatcher.ChangedValues changedValues1 = monitor.CheckForValueChange();
		Assert.Multiple(() =>
		{
			Assert.That(changedValues1.ChangedValuesDic, Has.Count.EqualTo(3));

			Assert.That(changedValues1.ChangedValuesDic[nameof(ArraySampleClass.IntValue)], Is.EqualTo(data.IntValue));
			Assert.That(changedValues1.ChangedValuesDic[nameof(ArraySampleClass.SampleString)], Is.EqualTo(data.SampleString));
			Assert.That(changedValues1.ChangedValuesDic[nameof(ArraySampleClass.SampleDoubleArr)], Is.EquivalentTo(data.SampleDoubleArr));
		});
	}
}
