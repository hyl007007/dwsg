using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class 加密
{
	public const string Key = "a12ecc465b02a3e8";

	public const string IV = "1653a1b62d5638d9";

	public static string EncryptString(string plainText)
	{
		if (plainText == null || plainText.Length <= 0)
		{
			throw new ArgumentNullException("plainText");
		}
		if ("a12ecc465b02a3e8".Length <= 0)
		{
			throw new ArgumentNullException("Key");
		}
		if ("1653a1b62d5638d9".Length <= 0)
		{
			throw new ArgumentNullException("IV");
		}
		byte[] inArray;
		using (Aes aes = Aes.Create())
		{
			aes.Key = Encoding.ASCII.GetBytes("a12ecc465b02a3e8");
			aes.IV = Encoding.ASCII.GetBytes("1653a1b62d5638d9");
			ICryptoTransform transform = aes.CreateEncryptor(aes.Key, aes.IV);
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (CryptoStream stream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
				{
					using (StreamWriter streamWriter = new StreamWriter(stream, Encoding.UTF8))
					{
						streamWriter.Write(plainText);
					}
					inArray = memoryStream.ToArray();
				}
			}
		}
		return Convert.ToBase64String(inArray);
	}

	public static string DecryptString(string cipherStr)
	{
		byte[] array = Convert.FromBase64String(cipherStr);
		if (array == null || array.Length == 0)
		{
			throw new ArgumentNullException("cipherText");
		}
		if ("a12ecc465b02a3e8".Length <= 0)
		{
			throw new ArgumentNullException("Key");
		}
		if ("1653a1b62d5638d9".Length <= 0)
		{
			throw new ArgumentNullException("IV");
		}
		string text = null;
		using (Aes aes = Aes.Create())
		{
			aes.Key = Encoding.ASCII.GetBytes("a12ecc465b02a3e8");
			aes.IV = Encoding.ASCII.GetBytes("1653a1b62d5638d9");
			ICryptoTransform transform = aes.CreateDecryptor(aes.Key, aes.IV);
			using (MemoryStream stream = new MemoryStream(array))
			{
				using (CryptoStream stream2 = new CryptoStream(stream, transform, CryptoStreamMode.Read))
				{
					using (StreamReader streamReader = new StreamReader(stream2, Encoding.UTF8))
					{
						return streamReader.ReadToEnd();
					}
				}
			}
		}
	}
}
