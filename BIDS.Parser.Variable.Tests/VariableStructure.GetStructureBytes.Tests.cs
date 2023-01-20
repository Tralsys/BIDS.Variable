namespace BIDS.Parser.Variable.Tests;

public class VariableStructure_GetStructureBytesTest
{
	[Test]
	public void Test()
	{
		VariableStructure structure = new(259, "SampleDataClass", new List<VariableStructure.IDataRecord>()
		{
			new VariableStructure.DataRecord(VariableDataType.Boolean, "PL_AtsS_White"),
			new VariableStructure.DataRecord(VariableDataType.Boolean, "PL_AtsS_Red"),
		});

		Assert.That(structure.GetStructureBytes(), Is.EquivalentTo(new byte[]
		{
			// Structure ID (0x0103 = 259)
			0x03,
			0x01,
			0x00,
			0x00,

			// Structure Name
			(byte)'S',
			(byte)'a',
			(byte)'m',
			(byte)'p',
			(byte)'l',
			(byte)'e',
			(byte)'D',
			(byte)'a',
			(byte)'t',
			(byte)'a',
			(byte)'C',
			(byte)'l',
			(byte)'a',
			(byte)'s',
			(byte)'s',
			(byte)'\0',

			// 1st Data Type (Boolean)
			0x00,
			0x00,
			0x00,
			0x00,

			(byte)'P',
			(byte)'L',
			(byte)'_',
			(byte)'A',
			(byte)'t',
			(byte)'s',
			(byte)'S',
			(byte)'_',
			(byte)'W',
			(byte)'h',
			(byte)'i',
			(byte)'t',
			(byte)'e',
			(byte)'\0',

			// 2nd Data Type (Boolean)
			0x00,
			0x00,
			0x00,
			0x00,

			(byte)'P',
			(byte)'L',
			(byte)'_',
			(byte)'A',
			(byte)'t',
			(byte)'s',
			(byte)'S',
			(byte)'_',
			(byte)'R',
			(byte)'e',
			(byte)'d',
			(byte)'\0',
		}));
	}
}
