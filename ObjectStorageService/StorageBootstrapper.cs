using Minio;
using Minio.DataModel.Args;

namespace ObjectStorageService
{
    public sealed class StorageBootstrapper : IStorageBootstrapper
    {
        private readonly IMinioClient _minio;
        private readonly StorageOptions _options;

        public StorageBootstrapper(IMinioClient minio, StorageOptions options)
        {
            _minio = minio;
            _options = options;
        }

        public async Task EnsureAsync(CancellationToken ct = default)
        {
            await EnsureBucketAsync(_options.Bucket, ct);
            await EnsureBucketAsync(_options.PatternsBucket, ct);
        }

        private async Task EnsureBucketAsync(string bucket, CancellationToken ct)
        {
            var exists = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), ct);
            if (!exists)
            {
                await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), ct);
            }
        }
    }
}
