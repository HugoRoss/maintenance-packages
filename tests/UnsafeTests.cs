// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.CompilerServices
{
    public class UnsafeTests
    {
        [Fact]
        public static unsafe void ReadInt32()
        {
            int expected = 10;
            void* address = Unsafe.AsPointer(ref expected);
            int ret = Unsafe.Read<int>(address);
            Assert.Equal(expected, ret);
        }

        [Fact]
        public static unsafe void WriteInt32()
        {
            int value = 10;
            int* address = (int*)Unsafe.AsPointer(ref value);
            int expected = 20;
            Unsafe.Write(address, expected);

            Assert.Equal(expected, value);
            Assert.Equal(expected, *address);
            Assert.Equal(expected, Unsafe.Read<int>(address));
        }

        [Fact]
        public static unsafe void WriteBytesIntoInt32()
        {
            int value = 20;
            int* intAddress = (int*)Unsafe.AsPointer(ref value);
            byte* byteAddress = (byte*)intAddress;
            for (int i = 0; i < 4; i++)
            {
                Unsafe.Write(byteAddress + i, (byte)i);
            }

            Assert.Equal(0, Unsafe.Read<byte>(byteAddress));
            Assert.Equal(1, Unsafe.Read<byte>(byteAddress + 1));
            Assert.Equal(2, Unsafe.Read<byte>(byteAddress + 2));
            Assert.Equal(3, Unsafe.Read<byte>(byteAddress + 3));

            Byte4 b4 = Unsafe.Read<Byte4>(byteAddress);
            Assert.Equal(0, b4.B0);
            Assert.Equal(1, b4.B1);
            Assert.Equal(2, b4.B2);
            Assert.Equal(3, b4.B3);

            int expected = (b4.B3 << 24) + (b4.B2 << 16) + (b4.B1 << 8) + (b4.B0);
            Assert.Equal(expected, value);
        }

        [Fact]
        public static unsafe void LongIntoCompoundStruct()
        {
            long value = 1234567891011121314L;
            long* longAddress = (long*)Unsafe.AsPointer(ref value);
            Byte4Short2 b4s2 = Unsafe.Read<Byte4Short2>(longAddress);
            Assert.Equal(162, b4s2.B0);
            Assert.Equal(48, b4s2.B1);
            Assert.Equal(210, b4s2.B2);
            Assert.Equal(178, b4s2.B3);
            Assert.Equal(4340, b4s2.S4);
            Assert.Equal(4386, b4s2.S6);

            b4s2.B0 = 1;
            b4s2.B1 = 1;
            b4s2.B2 = 1;
            b4s2.B3 = 1;
            b4s2.S4 = 1;
            b4s2.S6 = 1;
            Unsafe.Write(longAddress, b4s2);

            long expected = 281479288520961;
            Assert.Equal(expected, value);
            Assert.Equal(expected, Unsafe.Read<long>(longAddress));
        }

        [Fact]
        public static unsafe void ReadWriteDoublePointer()
        {
            int value1 = 10;
            int value2 = 20;
            int* valueAddress = (int*)Unsafe.AsPointer(ref value1);
            int** valueAddressPtr = &valueAddress;
            Unsafe.Write(valueAddressPtr, new IntPtr(&value2));

            Assert.Equal(20, *(*valueAddressPtr));
            Assert.Equal(20, Unsafe.Read<int>(valueAddress));
            Assert.Equal(new IntPtr(valueAddress), Unsafe.Read<IntPtr>(valueAddressPtr));
            Assert.Equal(20, Unsafe.Read<int>(Unsafe.Read<IntPtr>(valueAddressPtr).ToPointer()));
        }

        [Fact]
        public static unsafe void CopyToRef()
        {
            int value = 10;
            int destination = -1;
            Unsafe.Copy(ref destination, Unsafe.AsPointer(ref value));
            Assert.Equal(10, destination);
            Assert.Equal(10, value);

            int destination2 = -1;
            Unsafe.Copy(ref destination2, &value);
            Assert.Equal(10, destination2);
            Assert.Equal(10, value);
        }

        [Fact]
        public static unsafe void CopyToVoidPtr()
        {
            int value = 10;
            int destination = -1;
            Unsafe.Copy(Unsafe.AsPointer(ref destination), ref value);
            Assert.Equal(10, destination);
            Assert.Equal(10, value);

            int destination2 = -1;
            Unsafe.Copy(&destination2, ref value);
            Assert.Equal(10, destination2);
            Assert.Equal(10, value);
        }

        [Theory]
        [MemberData(nameof(SizeOfData))]
        public static unsafe void SizeOf<T>(int expected, T valueUnused)
        {
            // valueUnused is only present to enable Xunit to call the correct generic overload.
            Assert.Equal(expected, Unsafe.SizeOf<T>());
        }

        public static IEnumerable<object[]> SizeOfData()
        {
            yield return new object[] { 1, new sbyte() };
            yield return new object[] { 1, new byte() };
            yield return new object[] { 2, new short() };
            yield return new object[] { 2, new ushort() };
            yield return new object[] { 4, new int() };
            yield return new object[] { 4, new uint() };
            yield return new object[] { 8, new long() };
            yield return new object[] { 8, new ulong() };
            yield return new object[] { 4, new float() };
            yield return new object[] { 8, new double() };
            yield return new object[] { 4, new Byte4() };
            yield return new object[] { 8, new Byte4Short2() };
            yield return new object[] { 512, new Byte512() };
        }

        [Theory]
        [MemberData(nameof(InitBlockData))]
        public static unsafe void InitBlockStack(int numBytes, byte value)
        {
            byte* stackPtr = stackalloc byte[numBytes];
            Unsafe.InitBlock(stackPtr, value, (uint)numBytes);
            for (int i = 0; i < numBytes; i++)
            {
                Assert.Equal(stackPtr[i], value);
            }
        }

        [Theory]
        [MemberData(nameof(InitBlockData))]
        public static unsafe void InitBlockUnmanaged(int numBytes, byte value)
        {
            IntPtr allocatedMemory = PInvokeMarshal.AllocateMemory(numBytes);
            byte* bytePtr = (byte*)allocatedMemory.ToPointer();
            Unsafe.InitBlock(bytePtr, value, (uint)numBytes);
            for (int i = 0; i < numBytes; i++)
            {
                Assert.Equal(bytePtr[i], value);
            }
        }

        public static IEnumerable<object[]> InitBlockData()
        {
            yield return new object[] { 0, 1 };
            yield return new object[] { 1, 1 };
            yield return new object[] { 10, 0 };
            yield return new object[] { 10, 2 };
            yield return new object[] { 10, 255 };
            yield return new object[] { 10000, 255 };
        }

        [Theory]
        [MemberData(nameof(CopyBlockData))]
        public static unsafe void CopyBlock(int numBytes)
        {
            byte* source = stackalloc byte[numBytes];
            byte* destination = stackalloc byte[numBytes];

            for (int i = 0; i < numBytes; i++)
            {
                byte value = (byte)(i % 255);
                source[i] = value;
            }

            Unsafe.CopyBlock(destination, source, (uint)numBytes);

            for (int i = 0; i < numBytes; i++)
            {
                byte value = (byte)(i % 255);
                Assert.Equal(value, destination[i]);
                Assert.Equal(source[i], destination[i]);
            }
        }

        public static IEnumerable<object[]> CopyBlockData()
        {
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { 10 };
            yield return new object[] { 100 };
            yield return new object[] { 100000 };
        }

        [Fact]
        public static void As()
        {
            object o = "Hello";
            Assert.Equal("Hello", Unsafe.As<string>(o));
        }

        [Fact]
        public static void DangerousAs()
        {
            // Verify that As does not perform type checks
            object o = new Object();
            Assert.IsType(typeof(Object), Unsafe.As<string>(o));
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Byte4
    {
        [FieldOffset(0)]
        public byte B0;
        [FieldOffset(1)]
        public byte B1;
        [FieldOffset(2)]
        public byte B2;
        [FieldOffset(3)]
        public byte B3;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Byte4Short2
    {
        [FieldOffset(0)]
        public byte B0;
        [FieldOffset(1)]
        public byte B1;
        [FieldOffset(2)]
        public byte B2;
        [FieldOffset(3)]
        public byte B3;
        [FieldOffset(4)]
        public short S4;
        [FieldOffset(6)]
        public short S6;
    }

    public unsafe struct Byte512
    {
        public fixed byte Bytes[512];
    }
}
