var builder = WebApplication.CreateBuilder(args);

// Ativa controladores com views (MVC)
builder.Services.AddControllersWithViews();

// HttpClient nomeado para a API backend
builder.Services.AddHttpClient("backend", client =>
{
    client.BaseAddress = new Uri("http://localhost:7171/api/");
});

var app = builder.Build();

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Define a rota padrão para MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
