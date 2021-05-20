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
        private const int TaskCount = 500;
        private static ParallelOptions parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = 50 };

        static async Task Main()
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;

            //await Test();
            await Task.Run(TestMultipleConcurrentTasks);
            await Task.Run(TestMultipleConcurrentTasksReusingGrpcChannel);

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
            Console.WriteLine($"{DateTime.Now} - Starting {nameof(TestMultipleConcurrentTasks)} {TaskCount} tasks");
            
            var stopwatch = Stopwatch.StartNew();
            Parallel.For(0, TaskCount, parallelOptions, (i) =>
            {
                Console.WriteLine($"{DateTime.Now} - Start task {i}");
                using var http = GrpcChannel.ForAddress("http://localhost:10042");
                var calculator = http.CreateGrpcService<ICalculator>();
                var result = calculator.GetTime();
                Console.WriteLine($"{DateTime.Now} - Result task {i} {result.Time}");
            });
            stopwatch.Stop();

            Console.WriteLine($"{DateTime.Now} - {nameof(TestMultipleConcurrentTasks)} for {TaskCount} tasks took: {stopwatch.ElapsedMilliseconds}ms");

        }

        static void TestMultipleConcurrentTasksReusingGrpcChannel()
        {
            using var http = GrpcChannel.ForAddress("http://localhost:10042");
            var calculator = http.CreateGrpcService<ICalculator>();

            Console.WriteLine($"{DateTime.Now} - Starting {nameof(TestMultipleConcurrentTasksReusingGrpcChannel)} {TaskCount} tasks");
            var stopwatch = Stopwatch.StartNew();
            Parallel.For(0, TaskCount, parallelOptions, (i) =>
            {
                Console.WriteLine($"{DateTime.Now} - Start task {i}");
                var result = calculator.GetTime();
                Console.WriteLine($"{DateTime.Now} - Result task {i} {result.Time}");
            });
            stopwatch.Stop();

            Console.WriteLine($"{DateTime.Now} - {nameof(TestMultipleConcurrentTasksReusingGrpcChannel)} for {TaskCount} tasks took: {stopwatch.ElapsedMilliseconds}ms");

        }
    }
}
