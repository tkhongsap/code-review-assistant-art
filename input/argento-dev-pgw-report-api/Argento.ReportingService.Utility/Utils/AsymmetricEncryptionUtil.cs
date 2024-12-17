using System;
using System.Security.Cryptography;
using System.Text;

namespace Argento.ReportingService.Utility.Utils
{
    /// <summary>
    /// เข้ารหัสขาเดียว เข้ารหัสแล้วถอดรหัสไม่ได้
    /// </summary>
    public static class AsymmetricEncryptionUtil
    {
        ////**************
        //การเข้ารหัสแบบ hash จะไม่สามารถทำให้ข้อมูลเหมือนเดิมได้ลักษณะการใช้งานคือ
        //เก็บไว้ในฐานข้อมูลแล้วเปรียบกับข้อมูลที่ส่งเข้าว่าเหมือนกันหรือไม่
        //Algorithms     size digest
        //MD5
        //SHA1           28 byte
        //SHA256         44 byte
        //SHA384         64 byte
        //SHA512         88 byte
        ////***************
        public static string EncryptMD5(string data)
        {
            byte[] encryptedBytes = EncryptMD5(Encoding.UTF8.GetBytes(data));
            string encryptedString = Convert.ToBase64String(encryptedBytes);
            return encryptedString;
        }

        public static byte[] EncryptMD5(byte[] dataBytes)
        {
            using (var hasher = new MD5CryptoServiceProvider())
            {
                byte[] encryptedBytes = hasher.ComputeHash(dataBytes);
                return encryptedBytes;
            }
        }

        public static string EncryptSHA1(string data)
        {
            byte[] encrptedBytes = EncryptSHA1(Encoding.UTF8.GetBytes(data));
            string encryptedString = Convert.ToBase64String(encrptedBytes);
            return encryptedString;
        }

        public static byte[] EncryptSHA1(byte[] dataBytes)
        {
            using (var hasher = new SHA1Managed())
            {
                byte[] encryptedBytes = hasher.ComputeHash(dataBytes);
                return encryptedBytes;
            }
        }

        public static string EncryptSHA256(string data)
        {
            byte[] encryptedBytes = EncryptSHA256(Encoding.UTF8.GetBytes(data));
            string encryptedString = Convert.ToBase64String(encryptedBytes);
            return encryptedString;
        }

        public static byte[] EncryptSHA256(byte[] dataBytes)
        {
            using (var hasher = new SHA256Managed())
            {
                byte[] encryptedBytes = hasher.ComputeHash(dataBytes);
                return encryptedBytes;
            }
        }

        public static string EncryptSHA384(string data)
        {
            byte[] encryptedBytes = EncryptSHA384(Encoding.UTF8.GetBytes(data));
            string encryptedString = Convert.ToBase64String(encryptedBytes);
            return encryptedString;
        }

        public static byte[] EncryptSHA384(byte[] dataBytes)
        {
            using (var hasher = new SHA384Managed())
            {
                byte[] encryptedBytes = hasher.ComputeHash(dataBytes);
                return encryptedBytes;
            }
        }

        public static string EncryptSHA512(string data)
        {
            byte[] encryptedBytes = EncryptSHA512(Encoding.UTF8.GetBytes(data));
            string encryptedString = Convert.ToBase64String(encryptedBytes);
            return encryptedString;
        }

        public static byte[] EncryptSHA512(byte[] dataBytes)
        {
            using (var hasher = new SHA512Managed())
            {
                byte[] encryptedBytes = hasher.ComputeHash(dataBytes);
                return encryptedBytes;
            }
        }

        public static string EncryptHMACSHA256(string data, string key)
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] encryptedBytes = EncryptHMACSHA256(dataBytes, key);
            string encryptedString = Convert.ToBase64String(encryptedBytes);
            return encryptedString;
        }

        public static byte[] EncryptHMACSHA256(byte[] dataBytes, string key)
        {
            byte[] encryptedBytes = null;
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            using (var hasher = new HMACSHA256(keyBytes))
            {
                encryptedBytes = hasher.ComputeHash(dataBytes);
            }

            return encryptedBytes;
        }
    }
}