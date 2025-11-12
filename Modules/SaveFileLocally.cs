using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using nanoFramework.Json;

namespace temperature_logger.Modules
{
    internal static class JsonStorage
    {
        private const string folder = @"I:\data";

        public static void Save<T>(T obj, string fileName)
        {
            if (fileName == null || fileName.Length == 0)
                throw new ArgumentException("Invalid file name.", nameof(fileName));

            string path = PreparePath(fileName);

            try
            {
                string json = JsonConvert.SerializeObject(obj);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Save] Failed to write '{path}': {ex.Message}");
            }
        }

        public static T Load<T>(string fileName)
        {
            string path = Path.Combine(folder, Path.GetFileName(fileName));

            if (!File.Exists(path))
                return default(T);

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrEmpty(json))
                    return default(T);

                return (T)JsonConvert.DeserializeObject(json, typeof(T));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Load] Failed to read '{path}': {ex.Message}");
                return default(T);
            }
        }

        public static void Append<T>(T newItem, string fileName)
        {
            string path = PreparePath(fileName);

            try
            {
                ArrayList list = new ArrayList();

                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    if (!string.IsNullOrEmpty(json))
                    {
                        try
                        {
                            var existing = (T[])JsonConvert.DeserializeObject(json, typeof(T[]));
                            if (existing != null)
                            {
                                foreach (var item in existing)
                                    list.Add(item);
                            }
                        }
                        catch
                        {
                            Debug.WriteLine($"[Append] Corrupt JSON in '{path}', starting new array.");
                        }
                    }
                }

                list.Add(newItem);
                string newJson = JsonConvert.SerializeObject(list.ToArray(typeof(T)));
                File.WriteAllText(path, newJson);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Append] Failed to update '{path}': {ex.Message}");
            }
        }

        public static T[] ReadArray<T>(string fileName)
        {
            string path = Path.Combine(folder, Path.GetFileName(fileName));

            if (!File.Exists(path))
                return new T[0];

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrEmpty(json))
                    return new T[0];

                return (T[])JsonConvert.DeserializeObject(json, typeof(T[]));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ReadArray] Failed to parse '{path}': {ex.Message}");
                return new T[0];
            }
        }

        public static void Clear(string fileName)
        {
            string path = Path.Combine(folder, Path.GetFileName(fileName));
            try
            {
                File.WriteAllText(path, "[]");
                Console.WriteLine($"Cleared {path}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Clear] Failed: {ex.Message}");
            }
        }

        public static void Delete(string fileName)
        {
            string path = Path.Combine(folder, Path.GetFileName(fileName));
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Delete] Failed: {ex.Message}");
            }
        }

        private static string PreparePath(string fileName)
        {
            try
            {
                Directory.CreateDirectory(folder);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to create folder '{folder}': {ex.Message}");
            }

            return Path.Combine(folder, Path.GetFileName(fileName));
        }
    }
}