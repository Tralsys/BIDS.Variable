using System;
using System.Linq;
using BIDS.Parser.Variable;

using NUnit.Framework;

namespace BIDS.Parser.Variable.Tests;

public class ParseDataTypeRegisterCommandTests
{
	[TestCase(0)]
	[TestCase(1)]
	[TestCase(8)]
	public void ArgumentErrorTests(int len)
	{
		byte[] arr = new byte[16];

		Assert.Throws<ArgumentException>(() => VariableCmdParser.ParseDataTypeRegisterCommand(arr.AsSpan()[..len]));
	}

	[TestCase(0, new byte[] {
		(byte)'b',
		(byte)'o',
		(byte)'o',
		(byte)'l',
	}, VariableDataType.Boolean, "bool")]

	[TestCase(1, new byte[] {
		(byte)'i',
		(byte)'n',
		(byte)'t',
		(byte)'8',
	}, VariableDataType.Int8, "int8")]
	[TestCase(2, new byte[] {
		(byte)'i',
		(byte)'n',
		(byte)'t',
		(byte)'1',
		(byte)'6',
	}, VariableDataType.Int16, "int16")]
	[TestCase(3, new byte[] {
		(byte)'i',
		(byte)'n',
		(byte)'t',
		(byte)'3',
		(byte)'2',
	}, VariableDataType.Int32, "int32")]
	[TestCase(4, new byte[] {
		(byte)'i',
		(byte)'n',
		(byte)'t',
		(byte)'6',
		(byte)'4',
	}, VariableDataType.Int64, "int64")]

	[TestCase(6, new byte[] {
		(byte)'u',
		(byte)'i',
		(byte)'n',
		(byte)'t',
		(byte)'8',
	}, VariableDataType.UInt8, "uint8")]
	[TestCase(7, new byte[] {
		(byte)'u',
		(byte)'i',
		(byte)'n',
		(byte)'t',
		(byte)'1',
		(byte)'6',
	}, VariableDataType.UInt16, "uint16")]
	[TestCase(8, new byte[] {
		(byte)'u',
		(byte)'i',
		(byte)'n',
		(byte)'t',
		(byte)'3',
		(byte)'2',
	}, VariableDataType.UInt32, "uint32")]
	[TestCase(9, new byte[] {
		(byte)'u',
		(byte)'i',
		(byte)'n',
		(byte)'t',
		(byte)'6',
		(byte)'4',
	}, VariableDataType.UInt64, "uint64")]

	[TestCase(12, new byte[] {
		(byte)'f',
		(byte)'l',
		(byte)'o',
		(byte)'a',
		(byte)'t',
		(byte)'1',
		(byte)'6',
	}, VariableDataType.Float16, "float16")]
	[TestCase(13, new byte[] {
		(byte)'f',
		(byte)'l',
		(byte)'o',
		(byte)'a',
		(byte)'t',
		(byte)'3',
		(byte)'2',
	}, VariableDataType.Float32, "float32")]
	[TestCase(14, new byte[] {
		(byte)'f',
		(byte)'l',
		(byte)'o',
		(byte)'a',
		(byte)'t',
		(byte)'6',
		(byte)'4',
	}, VariableDataType.Float64, "float64")]
	public void SingleFieldTest(int type, byte[] name, VariableDataType expectedDataType, string expectedName)
	{
		// Command Type + Structure(Structure Name + DataRecord(Data Type + name))
		byte[] arr = new byte[8 + 1 + name.Length];

		int i = 0;
		// Command Type: Register Structure
		foreach (var v in BitConverter.GetBytes(0))
			arr[i++] = v;
		// Structure Name (string.Empty)
		arr[i++] = 0;
		foreach (var v in BitConverter.GetBytes(type))
			arr[i++] = v;
		foreach (var v in name)
			arr[i++] = v;

		var actual = VariableCmdParser.ParseDataTypeRegisterCommand(arr.AsSpan());
		Assert.Multiple(() =>
		{
			Assert.That(actual.Name, Is.EqualTo(string.Empty));
			Assert.That(actual.Records, Has.Count.EqualTo(1));
		});
		Assert.That(actual.Records[0], Is.EqualTo(
			new VariableStructure.DataRecord(expectedDataType, expectedName, Utils.GetDefaultValue(expectedDataType)))
		);
	}

