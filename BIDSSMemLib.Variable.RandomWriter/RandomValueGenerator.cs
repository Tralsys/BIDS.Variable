using System;

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
}
