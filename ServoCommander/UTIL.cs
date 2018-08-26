﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ServoCommander
{
    public static class Util
    {
        public static class KEY
        {
            public const string APP_PATH = "Software\\Super169\\ServoCommander";
            public const string LAST_CONNECTION = "Last Connection";
            public const string SERVO_VERSION = "Servo Version";
        }

        public enum InfoType
        {
            message, alert, error
        };

        public static byte UBTCheckSum(byte[] data, int startIdx = 0)
        {
            int sum = 0;
            for (int i = 2; i < 8; i++)
            {
                sum += data[startIdx + i];
            }
            sum %= 256;
            return (byte)sum;
        }

        public static byte UBTCheckSum(List<byte> data, int startIdx = 0)
        {
            int sum = 0;
            for (int i = 2; i < 8; i++)
            {
                sum += data[startIdx + i];
            }
            sum %= 256;
            return (byte)sum;
        }

        public static bool WriteRegistry(string key, object value)
        {
            bool success = false;
            try
            {
                RegistryView platformView = (Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
                RegistryKey registryBase = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, platformView);
                if (registryBase == null) return false;
                RegistryKey registryEntry = registryBase.CreateSubKey(KEY.APP_PATH);
                if (registryEntry != null)
                {
                    registryEntry.SetValue(key, value);
                    success = true;
                    registryEntry.Close();
                }
                registryBase.Close();
            }
            catch (Exception)
            {
            }
            return success;
        }


        public static object ReadRegistry(string key)
        {
            object value = null;
            try
            {
                RegistryView platformView = (Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
                RegistryKey registryBase = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, platformView);
                if (registryBase == null) return null;
                RegistryKey registryEntry = registryBase.OpenSubKey(KEY.APP_PATH);
                if (registryEntry != null)
                {
                    value = registryEntry.GetValue(key);
                    registryEntry.Close();
                }
                registryBase.Close();

            }
            catch (Exception)
            {
                value = null;
            }
            return value;
        }

        public static byte CalCheckSum(byte[] data)
        {
            int sum = 0;
            for (int i = 2; i < 8; i++)
            {
                sum += data[i];
            }
            sum %= 256;
            return (byte)sum;
        }


        public static byte GetInputByte(string data)
        {
            if (data.EndsWith("."))
            {
                data = data.Substring(0, data.Length - 1);
                return (byte)Convert.ToInt32(data, 10);
            }
            return (byte)Convert.ToInt32(data, 16);
        }

        public static string GetByteString(byte[] data, string separator = " ")
        {
            string output = BitConverter.ToString(data);
            return output.Replace("-", separator);
        }


        private static void tb_PreviewCommand(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9A-Fa-f.]+").IsMatch(e.Text);
        }

        private static void tb_PreviewInteger(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
        }

        private static void tb_PreviewHex(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9A-F]+").IsMatch(e.Text);
        }

        private static void tb_PreviewHexMix(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9A-F]+[.]?").IsMatch(e.Text);
        }



    }
}