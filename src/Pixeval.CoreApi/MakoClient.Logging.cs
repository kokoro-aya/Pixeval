using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pixeval.CoreApi.Net;
using Pixeval.CoreApi.Net.EndPoints;
using Pixeval.Utilities;

namespace Pixeval.CoreApi;

public partial class MakoClient
{
    private async Task<T[]> RunWithLoggerAsync<T>(Func<IAppApiEndPoint, Task<T[]>> task)
    {
        try
        {
            EnsureNotCancelled();

            return await task(MakoServices.GetRequiredService<IAppApiEndPoint>());
        }
        catch (Exception e)
        {
            LogException(e);
            return [];
        }
    }

    private async Task<IEnumerable<T>> RunWithLoggerAsync<T>(Func<IAppApiEndPoint, Task<IEnumerable<T>>> task)
    {
        try
        {
            EnsureNotCancelled();

            return await task(MakoServices.GetRequiredService<IAppApiEndPoint>());
        }
        catch (Exception e)
        {
            LogException(e);
            return [];
        }
    }

    private async Task<HttpResponseMessage> RunWithLoggerAsync(Func<IAppApiEndPoint, Task<HttpResponseMessage>> task)
    {
        try
        {
            EnsureNotCancelled();

            return await task(MakoServices.GetRequiredService<IAppApiEndPoint>());
        }
        catch (Exception e)
        {
            LogException(e);

            return new HttpResponseMessage(HttpStatusCode.RequestTimeout);
        }
    }

    private async Task RunWithLoggerAsync(Func<IAppApiEndPoint, Task> task)
    {
        try
        {
            EnsureNotCancelled();

            await task(MakoServices.GetRequiredService<IAppApiEndPoint>());
        }
        catch (Exception e)
        {
            LogException(e);
        }
    }

    private async Task<T> RunWithLoggerAsync<T>(Func<IAppApiEndPoint, Task<T>> task) where T : IFactory<T>
    {
        try
        {
            EnsureNotCancelled();

            return await task(MakoServices.GetRequiredService<IAppApiEndPoint>());
        }
        catch (Exception e)
        {
            LogException(e);
            return T.CreateDefault();
        }
    }

    private async Task<T> RunWithLoggerAsync<T>(Func<Task<T>> task) where T : IFactory<T>
    {
        try
        {
            EnsureNotCancelled();

            return await task();
        }
        catch (Exception e)
        {
            LogException(e);
            return T.CreateDefault();
        }
    }

    private async Task<T> RunWithLoggerAsync<T>(Func<Task<Result<T>>> task, Func<T> createDefault)
    {
        try
        {
            EnsureNotCancelled();

            var result = await task();
            switch (result)
            {
                case Result<T>.Success { Value: var value }:
                    return value;
                case Result<T>.Failure { Cause: { } e }:
                    LogException(e);
                    return createDefault();
                default:
                    return ThrowUtils.ArgumentOutOfRange<Result<T>, T>(result);
            }
        }
        catch (Exception e)
        {
            LogException(e);
            return createDefault();
        }
    }

    private Task<T> RunWithLoggerAsync<T>(Func<Task<Result<T>>> task) where T : IFactory<T>
    {
        return RunWithLoggerAsync(task, T.CreateDefault);
    }

    internal void LogException(Exception e)
    {
        Logger.LogError("MakoClient Exception", e);
        var now = DateTime.Now;
        if (now < CoolDown)
            return;
        CoolDown = now.AddSeconds(5);
        HttpClient.DefaultProxy = GetCurrentSystemProxy();
        var oldCollection = ServiceCollection;
        var old = MakoServices;
        ServiceCollection = [];
        MakoServices = BuildServiceProvider(ServiceCollection);
        Dispose(oldCollection);
        old.Dispose();
    }

    private DateTime CoolDown { get; set; } = DateTime.Now.AddSeconds(5);

    static MakoClient()
    {
        var type = typeof(HttpClient).Assembly.GetType("System.Net.Http.SystemProxyInfo");
        var method = type?.GetMethod("ConstructSystemProxy");
        var @delegate = method?.CreateDelegate<Func<IWebProxy>>();

        GetCurrentSystemProxy = @delegate ?? ThrowUtils.Throw<MissingMethodException, Func<IWebProxy>>("Unable to find proxy functions");
    }

    private static Func<IWebProxy> GetCurrentSystemProxy { get; }
}
