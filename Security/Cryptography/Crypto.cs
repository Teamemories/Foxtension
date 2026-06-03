using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Foxtension.Security.Cryptography
{
    public enum CryptoType
    {
        AES,
        RSA,
        SHA
    }
    public enum CryptoSize
    {
        Small,
        Medium,
        Large
    }
    public sealed class Crypto : IDisposable
    {
        private readonly CryptoType _type;
        private readonly CryptoSize _size;
        private HashAlgorithm HashType { get; set; } = SHA1.Create();
        private int KeySize { get; set; }
        public byte[]? AESKey { get; private set; }
        public RSAParameters RSAKeyPublic { get; private set; }
        public RSAParameters RSAKeyPrivate { get; private set; }

        public Crypto(CryptoType type, CryptoSize size)
        {
            _type = type;
            _size = size;
        }

        #region Create Key Size
        private void SetKeySize()
        {
            switch (_type)
            {
                case CryptoType.AES:
                    KeySize = _size switch
                    {
                        CryptoSize.Small => 128,
                        CryptoSize.Medium => 192,
                        CryptoSize.Large => 256,
                        _ => throw new ArgumentOutOfRangeException(nameof(_size), "Invalid AES key size.")
                    };
                    break;
                case CryptoType.RSA:
                    KeySize = _size switch
                    {
                        CryptoSize.Small => 2048,
                        CryptoSize.Medium => 3072,
                        CryptoSize.Large => 4096,
                        _ => throw new ArgumentOutOfRangeException(nameof(_size), "Invalid RSA key size.")
                    };
                    break;
                case CryptoType.SHA:
                    HashType = _size switch
                    {
                        CryptoSize.Small => SHA256.Create(),
                        CryptoSize.Medium => SHA384.Create(),
                        CryptoSize.Large => SHA512.Create(),
                        _ => throw new ArgumentOutOfRangeException(nameof(_size), "Invalid SHA key size.")
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_type));
            }
        }
        #endregion

        #region Generate Key
        public void GenerateKeys()
        {
            SetKeySize();
            switch (_type)
            {
                case CryptoType.AES:
                    using (Aes aes = Aes.Create())
                    {
                        aes.KeySize = KeySize;
                        aes.GenerateKey();
                        AESKey = aes.Key;
                    }
                    break;
                case CryptoType.RSA:
                    using (RSA rsa = RSA.Create())
                    {
                        rsa.KeySize = KeySize;
                        RSAKeyPrivate = rsa.ExportParameters(true);
                        RSAKeyPublic = rsa.ExportParameters(false);
                    }
                    break;
                default:
                    throw new NotSupportedException("Encryption type is not supported.");
            }
        }
        #endregion

        #region Encryption
        public byte[] Encrypt(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                throw new ArgumentNullException(nameof(data), "Entry data is empty.");

            byte[] input = Encoding.UTF8.GetBytes(data);
            byte[] output;
            switch (_type)
            {
                case CryptoType.AES:
                    if (AESKey is null || AESKey.Length is 0)
                        throw new InvalidOperationException("AES key not been created.");
                    if (File.Exists(data))
                        input = File.ReadAllBytes(data);

                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = AESKey;
                        aes.GenerateIV();

                        using (MemoryStream ms = new MemoryStream())
                        {
                            ms.Write(aes.IV, 0, aes.IV.Length);
                            using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                            {
                                cs.Write(input, 0, input.Length);
                                cs.FlushFinalBlock();
                            }
                            output = ms.ToArray();
                        }
                    }
                    break;
                case CryptoType.RSA:
                    if (RSAKeyPublic.Equals(default(RSAParameters)))
                        throw new InvalidOperationException("RSA Public key not been created.");

                    using (RSA rsa = RSA.Create())
                    {
                        rsa.ImportParameters(RSAKeyPublic);
                        output = rsa.Encrypt(input, RSAEncryptionPadding.OaepSHA256);
                    }
                    break;
                case CryptoType.SHA:
                    if (string.IsNullOrWhiteSpace(data))
                        throw new ArgumentNullException(nameof(data));
                    if (File.Exists(data))
                        input = File.ReadAllBytes(data);

                    using (HashType)
                        output = HashType.ComputeHash(input);
                    break;
                default:
                    throw new NotSupportedException("Encryption algorithm does not support.");
            }
            return output!;
        }
        #endregion

        #region Decryption
        public string Decrypt(byte[] data)
        {
            if (!data.Any())
                throw new ArgumentNullException(nameof(data), "Entry data is empty.");

            byte[] output = null!;

            switch (_type)
            {
                case CryptoType.AES:
                    if (AESKey is null || AESKey.Length is 0)
                        throw new InvalidOperationException("AES key not been created.");
                    if (data.Length < 16)
                        throw new ArgumentException("Invalid AES data.");

                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = AESKey;

                        byte[] iv = new byte[16];
                        Array.Copy(data, 0, iv, 0, 16);
                        aes.IV = iv;
                        using (MemoryStream ms = new MemoryStream(data, 16, data.Length - 16))
                        {
                            using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                            {
                                using (MemoryStream nms = new MemoryStream())
                                {
                                    cs.CopyTo(nms);
                                    output = nms.ToArray();
                                }
                            }
                        }
                    }
                    break;
                case CryptoType.RSA:
                    if (RSAKeyPrivate.Equals(default(RSAParameters)))
                        throw new InvalidOperationException("RSA Private key not been created.");

                    using (RSA rsa = RSA.Create())
                    {
                        rsa.ImportParameters(RSAKeyPrivate);
                        output = rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA256);
                    }
                    break;
                default:
                    throw new NotSupportedException("Encryption algorithm does not support.");
            }
            return Encoding.UTF8.GetString(output);
        }
        #endregion

        #region Helpers
        public bool HashCkeck(string data, string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentNullException(nameof(hash));

            string actualHash = BytesToHex(Encrypt(data));

            return string.Equals(actualHash.Trim(), hash.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public string BytesToHex(byte[] bytes)
        {
            string result = nameof(ArgumentException);
            if (_type.Equals(CryptoType.RSA) || _type.Equals(CryptoType.AES))
                result = Convert.ToBase64String(bytes);
            else if (_type.Equals(CryptoType.SHA))
            {
                StringBuilder sb = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));
                result = sb.ToString();
            }

            return result;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}