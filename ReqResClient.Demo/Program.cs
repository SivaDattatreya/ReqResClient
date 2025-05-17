using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReqResClient.Extensions;
using ReqResClient.Interfaces;
using ReqResClient.Models;

public class Program
{
    static async Task Main(string[] args)
    {
        //Build configuration
        var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

        //Configure services
        var serviceProvider = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            })
            .AddReqResClient(configuration)
            .BuildServiceProvider();

        var userService = serviceProvider.GetRequiredService<IExternalUserService>();

        Console.WriteLine("ReqRes API Client Demo\n");

        try
        {
            //Get a single user
            Console.WriteLine("Fetching user with Id 2...");
            User user = await userService.GetUserByIdAsync(2);
            Console.WriteLine($"User found: {user.FirstName} {user.LastName} ({user.Email})\n");

            //Get all users
            Console.WriteLine("Fetching all users....");
            IEnumerable<User> users = await userService.GetAllUsersAsync();
            Console.WriteLine("All Users:");

            foreach(var u in users)
            {
                Console.WriteLine($"- {u.Id}:{u.FirstName} {u.LastName} ({u.Email})");
            }

            //Try to get a non-existent user
            Console.WriteLine("\n trying to fetch a non-existent user(ID 999)....");
            try
            {
                await userService.GetUserByIdAsync(999);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error : {ex.Message}");
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine($"An error occurred : {ex.Message}");
        }
        Console.WriteLine("\n Press any key to exit...");
        Console.ReadKey();
    }
}