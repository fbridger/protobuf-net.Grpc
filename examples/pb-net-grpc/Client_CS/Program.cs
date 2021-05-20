using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using MegaCorp;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Client;
using Shared_CS;

namespace Client_CS
{
    class Program
    {
        static async Task Main()
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;

            //await Test();
            await Task.Run(TestMultipleConcurrentTasks);

            Console.WriteLine("Press [Enter] to exit");
            Console.ReadLine();
        }

        static async Task Test()
        {
            using var http = GrpcChannel.ForAddress("http://localhost:10042");
            var calculator = http.CreateGrpcService<ICalculator>();
            var result = await calculator.MultiplyAsync(new MultiplyRequest { X = 12, Y = 4 });
            Console.WriteLine(result.Result); // 48

            var clock = http.CreateGrpcService<ITimeService>();
            var counter = http.CreateGrpcService<ICounter>();
            using var cancel = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            var options = new CallOptions(cancellationToken: cancel.Token);

            try
            {
                await foreach (var time in clock.SubscribeAsync(new CallContext(options)))
                {
                    Console.WriteLine($"The time is now: {time.Time}");
                    var currentInc = await counter.IncrementAsync(new IncrementRequest { Inc = 1 });
                    Console.WriteLine($"Time received {currentInc.Result} times");
                }
            }
            catch (RpcException ex) { Console.WriteLine(ex); }
            catch (OperationCanceledException) { }
        }

        static void TestMultipleConcurrentTasks()
        {
            using var http = GrpcChannel.ForAddress("http://localhost:10042");
            var calculator = http.CreateGrpcService<ICalculator>();

            var pingTasks = new Task[1000];
            Console.WriteLine($"{DateTime.Now} - Starting {pingTasks.Length} tasks");
            var stopwatch = Stopwatch.StartNew();
            for (var i = 0; i < pingTasks.Length; i++)
            {
                pingTasks[i] = Task.Factory.StartNew(() =>
                {
                    var result = calculator.GetTime();
                    Console.WriteLine($"{DateTime.Now} - Result {result}");
                });
            }
            Task.WaitAll(pingTasks);
            stopwatch.Stop();

            Console.WriteLine($"{DateTime.Now} - Ping for {pingTasks.Length} tasks took: {stopwatch.ElapsedMilliseconds}ms");

        }
    }
}
