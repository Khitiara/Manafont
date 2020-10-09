using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Manafont.Packets.IO
{
    internal static class PacketDeserializers
    {
        private delegate void PacketFieldLoader<TPacket>(ref TPacket packet, Span<byte> memory);

        public delegate object PacketLoader(Span<byte> memory);

        private static readonly Lazy<MethodInfo> LazyUnsafeReadAligned = new Lazy<MethodInfo>(() =>
            typeof(Unsafe).GetMethod(nameof(Unsafe.ReadUnaligned))!);

        private static MethodInfo UnsafeReadAligned => LazyUnsafeReadAligned.Value;

        private static readonly Dictionary<Type, PacketLoader> Loaders =
            new Dictionary<Type, PacketLoader>();

        public static PacketLoader GetLoader(Type type) {
            if (Loaders.ContainsKey(type))
                return (Loaders[type])!;
            PacketLoader loader = CompileDeserializer(type);
            Loaders[type] = loader;
            return loader;
        }

        private static PacketFieldLoader<T> LoadNumeric<T>(FieldInfo field)
            where T : unmanaged {
            Type fieldType = field.FieldType;
            int offset = field.GetOffset().ToInt32(),
                size = Marshal.SizeOf(fieldType);
            return (ref T pkt, Span<byte> mem) => {
                Span<byte> slice = mem.Slice(offset, size);
                if (BitConverter.IsLittleEndian)
                    slice.Reverse();
                object? num = UnsafeReadAligned.MakeGenericMethod(fieldType)
                    .Invoke(null, new object[] {MemoryMarshal.GetReference(slice)});
                field.SetValue(pkt,
                    num);
                if (BitConverter.IsLittleEndian)
                    slice.Reverse(); // reverse it back
            };
        }

        private static PacketFieldLoader<T> LoadDateTime<T>(FieldInfo field)
            where T : unmanaged {
            int offset = field.GetOffset().ToInt32();
            return (ref T pkt, Span<byte> mem) => {
                Span<byte> slice = mem.Slice(offset, 8);
                if (BitConverter.IsLittleEndian)
                    slice.Reverse();
                long num = (long) UnsafeReadAligned.MakeGenericMethod(typeof(long))
                    .Invoke(null, new object[] {MemoryMarshal.GetReference(slice)})!;
                field.SetValue(pkt,
                    DateTime.FromBinary(num));
                if (BitConverter.IsLittleEndian)
                    slice.Reverse(); // reverse it back
            };
        }

        private static PacketFieldLoader<T> LoadString<T>(FieldInfo field)
            where T : unmanaged {
            MarshalAsAttribute marshalAsAttribute = field.GetCustomAttribute<MarshalAsAttribute>()!;
            (Encoding encoding, int charWidth) = field.DeclaringType?.StructLayoutAttribute?.CharSet switch {
                CharSet.None => throw new InvalidOperationException("Cannot have no set charset"),
                CharSet.Ansi => (Encoding.Default, 1),
                CharSet.Unicode => (Encoding.BigEndianUnicode, 2),
                CharSet.Auto => (Encoding.Default, 1),
                _ => throw new InvalidOperationException("Null or invalid charset")
            };
            int size = charWidth * marshalAsAttribute.SizeConst;
            return (ref T pkt, Span<byte> mem) => {
                Span<byte> slice = mem.Slice(field.GetOffset().ToInt32(), size);
                field.SetValue(pkt, encoding.GetString(slice));
            };
        }

        private static PacketFieldLoader<T> LoadField<T>(FieldInfo field)
            where T : unmanaged {
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
                    return LoadNumeric<T>(field);
                case TypeCode.String:
                    return LoadString<T>(field);
                case TypeCode.DateTime:
                    return LoadDateTime<T>(field);
                case TypeCode.Object:
                    throw new NotImplementedException();
                default:
                    throw new InvalidOperationException($"Cannot marshal {fieldType.FullName}");
            }
        }

        public static PacketLoader CompileDeserializer<T>()
            where T : unmanaged {
            Type type = typeof(T);
            if (!(type.IsLayoutSequential || type.IsExplicitLayout) || !type.IsValueType)
                throw new InvalidOperationException($"Cant marshal {type.FullName}");

            PacketFieldLoader<T> fieldLoaders = type
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(LoadField<T>).Aggregate((a, b) => a + b);

            return mem => {
                T pkt = new T();
                fieldLoaders(ref pkt, mem);
                return pkt;
            };
        }

        public static PacketLoader CompileDeserializer(Type type) =>
            typeof(PacketDeserializers).GetMethod(nameof(CompileDeserializer), Array.Empty<Type>())
                ?.MakeGenericMethod(type).Invoke(null, null) as PacketLoader ??
            throw new InvalidOperationException("What");
    }
}