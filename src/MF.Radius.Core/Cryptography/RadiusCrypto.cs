using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;
using System.Text;

namespace MF.Radius.Core.Cryptography;

/// <summary>
/// Provides general cryptographic operations for the RADIUS protocol.
/// </summary>
internal static class RadiusCrypto
{
    private const int Md5HashSize = 16;
    private const int BlockSize = 16;
    private const int AuthenticatorSize = 16;

    /// <summary>
    /// Decrypts a PAP password using the Shared Secret and Request Authenticator.
    /// Implementation according to RFC 2865.
    /// </summary>
    public static string DecryptPapPassword(ReadOnlySpan<byte> encrypted, ReadOnlySpan<byte> authenticator, string sharedSecret)
    {
        if (encrypted.Length == 0 || encrypted.Length % BlockSize != 0)
            throw new ArgumentException($"Encrypted password length must be a multiple of {BlockSize}", nameof(encrypted));

        if (authenticator.Length != AuthenticatorSize)
            throw new ArgumentException($"Authenticator must be exactly {AuthenticatorSize} bytes", nameof(authenticator));

        var decrypted = encrypted.Length <= 512
            ? stackalloc byte[encrypted.Length]
            : new byte[encrypted.Length];
        
        byte[] secretBytes = Encoding.UTF8.GetBytes(sharedSecret);
        Span<byte> hashInput = stackalloc byte[secretBytes.Length + BlockSize];
        secretBytes.CopyTo(hashInput);
        
        Span<byte> hash = stackalloc byte[Md5HashSize];
        var previousBlock = authenticator;

        for (var i = 0; i < encrypted.Length; i += BlockSize)
        {
            var currentBlock = encrypted.Slice(i, BlockSize);
            previousBlock.CopyTo(hashInput[secretBytes.Length..]);
            MD5.HashData(hashInput, hash);

            var vEnc = Vector128.Create(currentBlock);
            var vHash = Vector128.Create(hash);
            (vEnc ^ vHash).CopyTo(decrypted.Slice(i, BlockSize));

            previousBlock = currentBlock;
        }

        return Encoding.UTF8.GetString(decrypted).TrimEnd('\0');
    }

    /// <summary>
    /// Validates a standard CHAP (MD5) response.
    /// Implementation according to RFC 1994.
    /// </summary>
    public static bool ValidateChapResponse(
        byte chapId, 
        string storedPassword, 
        ReadOnlySpan<byte> challenge, 
        ReadOnlySpan<byte> response
    )
    {
        var pwdBytes = Encoding.UTF8.GetBytes(storedPassword);
        
        Span<byte> buffer = stackalloc byte[1 + pwdBytes.Length + challenge.Length];
        buffer[0] = chapId;
        pwdBytes.CopyTo(buffer[1..]);
        challenge.CopyTo(buffer[(1 + pwdBytes.Length)..]);

        Span<byte> hash = stackalloc byte[Md5HashSize];
        MD5.HashData(buffer, hash);
        
        return CryptographicOperations.FixedTimeEquals(hash, response);
    }

    /// <summary>
    /// Generates a Response Authenticator for Access-Accept/Reject/Challenge packets.
    /// </summary>
    public static void GenerateResponseAuthenticator(
        byte code,
        byte id,
        ushort length,
        ReadOnlySpan<byte> requestAuth,
        ReadOnlySpan<byte> attributesPart,
        string sharedSecret,
        Span<byte> destination
    )
    {
        var secretBytes = Encoding.ASCII.GetBytes(sharedSecret);
        var totalLen = 4 + AuthenticatorSize + attributesPart.Length + secretBytes.Length;

        var buffer = ArrayPool<byte>.Shared.Rent(totalLen);
        try
        {
            buffer[0] = code;
            buffer[1] = id;
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2, 2), length);
            requestAuth.CopyTo(buffer.AsSpan(4, 16));
            attributesPart.CopyTo(buffer.AsSpan(20, attributesPart.Length));
            secretBytes.CopyTo(buffer.AsSpan(20 + attributesPart.Length, secretBytes.Length));

            MD5.HashData(buffer.AsSpan(0, totalLen), destination);
        }
        finally { ArrayPool<byte>.Shared.Return(buffer); }
    }
    
    /// <summary>
    /// Generates a Request Authenticator for Accounting-Request/CoA-Request/Disconnect-Request.
    /// Formula: MD5(Code + Identifier + Length + 16x00 + Attributes + SharedSecret).
    /// </summary>
    public static void GenerateRequestAuthenticator(
        byte code,
        byte id,
        ushort length,
        ReadOnlySpan<byte> attributesPart,
        string sharedSecret,
        Span<byte> destination
    )
    {
        if (destination.Length < AuthenticatorSize)
            throw new ArgumentException($"Destination must be at least {AuthenticatorSize} bytes.", nameof(destination));
    
        var secretBytes = Encoding.ASCII.GetBytes(sharedSecret);
        var totalLen = 4 + AuthenticatorSize + attributesPart.Length + secretBytes.Length;
    
        var buffer = ArrayPool<byte>.Shared.Rent(totalLen);
        try
        {
            buffer[0] = code;
            buffer[1] = id;
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2, 2), length);
    
            // Request Authenticator field is zeroed during hash computation.
            buffer.AsSpan(4, AuthenticatorSize).Clear();
    
            attributesPart.CopyTo(buffer.AsSpan(20, attributesPart.Length));
            secretBytes.CopyTo(buffer.AsSpan(20 + attributesPart.Length, secretBytes.Length));
    
            MD5.HashData(buffer.AsSpan(0, totalLen), destination);
            
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
    

    /// <summary>
    /// Validates the Message-Authenticator attribute (Type 80) using HMAC-MD5.
    /// Implementation according to RFC 2869.
    /// </summary>
    public static bool ValidateMessageAuthenticator(ReadOnlyMemory<byte> packetData, string sharedSecret)
    {
        var span = packetData.Span;
        var length = BinaryPrimitives.ReadUInt16BigEndian(span[2..4]);
        
        var offset = 20;
        var authOffset = -1;

        while (offset + 2 < length)
        {
            var type = span[offset];
            var len = span[offset + 1];
            if (len < 2 || offset + len > length) break;
            if (type == 80 && len == 18) { authOffset = offset + 2; break; }
            offset += len;
        }

        if (authOffset == -1) return true; 

        var receivedAuth = span.Slice(authOffset, 16);
        using var hmac = new HMACMD5(Encoding.UTF8.GetBytes(sharedSecret));
        
        var temp = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            packetData.Span[..length].CopyTo(temp);
            for (var i = 0; i < 16; i++) temp[authOffset + i] = 0;
            var computedAuth = hmac.ComputeHash(temp, 0, length);
            return CryptographicOperations.FixedTimeEquals(receivedAuth, computedAuth);
        }
        finally { ArrayPool<byte>.Shared.Return(temp); }
    }
    
}
