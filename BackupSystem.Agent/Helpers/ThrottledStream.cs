using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BackupSystem.Agent.Helpers
{
    public class ThrottledStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly long _maxBytesPerSecond;
        private long _processedBytes = 0;
        private readonly Stopwatch _stopwatch = new Stopwatch();

        public ThrottledStream(Stream baseStream, long maxBytesPerSecond)
        {
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _maxBytesPerSecond = maxBytesPerSecond;
            _stopwatch.Start();
        }

        // Asenkron okuma işlemi yapıldığında devreye girer.
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // Orijinal dosyadan baytları oku
            int bytesRead = await _baseStream.ReadAsync(buffer, offset, count, cancellationToken);

            // Hız sınırını aşıp aşmadığımızı kontrol et.
            await ThrottleAsync(bytesRead, cancellationToken);

            return bytesRead;
        }

        private async Task ThrottleAsync(int bytesProcessed, CancellationToken cancellationToken)
        {
            // Limitsiz mi?
            if (_maxBytesPerSecond <= 0 || bytesProcessed <= 0) return;

            _processedBytes += bytesProcessed;

            // Şu ana kadar işlenen baytların işlenmesi GEREKEN süreyi hesapla.
            long targetTimeMs = (_processedBytes * 1000) / _maxBytesPerSecond;
            long actualTimeMs = _stopwatch.ElapsedMilliseconds;

            // Eğer veriyi olması gerekenden DAHA HIZLI işlediysek, aradaki fark kadar Delay
            if (targetTimeMs > actualTimeMs)
            {
                await Task.Delay((int)(targetTimeMs - actualTimeMs), cancellationToken);
            }
        }

        // --- Stream Sınıfının Zorunlu Metotlarını Orijinal Akışa Yönlendiriyoruz ---
        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => _baseStream.CanWrite;
        public override long Length => _baseStream.Length;
        public override long Position { get => _baseStream.Position; set => _baseStream.Position = value; }
        public override void Flush() => _baseStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _baseStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);
        public override void SetLength(long value) => _baseStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _baseStream.Write(buffer, offset, count);
    }
}