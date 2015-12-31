//#define USE_LOCKS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreadingTest1
{
    using System.Collections.Concurrent;
    using System.IO;
    using System.Runtime.Remoting.Channels;
    using System.Threading;

    internal class Program
    {
        private static void Main(string[] args)
        {
            //  http://holsee.com/tag/c/
            var go = DateTime.Now.AddSeconds(2);

            //TEST 1
            //var tasks = (from i in Enumerable.Range(0,100)
            //            select makeThread(go)).ToArray();

            //Task.WaitAll(tasks);

            //var collectionClasses = tasks.Select(x => x.Result).ToArray();


            ConcurrentBag<CollectionClass> collectionClasses = new ConcurrentBag<CollectionClass>();
            Parallel.For(0, 10, i =>
            {
                var c = ThreadWork(go);
                collectionClasses.Add(c);
            });
            //Console.WriteLine("{0} Items",collectionClasses.Count);

            Console.WriteLine();
            Console.WriteLine("=============================");
            var cCheck = collectionClasses.First();
            if (collectionClasses.All(c => Object.ReferenceEquals(c, cCheck)))
            {
                Console.WriteLine("All Good on object reference");
            }
            else
            {
                Console.WriteLine("Fail object reference");
            }

            if (collectionClasses.All(x => x.TotalNumbers == cCheck.TotalNumbers))
            {
                Console.WriteLine("All Good on Total numbers");
            }
            else
            {
                Console.WriteLine("Fail total numbers");
            }
        }

        private static Task<CollectionClass> makeThread(DateTime go)
        {
            return Task.Run(() => { return ThreadWork(go); });
        }

        private static CollectionClass ThreadWork(DateTime go,bool safe = false)
        {
            while (DateTime.Now < go)
            {
                Thread.Sleep(10);
            }

            if (safe)
            {
                return ThreadSafeCacheClass.Numbers;
            }
            else
            {
                return CacheClass.Numbers;
            }
        }
    }

    internal class ThreadSafeCacheClass
    {
        private static Lazy<CollectionClass> _numbers = new Lazy<CollectionClass>(() => new CollectionClass());

        public static CollectionClass Numbers
        {
            get
            {
                try
                {
                    _numbers.Value.Refresh();
                }
                catch (IndexOutOfRangeException iex)
                {
                    Console.WriteLine("Index out of range for ID " + _numbers.Value.InstanceID);
                }
                catch (ArgumentException argex)
                {
                    Console.WriteLine("Argument exception for ID " + _numbers.Value.InstanceID);
                }
                catch (NullReferenceException nre)
                {
                    Console.WriteLine("NullReference exception, numbers not initialized");
                }


                return _numbers.Value;
            }
        }
    }

    internal class CacheClass
    {
        private static CollectionClass _numbers;

        public static CollectionClass Numbers
        {
            get
            {
                if (_numbers == null)
                {
                    _numbers = new CollectionClass();
                }

                try
                {
                    _numbers.Refresh();
                }
                catch (IndexOutOfRangeException iex)
                {
                    Console.WriteLine("Index out of range for ID " + _numbers.InstanceID);
                }
                catch (ArgumentException argex)
                {
                    Console.WriteLine("Argument exception for ID " + _numbers.InstanceID);
                }
                catch (NullReferenceException nre)
                {
                    Console.WriteLine("NullReference exception, numbers not initialized");
                }


                return _numbers;
            }
        }
    }

    internal class CollectionClass
    {
        private string _instanceID;
#if USE_LOCKS
        private ReaderWriterLockSlim _lock;
#endif
        private List<int> _numbers;

        public CollectionClass()
        {
            _instanceID = Guid.NewGuid().ToString();
            Console.WriteLine("Object Created");
#if USE_LOCKS
            _lock = new ReaderWriterLockSlim();
#endif
        }

        public void Refresh()
        {
#if USE_LOCKS
            if (_lock.IsReadLockHeld)
            {
                return;
            }

            _lock.EnterWriteLock();
#endif


            try
            {
                _numbers = new List<int>();
                for (int i = 0; i < 10; i++)
                {
                    _numbers.Add(i);
                    //Thread.Sleep(2);
                }
                Console.WriteLine("Refresh Ran");
            }
            finally
            {
#if USE_LOCKS
                _lock.ExitWriteLock();
#endif
            }
        }

        public int TotalNumbers
        {
            get
            {
#if USE_LOCKS
                _lock.EnterReadLock();
#endif
                try
                {
                    if (_numbers != null)
                    {
                        return _numbers.Count;
                    }
                    else
                    {
                        Console.WriteLine("Attempt to check total numbers of {0}, but wasa null", _instanceID);
                        return 0;
                    }
                }
                finally
                {
#if USE_LOCKS
                    _lock.ExitReadLock();
#endif
                }
            }
        }

        public string InstanceID
        {
            get { return _instanceID; }
        }
    }
}