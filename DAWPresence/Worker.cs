using System.Reflection;
using DiscordRPC;
using YamlDotNet.Serialization;

namespace DAWPresence;

internal sealed class Worker : BackgroundService
{
	private const string CONFIG_FILE_NAME = "config.yml";

	private const string CREDIT = "FL-RP by BellevenHeaven";
	private readonly ILogger<Worker> _logger;
	private readonly AppConfiguration _configuration;
	private DiscordRpcClient? client;
	private DateTime? startTime;

	public Worker(ILogger<Worker> logger)
	{
		_logger = logger;
		if (File.Exists(CONFIG_FILE_NAME))
		{
			_logger.LogInformation("Cargando archivo de configuracion...");
			_configuration =
				new Deserializer().Deserialize<AppConfiguration>(
					File.ReadAllText(CONFIG_FILE_NAME));
		}
		else
		{
			_configuration = new AppConfiguration();
			_logger.LogInformation("Creando archivo de configuracion...");
			File.WriteAllText(CONFIG_FILE_NAME,
				new SerializerBuilder().Build().Serialize(_configuration));
		}
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		IEnumerable<Daw?> registeredDaws = Assembly.GetExecutingAssembly().GetTypes()
			.Where(t => t.IsSubclassOf(typeof(Daw)))
			.Select(t => (Daw?)Activator.CreateInstance(t));
		IEnumerable<Daw?> regDawArray = registeredDaws as Daw[] ?? registeredDaws.ToArray();

		foreach (Daw? r in regDawArray)
			Console.WriteLine($"{r.DisplayName} ha sido registrado");

		while (!stoppingToken.IsCancellationRequested)
		{
			Daw? daw = regDawArray.FirstOrDefault(d => d.IsRunning);
			Console.Clear();
			if (daw is null)
			{
				client?.ClearPresence();
				client?.Dispose();
				startTime = null;
				Console.WriteLine("No se esta ejecutando el DAW");
				return;
			}

			startTime ??= DateTime.UtcNow;
			
			Console.WriteLine("Detectado: " + daw.DisplayName);
			Console.WriteLine("Proyecto: " + daw.GetProjectNameFromProcessWindow());

			if (client is null || client.ApplicationID != daw.ApplicationId)
			{
				client?.ClearPresence();
				client?.Dispose();
				client = new DiscordRpcClient(daw.ApplicationId);
				client.Initialize();
			}

			client.SetPresence(new RichPresence
			{
				Details = daw.GetProjectNameFromProcessWindow() != ""
					? _configuration.WorkingPrefixText + daw.GetProjectNameFromProcessWindow()
					: _configuration.IdleText,
				State = "",
				Assets = new Assets
				{
					LargeImageKey = _configuration.UseCustomImage
						? _configuration.CustomImageKey
						: daw.ImageKey,
					LargeImageText = CREDIT
				},
				Timestamps = new Timestamps
				{
					Start = startTime?.Add(-_configuration.Offset)
				}
			});

			await Task.Delay(_configuration.UpdateInterval, stoppingToken);
		}
	}
}