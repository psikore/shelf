using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.Serialization;

[DataContract]
public class MyPayload
{
    [DataMember] public int UserId { get; set; }
    [DataMember] public string Messsage { get; set; }
    [DataMember] public bool Flag { get; set; }


    public static string ProcessPayload(byte[] data)
    {
        byte flag = data[0];
        using (var ms = new MemoryStream(data))
        {
            ms.Position = 1;    // skip flag
            if (flag == 1)
            {
                byte[] iv = new byte[16];
                ms.Read(iv, 0, 16);
                using (var aes = Aes.Create())
                {
                    aes.Key = Key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    using (var decryptor = aes.CreateDecryptor())
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs, Encoding.UTF8))
                    {
                        return sr.ReadToEnd();
                    }                    
                }
            }
            else if (flag == 0)
            {
                using (var sr = new StreamReader(ms, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
            else
            {
                throw new InvalidOperationException("Invalid flag");
            }
        }    
    }
}

public class EncryptRequest
{
    public string ciphertext { get; set; }
}

