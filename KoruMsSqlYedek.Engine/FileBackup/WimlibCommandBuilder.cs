using System;
using System.IO;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.FileBackup
{
    /// <summary>
    /// wimlib-imagex komut satırı argümanlarını üretir ve çalıştırılabilir dosyayı bulur.
    /// Saf fonksiyonlardan oluşur — süreç başlatmaz, böylece birim testlerle doğrulanabilir.
    /// </summary>
    public static class WimlibCommandBuilder
    {
        /// <summary>Uygulama dizini altında wimlib ikililerinin arandığı alt klasör.</summary>
        public const string NativeSubDirectory = @"Native\wimlib";

        /// <summary>Çalıştırılabilir dosya adı.</summary>
        public const string ExecutableName = "wimlib-imagex.exe";

        /// <summary>
        /// Sıkıştırma seviyesini wimlib bayrağına çevirir.
        /// <see cref="DiskImageCompression.Solid"/> ayrı bir bayraktır (<c>--solid</c>),
        /// diğerleri <c>--compress=</c> ile verilir.
        /// </summary>
        public static string GetCompressionFlag(DiskImageCompression compression) => compression switch
        {
            DiskImageCompression.None  => "--compress=none",
            DiskImageCompression.Fast  => "--compress=fast",
            DiskImageCompression.Max   => "--compress=maximum",
            DiskImageCompression.Solid => "--solid",
            _ => "--solid"
        };

        /// <summary>
        /// Sürücü yolunu wimlib'in beklediği biçime getirir: "C:" → "C:\".
        /// wimlib capture kaynağı bir dizin olarak ister; kök için sondaki ters bölü zorunludur.
        /// </summary>
        public static string NormalizeSourcePath(string volumePath)
        {
            if (string.IsNullOrWhiteSpace(volumePath))
                throw new ArgumentException("Sürücü yolu boş olamaz.", nameof(volumePath));

            string trimmed = volumePath.Trim();

            // "C:" gibi salt sürücü belirteci kök dizine çevrilir
            if (trimmed.Length == 2 && trimmed[1] == ':')
                return trimmed + Path.DirectorySeparatorChar;

            return trimmed;
        }

        /// <summary>
        /// Bir sürücü için üretilecek imaj dosyasının adını oluşturur.
        /// Örnek: C: → "C_20260809_143000.wim"
        /// </summary>
        public static string BuildImageFileName(string volumePath, DateTime timestamp)
        {
            if (string.IsNullOrWhiteSpace(volumePath))
                throw new ArgumentException("Sürücü yolu boş olamaz.", nameof(volumePath));

            // Dosya adında geçersiz olan ':' ve ters bölüleri temizle
            string label = volumePath.Replace(":", "").Replace("\\", "").Replace("/", "").Trim();
            if (string.IsNullOrEmpty(label))
                label = "volume";

            return $"{label}_{timestamp:yyyyMMdd_HHmmss}.wim";
        }

        /// <summary>
        /// <c>wimlib-imagex capture</c> argüman dizesini üretir.
        /// </summary>
        /// <param name="volumePath">Kaynak sürücü (örn. "C:").</param>
        /// <param name="outputFilePath">Üretilecek .wim dosyasının tam yolu.</param>
        /// <param name="compression">Sıkıştırma seviyesi.</param>
        /// <param name="writeIntegrityTable">Bütünlük tablosu yazılsın mı?</param>
        /// <remarks>
        /// <c>--snapshot</c> wimlib'in kendi VSS gölge kopyasını almasını sağlar; açık/kilitli
        /// dosyalar bu sayede okunabilir ve yönetici yetkisi gerektirir.
        /// ACL, hardlink, alternate data stream ve reparse point'ler varsayılan olarak korunur —
        /// sistem sürücüsünün geri yüklenebilmesi için bunlar zorunludur, bastırılmamalıdır.
        /// </remarks>
        public static string BuildCaptureArguments(
            string volumePath,
            string outputFilePath,
            DiskImageCompression compression,
            bool writeIntegrityTable)
        {
            if (string.IsNullOrWhiteSpace(outputFilePath))
                throw new ArgumentException("Çıktı yolu boş olamaz.", nameof(outputFilePath));

            string source = NormalizeSourcePath(volumePath);
            string imageName = $"{volumePath.TrimEnd('\\', '/')} {DateTime.Now:yyyy-MM-dd HH:mm}";
            string integrity = writeIntegrityTable ? " --check" : string.Empty;

            return $"capture \"{source}\" \"{outputFilePath}\" \"{imageName}\" " +
                   $"--snapshot {GetCompressionFlag(compression)}{integrity}";
        }

        /// <summary>
        /// wimlib-imagex.exe konumunu çözer.
        /// Sırasıyla: uygulama dizini altındaki Native\wimlib klasörü, uygulama dizininin kendisi,
        /// son olarak PATH üzerinde arama yapılır.
        /// </summary>
        /// <param name="baseDirectory">Uygulama dizini. null ise <see cref="AppContext.BaseDirectory"/> kullanılır.</param>
        /// <returns>Bulunan tam yol; bulunamazsa null.</returns>
        public static string ResolveExecutablePath(string baseDirectory = null)
        {
            string root = baseDirectory ?? AppContext.BaseDirectory;

            if (!string.IsNullOrEmpty(root))
            {
                string bundled = Path.Combine(root, NativeSubDirectory, ExecutableName);
                if (File.Exists(bundled))
                    return bundled;

                string alongside = Path.Combine(root, ExecutableName);
                if (File.Exists(alongside))
                    return alongside;
            }

            // PATH üzerinde ara — kullanıcı wimlib'i ayrıca kurmuş olabilir
            string pathVar = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathVar))
            {
                foreach (string dir in pathVar.Split(Path.PathSeparator))
                {
                    if (string.IsNullOrWhiteSpace(dir)) continue;

                    try
                    {
                        string candidate = Path.Combine(dir.Trim(), ExecutableName);
                        if (File.Exists(candidate))
                            return candidate;
                    }
                    catch (ArgumentException)
                    {
                        // PATH içinde geçersiz karakter içeren girdiler olabilir — atla
                    }
                }
            }

            return null;
        }
    }
}
