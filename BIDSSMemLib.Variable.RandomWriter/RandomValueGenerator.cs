using System;
using System.Collections.Generic;

using BIDS.Parser.Variable;

namespace BIDSSMemLib.Variable.RandomWriter;

public class RandomValueGenerator
{
	public static RandomValueGenerator Default { get; } = new();

	public readonly Random Rand = new();

	public RandomValueGenerator() : this(new Random()) { }

	public RandomValueGenerator(int Seed) : this(new Random(Seed)) { }

	public RandomValueGenerator(Random rand)
	{
		this.Rand = rand;
	}

	public bool GetBool()
		=> Rand.Next(1) == 1;

	public Half GetFloat16()
		=> Half.Parse((Rand.NextSingle() * GetSign()).ToString());
	public float GetFloat32()
		=> Rand.NextSingle() * GetSign();
	public double GetFloat64()
		=> Rand.NextDouble() * GetSign();

	public sbyte GetSign()
		=> GetBool() ? (sbyte)1 : (sbyte)-1;

	const int FRACTION_MINMAX = 0x400;
	public double GetFraction()
	=> double.Parse($"1e{Rand.Next(-FRACTION_MINMAX, FRACTION_MINMAX)}");

	public sbyte GetInt8()
		=> (sbyte)Rand.Next(sbyte.MinValue, sbyte.MaxValue);
	public short GetInt16()
		=> (short)Rand.Next(short.MinValue, short.MaxValue);
	public int GetInt32()
		=> Rand.Next(int.MinValue, int.MaxValue);
	public long GetInt64()
		=> Rand.NextInt64(long.MinValue, long.MaxValue);

	public byte GetUInt8()
	=> (byte)Rand.Next(byte.MaxValue);
	public ushort GetUInt16()
		=> (ushort)Rand.Next(ushort.MaxValue);
	public uint GetUInt32()
		=> (uint)Rand.NextInt64(uint.MaxValue);
	public ulong GetUInt64()
		=> (((ulong)Rand.NextInt64()) << 1) + (GetBool() ? 1u : 0);

	public object Get(VariableDataType type)
		=> type switch
		{
			VariableDataType.Boolean => GetBool(),

			VariableDataType.Float16 => GetFloat16(),
			VariableDataType.Float32 => GetFloat32(),
			VariableDataType.Float64 => GetFloat64(),

			VariableDataType.Int8 => GetInt8(),
			VariableDataType.Int16 => GetInt16(),
			VariableDataType.Int32 => GetInt32(),
			VariableDataType.Int64 => GetInt64(),

			VariableDataType.UInt8 => GetUInt8(),
			VariableDataType.UInt16 => GetUInt16(),
			VariableDataType.UInt32 => GetUInt32(),
			VariableDataType.UInt64 => GetUInt64(),

			_ => throw new NotSupportedException($"The Type {type} is not supported")
		};

	public Array GetArray(in VariableDataType type)
		=> GetArray(type, GetUInt8());

	public Array GetArray(in VariableDataType type, in int length)
		=> type switch
		{
			VariableDataType.Boolean => GetArray(length, GetBool),

			VariableDataType.Float16 => GetArray(length, GetFloat16),
			VariableDataType.Float32 => GetArray(length, GetFloat32),
			VariableDataType.Float64 => GetArray(length, GetFloat64),

			VariableDataType.Int8 => GetArray(length, GetInt8),
			VariableDataType.Int16 => GetArray(length, GetInt16),
			VariableDataType.Int32 => GetArray(length, GetInt32),
			VariableDataType.Int64 => GetArray(length, GetInt64),

			VariableDataType.UInt8 => GetArray(length, GetUInt8),
			VariableDataType.UInt16 => GetArray(length, GetUInt16),
			VariableDataType.UInt32 => GetArray(length, GetUInt32),
			VariableDataType.UInt64 => GetArray(length, GetUInt64),

			_ => throw new NotSupportedException($"The Type {type} is not supported")
		};

