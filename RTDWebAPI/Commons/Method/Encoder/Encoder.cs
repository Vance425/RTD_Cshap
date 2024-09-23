using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RTDWebAPI.Commons.Method.Encoder
{
    public class Encoder
    {
        #region MD5 加密
        public static string ToMD5(string strs)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] bytes = Encoding.Default.GetBytes(strs);//將要加密的字串轉換為位元組陣列
            byte[] encryptdata = md5.ComputeHash(bytes);//將字串加密後也轉換為字元陣列
            return Convert.ToBase64String(encryptdata);//將加密後的位元組陣列轉換為加密字串
        }

        /// <summary>
        /// 建立雜湊字串適用於任何 MD5 雜湊函式 （在任何平臺） 上建立 32 個字元的十六進位制格式雜湊字串
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string Get32MD5One(string source)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(source));
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                string hash = sBuilder.ToString();
                return hash.ToUpper();
            }
        }

        /// <summary>
        /// 獲取16位md5加密
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string Get16MD5One(string source)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(source));
                //轉換成字串，並取9到25位
                string sBuilder = BitConverter.ToString(data, 4, 8);
                //BitConverter轉換出來的字串會在每個字元中間產生一個分隔符，需要去除掉
                sBuilder = sBuilder.Replace("-", "");
                return sBuilder.ToString().ToUpper();
            }
        }

        //// <summary>
        /// </summary>
        /// <param name="strSource">需要加密的明文</param>
        /// <returns>返回32位加密結果，該結果取32位加密結果的第9位到25位</returns>
        public static string Get32MD5Two(string source)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            //獲取密文位元組陣列
            byte[] bytResult = md5.ComputeHash(Encoding.Default.GetBytes(source));
            //轉換成字串，32位
            string strResult = BitConverter.ToString(bytResult);
            //BitConverter轉換出來的字串會在每個字元中間產生一個分隔符，需要去除掉
            strResult = strResult.Replace("-", "");
            return strResult.ToUpper();
        }

        //// <summary>
        /// </summary>
        /// <param name="strSource">需要加密的明文</param>
        /// <returns>返回16位加密結果，該結果取32位加密結果的第9位到25位</returns>
        public static string Get16MD5Two(string source)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            //獲取密文位元組陣列
            byte[] bytResult = md5.ComputeHash(Encoding.Default.GetBytes(source));
            //轉換成字串，並取9到25位
            string strResult = BitConverter.ToString(bytResult, 4, 8);
            //BitConverter轉換出來的字串會在每個字元中間產生一個分隔符，需要去除掉
            strResult = strResult.Replace("-", "");
            return strResult.ToUpper();
        }
        #endregion

        #region DES加密
        /// <summary>
        /// DES加密
        /// </summary>
        /// <param name="data">加密資料</param>
        /// <param name="key">8位字元的金鑰字串</param>
        /// <param name="iv">8位字元的初始化向量字串</param>
        /// <returns></returns>
        public static string DESEncrypt(string data, string key, string iv)
        {
            byte[] byKey = Encoding.ASCII.GetBytes(key);
            byte[] byIV = Encoding.ASCII.GetBytes(iv);

            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            int i = cryptoProvider.KeySize;
            MemoryStream ms = new MemoryStream();
            CryptoStream cst = new CryptoStream(ms, cryptoProvider.CreateEncryptor(byKey, byIV), CryptoStreamMode.Write);

            StreamWriter sw = new StreamWriter(cst);
            sw.Write(data);
            sw.Flush();
            cst.FlushFinalBlock();
            sw.Flush();
            return Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
        }

        /// <summary>
        /// DES解密
        /// </summary>
        /// <param name="data">解密資料</param>
        /// <param name="key">8位字元的金鑰字串(需要和加密時相同)</param>
        /// <param name="iv">8位字元的初始化向量字串(需要和加密時相同)</param>
        /// <returns></returns>
        public static string DESDecrypt(string data, string key, string iv)
        {
            byte[] byKey = Encoding.ASCII.GetBytes(key);
            byte[] byIV = Encoding.ASCII.GetBytes(iv);

            byte[] byEnc;
            try
            {
                byEnc = Convert.FromBase64String(data);
            }
            catch
            {
                return null;
            }

            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            MemoryStream ms = new MemoryStream(byEnc);
            CryptoStream cst = new CryptoStream(ms, cryptoProvider.CreateDecryptor(byKey, byIV), CryptoStreamMode.Read);
            StreamReader sr = new StreamReader(cst);
            return sr.ReadToEnd();
        }
        #endregion

        #region DSA 加密
        /// <summary> 
        /// RSA加密資料 
        /// </summary> 
        /// <param name="express">要加密資料</param> 
        /// <param name="KeyContainerName">密匙容器的名稱</param> 
        /// <returns></returns> 
        public static string RSAEncryption(string express, string KeyContainerName = null)
        {

            CspParameters param = new CspParameters();
            param.KeyContainerName = KeyContainerName ?? "zhiqiang"; //密匙容器的名稱，保持加密解密一致才能解密成功
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(param))
            {
                byte[] plaindata = Encoding.Default.GetBytes(express);//將要加密的字串轉換為位元組陣列
                byte[] encryptdata = rsa.Encrypt(plaindata, false);//將加密後的位元組資料轉換為新的加密位元組陣列
                return Convert.ToBase64String(encryptdata);//將加密後的位元組陣列轉換為字串
            }
        }
        /// <summary> 
        /// RSA解密資料 
        /// </summary> 
        /// <param name="express">要解密資料</param> 
        /// <param name="KeyContainerName">密匙容器的名稱</param> 
        /// <returns></returns> 
        public static string RSADecrypt(string ciphertext, string KeyContainerName = null)
        {
            CspParameters param = new CspParameters();
            param.KeyContainerName = KeyContainerName ?? "zhiqiang"; //密匙容器的名稱，保持加密解密一致才能解密成功
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(param))
            {
                byte[] encryptdata = Convert.FromBase64String(ciphertext);
                byte[] decryptdata = rsa.Decrypt(encryptdata, false);
                return Encoding.Default.GetString(decryptdata);
            }
        }
        #endregion

        #region Base64加密解密
        /// <summary>
        /// Base64加密
        /// </summary>
        /// <param name="input">需要加密的字串</param>
        /// <returns></returns>
        public static string Base64Encrypt(string input)
        {
            return Base64Encrypt(input, new UTF8Encoding());
        }

        /// <summary>
        /// Base64加密
        /// </summary>
        /// <param name="input">需要加密的字串</param>
        /// <param name="encode">字元編碼</param>
        /// <returns></returns>
        public static string Base64Encrypt(string input, Encoding encode)
        {
            return Convert.ToBase64String(encode.GetBytes(input));
        }

        /// <summary>
        /// Base64解密
        /// </summary>
        /// <param name="input">需要解密的字串</param>
        /// <returns></returns>
        public static string Base64Decrypt(string input)
        {
            return Base64Decrypt(input, new UTF8Encoding());
        }

        /// <summary>
        /// Base64解密
        /// </summary>
        /// <param name="input">需要解密的字串</param>
        /// <param name="encode">字元的編碼</param>
        /// <returns></returns>
        public static string Base64Decrypt(string input, Encoding encode)
        {
            return encode.GetString(Convert.FromBase64String(input));
        }
        #endregion

        #region SHA 加密
        // SHA為不可逆加密方式
        public static string SHA1Encrypt(string Txt)
        {
            var bytes = Encoding.Default.GetBytes(Txt);
            var SHA = new SHA1CryptoServiceProvider();
            var encryptbytes = SHA.ComputeHash(bytes);
            return Convert.ToBase64String(encryptbytes);
        }
        public static string SHA256Encrypt(string Txt)
        {
            var bytes = Encoding.Default.GetBytes(Txt);
            var SHA256 = new SHA256CryptoServiceProvider();
            var encryptbytes = SHA256.ComputeHash(bytes);
            return Convert.ToBase64String(encryptbytes);
        }
        public static string SHA384Encrypt(string Txt)
        {
            var bytes = Encoding.Default.GetBytes(Txt);
            var SHA384 = new SHA384CryptoServiceProvider();
            var encryptbytes = SHA384.ComputeHash(bytes);
            return Convert.ToBase64String(encryptbytes);
        }
        public string SHA512Encrypt(string Txt)
        {
            var bytes = Encoding.Default.GetBytes(Txt);
            var SHA512 = new SHA512CryptoServiceProvider();
            var encryptbytes = SHA512.ComputeHash(bytes);
            return Convert.ToBase64String(encryptbytes);
        }
        #endregion

        #region AES 加密
        /// <summary>
        ///  AES 加密
        /// </summary>
        /// <param name="str">明文（待加密）</param>
        /// <param name="key">密文</param>
        /// <returns></returns>
        public string AesEncrypt(string str, string key)
        {
            if (string.IsNullOrEmpty(str)) return null;
            byte[] toEncryptArray = Encoding.UTF8.GetBytes(str);

            RijndaelManaged rm = new RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            ICryptoTransform cTransform = rm.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Convert.ToBase64String(resultArray);
        }

        /// <summary>
        ///  AES 解密
        /// </summary>
        /// <param name="str">明文（待解密）</param>
        /// <param name="key">密文</param>
        /// <returns></returns>
        public string AesDecrypt(string str, string key)
        {
            if (string.IsNullOrEmpty(str)) return null;
            byte[] toEncryptArray = Convert.FromBase64String(str);

            RijndaelManaged rm = new RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            ICryptoTransform cTransform = rm.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return Encoding.UTF8.GetString(resultArray);
        }
        #endregion
    }
}
