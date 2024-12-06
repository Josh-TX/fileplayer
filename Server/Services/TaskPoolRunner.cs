using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class TaskPoolRunner
{
    private readonly int _poolSize;
    private readonly SemaphoreSlim _semaphore;

    // Constructor that accepts pool size (concurrency level)
    public TaskPoolRunner(int poolSize)
    {
        _poolSize = poolSize;
        _semaphore = new SemaphoreSlim(_poolSize);
    }

    // Run method that takes a list of items and a lambda to map T -> Task<U>
    public async Task<List<U>> Run<T,U>(IEnumerable<T> items, Func<T, Task<U>> taskFunc)
    {
        var tasks = new List<Task<U>>();

        foreach (var item in items)
        {
            // Wait for a free slot
            await _semaphore.WaitAsync();

            tasks.Add(RunTaskAsync(item, taskFunc));
        }

        // Wait for all tasks to complete
        var results = await Task.WhenAll(tasks);

        return results.ToList();
    }

    // Helper method to run the task and release the semaphore once done
    private async Task<U> RunTaskAsync<T,U>(T item, Func<T, Task<U>> taskFunc)
    {
        try
        {
            return await taskFunc(item);
        }
        finally
        {
            // Release the semaphore when the task is done
            _semaphore.Release();
        }
    }
}
