var builder = WebApplication.CreateBuilder(args);

// Ativa suporte a controladores com views (MVC padr�o)
builder.Services.AddControllersWithViews();

// Configura um HttpClient nomeado para comunica��o com o backend
builder.Services.AddHttpClient("backend", client =>
{
    client.BaseAddress = new Uri("http://localhost:7171/api/");
});

var app = builder.Build();

// Middleware de tratamento de exce��es e seguran�a
if (!app.Environment.IsDevelopment())
{
    // Redireciona para p�gina de erro personalizada em produ��o
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Ativa cabe�alho HTTP Strict Transport Security
}

// Redireciona HTTP para HTTPS automaticamente
app.UseHttpsRedirection();

// Permite servir ficheiros est�ticos (CSS, JS, imagens, etc.)
app.UseStaticFiles();

// Ativa o pipeline de routing
app.UseRouting();

// (Opcional) Middleware de autoriza��o, pode ser expandido
app.UseAuthorization();

// Define a rota padr�o do padr�o MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Inicia a aplica��o
app.Run();
