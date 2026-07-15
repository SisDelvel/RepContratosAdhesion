var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSession(); // ? nuevo

builder.Services.AddSingleton<Contratos_Adhesion.Services.IDbConnectionFactory,
                               Contratos_Adhesion.Services.DbConnectionFactory>();
builder.Services.AddScoped<Contratos_Adhesion.Services.IRepositorioContratoNuevos,
                            Contratos_Adhesion.Services.RepositorioContratoNuevos>();
builder.Services.AddScoped<Contratos_Adhesion.Services.IRepositorioContratoSeminuevos,
                            Contratos_Adhesion.Services.RepositorioContratoSeminuevos>();
builder.Services.AddScoped<Contratos_Adhesion.Services.IRepositorioContratoServicio,
                            Contratos_Adhesion.Services.RepositorioContratoServicio>();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseSession(); // ? nuevo

app.MapControllerRoute(
    name: "contrato_con_negocio",
    pattern: "{id:int}/{negocio:int}",
    defaults: new { controller = "Home", action = "Index" }
);
app.MapControllerRoute(
    name: "contrato_directo",
    pattern: "{id:int}",
    defaults: new { controller = "Home", action = "Index" }
);
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();