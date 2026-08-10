using BusTicketing.Application.Common.Interfaces;
using QRCoder;

namespace BusTicketing.Infrastructure.Services;

public class QrCodeService : IQrCodeService
{
    public async Task<byte[]> GeneratePngAsync(string payload, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(20);
    }
}
