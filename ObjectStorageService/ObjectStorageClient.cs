using System.Text;
using System.Text.Json;
using Minio;
using Minio.DataModel.Args;
using SmartLearning.Contracts;

namespace ObjectStorageService
{
    public class ObjectStorageClient : IObjectStorageClient
    {
        private readonly IMinioClient _minio;
        private readonly StorageOptions _opts;

        public ObjectStorageClient(IMinioClient minio, StorageOptions opts)
        {
            _minio = minio;
            _opts = opts;
        }

        private enum CheckStage { Load, Build, Reflect, Llm }

        private static bool TryParseStage(string stage, out CheckStage s)
        {
            switch (stage?.ToLowerInvariant())
            {
                case "load": s = CheckStage.Load; return true;
                case "build": s = CheckStage.Build; return true;
                case "reflect": s = CheckStage.Reflect; return true;
                case "llm": s = CheckStage.Llm; return true;
                default: s = default; return false;
            }
        }

        private static class StorageKeys
        {
            public static string StageSegment(CheckStage stage) => stage switch
            {
                CheckStage.Load => "00-load",
                CheckStage.Build => "01-build",
                CheckStage.Reflect => "02-reflect",
                CheckStage.Llm => "03-llm",
                _ => throw new ArgumentOutOfRangeException(nameof(stage))
            };

            public static string Base(Guid userId, long taskId)
                => $"submissions/{userId}/{taskId}";

            public static string StagePrefix(Guid userId, long taskId, CheckStage stage)
                => $"{Base(userId, taskId)}/{StageSegment(stage)}";

            public static string File(Guid userId, long taskId, CheckStage stage, string name)
                => $"{StagePrefix(userId, taskId, stage)}/{name}";
        }

        private static string FileNameForType<T>()
        {
            if (typeof(T) == typeof(string)) return "file.txt";
            if (typeof(T) == typeof(byte[])) return "file.bin";
            return "file.json";
        }

        public async Task WriteFile<T>(T data, Guid checkingId, Guid userId, long TaskId, string stage, CancellationToken token)
        {
            if (!TryParseStage(stage, out var s))
                throw new ArgumentException("stage must be: load|build|reflect|llm", nameof(stage));

            byte[] bytes;
            string contentType;

            if (data is string str)
            {
                bytes = Encoding.UTF8.GetBytes(str);
                contentType = "text/plain";
            }
            else if (data is byte[] b)
            {
                bytes = b;
                contentType = "application/octet-stream";
            }
            else
            {
                bytes = JsonSerializer.SerializeToUtf8Bytes(data);
                contentType = "application/json";
            }

            var key = StorageKeys.File(userId, TaskId, s, FileNameForType<T>());
            using var ms = new MemoryStream(bytes);
            await _minio.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_opts.Bucket)
                .WithObject(key)
                .WithStreamData(ms)
                .WithObjectSize(ms.Length)
                .WithContentType(contentType), token);
        }

        public async Task<T> ReadFile<T>(Guid checkingId, Guid userId, long TaskId, string stage, CancellationToken token)
        {
            if (!TryParseStage(stage, out var s))
                throw new ArgumentException("stage must be: load|build|reflect|llm", nameof(stage));

            var key = StorageKeys.File(userId, TaskId, s, FileNameForType<T>());

            byte[]? result = null;
            await _minio.GetObjectAsync(new GetObjectArgs()
                .WithBucket(_opts.Bucket)
                .WithObject(key)
                .WithCallbackStream(stream =>
                {
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    result = ms.ToArray();
                }), token);

            if (result is null) return default!;

            if (typeof(T) == typeof(string))
                return (T)(object)Encoding.UTF8.GetString(result);

            if (typeof(T) == typeof(byte[]))
                return (T)(object)result;

            return JsonSerializer.Deserialize<T>(result) ?? default!;
        }
    }
}