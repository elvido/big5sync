﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Management;

namespace Syncless.Core
{
    public class DeviceWatcher
    {
        private static DeviceWatcher _instance;
        public static DeviceWatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DeviceWatcher();
                }
                return _instance;
            }
        }
        private List<DriveInfo> connectedDrives ;

        private DeviceWatcher()
        {
            connectedDrives = RetrieveAllDrives();
            AddInsertUSBHandler();
            AddRemoveUSBHandler();
        }

        private List<DriveInfo> RetrieveAllDrives()
        {
            List<DriveInfo> drives = new List<DriveInfo>();
            foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
            {
                if (driveInfo.DriveType == DriveType.Fixed || driveInfo.DriveType == DriveType.Removable)
                {
                    drives.Add(driveInfo);
                }
            }
            return drives;
        }

        private void AddInsertUSBHandler()
        {
            WqlEventQuery query;
            ManagementScope scope = new ManagementScope("root\\CIMV2");
            scope.Options.EnablePrivileges = true;
            ManagementEventWatcher watcher = null;
            try
            {
                query = new WqlEventQuery();
                query.EventClassName = "__InstanceCreationEvent";
                query.WithinInterval = new TimeSpan(0, 0, 3);
                query.Condition = "TargetInstance ISA 'Win32_USBControllerdevice'";
                watcher = new ManagementEventWatcher(scope, query);
                watcher.EventArrived += USBInserted;
                watcher.Start();
            }
            catch(Exception e)
            {
                if (watcher != null)
                {
                    watcher.Stop();
                }
                throw e;
            }
        }

        private void USBInserted(object sender, EventArrivedEventArgs e)
        {
            IMonitorControllerInterface control = ServiceLocator.MonitorI;
            List<DriveInfo> newConnectedDrives = RetrieveAllDrives();
            int i = 0;
            foreach (DriveInfo drive in newConnectedDrives)
            {
                if (drive.Name.Equals(connectedDrives[i].Name))
                {
                    i++;
                }
                else
                {
                    control.HandleDriveChange(drive, Syncless.Monitor.DriveChangeType.DRIVE_IN);
                }
            }
            connectedDrives = newConnectedDrives;
        }

        private void AddRemoveUSBHandler()
        {
            WqlEventQuery query;
            ManagementScope scope = new ManagementScope("root\\CIMV2");
            scope.Options.EnablePrivileges = true;
            ManagementEventWatcher watcher = null;
            try
            {
                query = new WqlEventQuery();
                query.EventClassName = "__InstanceDeletionEvent";
                query.WithinInterval = new TimeSpan(0, 0, 1);
                query.Condition = "TargetInstance ISA 'Win32_USBControllerdevice'";
                watcher = new ManagementEventWatcher(scope, query);
                watcher.EventArrived += USBRemoved;
                watcher.Start();
            }
            catch (Exception e)
            {
                if (watcher != null)
                {
                    watcher.Stop();
                }
                throw e;
            }
        }

        private void USBRemoved(object sender, EventArrivedEventArgs e)
        {
            IMonitorControllerInterface control = ServiceLocator.MonitorI;
            List<DriveInfo> newConnectedDrives = RetrieveAllDrives();
            int i = 0;
            foreach (DriveInfo drive in connectedDrives)
            {
                if (drive.Name.Equals(newConnectedDrives[i].Name))
                {
                    i++;
                }
                else
                {
                    control.HandleDriveChange(drive, Syncless.Monitor.DriveChangeType.DRIVE_OUT);
                }
            }
            connectedDrives = newConnectedDrives;
        }

    }
}
