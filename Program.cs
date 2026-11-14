using FixFlow.Data;
using FixFlow.Models;
using FixFlow.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("FixFlowDb")
    ?? throw new InvalidOperationException("The ConnectionStrings:FixFlowDb setting is required.");

builder.Services.AddDbContext<FixFlowDbContext>(options =>
    options.UseMySQL(connectionString));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
        options.Cookie.Name = "FixFlow.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(UserRoles.OwnerAdmin));
    options.AddPolicy("FrontDeskOrAdmin", policy => policy.RequireRole(UserRoles.OwnerAdmin, UserRoles.FrontDesk));
    options.AddPolicy("AllStaff", policy => policy.RequireRole(UserRoles.OwnerAdmin, UserRoles.FrontDesk, UserRoles.Technician));
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
})
.AddMvcOptions(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<JobReferenceService>();
builder.Services.AddScoped<JobWorkflowService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<JobAccessService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<DevelopmentDataSeeder>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();
}

app.MapRazorPages();
app.Run();
