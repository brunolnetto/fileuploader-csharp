using UploaderMVP.Controllers;
using UploaderMVP.Models;
using UploaderMVP.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IFileUploader, SerializedUploader>();
builder.Services.AddScoped<IFileUploader, ParallelizedUploader>();
builder.Services.AddScoped<IFileUploader, AsynchronousUploader>();

// Register the required string dependency
builder.Services.AddSingleton<string>("YourStringValue");

builder.Services.AddControllersWithViews();
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();

}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGet("/", () => "Hello World!");
app.MapGet("/upload", () => { 
    return new FileUploadController().Upload(); });
app.MapPost("/upload", (FileUploadViewModel model, CancellationToken cancellationToken) => {
    return new FileUploadController().UploadPost(model, cancellationToken);
});

app.Run();
