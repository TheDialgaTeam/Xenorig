#pragma warning disable IL2026

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using TheDialgaTeam.Core.Logging.Microsoft;
using Xenopool.Server.Database;
using Xenopool.Server.Options;
using Xenopool.Server.Pool;
using Xenopool.Server.RpcWallet;
using Xenopool.Server.SoloMining;

namespace Xenopool.Server;

public static class Program
{
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainOnUnhandledException;

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorPages();

        builder.Services.AddSingleton<RpcWalletNetwork>();
        builder.Services.AddSingleton<SoloMiningNetwork>();
        builder.Services.AddSingleton<PoolClientCollection>();

        builder.Services.AddHostedService<ConsoleService>();

        builder.Services.AddOptions<XenopoolOptions>().BindConfiguration("Xenopool", options => options.BindNonPublicProperties = true);

        builder.Services.AddGrpc();

        builder.Services.AddDbContextFactory<SqliteDatabaseContext>(optionsBuilder =>
        {
            optionsBuilder.UseSqlite($"Data Source={Path.Combine(builder.Environment.ContentRootPath, "data.db")}");
        });

        builder.Logging.AddLoggerTemplateFormatter(options =>
        {
            options.SetDefaultTemplate(formattingBuilder => formattingBuilder.SetGlobal(messageFormattingBuilder => messageFormattingBuilder.SetPrefix((in LoggerTemplateEntry _) => $"{AnsiEscapeCodeConstants.DarkGrayForegroundColor}{DateTime.Now:yyyy-MM-dd HH:mm:ss}{AnsiEscapeCodeConstants.Reset} ")));
            options.SetTemplate<ConsoleService>(formattingBuilder => formattingBuilder.SetGlobal(messageFormattingBuilder => messageFormattingBuilder.SetPrefix(string.Empty)));
        });

        var app = builder.Build();
        
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.UseHttpsRedirection();
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        app.UseRouting();
        app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

        app.MapGrpcService<PoolService>();

        app.MapRazorPages();
        app.MapControllers();
        app.MapFallbackToFile("index.html");

        app.Run();
    }

    private static void OnCurrentDomainOnUnhandledException(object _, UnhandledExceptionEventArgs eventArgs)
    {
        if (!eventArgs.IsTerminating) return;

        var crashFileLocation = Path.Combine(AppContext.BaseDirectory, $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_crash.log");
        File.WriteAllText(crashFileLocation, eventArgs.ExceptionObject.ToString());
    }
}