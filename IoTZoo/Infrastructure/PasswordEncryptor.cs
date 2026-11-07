// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace Infrastructure;

public static class PasswordEncryptor
{
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public static string Encrypt(string plainText, string base64Key)
    {
        byte[] key = GetKey(base64Key);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] plain = Encoding.UTF8.GetBytes(plainText);
        byte[] cipher = new byte[plain.Length];
        byte[] tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag);

        return Convert.ToBase64String([.. nonce, .. tag, .. cipher]);
    }

    public static string Decrypt(string cipherText, string base64Key)
    {
        byte[] key = GetKey(base64Key);
        byte[] data = Convert.FromBase64String(cipherText);
        if (data.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Encrypted value is invalid.");
        }

        byte[] nonce = data[..NonceSize];
        byte[] tag = data[NonceSize..(NonceSize + TagSize)];
        byte[] cipher = data[(NonceSize + TagSize)..];
        byte[] plain = new byte[cipher.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);

        return Encoding.UTF8.GetString(plain);
    }

    private static byte[] GetKey(string base64Key)
    {
        byte[] key = Convert.FromBase64String(base64Key);
        if (key.Length != KeySize)
        {
            throw new CryptographicException($"Encryption key must be {KeySize} bytes (Base64 encoded).");
        }

        return key;
    }
}
