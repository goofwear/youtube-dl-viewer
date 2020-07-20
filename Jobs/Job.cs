﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace youtube_dl_viewer.Jobs
{
    public abstract class Job
    {
        public readonly string Source;
        
        protected readonly List<IJobProxy> Proxies = new List<IJobProxy>();
        protected Thread Thread;

        public int ProxyCount => Proxies.Count;
        
        public bool Running = false;

        protected object SuperLock => _manager.LockObject;
        
        public abstract string Name { get; }

        private AbsJobManager _manager;
        
        protected Job(AbsJobManager man,string source)
        {
            Source = source;
            _manager = man;
        }

        public void Start()
        {
            Running = true;
            Thread = new Thread(JobRun);
            Thread.Start();
        }

        private void JobRun()
        {
            try
            {
                var sw = Stopwatch.StartNew();

                Run();

                Console.Out.WriteLine($"Job [{Name}] finished after {(sw.Elapsed):g}");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error in Job:");
                Console.Error.WriteLine(e);
            }
            finally
            {
                Unregister();
                Running = false;
                KillProxies();
                
                GC.Collect(); // ?!?
            }
        }

        protected void KillProxies()
        {
            lock (SuperLock)
            {
                if (!Proxies.Any()) return;
                
                Console.Out.WriteLine($"Manually detach {Proxies.Count} proxies from Job [{Name}]");
                        
                foreach (var proxy in Proxies) proxy.Kill();
                Proxies.Clear();
            }
        }

        public void Unregister()
        {
            _manager.Unregister(this);
        }

        public abstract void Abort();
        
        public JobProxy<T> AddProxy<T>(JobProxy<T> proxy) where T : Job // Only call me in lock(...)
        {
            Proxies.Add(proxy);
            return proxy;
        }

        public void UnregisterProxy(IJobProxy proxy)
        {
            lock (SuperLock)
            {
                Console.Out.WriteLine($"Detach proxy from Job [{Name}] ({ProxyCount - 1} attached proxies)");
                Proxies.Remove(proxy);
            }
        }
        
        protected abstract void Run();
    }
}