	[Test]
	public void SingleArrayFieldTest()
	{
		byte[] arr = new byte[]
		{
			// Command Type: Register Structure
			0,
			0,
			0,
			0,

			// Structure Name: string.Empty
			0,

			// IDataRecord.DataType: array
			17,
			0,
			0,
			0,

			// ArrayStructure.ElementDataType: Int32
			3,
			0,
			0,
			0,

			// IDataRecord.Name: "a"
			(byte)'a',
			(byte)'\0',
		};

		var actual = VariableCmdParser.ParseDataTypeRegisterCommand(arr.AsSpan());

		Assert.That(actual.Records, Has.Count.EqualTo(1));

		if (actual.Records[0] is not VariableStructure.ArrayDataRecord actualRecord)
		{
			Assert.Fail("Type missmatch (it was not ArrayDataRecord)");
			return;
		}

		Assert.Multiple(() =>
		{
			Assert.That(actual.Records[0], Is.EqualTo(
				new VariableStructure.ArrayDataRecord(VariableDataType.Int32, "a", actualRecord.ValueArray))
			);

			Assert.That(actualRecord.ValueArray, Is.EquivalentTo(Array.Empty<int>()));
		});
	}

	[Test]
	public void EmptyFieldNameTest()
	{
		byte[] arr =
		{
			// Command Type: Register Structure
			0,
			0,
			0,
			0,

			// Structure Name: string.Empty
			0,

			// IDataRecord.Type: Boolean
			0,
			0,
			0,
			0,

			// IDataRecord.Name: string.Empty
			0,
		};

		var actual = VariableCmdParser.ParseDataTypeRegisterCommand(arr.AsSpan());

		Assert.That(actual.Records, Has.Count.EqualTo(1));

		Assert.That(actual.Records[0], Is.EqualTo(
			new VariableStructure.DataRecord(VariableDataType.Boolean, "", false))
		);
	}

	[Test]
	public void MultiFieldTest()
	{
		static byte[] getBytes(int type, string name)
			=> BitConverter.GetBytes(type).Concat(System.Text.Encoding.UTF8.GetBytes(name)).Append((byte)0x00).ToArray();

		byte[] arr =
		{
			// Command Type: Register Structure
			0,
			0,
			0,
			0,

			// Structure Name: string.Empty
			0,
		};

		arr = arr
			.Concat(getBytes(0, "a"))

			.Concat(getBytes(1, "b"))
			.Concat(getBytes(2, "c"))
			.Concat(getBytes(3, "d"))
			.Concat(getBytes(4, "e"))

			.Concat(getBytes(6, "f"))
			.Concat(getBytes(7, "g"))
			.Concat(getBytes(8, "h"))
			.Concat(getBytes(9, "i"))

			// Arrayはテストが面倒なのでテストから除外する
			//.Concat(new byte[] { 17, 0, 0, 0 })
			//.Concat(getBytes(12, "array"))
#if NET5_0_OR_GREATER
			.Concat(getBytes(12, "j"))
#endif
			.Concat(getBytes(13, "k"))
			.Concat(getBytes(14, "l"))
			.ToArray();

		var actual = VariableCmdParser.ParseDataTypeRegisterCommand(arr.AsSpan());

		VariableStructure.IDataRecord[] expected = new VariableStructure.IDataRecord[]
		{
			new VariableStructure.DataRecord(VariableDataType.Boolean, "a", false),

			new VariableStructure.DataRecord(VariableDataType.Int8, "b", (sbyte)0),
			new VariableStructure.DataRecord(VariableDataType.Int16, "c", (short)0),
			new VariableStructure.DataRecord(VariableDataType.Int32, "d", (int)0),
			new VariableStructure.DataRecord(VariableDataType.Int64, "e", (long)0),

			new VariableStructure.DataRecord(VariableDataType.UInt8, "f", (byte)0),
			new VariableStructure.DataRecord(VariableDataType.UInt16, "g", (ushort)0),
			new VariableStructure.DataRecord(VariableDataType.UInt32, "h", (uint)0),
			new VariableStructure.DataRecord(VariableDataType.UInt64, "i", (ulong)0),

			//new VariableStructure.ArrayDataRecord(VariableDataType.Float16, "array", Array.Empty<Half>()),

#if NET5_0_OR_GREATER
			new VariableStructure.DataRecord(VariableDataType.Float16, "j", default(Half)),
#endif
			new VariableStructure.DataRecord(VariableDataType.Float32, "k", (float)0),
			new VariableStructure.DataRecord(VariableDataType.Float64, "l", (double)0),
		};

		Assert.That(actual.Records, Is.EquivalentTo(expected));
	}

	[Test]
	public void StructureNameTest()
	{
		static IEnumerable<byte> getBytes(int type, string name)
			=> BitConverter.GetBytes(type).Concat(System.Text.Encoding.UTF8.GetBytes(name)).Append((byte)0x00);
		byte[] arr =
		{
			// Command Type: Register Structure
			0,
			0,
			0,
			0,

			// Structure Name: "abcd"
			(byte)'a',
			(byte)'b',
			(byte)'c',
			(byte)'d',
			0,
		};

		arr = arr.Concat(getBytes(1, "a")).ToArray();

		var actual = VariableCmdParser.ParseDataTypeRegisterCommand(arr.AsSpan());

		Assert.That(actual.Name, Is.EqualTo("abcd"));
	}
}
