var builder = WebApplication.CreateBuilder(args);

// Ativa suporte a controladores com views (MVC padrão)
builder.Services.AddControllersWithViews();

// Configura um HttpClient nomeado para comunicação com o backend
builder.Services.AddHttpClient("backend", client =>
{
    client.BaseAddress = new Uri("http://localhost:7171/api/");
});

var app = builder.Build();

// Middleware de tratamento de exceções e segurança
if (!app.Environment.IsDevelopment())
{
    // Redireciona para página de erro personalizada em produção
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Ativa cabeçalho HTTP Strict Transport Security
}

// Redireciona HTTP para HTTPS automaticamente
app.UseHttpsRedirection();

// Permite servir ficheiros estáticos (CSS, JS, imagens, etc.)
app.UseStaticFiles();

// Ativa o pipeline de routing
app.UseRouting();

// (Opcional) Middleware de autorização, pode ser expandido
app.UseAuthorization();

// Define a rota padrão do padrão MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Inicia a aplicação
app.Run();
