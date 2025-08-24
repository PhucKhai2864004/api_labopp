using Business_Logic.Interfaces;
using Business_Logic.Services;
using LabAssistantOPP_LAO.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Business_Logic.Interfaces.Teacher;
using Business_Logic.Services.Teacher;
using Business_Logic.Interfaces.Admin;
using Business_Logic.Services.Admin;
using Microsoft.AspNetCore.Http.Features;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using Business_Logic.Interfaces.Workers.Grading;
using Business_Logic.Services.Grading;
using Business_Logic.Interfaces.Workers.Docker;
using StackExchange.Redis;
using Business_Logic.Interfaces.Grading.grading_system.backend.Workers;
using Business_Logic.Services.FapSync;
using Business_Logic.Services.AI;
using Business_Logic.Interfaces.AI;

var builder = WebApplication.CreateBuilder(args);
var redisConnection = builder.Configuration.GetConnectionString("Redis");
// Add services to the container.




builder.Services.AddDbContext<LabOopChangeV6Context>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DB"),
		sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()));
builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
		options.JsonSerializerOptions.ReferenceHandler = null;
		options.JsonSerializerOptions.WriteIndented = true;
		options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
	});

builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowFrontend", policy =>
	{
		policy
			.SetIsOriginAllowed(origin =>
			{
				var uri = new Uri(origin);
				return uri.Host.EndsWith("vercel.app") ||
					   origin == "http://localhost:5173" ||
					   origin == "https://drive.wukongfood.site"; // ✅ thêm domain này
			})
			.AllowAnyHeader()
			.AllowAnyMethod()
			.AllowCredentials();
	});
});

builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddSingleton<DockerRunner>();
builder.Services.AddSingleton<GradingWorkerPool>();
builder.Services.AddScoped<SubmissionGradingWorker>();//Cần có để mỗi worker dùng

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
	var configuration = sp.GetRequiredService<IConfiguration>();
	var redisConnectionString = configuration.GetConnectionString("Redis");
	return ConnectionMultiplexer.Connect(redisConnectionString);
});

builder.Services.AddScoped<IRedisService, RedisService>();


builder.Services.AddCap(x =>
{
    x.UseRedis(builder.Configuration.GetConnectionString("Redis"));
	// hoặc cấu hình từ appsettings
	x.UseEntityFramework<LabOopChangeV6Context>(); // dùng bộ nhớ tạm (thay bằng EF nếu có DB)
	x.FailedRetryCount = 5;
	x.FailedRetryInterval = 10;
});


builder.Services.AddSignalR();


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new() { Title = "LabAssistantOPP_LAO.WebApi", Version = "v1" });

	// ✅ Thêm cấu hình để Swagger hiểu JWT
	c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
	{
		Name = "Authorization",
		Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
		Scheme = "Bearer",
		BearerFormat = "JWT",
		In = Microsoft.OpenApi.Models.ParameterLocation.Header,
		Description = "Nhập token theo định dạng: Bearer {your JWT token}"
	});

	c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
	{
		{
			new Microsoft.OpenApi.Models.OpenApiSecurityScheme
			{
				Reference = new Microsoft.OpenApi.Models.OpenApiReference
				{
					Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
					Id = "Bearer"
				}
			},
			new string[] {}
		}
	});
    c.AddServer(new Microsoft.OpenApi.Models.OpenApiServer
    {
        Url = "/api-labopp"
    });
});
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITeacherDashboardService, TeacherDashboardService>();
builder.Services.AddScoped<ITeacherAssignmentService, TeacherAssignmentService>();
builder.Services.AddScoped<ITeacherSubmissionService, TeacherSubmissionService>();
builder.Services.AddScoped<ITeacherStudentService, TeacherStudentService>();
builder.Services.AddScoped<ITeacherLocService, TeacherLocService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<FapSyncService>();
builder.Services.AddHttpClient<IAIService, AIService>();


var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"];

builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
	options.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidateLifetime = true,
		ValidateIssuerSigningKey = true,
		ValidIssuer = jwtSettings["Issuer"],
		ValidAudience = jwtSettings["Audience"],
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
		RoleClaimType = ClaimTypes.Role, // 🟢 rất quan trọng cho [Authorize(Roles = "...")]
		NameClaimType = ClaimTypes.Email
	};

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            // Nếu request đến từ SignalR hub path => lấy token từ query
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationHub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 20 * 1024 * 1024; // 20MB, đặt cao hơn 10MB để đảm bảo
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 20 * 1024 * 1024; // 20MB
});

builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();



var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LabOopChangeV6Context>();
    try
    {
        Console.WriteLine("🔍 Checking SQL Server connection...");
        db.Database.OpenConnection();
        Console.WriteLine("✅ Connected to SQL Server successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine("🔥 SQL Connection Error:");
        Console.WriteLine(ex.ToString());
    }

}

// Configure the HTTP request pipeline.
app.UsePathBase("/api-labopp");

app.UseSwagger();
	app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/api-labopp/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = "swagger"; // <- khớp với URL hiện tại của bạn
    });

app.MapGet("/", () => Results.Ok("✅ API is running. Use /swagger to view documentation."));

app.UseStaticFiles();

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHub<NotificationHub>("/notificationHub");

app.MapHub<SubmissionHub>("/hubs/submission");

app.Run();
