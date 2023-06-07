using AisLogistics.App.Models;
using FluentFTP;
using Microsoft.Extensions.Options;

namespace AisLogistics.App.Service
{
    public class FtpService: IFtpService
    {
        private FtpClient _ftpClient;
        private readonly ILogger<FtpService> _logger;

        public FtpService(ILogger<FtpService> logger)
        {
            _logger = logger;
          /*  _ftpClient = new FtpClient(host: ftpSettingsModel.Server, port: ftpSettingsModel.Port,
                user: ftpSettingsModel.Login, pass: ftpSettingsModel.Password);*/
        }

        private async Task Init(FtpSettingsModel ftp)
        {
            _ftpClient = new FtpClient(host: ftp.Server, port: 21, user: ftp.Login, pass: ftp.Password);
            await _ftpClient.ConnectAsync();
        }

        public async Task<FtpStatus> UploadFileAsync(IFormFile file, string remotePath, FtpSettingsModel ftp)
        {
            try
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                return await UploadFileAsync(ms.ToArray(), remotePath, ftp);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.InnerException?.Message);
                return FtpStatus.Failed;
            }
        }

        public async Task<FtpStatus> UploadFileAsync(byte[] bytes, string remotePath, FtpSettingsModel ftp)
        {
            try
            {
                await Init(ftp);
                return await _ftpClient.UploadAsync(bytes, remotePath, FtpRemoteExists.Overwrite, true);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.InnerException?.Message);
                return FtpStatus.Failed;
            }
            finally
            {
                await _ftpClient.DisconnectAsync();
            }
        }

        public async Task<byte[]> DownloadFileAsync(string remotePath, FtpSettingsModel ftp)
        {
            try
            {
                await Init(ftp);
                return await _ftpClient.DownloadAsync(remotePath, CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.InnerException?.Message);
                return null;
            }
            finally
            {
                await _ftpClient.DisconnectAsync();
            }
        }
    }
}