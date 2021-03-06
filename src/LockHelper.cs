using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DockerSub
{
    public static class LockHelper
    {
        public static T DoubleCheckedLock<T>(object lockObj, ref T value, Func<T, bool> checkLockCondition, Func<T> getValue)
        {
            if (checkLockCondition(value))
            {
                lock (lockObj)
                {
                    if (checkLockCondition(value))
                    {
                        value = getValue();
                    }
                }
            }

            return value;
        }

        public static TValue DoubleCheckedLockLookup<TKey, TValue>(
            object lockObj, IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> getValue)
        {
            if (!dictionary.TryGetValue(key, out TValue value))
            {
                lock (lockObj)
                {
                    if (!dictionary.TryGetValue(key, out value))
                    {
                        value = getValue();
                        dictionary.Add(key, value);
                    }
                }
            }

            return value;
        }

        public static async Task<T> DoubleCheckedLockAsync<T>(
            this SemaphoreSlim semaphore, Func<T> getValue, Func<T, bool> checkLockCondition, Func<Task> lockedAction)
        {
            T value = getValue();
            if (checkLockCondition(value))
            {
                await semaphore.LockAsync(async () =>
                {
                    value = getValue();
                    if (checkLockCondition(value))
                    {
                        await lockedAction();
                        value = getValue();
                    }
                });
            }

            return value;
        }

        public static async Task<TValue> DoubleCheckedLockLookupAsync<TKey, TValue>(
            this SemaphoreSlim semaphore, IDictionary<TKey, TValue> dictionary, TKey key, Func<Task<TValue>> getValue)
        {
            if (!dictionary.TryGetValue(key, out TValue value))
            {
                await semaphore.LockAsync(async () =>
                {
                    if (!dictionary.TryGetValue(key, out value))
                    {
                        value = await getValue();
                        dictionary.Add(key, value);
                    }
                });
            }

            return value;
        }

        public static async Task LockAsync(this SemaphoreSlim semaphore, Func<Task> func)
        {
            await semaphore.WaitAsync();

            try
            {
                await func();
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
