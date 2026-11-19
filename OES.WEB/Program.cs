using Microsoft.AspNetCore.Authentication.Cookies;
using OES.DATA.Authentication;
using OES.DATA.Database;
using OES.DATA.Examinations;
using OES.DATA.Notifications;
using OES.SERVICES.Ai;
using OES.SERVICES.Authentication;
using OES.SERVICES.Database;
using OES.SERVICES.Examinations;
using OES.SERVICES.Notifications;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
{
    {
    {
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("global", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "OES.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    })
    .AddCookie("ExternalCookie", options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddCookie("TwoFactorCookie", options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.SignInScheme = "ExternalCookie";
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
            ?? builder.Configuration["web:client_id"]
            ?? string.Empty;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
            ?? builder.Configuration["web:client_secret"]
            ?? string.Empty;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student"));
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("The DefaultConnection connection string is required.");

builder.Services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connectionString));
builder.Services.AddSingleton<IJwtService>(_ => new JwtService(jwtSecretKey, jwtIssuer, jwtAudience));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IExamRepository, ExamRepository>();
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IAttemptRepository, AttemptRepository>();
builder.Services.AddScoped<IAttemptService, AttemptService>();
builder.Services.AddScoped<IResultRepository, ResultRepository>();
builder.Services.AddScoped<IResultService, ResultService>();
builder.Services.AddScoped<IAttemptRepository, AttemptRepository>();
builder.Services.AddScoped<IAttemptService, AttemptService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDbSeeder, DbSeeder>();

builder.Services.AddHttpClient("GeminiClient");
builder.Services.AddScoped<IAiService>(sp =>
{
    var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var client = clientFactory.CreateClient("GeminiClient");
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<GeminiAiService>>();
    var apiKey = config["Gemini:ApiKey"] ?? string.Empty;
    var model = config["Gemini:Model"] ?? "gemini-1.5-flash";
    return new GeminiAiService(client, apiKey, model, logger);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseHttpLogging();

app.UseRouting();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapStaticAssets();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapRazorPages()
   .RequireRateLimiting("global")
   .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IDbSeeder>();
    await seeder.SeedAsync();
}

app.Run();
