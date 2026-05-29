using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLyNhaHangDemo.Hubs;
using QuanLyNhaHangDemo.Models;
using QuanLyNhaHangDemo.Repository;
using QuanLyNhaHangDemo.Services;

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<DataContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 30)), // Ép chạy bản 8.0 offline, không check mạng
        mysqlOptions => mysqlOptions.EnableRetryOnFailure()
    )
);


// =====================================================
// IDENTITY
// =====================================================
builder.Services.AddIdentity<AppUserModel, IdentityRole>(options =>
{
    // Password
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 4;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    // Confirm
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;

    // Email
    options.User.RequireUniqueEmail = false;
})
.AddEntityFrameworkStores<DataContext>()
.AddDefaultTokenProviders();


// =====================================================
// COOKIE LOGIN
// =====================================================
builder.Services.ConfigureApplicationCookie(options =>
{
    // Chưa login sẽ về đây
    options.LoginPath = "/Account/Login";

    // Không đủ quyền
    options.AccessDeniedPath = "/Account/AccessDenied";

    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});


// =====================================================
// MVC
// =====================================================
builder.Services.AddControllersWithViews();


// =====================================================
// SESSION
// =====================================================
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.IsEssential = true;
});


// =====================================================
// SWAGGER
// =====================================================
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Restaurant API",
        Version = "v1"
    });
});


// =====================================================
// CORS
// =====================================================
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7272",
                "http://localhost:5272")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


// =====================================================
// SIGNALR
// =====================================================
builder.Services.AddSignalR();


// =====================================================
// SERVICES
// =====================================================
builder.Services.AddScoped<IOrderStateService, OrderStateService>();


var app = builder.Build();


// =====================================================
// TỰ ĐỘNG CHẠY MIGRATION TẠO BẢNG KHI DEPLOY
// =====================================================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DataContext>();

    // Tự động Migrate cấu trúc bảng sang MySQL trên Railway
    context.Database.Migrate();

    // Nếu chạy ở máy Local (Development) thì mới đổ dữ liệu Seed cứng
    if (app.Environment.IsDevelopment())
    {
        SeedData.SeedingData(context);
    }
}


// =====================================================
// DEVELOPMENT (SWAGGER)
// =====================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// =====================================================
// MIDDLEWARE
// =====================================================
app.UseStatusCodePagesWithRedirects(
    "/Home/Error?statuscode={0}");

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();

app.UseAuthorization();

app.UseCors("AllowFrontend");


// =====================================================
// ROUTES
// =====================================================

// AREA
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}"
);

// DEFAULT
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"
);


// API Controllers
app.MapControllers();



// SignalR
app.MapHub<OrderHub>("/hubs/order");


app.Run();