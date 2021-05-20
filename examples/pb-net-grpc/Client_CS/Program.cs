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
        private const int TotalRequests = 100;
        private const bool LogDetails = false;
        private static ParallelOptions parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = 50 };

        static void Main()
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;

            Test();
            TestReusingGrpcChannel();
            TestMultipleConcurrentTasks();
            TestMultipleConcurrentTasksReusingGrpcChannel();
            //CallGrpc().Wait();

            Console.WriteLine("Press [Enter] to exit");
            Console.ReadLine();
        }

        static async Task CallGrpc()
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

        static void Test()
        {
            Console.WriteLine($"{DateTime.Now} - Starting {nameof(Test)} {TotalRequests} tasks");

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < TotalRequests; i++)
            {
                if (LogDetails) Console.WriteLine($"{DateTime.Now} - Start task {i}");
                using var http = GrpcChannel.ForAddress("http://localhost:10042");
                var calculator = http.CreateGrpcService<ICalculator>();
                var result = calculator.GetTime();
                if (LogDetails) Console.WriteLine($"{DateTime.Now} - End task {i}: {result}");

            }
            stopwatch.Stop();

            Console.WriteLine($"{DateTime.Now} - {nameof(Test)} for {TotalRequests} tasks took: {stopwatch.ElapsedMilliseconds}ms");

        }

        static void TestReusingGrpcChannel()
        {
            using var http = GrpcChannel.ForAddress("http://localhost:10042");
            var calculator = http.CreateGrpcService<ICalculator>();
            Console.WriteLine($"{DateTime.Now} - Starting {nameof(TestReusingGrpcChannel)} {TotalRequests} tasks");

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < TotalRequests; i++)
            {
                if (LogDetails) Console.WriteLine($"{DateTime.Now} - Start task {i}");
                var result = calculator.GetTime();
                if (LogDetails) Console.WriteLine($"{DateTime.Now} - End task {i}: {result}");

            }
            stopwatch.Stop();

            Console.WriteLine($"{DateTime.Now} - {nameof(TestReusingGrpcChannel)} for {TotalRequests} tasks took: {stopwatch.ElapsedMilliseconds}ms");

        }

        static void TestMultipleConcurrentTasks()
        {
            Console.WriteLine($"{DateTime.Now} - Starting {nameof(TestMultipleConcurrentTasks)} {TotalRequests} tasks");
            
            var stopwatch = Stopwatch.StartNew();
            Parallel.For(0, TotalRequests, parallelOptions, (i) =>
            {
                if (LogDetails) Console.WriteLine($"{DateTime.Now} - Start task {i}");
                using var http = GrpcChannel.ForAddress("http://localhost:10042");
                var calculator = http.CreateGrpcService<ICalculator>();
                var result = calculator.GetTime();
                if (LogDetails) Console.WriteLine($"{DateTime.Now} - Result task {i} {result.Time}");
            });
            stopwatch.Stop();

            Console.WriteLine($"{DateTime.Now} - {nameof(TestMultipleConcurrentTasks)} for {TotalRequests} tasks took: {stopwatch.ElapsedMilliseconds}ms");

        }

        static void TestMultipleConcurrentTasksReusingGrpcChannel()
        {
            using var http = GrpcChannel.ForAddress("http://localhost:10042");
            var calculator = http.CreateGrpcService<ICalculator>();

            Console.WriteLine($"{DateTime.Now} - Starting {nameof(TestMultipleConcurrentTasksReusingGrpcChannel)} {TotalRequests} tasks");
            var stopwatch = Stopwatch.StartNew();
            Parallel.For(0, TotalRequests, parallelOptions, (i) =>
            {
                if (LogDetails) Console.WriteLine($"{DateTime.Now} - Start task {i}");
                var result = calculator.GetTime();
                if (LogDetails) Console.WriteLine($"{DateTime.Now} - Result task {i} {result.Time}");
            });
            stopwatch.Stop();

            Console.WriteLine($"{DateTime.Now} - {nameof(TestMultipleConcurrentTasksReusingGrpcChannel)} for {TotalRequests} tasks took: {stopwatch.ElapsedMilliseconds}ms");

        }
    }
}
