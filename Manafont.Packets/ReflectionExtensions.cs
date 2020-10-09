using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Manafont.Packets
{
    internal static class ReflectionExtensions
    {
        public delegate void PacketFieldLoader<TPacket>(ref TPacket packet, Span<byte> memory);

        public delegate object PacketLoader(Span<byte> memory);

        public delegate void PacketSerializer<in TPacket>(TPacket packet, Span<byte> memory);

        private static readonly Lazy<MethodInfo> LazyUnsafeReadAligned = new Lazy<MethodInfo>(() =>
            typeof(Unsafe).GetMethod(nameof(Unsafe.ReadUnaligned))!);

        private static MethodInfo UnsafeReadAligned => LazyUnsafeReadAligned.Value;

        private static readonly Dictionary<Type, PacketLoader> Loaders =
            new Dictionary<Type, PacketLoader>();

        public static PacketLoader GetLoader(Type type){
            if (Loaders.ContainsKey(type))
                return (Loaders[type])!;
            PacketLoader loader = CompileDeserializer(type);
            Loaders[type] = loader;
            return loader;
        }

        private static void LoadNumeric<T>(ref T pkt, Span<byte> mem, FieldInfo field)
            where T : unmanaged {
            Type fieldType = field.FieldType;
            int offset = field.GetOffset().ToInt32(),
                size = Marshal.SizeOf(fieldType);
            Span<byte> slice = mem.Slice(offset, size);
            if (BitConverter.IsLittleEndian)
                slice.Reverse();
            object? num = UnsafeReadAligned.MakeGenericMethod(fieldType)
                .Invoke(null, new object[] {MemoryMarshal.GetReference(slice)});
            field.SetValue(pkt,
                num);
            if (BitConverter.IsLittleEndian)
                slice.Reverse(); // reverse it back
        }

        private static void LoadDateTime<T>(ref T pkt, Span<byte> mem, FieldInfo field)
            where T : unmanaged {
            int offset = field.GetOffset().ToInt32();
            Span<byte> slice = mem.Slice(offset, 8);
            if (BitConverter.IsLittleEndian)
                slice.Reverse();
            long num = (long) UnsafeReadAligned.MakeGenericMethod(typeof(long))
                .Invoke(null, new object[] {MemoryMarshal.GetReference(slice)})!;
            field.SetValue(pkt,
                DateTime.FromBinary(num));
            if (BitConverter.IsLittleEndian)
                slice.Reverse(); // reverse it back
        }

        private static void LoadString<T>(ref T pkt, Span<byte> mem, FieldInfo field)
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
            Span<byte> slice = mem.Slice(field.GetOffset().ToInt32(), size);
            field.SetValue(pkt, encoding.GetString(slice));
        }

        internal static PacketFieldLoader<T> LoadField<T>(FieldInfo field)
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
                    return (ref T pkt, Span<byte> mem) => LoadNumeric(ref pkt, mem, field);
                case TypeCode.String:
                    return (ref T pkt, Span<byte> mem) => LoadString(ref pkt, mem, field);
                case TypeCode.DateTime:
                    return (ref T pkt, Span<byte> mem) => LoadDateTime(ref pkt, mem, field);
                case TypeCode.Object:
                    throw new NotImplementedException();
                default:
                    throw new InvalidOperationException($"Cannot marshal {fieldType.FullName}");
            }
        }

        public static PacketLoader CompileDeserializer(Type type) {
            if (!(type.IsLayoutSequential || type.IsExplicitLayout))
                throw new InvalidOperationException($"Cant marshal {type.FullName}");
            throw new NotImplementedException();
        }

        private static IntPtr GetOffset(this MemberInfo member) {
            return Marshal.OffsetOf(member.DeclaringType!, member.Name);
        }
    }
}