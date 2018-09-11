using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using VTChain.Base.DataType;
using VTChain.Base.Extensions;

namespace VTChain.Base.IO
{

    public static class IOHelper
    {
        public static T AsSerializable<T>(this byte[] value, int start = 0) where T : ISerializable, new()
        {
            using (MemoryStream ms = new MemoryStream(value, start, value.Length - start, false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                return reader.ReadSerializable<T>();
            }
        }

        public static ISerializable AsSerializable(this byte[] value, Type type)
        {
            if (!typeof(ISerializable).GetTypeInfo().IsAssignableFrom(type))
                throw new InvalidCastException();
            ISerializable serializable = (ISerializable)Activator.CreateInstance(type);
            using (MemoryStream ms = new MemoryStream(value, false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                serializable.Deserialize(reader);
            }
            return serializable;
        }

        public static T[] AsSerializableArray<T>(this byte[] value, int max = 0x10000000) where T : ISerializable, new()
        {
            using (MemoryStream ms = new MemoryStream(value, false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                return reader.ReadSerializableArray<T>(max);
            }
        }

        public static int GetVarSize(int value)
        {
            if (value < 0xFD)
                return sizeof(byte);
            else if (value <= 0xFFFF)
                return sizeof(byte) + sizeof(ushort);
            else
                return sizeof(byte) + sizeof(uint);
        }

        public static int GetVarSize<T>(this T[] value)
        {
            int value_size;
            Type t = typeof(T);
            if (typeof(ISerializable).IsAssignableFrom(t))
            {
                value_size = value.OfType<ISerializable>().Sum(p => p.Size);
            }
            else if (t.GetTypeInfo().IsEnum)
            {
                int element_size;
                Type u = t.GetTypeInfo().GetEnumUnderlyingType();
                if (u == typeof(sbyte) || u == typeof(byte))
                    element_size = 1;
                else if (u == typeof(short) || u == typeof(ushort))
                    element_size = 2;
                else if (u == typeof(int) || u == typeof(uint))
                    element_size = 4;
                else //if (u == typeof(long) || u == typeof(ulong))
                    element_size = 8;
                value_size = value.Length * element_size;
            }
            else
            {
                value_size = value.Length * Marshal.SizeOf<T>();
            }
            return GetVarSize(value.Length) + value_size;
        }

        public static int GetVarSize(this string value)
        {
            int size = Encoding.UTF8.GetByteCount(value);
            return GetVarSize(size) + size;
        }


        #region BinaryReader

        public static byte[] ReadBytesWithGrouping(this BinaryReader reader)
        {
            const int GROUP_SIZE = 16;
            using (MemoryStream ms = new MemoryStream())
            {
                int padding = 0;
                do
                {
                    byte[] group = reader.ReadBytes(GROUP_SIZE);
                    padding = reader.ReadByte();
                    if (padding > GROUP_SIZE)
                        throw new FormatException();
                    int count = GROUP_SIZE - padding;
                    if (count > 0)
                        ms.Write(group, 0, count);
                } while (padding == 0);
                return ms.ToArray();
            }
        }

        public static string ReadFixedString(this BinaryReader reader, int length)
        {
            byte[] data = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(data.TakeWhile(p => p != 0).ToArray());
        }

        public static T ReadSerializable<T>(this BinaryReader reader) where T : ISerializable, new()
        {
            T obj = new T();
            obj.Deserialize(reader);
            return obj;
        }

        public static T[] ReadSerializableArray<T>(this BinaryReader reader, int max = 0x10000000) where T : ISerializable, new()
        {
            T[] array = new T[reader.ReadVarInt((ulong)max)];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = new T();
                array[i].Deserialize(reader);
            }
            return array;
        }

        public static byte[] ReadExactly(this BinaryReader reader, int count)
        {
            var buffer = new byte[count];
            if (count == 0)
                return buffer;

            if (count != reader.Read(buffer, 0, count))
                throw new InvalidOperationException();

            return buffer;
        }

        public static byte[] ReadVarBytesInt(this BinaryReader reader)
        {
            var length = reader.ReadVarInt().ToIntChecked();
            return reader.ReadExactly(length);
        }

        public static byte[] ReadVarBytes(this BinaryReader reader, int max = 0X7fffffc7)
        {
            return reader.ReadBytes((int)reader.ReadVarInt((ulong)max));
        }

        public static byte[] ReadVarBytesBuffer(this byte[] bytes,ref int offset)
        {
            int length = bytes.Length - offset;
            var encoded = new byte[length];
            Buffer.BlockCopy(bytes, offset, encoded, 0, length);

            return encoded;
        }

        public static ulong ReadVarInt(this BinaryReader reader, ulong max = ulong.MaxValue)
        {
            byte fb = reader.ReadByte();
            ulong value;
            if (fb == 0xFD)
                value = reader.ReadUInt16();
            else if (fb == 0xFE)
                value = reader.ReadUInt32();
            else if (fb == 0xFF)
                value = reader.ReadUInt64();
            else
                value = fb;
            if (value > max) throw new FormatException();
            return value;
        }

        public static string ReadVarString(this BinaryReader reader)
        {
            var rawBytes = reader.ReadVarBytes();
            return Encoding.ASCII.GetString(rawBytes);
        }

        public static string ReadVarStringASCII(this BinaryReader reader)
        {
            var rawBytes = reader.ReadVarBytesInt();
            return Encoding.ASCII.GetString(rawBytes);
        }

        public static string ReadVarString(this BinaryReader reader, int max = 0X7fffffc7)
        {
            return Encoding.UTF8.GetString(reader.ReadVarBytes(max));
        }

        public static bool ReadBool(this BinaryReader reader)
        {
            return reader.ReadByte() != 0;
        }

        public static UInt16 ReadUInt16BE(this BinaryReader reader)
        {
            using (var reverse = reader.ReverseRead(2))
            {
                 return reverse.ReadUInt16();
            }
               
        }

        public static UInt256 ReadUInt256(this BinaryReader reader)
        {
            return new UInt256(reader.ReadVarBytes(32));
        }

        public static UInt64 ReadVarInt(this BinaryReader reader)
        {
            var value = reader.ReadByte();
            if (value < 0xFD)
                return value;
            else if (value == 0xFD)
                return reader.ReadUInt16();
            else if (value == 0xFE)
                return reader.ReadUInt32();
            else if (value == 0xFF)
                return reader.ReadUInt64();
            else
                throw new Exception();
        }

        public static UInt64 ReadVarInt(this byte[] buffer, ref int offset)
        {
            UInt64 value = buffer[offset];
            offset += 1;

            if (value < 0xFD)
            {
                return value;
            }
            else if (value == 0xFD)
            {
                value = Bits.ToUInt16(buffer, offset);
                offset += 2;
                return value;
            }
            else if (value == 0xFE)
            {
                value = Bits.ToUInt32(buffer, offset);
                offset += 4;
                return value;
            }
            else if (value == 0xFF)
            {
                value = Bits.ToUInt64(buffer, offset);
                offset += 8;
                return value;
            }
            else
                throw new Exception();
        }

        public static ImmutableArray<byte> ReadVarBytesImmutable(this byte[] buffer, ref int offset)
        {
            var length = buffer.ReadVarInt(ref offset).ToIntChecked();

            var value = ImmutableArray.Create(buffer, offset, length);
            offset += length;

            return value;
        }

        public static ImmutableArray<T> ReadList<T>(this BinaryReader reader, Func<T> decode)
        {
            var length = reader.ReadVarInt().ToIntChecked();

            var list = ImmutableArray.CreateBuilder<T>(length);
            for (var i = 0; i < length; i++)
            {
                list.Add(decode());
            }

            return list.ToImmutable();
        }

        public static T[] ReadArray<T>(this BinaryReader reader, Func<T> decode)
        {
            var length = reader.ReadVarInt().ToIntChecked();

            var list = new T[length];
            for (var i = 0; i < length; i++)
            {
                list[i] = decode();
            }

            return list;
        }

        private static BinaryReader ReverseRead(this BinaryReader reader, int length)
        {
            var bytes = reader.ReadExactly(length);
            Array.Reverse(bytes);

            var stream = new MemoryStream(bytes);
            return new BinaryReader(stream, Encoding.ASCII, leaveOpen: false);
        }
        #endregion


        #region BinayWriter

        public static byte[] ToArray(this ISerializable value)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                value.Serialize(writer);
                writer.Flush();
                return ms.ToArray();
            }
        }

