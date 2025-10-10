namespace Vibecheck.Server;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var server = new ServerHost();
        await server.Run(null, args);

        return 0;
    }
}
