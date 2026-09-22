using Infrastructure.Services;

var services = new Services();

services.OnServiceMessage += (s, e) => Console.WriteLine($"{e.Message}");

await services.Initialize(CancellationToken.None);
