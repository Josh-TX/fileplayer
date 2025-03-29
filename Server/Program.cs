using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;

//verifying /data folder here because otherwise there will be very messy errors later
var dataFolderPath = Path.Combine(BasePathHelper.BasePath, "data");
if (!Directory.Exists(dataFolderPath))
{
    Console.WriteLine($"Error: /data directory doesn't exist");
    Environment.Exit(1);
}
try
{
    var fileNames = Directory.GetFiles(dataFolderPath);
    Console.WriteLine("/data directory contains " + fileNames.Length + " files");
}
catch (Exception ex)
{
    Console.WriteLine($"Error attempting to read the /data folder: {ex.Message}");
    Environment.Exit(1);
}


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
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(dataFolderPath),
    RequestPath = "/data",
    ServeUnknownFileTypes = true
});

app.UseCors();

app.MapControllers();

// Run the application
app.Run();