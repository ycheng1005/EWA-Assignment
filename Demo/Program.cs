global using Demo;
global using Demo.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSqlServer<DB>($@"
    Data Source=(LocalDB)\MSSQLLocalDB;
    AttachDbFilename={builder.Environment.ContentRootPath}\DB.mdf;
");
builder.Services.AddAuthentication().AddCookie();
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Helper>();
builder.Services.AddWebOptimizer(pipeline =>
{
    pipeline.AddCssBundle("/bundle.css",
        "/css/pager.css",
        "/css/app.css");

    pipeline.AddJavaScriptBundle("/bundle.js",
        "/js/jquery.min.js",
        "/js/jquery.unobtrusive-ajax.min.js",
        "/js/jquery.validate.min.js",
        "/js/jquery.validate.unobtrusive.min.js",
        "/js/app.js");
});


var app = builder.Build();
app.UseHttpsRedirection();
app.UseWebOptimizer();
app.UseStaticFiles();
app.UseSession();
app.UseRequestLocalization(options => options.SetDefaultCulture("en-MY"));
app.MapDefaultControllerRoute();
app.Run();
