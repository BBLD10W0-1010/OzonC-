using ClientStat.Application.Interfaces;
using ClientStat.Application.Services;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddSingleton<IOrderRepository, CsvOrderRepository>();
services.AddTransient<IClientSummaryService, ClientSummaryService>();

services.AddSingleton<ISummaryWriterFactory, SummaryWriterFactory>();

services.AddTransient<ConsoleSummaryWriter>();
services.AddTransient<FileSummaryWriter>();

services.AddTransient<ApplicationRunner>();

var serviceProvider = services.BuildServiceProvider();

var app = serviceProvider.GetRequiredService<ApplicationRunner>();

app.Run(args);