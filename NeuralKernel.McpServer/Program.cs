using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddMcpServer(server =>
{
}).WithTools<TestTool>().WithHttpTransport(transport =>
{
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapMcp("/mcp");

app.Run();

[McpServerToolType]
public class TestTool
{
    [Description("预测报数")]
    [McpServerTool(Name = "yc", Title = "预测报数", ReadOnly = true, Idempotent = true)]
    public async ValueTask<double> YC() => await ValueTask.FromResult(new Random().NextDouble());
}