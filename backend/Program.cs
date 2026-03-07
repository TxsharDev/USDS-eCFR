var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "eCFR Regulatory Analyzer API");

app.Run();
