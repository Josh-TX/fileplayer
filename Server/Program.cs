using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Enable CORS to allow requests from any domain
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()  // Allow requests from any origin (domain)
              .AllowAnyHeader()  // Allow any headers
              .AllowAnyMethod(); // Allow any HTTP method (GET, POST, etc.)
    });
});
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 1024L * 1024 * 1024 * 2; // 2 GB
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1024L * 1024 * 1024 * 2; // 2 GB
});

builder.Services.AddSingleton<FileInfoService>();
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<DownloadService>();

var app = builder.Build();

app.UseDefaultFiles();
// Serve files from wwwroot folder
app.UseStaticFiles();

// Serve files from the "data" folder
var dataFolderPath = Path.Combine(BasePathHelper.BasePath, "data");
if (Directory.Exists(dataFolderPath))
{
    try
    {
        // When the host folder mounted onto /data lacks read permission, we get a very strange error. Hopefully this will clean up the error
        var testAccess = Directory.EnumerateFileSystemEntries(dataFolderPath).Any();

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(dataFolderPath),
            RequestPath = "/data",
            ServeUnknownFileTypes = true
        });
    }
    catch (UnauthorizedAccessException)
    {
        Console.WriteLine("Error: Insufficient permissions to read from /data. Please ensure the directory has appropriate read access.");
        Environment.Exit(1);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unexpected error accessing /data: {ex.Message}");
        Environment.Exit(1);
    }
}
else
{
    Console.WriteLine($"Error: /data directory doesn't exist");
    Environment.Exit(1);
}
app.UseCors();

app.MapControllers();

// Run the application
app.Run();