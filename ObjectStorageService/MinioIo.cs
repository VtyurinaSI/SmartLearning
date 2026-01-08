using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;

namespace ObjectStorageService
{
    public static class MinioIo
    {
        public static async Task ClearSubmissionAsync(IMinioClient mc, string bucket, Guid userId, long taskId, CancellationToken ct)
        {
            var prefix = $"submissions/{userId:D}/{taskId}/";

            var objects = mc.ListObjectsEnumAsync(
                new ListObjectsArgs()
                    .WithBucket(bucket)
                    .WithPrefix(prefix)
                    .WithRecursive(true),
                ct);

            await foreach (var obj in objects.WithCancellation(ct))
            {
                await mc.RemoveObjectAsync(
                    new RemoveObjectArgs()
                        .WithBucket(bucket)
                        .WithObject(obj.Key),
                    ct);
            }
        }

        public static async Task PutAsync(IMinioClient mc, string bucket, string key, byte[] bytes, string contentType, CancellationToken ct)
        {
            using var ms = new MemoryStream(bytes);
            await mc.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucket).WithObject(key)
                .WithStreamData(ms).WithObjectSize(ms.Length)
                .WithContentType(contentType), ct);
        }

        public static async Task<byte[]?> GetAsync(IMinioClient mc, string bucket, string key, CancellationToken ct)
        {
            byte[]? result = null;

            await mc.GetObjectAsync(new GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(key)
                .WithCallbackStream(s =>
                {
                    using var ms = new MemoryStream();
                    s.CopyTo(ms);
                    result = ms.ToArray();
                }), ct);
            return result;
        }

        public static Task<List<string>> ListKeysAsync(IMinioClient mc, string bucket, string prefix, bool recursive, CancellationToken ct)
        {
            return ListKeysInternalAsync(mc, bucket, prefix, recursive, ct);
        }

        private static async Task<List<string>> ListKeysInternalAsync(IMinioClient mc, string bucket, string prefix, bool recursive, CancellationToken ct)
        {
            var acc = new List<string>();
            var objects = mc.ListObjectsEnumAsync(new ListObjectsArgs()
                .WithBucket(bucket)
                .WithPrefix(prefix)
                .WithRecursive(recursive), ct);

            await foreach (var obj in objects.WithCancellation(ct))
            {
                if (!string.IsNullOrEmpty(obj.Key) && !obj.Key.EndsWith("/"))
                    acc.Add(obj.Key);
            }

            return acc;
        }
    }
}
