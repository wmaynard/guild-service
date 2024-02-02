using System.Reflection;

namespace Rumble.Platform.Guilds;

public class Program
{
	public static void Main(string[] args)
	{
		if (args.Contains("-version"))
		{
			AssemblyName assembly = Assembly.GetExecutingAssembly().GetName();
			Console.WriteLine($"{assembly.Name}:{assembly.Version}");
			return;
		}
		CreateHostBuilder(args).Build().Run();
	}

	public static IHostBuilder CreateHostBuilder(string[] args) =>
		Host.CreateDefaultBuilder(args)
		    .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
}