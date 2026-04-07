using System;
using Serilog;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Cloud
{
    /// <summary>
    /// CloudProviderType'a göre uygun ICloudProvider örneği oluşturan fabrika.
    /// Tüm provider türlerini destekler: Google Drive, FTP/SFTP, UNC.
    /// </summary>
    public class CloudProviderFactory : ICloudProviderFactory
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<CloudProviderFactory>();

        public ICloudProvider CreateProvider(CloudProviderType type)
        {
            switch (type)
            {
                case CloudProviderType.GoogleDrivePersonal:
                    return new GoogleDriveProvider(type);

                case CloudProviderType.Ftp:
                case CloudProviderType.Ftps:
                case CloudProviderType.Sftp:
                    return new FtpSftpProvider(type);

                case CloudProviderType.UncPath:
                    return new LocalNetworkProvider(type);

                default:
                    Log.Error("Desteklenmeyen provider türü: {Type}", type);
                    throw new ArgumentOutOfRangeException(
                        nameof(type),
                        type,
                        $"Desteklenmeyen cloud provider türü: {type}");
            }
        }

        public bool IsSupported(CloudProviderType type)
        {
            switch (type)
            {
                case CloudProviderType.GoogleDrivePersonal:
                case CloudProviderType.Ftp:
                case CloudProviderType.Ftps:
                case CloudProviderType.Sftp:
                case CloudProviderType.UncPath:
                    return true;
                default:
                    return false;
            }
        }
    }
}
