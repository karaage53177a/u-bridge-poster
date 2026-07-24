using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;

namespace u_bridge_poster {
    public class Program {
        public static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();

            builder.Services.Configure<ForwardedHeadersOptions>(options => {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseAuthorization();

            string posterDir = @"/mnt/poster/";
            var startedAt = DateTimeOffset.UtcNow;

            app.MapGet("/view", () => {
                if (!Directory.Exists(posterDir)) {
                    if (!Directory.Exists(posterDir)) {
                        return Results.Text("Poster directory not found.", statusCode: StatusCodes.Status404NotFound);
                    }
                }

                var files = Directory.GetFiles(posterDir)
                    .Where(f =>
                        f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => f)
                    .ToArray();

                if (files.Length == 0) {
                    return Results.Text("images not found.", statusCode: StatusCodes.Status404NotFound);
                }

                // 起動時刻からの経過時間で画像切り替え
                var interval_s = 60;
                long elapsedSeconds = (long)(DateTimeOffset.UtcNow - startedAt).TotalSeconds;
                long slot = elapsedSeconds / interval_s;
                int index = (int)(slot % files.Length);
                string file = files[index];

                var provider = new FileExtensionContentTypeProvider();
                if (!provider.TryGetContentType(file, out var contentType)) {
                    contentType = "application/octet-stream";
                }

                return Results.File(file, contentType, enableRangeProcessing: false, lastModified: File.GetLastWriteTimeUtc(file));
            });

            app.Run();
        }
    }
}
