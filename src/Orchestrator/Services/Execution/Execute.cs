using System.Text;
using Orchestrator.Models;
using Wasmtime;

namespace Orchestrator.Services.Execution;

public class Execute(Engine engine)
{
    public async Task<ApplicationOutput> DoInference(ApplicationInput input)
    {
        using var module = Module.FromFile(engine, Path.Combine(Environment.CurrentDirectory, "Apps/app.wasm"));
        using var linker = new Linker(engine);
        using var store = new Store(engine);
        linker.AllowShadowing = true;
        Instance instance = null;
        
        linker.Define(
            "runtime",
            "_http_post_request_runtime",
            Function.FromCallback(store,  (int a) =>
            {
                var inputString =  instance.GetMemories().First().Memory.ReadNullTerminatedString(a);
                var obj = System.Text.Json.JsonSerializer.Deserialize<HttpPostRequest>(inputString);
                var result =  SendPostRequest(obj).Result;
                var allocator = instance.GetFunction<int, int>("allocatemem");
                var allocatedMemoryPointer = allocator(Encoding.ASCII.GetByteCount(result!));
                instance.GetMemories().First().Memory.WriteString(allocatedMemoryPointer, result);
                return allocatedMemoryPointer;
            })
        );
        instance = linker.Instantiate(store, module);
        var run = instance.GetFunction<int, int>("run");
        var allocator = instance.GetFunction<int, int>("allocatemem");
        var free = instance.GetFunction<int, int>("freemem");
        var allocatedMemoryPointer = allocator(Encoding.ASCII.GetByteCount(input.Input!));
        instance.GetMemories().First().Memory.WriteString(allocatedMemoryPointer, input.Input!);
        var outResultPointer = run.Invoke(allocatedMemoryPointer);
        free(allocatedMemoryPointer);
        var stringFromPointer = instance.GetMemories().First().Memory.ReadNullTerminatedString(outResultPointer);
        return new ApplicationOutput
        {
            Output = stringFromPointer
        };
        
    }

    public static async Task<string> SendPostRequest(HttpPostRequest request)
    {
        var client = new HttpClient();
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, request.URL);

        foreach (var header  in request.Headers)
        {
            httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
         var content = new StringContent(
             request.Body,
            null, "application/json");
         httpRequest.Content = content;
        var response = await client.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadAsStringAsync());

    }

}