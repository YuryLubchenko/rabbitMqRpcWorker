using System;
using System.Threading.Tasks;

namespace Sender
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var rpcClient = new RpcClient();
            
            var exit = false;

            while (!exit)
            {
                Console.WriteLine("Enter fib number or q to exit:");
                var str = Console.ReadLine();

                if (string.Equals("q", str, StringComparison.InvariantCultureIgnoreCase))
                {
                    exit = true;
                    continue;
                }

                if (!int.TryParse(str, out var n))
                {
                    Console.WriteLine("Invalid format");
                    continue;
                }

                Console.WriteLine($"Requesting {n}-th fib number");

                var fib = await rpcClient.CallAsync(n);

                Console.WriteLine($"{n}-th fib number is {fib}");
            }
        }
    }
}