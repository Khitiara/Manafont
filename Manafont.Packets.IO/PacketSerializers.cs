using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Manafont.Packets.IO
{
    public static class PacketSerializers
    {
        public delegate void PacketSaver(object packet, Span<byte> memory);


        private static readonly Lazy<MethodInfo> LazyUnsafeWriteAligned = new Lazy<MethodInfo>(() =>
            typeof(Unsafe).GetMethod(nameof(Unsafe.WriteUnaligned))!);

        private static MethodInfo UnsafeWriteAligned => LazyUnsafeWriteAligned.Value;

        private static PacketSaver SaveNumeric(FieldInfo field) {
            Type fieldType = field.FieldType;
            int offset = field.GetOffset().ToInt32(),
                size = Marshal.SizeOf(fieldType);
            return (pkt, mem) => {
                Span<byte> slice = mem.Slice(offset, size);
                if (BitConverter.IsLittleEndian)
                    slice.Reverse();
                UnsafeWriteAligned.MakeGenericMethod(fieldType)
                    .Invoke(null, new[] {MemoryMarshal.GetReference(slice), field.GetValue(pkt)!});
                if (BitConverter.IsLittleEndian)
                    slice.Reverse(); // reverse it back
            };
        }

        private static PacketSaver SaveDateTime(FieldInfo field) {
            Type fieldType = field.FieldType;
            int offset = field.GetOffset().ToInt32(),
                size = Marshal.SizeOf(fieldType);
            return (pkt, mem) => {
                Span<byte> slice = mem.Slice(offset, size);
                if (BitConverter.IsLittleEndian)
                    slice.Reverse();
                DateTime value = (DateTime) field.GetValue(pkt)!;
                UnsafeWriteAligned.MakeGenericMethod(fieldType)
                    .Invoke(null, new object[] {MemoryMarshal.GetReference(slice), value.ToBinary()});
                if (BitConverter.IsLittleEndian)
                    slice.Reverse(); // reverse it back
            };
        }


        private static PacketSaver SaveString(FieldInfo field) {
            MarshalAsAttribute marshalAsAttribute = field.GetCustomAttribute<MarshalAsAttribute>()!;
            (Encoding encoding, int charWidth) = field.DeclaringType?.StructLayoutAttribute?.CharSet switch {
                CharSet.None => throw new InvalidOperationException("Cannot have no set charset"),
                CharSet.Ansi => (Encoding.Default, 1),
                CharSet.Unicode => (Encoding.BigEndianUnicode, 2),
                CharSet.Auto => (Encoding.Default, 1),
                _ => throw new InvalidOperationException("Null or invalid charset")
            };
            int size = charWidth * marshalAsAttribute.SizeConst;
            return (pkt, mem) => {
                Span<byte> slice = mem.Slice(field.GetOffset().ToInt32(), size);
                string value = (string) field.GetValue(pkt)!;
                encoding.GetBytes(value, slice);
            };
        }

        private static PacketSaver LoadField(FieldInfo field) {
            Type fieldType = field.FieldType;
            switch (Type.GetTypeCode(fieldType)) {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Boolean:
                case TypeCode.Single:
                case TypeCode.Char:
                    return SaveNumeric(field);
                case TypeCode.String:
                    return SaveString(field);
                case TypeCode.DateTime:
                    return SaveDateTime(field);
                case TypeCode.Object:
                    throw new NotImplementedException();
                default:
                    throw new InvalidOperationException($"Cannot marshal {fieldType.FullName}");
            }
        }

        public static PacketSaver CompileSerializer<T>()
            where T : unmanaged =>
            CompileSerializer(typeof(T));

        public static PacketSaver CompileSerializer(Type type) {
            if (!(type.IsLayoutSequential || type.IsExplicitLayout) || !type.IsValueType)
                throw new InvalidOperationException($"Cant marshal {type.FullName}");

            return type
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(LoadField).Aggregate((a, b) => a + b);
        }


        private static readonly Dictionary<Type, PacketSaver> Savers =
            new Dictionary<Type, PacketSaver>();

        public static PacketSaver GetSaver(Type type) {
            if (Savers.ContainsKey(type))
                return Savers[type]!;

            PacketSaver saver = CompileSerializer(type);
            Savers[type] = saver;
            return saver;
        }
    }
}