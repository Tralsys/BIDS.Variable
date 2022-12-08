using BIDS.Parser.Variable;

namespace BIDS.Parser.Variable.Tests.UtilsTests;

public class MemberInfoToVariableDataRecordListTests
{
	class SampleBaseClass
	{
		public int SampleBaseIntProperty { get; }
		public long SampleBaseLongField = 0;

		private int PrivateBaseIntProperty { get; }
		protected string ProtectedBaseStringField = string.Empty;
		internal char InternalBaseCharProperty { get; }

		public event EventHandler? BasePublicEvent;
		private event EventHandler? BasePrivateEvent;

		public void BasePublicMethod() => BasePublicEvent?.Invoke(this, new());
		private void BasePrivateMethod() => BasePrivateEvent?.Invoke(this, new());

		static public byte PublicBaseStaticCharProperty { get; }
	}

	class SampleClass : SampleBaseClass
	{
		public int SampleIntProperty { get; }
		public long[]? SampleLongArrayField = Array.Empty<long>();

		private int PrivateIntProperty { get; }
		protected string ProtectedStringField = string.Empty;
		internal char InternalCharProperty { get; }
	}

	[Test]
	public void NormalTest()
	{
		var actual = typeof(SampleClass).ToVariableDataRecordList();

		Assert.Multiple(() =>
		{
			Assert.That(
				actual,
				Is.EquivalentTo(new List<VariableStructure.IDataRecord>()
				{
				new VariableStructure.DataRecord(VariableDataType.Int32, nameof(SampleClass.SampleIntProperty), (int)0),
				new VariableStructure.DataRecord(VariableDataType.Int32, nameof(SampleBaseClass.SampleBaseIntProperty), (int)0),

				actual[2],
				new VariableStructure.DataRecord(VariableDataType.Int64, nameof(SampleBaseClass.SampleBaseLongField), (long)0),
				})
			);

			var expect_2 = new VariableStructure.ArrayDataRecord(VariableDataType.Int64, nameof(SampleClass.SampleLongArrayField), Array.Empty<int>());

			if (actual[2] is not VariableStructure.IArrayDataRecordWithValue actual_2)
			{
				Assert.Fail("Invalid Type (actual[1])");
				return;
			}

			Assert.That(actual_2, Is.EqualTo(expect_2 with
			{
				ValueArray = actual_2.ValueArray
			}));

			Assert.That(actual_2.ValueArray, Is.EquivalentTo(expect_2.ValueArray));
		});
	}
}