        public static byte[] ToByteArray<T>(this T[] value) where T : ISerializable
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                writer.Write(value);
                writer.Flush();
                return ms.ToArray();
            }
        }

        public static void Write(this BinaryWriter writer, ISerializable value)
        {
            value.Serialize(writer);
        }

        public static void Write<T>(this BinaryWriter writer, T[] value) where T : ISerializable
        {
            writer.WriteVarInt(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                value[i].Serialize(writer);
            }
        }

        public static void WriteBytesWithGrouping(this BinaryWriter writer, byte[] value)
        {
            const int GROUP_SIZE = 16;
            int index = 0;
            int remain = value.Length;
            while (remain >= GROUP_SIZE)
            {
                writer.Write(value, index, GROUP_SIZE);
                writer.Write((byte)0);
                index += GROUP_SIZE;
                remain -= GROUP_SIZE;
            }
            if (remain > 0)
                writer.Write(value, index, remain);
            int padding = GROUP_SIZE - remain;
            for (int i = 0; i < padding; i++)
                writer.Write((byte)0);
            writer.Write((byte)padding);
        }

        public static void WriteFixedString(this BinaryWriter writer, string value, int length)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (value.Length > length)
                throw new ArgumentException();
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length > length)
                throw new ArgumentException();
            writer.Write(bytes);
            if (bytes.Length < length)
                writer.Write(new byte[length - bytes.Length]);
        }

        public static void WriteVarBytes(this BinaryWriter writer, byte[] value)
        {
            writer.WriteVarInt(value.Length);
            writer.Write(value);
        }

        public static void WriteBytes(this BinaryWriter writer, byte[] value)
        {
            writer.Write(value);
        }

        public static void WriteBytesNoLength(this BinaryWriter writer,int length, byte[] value)
        {
            writer.Write(value);
        }

        public static void WriteBytes(this BinaryWriter writer, int length, byte[] value)
        {
            if (value.Length != length)
                throw new ArgumentException();

            writer.WriteVarBytes(value);
        }

        public static void WriteVarInt(this BinaryWriter writer, long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException();
            if (value < 0xFD)
            {
                writer.Write((byte)value);
            }
            else if (value <= 0xFFFF)
            {
                writer.Write((byte)0xFD);
                writer.Write((ushort)value);
            }
            else if (value <= 0xFFFFFFFF)
            {
                writer.Write((byte)0xFE);
                writer.Write((uint)value);
            }
            else
            {
                writer.Write((byte)0xFF);
                writer.Write(value);
            }
        }

        public static void WriteVarString(this BinaryWriter writer, string value)
        {
            var encoded = Encoding.ASCII.GetBytes(value);
            writer.WriteVarBytes(encoded);
        }

        public static void WriteBool(this BinaryWriter writer, bool value)
        {
            writer.Write((byte)(value ? 1 : 0));
        }

        public static void Write1Byte(this BinaryWriter writer, Byte value)
        {
            writer.Write(value);
        }

        public static void WriteUInt16(this BinaryWriter writer, UInt16 value)
        {
            writer.Write(value);
        }

        public static void WriteUInt16BE(this BinaryWriter writer, UInt16 value)
        {
            writer.ReverseWrite(2, reverseWriter => reverseWriter.WriteUInt16(value));
        }

        public static void WriteUInt32(this BinaryWriter writer, UInt32 value)
        {
            writer.Write(value);
        }

        public static void WriteInt32(this BinaryWriter writer, Int32 value)
        {
            writer.Write(value);
        }

        public static void WriteUInt64(this BinaryWriter writer, UInt64 value)
        {
            writer.Write(value);
        }

        public static void WriteInt64(this BinaryWriter writer, Int64 value)
        {
            writer.Write(value);
        }

        public static void WriteUInt256(this BinaryWriter writer, UInt256 value)
        {
            writer.Write(value.ToArray());
        }

        public static void WriteList<T>(this BinaryWriter writer, IReadOnlyList<T> list, Action<T> encode)
        {
            writer.WriteUInt64((UInt64)list.Count);

            for (var i = 0; i < list.Count; i++)
            {
                encode(list[i]);
            }
        }

        public static void WriteArray<T>(this BinaryWriter writer, T[] list, Action<T> encode)
        {
            writer.WriteUInt64((UInt64)list.Length);

            for (var i = 0; i < list.Length; i++)
            {
                encode(list[i]);
            }
        }

        private static void ReverseWrite(this BinaryWriter writer, int length, Action<BinaryWriter> write)
        {
            var bytes = new byte[length];
            using (var stream = new MemoryStream(bytes))
            using (var reverseWriter = new BinaryWriter(stream))
            {
                write(reverseWriter);

                // verify that the correct amount of bytes were writtern
                if (reverseWriter.BaseStream.Position != length)
                    throw new InvalidOperationException();
            }
            Array.Reverse(bytes);

            writer.WriteBytes(bytes);
        }

        public static void WriteVarStringUTF8(this BinaryWriter writer, string value)
        {
            writer.WriteVarBytes(Encoding.UTF8.GetBytes(value));
        }
        #endregion
    }
}