	public T[] GetArray<T>(int length, Func<T> Generator)
	{
		T[] arr = new T[length];

		for (int i = 0; i < arr.Length; i++)
			arr[i] = Generator();

		return arr;
	}

	#region Variable Structure
	static readonly VariableDataType[] DataTypes = new VariableDataType[]
	{
		VariableDataType.Array,

		VariableDataType.Boolean,
		VariableDataType.Float16,
		VariableDataType.Float32,
		VariableDataType.Float64,

		VariableDataType.Int16,
		VariableDataType.Int32,
		VariableDataType.Int64,
		VariableDataType.Int8,

		VariableDataType.UInt16,
		VariableDataType.UInt32,
		VariableDataType.UInt64,
		VariableDataType.UInt8,
	};

	public VariableDataType GetDataType()
		=> DataTypes[Rand.Next(DataTypes.Length - 1)];

	const int STRUCTURE_ELEM_MAX_COUNT = 15;

	public VariableStructure GetRandomStructure(string name)
	{
		List<VariableStructure.IDataRecord> dataRecords = new();

		int elemCount = Rand.Next(STRUCTURE_ELEM_MAX_COUNT);

		HashSet<int> elemNameNums = new();

		for (int i = 0; i < elemCount; i++)
		{
			int elemNameNumber = GetInt32();
			while (elemNameNums.Contains(elemNameNumber))
				elemNameNumber = GetInt32();

			dataRecords[i] = GetDataRecordWithRandomValue($"{name}.{elemNameNumber}");
		}

		return new(GetInt32(), name, dataRecords);
	}

	#region GetDataRecord WithRandomValue
	public VariableStructure.IDataRecord GetDataRecordWithRandomValue(string name)
		=> GetDataRecordWithRandomValue(name, GetDataType());

	public VariableStructure.IDataRecord GetDataRecordWithRandomValue(string name, VariableDataType type)
		=> type == VariableDataType.Array ? GetArrayStructureWithRandomValue(name) : new VariableStructure.DataRecord(type, name, Get(type));

	public VariableStructure.ArrayStructure GetArrayStructureWithRandomValue(string name)
	{
		VariableDataType type = GetDataType();

		while (type == VariableDataType.Array)
			type = GetDataType();

		return GetArrayStructureWithRandomValue(name, type);
	}

	public VariableStructure.ArrayStructure GetArrayStructureWithRandomValue(string name, VariableDataType type)
	{
		if (type == VariableDataType.Array)
			throw new NotSupportedException("Nested Array is not Supported");

		return new VariableStructure.ArrayStructure(type, name, GetArray(type));
	}
	#endregion

	public VariableStructurePayload GetRandomPayload(in VariableStructure structure)
	{
		VariableStructurePayload payload = new(structure.DataTypeId);

		int modifyDecisionMaxValue = (structure.Records.Count / 2);

		foreach (var v in structure.Records)
		{
			if (Rand.Next(modifyDecisionMaxValue) != 0)
				continue;

			payload.Add(v.Name, WithRandomValue(v));
		}

		return payload;
	}

	#region IDataRecord WithRandomValue
	public VariableStructure.IDataRecord WithRandomValue(in VariableStructure.IDataRecord dataRecord)
		=> dataRecord switch
		{
			VariableStructure.DataRecord v => WithRandomValue(v),
			VariableStructure.ArrayStructure v => WithRandomValue(v),

			_ => throw new NotSupportedException($"The IDataRecord {dataRecord.GetType()} is not supported")
		};

	public VariableStructure.DataRecord WithRandomValue(in VariableStructure.DataRecord dataRecord)
		=> dataRecord with
		{
			Value = Get(dataRecord.Type)
		};

	public VariableStructure.ArrayStructure WithRandomValue(in VariableStructure.ArrayStructure dataRecord)
		=> dataRecord with
		{
			ValueArray = GetArray(dataRecord.ElemType)
		};
	#endregion

	#endregion
}
