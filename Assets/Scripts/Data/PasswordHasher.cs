using System;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// 비밀번호 저장/검증 (PBKDF2-HMAC-SHA256, 사용자별 랜덤 salt).
///
/// 이전 버전은 비밀번호를 Base64 로만 바꿔 저장했기 때문에 DB 파일만 있으면 누구나 원문을 복원할 수 있었다.
/// 저장 형식: pbkdf2_sha256$반복횟수$salt(Base64)$hash(Base64)
/// 이전 형식(Base64)도 검증은 되며, 로그인에 성공하면 호출하는 쪽에서 새 형식으로 다시 저장한다(needsUpgrade).
/// </summary>
public static class PasswordHasher
{
    private const string Prefix = "pbkdf2_sha256";
    private const int Iterations = 100000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string password)
    {
        if (password == null) throw new ArgumentNullException(nameof(password));

        byte[] salt = new byte[SaltSize];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        byte[] hash = Pbkdf2Sha256(Encoding.UTF8.GetBytes(password), salt, Iterations, HashSize);

        return Prefix + "$" + Iterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
    }

    public static bool Verify(string password, string stored)
    {
        bool needsUpgrade;
        return Verify(password, stored, out needsUpgrade);
    }

    /// <param name="needsUpgrade">true 이면 저장된 값이 예전 형식이므로 Hash(password) 로 다시 저장해야 한다.</param>
    public static bool Verify(string password, string stored, out bool needsUpgrade)
    {
        needsUpgrade = false;

        if (password == null || string.IsNullOrEmpty(stored))
        {
            return false;
        }

        if (stored.StartsWith(Prefix + "$", StringComparison.Ordinal))
        {
            string[] parts = stored.Split('$');
            int iterations;

            if (parts.Length != 4 || !int.TryParse(parts[1], out iterations) || iterations <= 0)
            {
                return false;
            }

            byte[] salt;
            byte[] expected;

            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expected = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            if (expected.Length == 0)
            {
                return false;
            }

            byte[] actual = Pbkdf2Sha256(Encoding.UTF8.GetBytes(password), salt, iterations, expected.Length);

            bool ok = FixedTimeEquals(actual, expected);
            needsUpgrade = ok && iterations < Iterations;
            return ok;
        }

        // 예전 형식: Base64(UTF8(비밀번호))
        string legacy = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));
        bool legacyOk = FixedTimeEquals(Encoding.UTF8.GetBytes(legacy), Encoding.UTF8.GetBytes(stored));
        needsUpgrade = legacyOk;
        return legacyOk;
    }

    public static bool IsHashed(string stored)
    {
        return stored != null && stored.StartsWith(Prefix + "$", StringComparison.Ordinal);
    }

    // RFC 8018 PBKDF2. Rfc2898DeriveBytes 의 SHA256 오버로드는 Unity 의 API 호환 수준에 따라 없을 수 있어 직접 구현한다.
    private static byte[] Pbkdf2Sha256(byte[] password, byte[] salt, int iterations, int length)
    {
        using (HMACSHA256 hmac = new HMACSHA256(password))
        {
            const int hashLength = 32;
            int blockCount = (length + hashLength - 1) / hashLength;

            byte[] output = new byte[length];
            byte[] block = new byte[salt.Length + 4];
            Buffer.BlockCopy(salt, 0, block, 0, salt.Length);

            int offset = 0;

            for (int blockIndex = 1; blockIndex <= blockCount; blockIndex++)
            {
                block[salt.Length] = (byte)(blockIndex >> 24);
                block[salt.Length + 1] = (byte)(blockIndex >> 16);
                block[salt.Length + 2] = (byte)(blockIndex >> 8);
                block[salt.Length + 3] = (byte)blockIndex;

                byte[] u = hmac.ComputeHash(block);
                byte[] t = (byte[])u.Clone();

                for (int i = 1; i < iterations; i++)
                {
                    u = hmac.ComputeHash(u);

                    for (int j = 0; j < t.Length; j++)
                    {
                        t[j] ^= u[j];
                    }
                }

                int count = Math.Min(hashLength, length - offset);
                Buffer.BlockCopy(t, 0, output, offset, count);
                offset += count;
            }

            return output;
        }
    }

    private static bool FixedTimeEquals(byte[] a, byte[] b)
    {
        if (a == null || b == null || a.Length != b.Length)
        {
            return false;
        }

        int diff = 0;

        for (int i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i];
        }

        return diff == 0;
    }
}
