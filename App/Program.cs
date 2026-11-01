using App.Hosting;

var builder = WebApplication.CreateBuilder(args);

var app = AppBootstrap.Build(builder);
app.Run();
