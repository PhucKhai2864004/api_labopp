using LabAssistantOPP_LAO.Models.Data;
using Microsoft.EntityFrameworkCore;
using NewGradingTest.Controllers;
using NewGradingTest.grading_system.backend.Docker;
using NewGradingTest.grading_system.backend.Workers;
using NewGradingTest.Models;
using NewGradingTest.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<LabOppContext>(options =>
{
	options.UseSqlServer(builder.Configuration.GetConnectionString("DB"));
});
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddSingleton<DockerRunner>();
builder.Services.AddSingleton<GradingWorkerPool>();
builder.Services.AddScoped<SubmissionGradingWorker>();//Cần có để mỗi worker dùng
builder.Services.AddSignalR();
builder.Services.AddControllers().AddJsonOptions(options =>
{
	options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
	options.JsonSerializerOptions.WriteIndented = true;

});



builder.Services.AddCap(x =>
{
	x.UseRedis("localhost"); // hoặc cấu hình từ appsettings
	x.UseInMemoryStorage(); // dùng bộ nhớ tạm (thay bằng EF nếu có DB)
	x.FailedRetryCount = 3;
});


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapHub<SubmissionHub>("/hubs/submission");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();