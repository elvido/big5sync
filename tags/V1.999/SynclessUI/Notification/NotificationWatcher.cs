﻿/*
 * 
 * Author: Steve Teo Wai Ming
 * 
 */

using System;
using System.Threading;
using System.Windows.Threading;
using Syncless.Core;
using Syncless.Notification;

namespace SynclessUI.Notification
{
    internal class NotificationWatcher : IQueueObserver
    {
        private MainWindow _main;

        private Thread workerThread;
        private readonly EventWaitHandle _wh = new AutoResetEvent(false);

        public NotificationWatcher(MainWindow main)
        {
            _main = main;

            ServiceLocator.UINotificationQueue().AddObserver(this);

            workerThread = new Thread(Run);
        }
        public void Update()
        {
            _wh.Set();
            //workerThread.Interrupt();
        }
        public delegate void RunDel();

        public void Start()
        {
            workerThread.Start();
        }
        public void Stop()
        {
            workerThread.Abort();
        }
        private void Run()
        {
            while (true)
            {
                if (!ServiceLocator.UINotificationQueue().HasNotification())
                {
                    try
                    {
                        _wh.WaitOne();
                        //Thread.Sleep(SLEEP_TIME);
                        continue;
                    }
                    catch (ThreadInterruptedException)
                    {

                    }
                    catch (ThreadAbortException)
                    {
                        break;
                    }
                }
                else
                {
                    try
                    {
                        AbstractNotification notification = ServiceLocator.UINotificationQueue().Dequeue();
                        Handle(notification);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        //Handle Exception by printing the debug log.
                    }
                }
            }

        }

        private void Handle(AbstractNotification notification)
        {
            if (notification.NotificationCode.Equals(NotificationCode.SyncStartNotification))
            {
                SyncStartNotification ssNotification = notification as SyncStartNotification;
                if (ssNotification != null)
                {
                    _main.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                    {
                        Console.WriteLine("Sync Start Notify");
                        _main.CurrentProgress = ssNotification.Progress;
                        new SyncProgressWatcher(_main, ssNotification.TagName, ssNotification.Progress);
                        Console.WriteLine("Sync Start Notify End");
                    }));
                }
            }
            else if (notification.NotificationCode.Equals(NotificationCode.SyncCompleteNotification))
            {
                SyncCompleteNotification scNotification = notification as SyncCompleteNotification;
                if (scNotification != null)
                {
                    _main.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        (Action)(() =>
                        {
                            Console.WriteLine("Sync Complete Notify");
                            _main.TagChanged(scNotification.TagName);
                            Console.WriteLine("Sync Complete Notify End");
                        }));
                }
            }
            else if (notification.NotificationCode.Equals(NotificationCode.NothingToSyncNotification))
            {
                NothingToSyncNotification ntsNotification = notification as NothingToSyncNotification;
                if (ntsNotification != null)
                {
                    _main.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        (Action)(() =>
                        {

                            _main.NotifyNothingToSync(ntsNotification.TagName);
                        }));
                }
            }
            else if (notification.NotificationCode.Equals(NotificationCode.AutoSyncCompleteNotification))
            {
                AutoSyncCompleteNotification ascNotification = notification as AutoSyncCompleteNotification;
                if (ascNotification != null)
                {
                    _main.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        (Action)(() =>
                        {
                            _main.NotifyAutoSyncComplete(ascNotification.Path);
                        }));
                }
            }
        }
    }
}