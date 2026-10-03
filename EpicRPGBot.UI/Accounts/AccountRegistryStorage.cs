#nullable disable

using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace EpicRPGBot.UI.Accounts
{
    internal interface IAccountRegistryStorage
    {
        AccountRegistryDocument Read();
        void Write(AccountRegistryDocument document);
    }

    internal sealed class AccountRegistryStorage : IAccountRegistryStorage
    {
        private readonly string _registryPath;

        public AccountRegistryStorage(string settingsRoot)
        {
            _registryPath = Path.Combine(settingsRoot, "accounts.json");
        }

        public AccountRegistryDocument Read()
        {
            string serialized;
            try { serialized = File.ReadAllText(_registryPath); }
            catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }

            try
            {
                return JsonSerializer.Deserialize<AccountRegistryDocument>(serialized) ??
                    throw new InvalidDataException($"Account registry '{_registryPath}' contains no document.");
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException($"Account registry '{_registryPath}' contains invalid JSON.", exception);
            }
        }

        public void Write(AccountRegistryDocument document)
        {
            var serialized = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(Path.GetDirectoryName(_registryPath));
            var stagingPath = _registryPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                WriteStagedDocument(stagingPath, serialized);
                if (File.Exists(_registryPath)) File.Replace(stagingPath, _registryPath, null);
                else File.Move(stagingPath, _registryPath);
            }
            finally
            {
                if (File.Exists(stagingPath)) File.Delete(stagingPath);
            }
        }

        private static void WriteStagedDocument(string stagingPath, string serialized)
        {
            using (var stream = new FileStream(stagingPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(serialized);
                writer.Flush();
                stream.Flush(true);
            }
        }
    }
}
