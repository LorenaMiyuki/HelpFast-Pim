using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using HelpFast_Pim.Data;
using HelpFast_Pim.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using System;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;

namespace HelpFast_Pim
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configuration and services
            builder.Services.AddControllersWithViews();
            builder.Services.AddHttpClient();

            // register DbContext (uses DefaultConnection from appsettings.json)
            // Use InMemory database during Development to avoid hard-failing when Azure SQL is unreachable locally.
            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("HelpFast_Dev_Fallback"));
            }
            else
            {
                var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection");
                // Adicionado EnableRetryOnFailure e CommandTimeout para resiliência contra falhas transitórias do Azure SQL
                builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(
                        defaultConn,
                        sqlOptions =>
                        {
                            sqlOptions.EnableRetryOnFailure(
                                maxRetryCount: 5,
                                maxRetryDelay: TimeSpan.FromSeconds(10),
                                errorNumbersToAdd: null);
                            sqlOptions.CommandTimeout(60);
                        }));
            }

            // registrar serviço de usuário (garante que o tipo seja encontrado)
            builder.Services.AddScoped<IUsuarioService, UsuarioService>();

            // registrar serviços de chat e resultados da IA
            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddScoped<IChatIaResultService, ChatIaResultService>();

            // Authentication - cookie
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                });

            var app = builder.Build();

            // middleware
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }
            app.UseStaticFiles();

            // Middleware para desabilitar cache HTTP em todas as requisições
            app.Use(async (context, next) =>
            {
                context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                context.Response.Headers["Pragma"] = "no-cache";
                context.Response.Headers["Expires"] = "0";
                await next();
            });

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // rota padrão alterada para abrir a tela de login por padrão
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            await app.RunAsync();
        }
    }
}
