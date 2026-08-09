using System;
using System.IO;

namespace KoruMsSqlYedek.Engine.Plugins
{
    /// <summary>
    /// Plus eklentisinin kurulu olup olmadığını bildirir.
    /// Arayüzün özelliği "Plus" olarak göstermesi için kullanılır.
    /// </summary>
    /// <remarks>
    /// Bu yalnızca <em>gösterim</em> amaçlıdır. Gerçek yaptırım eklentinin varlığından gelir:
    /// eklenti yoksa <c>IDiskImageService</c> hiç kayıtlı olmaz ve iş akışı adımı atlar.
    /// Buradaki kontrolü atlatmak bir işe yaramaz — çalıştıracak kod ortada yoktur.
    /// </remarks>
    public static class PlusAvailability
    {
        /// <summary>Plus eklenti derlemesinin dosya adı.</summary>
        public const string PlusAssemblyFileName = "KoruMsSqlYedek.Plus.dll";

        /// <summary>
        /// Plus eklentisi uygulama dizinindeki <c>Plugins</c> klasöründe var mı?
        /// </summary>
        /// <param name="baseDirectory">Uygulama dizini. null ise <see cref="AppContext.BaseDirectory"/>.</param>
        public static bool IsPlusInstalled(string baseDirectory = null)
        {
            string root = baseDirectory ?? AppContext.BaseDirectory;
            if (string.IsNullOrEmpty(root))
                return false;

            try
            {
                return File.Exists(Path.Combine(root, PluginLoader.PluginDirectoryName, PlusAssemblyFileName));
            }
            catch (Exception)
            {
                // Erişim/yol hatası eklenti yok sayılır — arayüz özelliği kapalı gösterir
                return false;
            }
        }
    }
}
