namespace BIDS.Parser.Variable.Tests.UtilsTests;

public class TypeToVariableDataRecordTests
{
	[TestCase(typeof(int), "ABC", VariableDataType.Int32)]
	public void NormalTest_WithTypeAndName_ReturnDataRecord(Type type, string name, VariableDataType expectedDataType)
	{
		Assert.That(
			Utils.ToVariableDataRecord(type, name),
			Is.EqualTo(new VariableStructure.DataRecord(expectedDataType, name, Utils.GetDefaultValue(expectedDataType)))
		);
	}

	[TestCase(typeof(int[]), "ABC", VariableDataType.Int32)]
	[TestCase(typeof(string), "ABC", VariableDataType.UInt8)]
	public void NormalTest_WithTypeAndName_ReturnArrayStructure(Type type, string name, VariableDataType expectedDataType)
	{
		var expected = new VariableStructure.ArrayDataRecord(expectedDataType, name, Utils.GetSpecifiedTypeArray(expectedDataType, 0));

		if (Utils.ToVariableDataRecord(type, name) is not VariableStructure.IArrayDataRecordWithValue actual)
		{
			Assert.Fail("actual was not `IArrayDataRecordWithValue");
			return;
		}

		Assert.That(
			actual,
			Is.EqualTo(expected with
			{
				ValueArray = actual.ValueArray
			})
		);

		Assert.That(actual.ValueArray, Is.EquivalentTo(expected.ValueArray));
	}

	[TestCase(typeof(object[][]), "ABC", typeof(NotSupportedException))]
	[TestCase(typeof(object[]), "ABC", typeof(NotSupportedException))]
	[TestCase(typeof(DateTime[]), "ABC", typeof(NotSupportedException))]
	public void ErrorTest_WithTypeAndName(Type type, string name, Type expectedExceptionType)
	{
		Assert.Throws(
			expectedExceptionType,
			() => Utils.ToVariableDataRecord(type, name)
		);
	}

	class SampleClass
	{
		public event EventHandler? SampleEvent;

		public int SampleProperty { get; set; }
		public int SampleField = 0;

		public void SampleMethod() => SampleEvent?.Invoke(this, new());
	}

	[TestCase(nameof(SampleClass.SampleProperty), VariableDataType.Int32)]
	[TestCase(nameof(SampleClass.SampleField), VariableDataType.Int32)]
	public void NormalTest_WithMemberInfo(string memberName, VariableDataType expectedDataType)
	{
		Assert.That(
			typeof(SampleClass).GetMember(memberName)[0].ToVariableDataRecord(),
			Is.EqualTo(new VariableStructure.DataRecord(expectedDataType, memberName, Utils.GetDefaultValue(expectedDataType)))
		);
	}

	[TestCase(nameof(SampleClass.SampleMethod))]
	[TestCase(nameof(SampleClass.SampleEvent))]
	public void ErrorTest_WithMemberInfo(string memberName)
	{
		Assert.Throws(
			typeof(ArgumentException),
			() => typeof(SampleClass).GetMember(memberName)[0].ToVariableDataRecord()
		);
	}
}
