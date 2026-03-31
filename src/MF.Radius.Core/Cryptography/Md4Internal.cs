using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace MF.Radius.Core.Cryptography;

/// <summary>
/// Provides an internal implementation of the MD4 cryptographic hash algorithm, as defined in RFC 1320.
/// This class is used internally for hashing operations and is not intended for external use.
/// </summary>
internal static class Md4Internal
{
    /// <summary>
    /// Computes the MD4 hash of the input data and writes the result to the specified destination buffer.
    /// </summary>
    /// <param name="data">The input data to be hashed.</param>
    /// <param name="destination">The buffer where the computed hash will be written. Must be at least 16 bytes long.</param>
    /// <exception cref="ArgumentException">Thrown when the destination buffer is not large enough to hold the hash.</exception>
    public static void HashData(ReadOnlySpan<byte> data, Span<byte> destination)
    {
        // 1. Инициализация переменных (RFC 1320)
        uint a = 0x67452301;
        uint b = 0xefcdab89;
        uint c = 0x98badcfe;
        uint d = 0x10325476;

        // 2. Padding (Дополнение)
        // Рассчитываем длину: данные + 1 байт (0x80) + padding + 8 байт (длина)
        int dataLen = data.Length;
        int tail = dataLen % 64;
        int paddingLen = (tail < 56) ? (56 - tail) : (120 - tail);
        int totalLen = dataLen + paddingLen + 8;

        byte[] paddedData = new byte[totalLen];
        data.CopyTo(paddedData);
        
        // Добавляем один бит '1' (байт 0x80)
        paddedData[dataLen] = 0x80;
        
        // Добавляем длину сообщения в битах (64-битное число, Little-Endian)
        BinaryPrimitives.WriteUInt64LittleEndian(paddedData.AsSpan(totalLen - 8), (ulong)dataLen * 8);

        // 3. Processing (Обработка)
        Span<uint> x = stackalloc uint[16];
        for (int i = 0; i < totalLen; i += 64)
        {
            var block = paddedData.AsSpan(i, 64);
            for (int j = 0; j < 16; j++)
                x[j] = BinaryPrimitives.ReadUInt32LittleEndian(block.Slice(j * 4, 4));

            uint aa = a, bb = b, cc = c, dd = d;

            // Round 1
            a = Ff(a, b, c, d, x[0], 3);  d = Ff(d, a, b, c, x[1], 7);
            c = Ff(c, d, a, b, x[2], 11); b = Ff(b, c, d, a, x[3], 19);
            a = Ff(a, b, c, d, x[4], 3);  d = Ff(d, a, b, c, x[5], 7);
            c = Ff(c, d, a, b, x[6], 11); b = Ff(b, c, d, a, x[7], 19);
            a = Ff(a, b, c, d, x[8], 3);  d = Ff(d, a, b, c, x[9], 7);
            c = Ff(c, d, a, b, x[10], 11); b = Ff(b, c, d, a, x[11], 19);
            a = Ff(a, b, c, d, x[12], 3); d = Ff(d, a, b, c, x[13], 7);
            c = Ff(c, d, a, b, x[14], 11); b = Ff(b, c, d, a, x[15], 19);

            // Round 2
            a = Gg(a, b, c, d, x[0], 3);  d = Gg(d, a, b, c, x[4], 5);
            c = Gg(c, d, a, b, x[8], 9);  b = Gg(b, c, d, a, x[12], 13);
            a = Gg(a, b, c, d, x[1], 3);  d = Gg(d, a, b, c, x[5], 5);
            c = Gg(c, d, a, b, x[9], 9);  b = Gg(b, c, d, a, x[13], 13);
            a = Gg(a, b, c, d, x[2], 3);  d = Gg(d, a, b, c, x[6], 5);
            c = Gg(c, d, a, b, x[10], 9); b = Gg(b, c, d, a, x[14], 13);
            a = Gg(a, b, c, d, x[3], 3);  d = Gg(d, a, b, c, x[7], 5);
            c = Gg(c, d, a, b, x[11], 9); b = Gg(b, c, d, a, x[15], 13);

            // Round 3
            a = Hh(a, b, c, d, x[0], 3);  d = Hh(d, a, b, c, x[8], 9);
            c = Hh(c, d, a, b, x[4], 11); b = Hh(b, c, d, a, x[12], 15);
            a = Hh(a, b, c, d, x[2], 3);  d = Hh(d, a, b, c, x[10], 9);
            c = Hh(c, d, a, b, x[6], 11); b = Hh(b, c, d, a, x[14], 15);
            a = Hh(a, b, c, d, x[1], 3);  d = Hh(d, a, b, c, x[9], 9);
            c = Hh(c, d, a, b, x[5], 11); b = Hh(b, c, d, a, x[13], 15);
            a = Hh(a, b, c, d, x[3], 3);  d = Hh(d, a, b, c, x[11], 9);
            c = Hh(c, d, a, b, x[7], 11); b = Hh(b, c, d, a, x[15], 15);

            a += aa; b += bb; c += cc; d += dd;
        }

        // 4. Result
        BinaryPrimitives.WriteUInt32LittleEndian(destination[..4], a);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(4, 4), b);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(8, 4), c);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(12, 4), d);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Ff(uint a, uint b, uint c, uint d, uint x, int s) 
        => BitOperations.RotateLeft(a + ((b & c) | (~b & d)) + x, s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Gg(uint a, uint b, uint c, uint d, uint x, int s) 
        => BitOperations.RotateLeft(a + ((b & c) | (b & d) | (c & d)) + x + 0x5a827999, s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Hh(uint a, uint b, uint c, uint d, uint x, int s) 
        => BitOperations.RotateLeft(a + (b ^ c ^ d) + x + 0x6ed9eba1, s);
    
}
