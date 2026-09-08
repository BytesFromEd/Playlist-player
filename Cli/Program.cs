using Infrastructure.Services;

var services = new Services();

services.OnServiceMessage += (s, e) => Console.WriteLine($"{e.Message}");

await services.Initialize(CancellationToken.None);

services.CreateTable();

var playlist = services.GetPlaylist("PLebv-XoARUkw6PruMaKKOFWhvEjB96BSp");

playlist = await services.RefreshPlaylist(playlist, CancellationToken.None);
