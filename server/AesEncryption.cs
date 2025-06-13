using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Classe utilitária para cifragem e decifragem de texto utilizando o algoritmo AES (Advanced Encryption Standard).
/// </summary>
public static class AesEncryption
{
    /// <summary>
    /// Descifra um texto codificado em base64 utilizando AES com a chave fornecida.
    /// O vetor de inicialização (IV) é assumido como estando nos primeiros 16 bytes do conteúdo cifrado.
    /// </summary>
    /// <param name="cipherTextBase64">Texto cifrado em base64 com IV incluído.</param>
    /// <param name="key">Chave AES (16 bytes para AES-128).</param>
    /// <returns>Texto plano descifrado.</returns>
    public static string DecryptStringAes(string cipherTextBase64, byte[] key)
    {
        byte[] fullCipher = Convert.FromBase64String(cipherTextBase64);

        // Extrair o vetor de inicialização (IV) - 16 bytes
        byte[] iv = new byte[16];
        byte[] cipher = new byte[fullCipher.Length - iv.Length];

        Array.Copy(fullCipher, iv, iv.Length);
        Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        // Criar o decifrador com a chave e IV
        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(cipher);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cs, Encoding.UTF8);

        return reader.ReadToEnd(); // Retornar texto plano
    }

    /// <summary>
    /// Cifra um texto plano utilizando AES e devolve o resultado em base64.
    /// O vetor de inicialização (IV) gerado é incluído no início do resultado.
    /// </summary>
    /// <param name="plainText">Texto a ser cifrado.</param>
    /// <param name="key">Chave AES (16 bytes para AES-128).</param>
    /// <returns>Texto cifrado em base64 com IV incluído.</returns>
    public static string EncryptStringAes(string plainText, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV(); // Gerar novo IV para esta cifragem

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();

        // Escrever o IV no início do stream (necessário para posterior descifragem)
        ms.Write(aes.IV, 0, aes.IV.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var writer = new StreamWriter(cs))
        {
            writer.Write(plainText); // Cifrar o conteúdo
        }

        // Devolver o conteúdo completo (IV + dados cifrados) como base64
        return Convert.ToBase64String(ms.ToArray());
    }
}
