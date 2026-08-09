using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Autofac;
using Serilog;

namespace KoruMsSqlYedek.Engine.Plugins
{
    /// <summary>
    /// Uygulama dizinindeki <c>Plugins</c> klasöründen eklenti derlemelerini yükler ve
    /// içlerindeki Autofac modüllerini kaba kaydeder.
    /// </summary>
    /// <remarks>
    /// Ücretli (Plus) özellikler çekirdek dağıtımda <em>hiç bulunmaz</em>; ayrı bir derleme
    /// olarak gelir. Eklenti yoksa ilgili servis arayüzü kayıtsız kalır ve çağıran taraf
    /// özelliği kapalı kabul eder — kaldırılacak bir bayrak ya da atlanacak bir kontrol yoktur.
    /// </remarks>
    public static class PluginLoader
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(typeof(PluginLoader));

        /// <summary>Eklentilerin arandığı alt klasör.</summary>
        public const string PluginDirectoryName = "Plugins";

        /// <summary>
        /// Eklenti klasöründeki derlemeleri yükleyip Autofac modüllerini kaydeder.
        /// Klasör yoksa veya boşsa sessizce hiçbir şey yapmaz — bu, ücretsiz sürümün normal halidir.
        /// </summary>
        /// <param name="builder">Kayıtların ekleneceği kap oluşturucu.</param>
        /// <param name="baseDirectory">Uygulama dizini. null ise <see cref="AppContext.BaseDirectory"/>.</param>
        /// <returns>Yüklenen eklenti derlemelerinin adları.</returns>
        public static IReadOnlyList<string> LoadInto(ContainerBuilder builder, string baseDirectory = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            var loaded = new List<string>();
            string root = baseDirectory ?? AppContext.BaseDirectory;
            if (string.IsNullOrEmpty(root))
                return loaded;

            string pluginDir = Path.Combine(root, PluginDirectoryName);
            if (!Directory.Exists(pluginDir))
            {
                Log.Debug("Eklenti klasörü yok, ücretsiz sürüm olarak devam ediliyor: {Dir}", pluginDir);
                return loaded;
            }

            string[] files;
            try
            {
                files = Directory.GetFiles(pluginDir, "*.dll", SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Eklenti klasörü okunamadı: {Dir}", pluginDir);
                return loaded;
            }

            foreach (string file in files)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);
                    builder.RegisterAssemblyModules(assembly);
                    loaded.Add(assembly.GetName().Name);

                    Log.Information("Eklenti yüklendi: {Name} ({Version})",
                        assembly.GetName().Name, assembly.GetName().Version);
                }
                catch (BadImageFormatException)
                {
                    // Yönetilmeyen DLL (örn. native bağımlılık) — eklenti değil, atla
                    Log.Debug("Yönetilmeyen DLL atlandı: {File}", Path.GetFileName(file));
                }
                catch (Exception ex)
                {
                    // Bozuk bir eklenti tüm uygulamayı düşürmemeli
                    Log.Error(ex, "Eklenti yüklenemedi: {File}", Path.GetFileName(file));
                }
            }

            return loaded;
        }
    }
}
