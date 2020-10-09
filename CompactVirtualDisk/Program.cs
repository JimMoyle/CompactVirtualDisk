using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;

namespace CompactVirtualDisk
{
    class Program
    {
        static bool CompactVHD(bool FileSystemAware)
        {
            string vhdx = @"c:\Temp\vhdx\test - Copy.vhdx";
            bool bResult = false;
            VirtDisk.VIRTUAL_STORAGE_TYPE vst = new VirtDisk.VIRTUAL_STORAGE_TYPE();
            vst.DeviceId = VirtDisk.VIRTUAL_STORAGE_TYPE_DEVICE_VHDX;
            vst.VendorId = VirtDisk.VIRTUAL_STORAGE_TYPE_VENDOR_MICROSOFT;
            VirtDisk.VIRTUAL_DISK_ACCESS_MASK mask = VirtDisk.VIRTUAL_DISK_ACCESS_MASK.VIRTUAL_DISK_ACCESS_METAOPS;
            VirtualDiskSafeHandle virtualDiskHandle = new VirtualDiskSafeHandle();

            if (FileSystemAware)
            {
                mask |= VirtDisk.VIRTUAL_DISK_ACCESS_MASK.VIRTUAL_DISK_ACCESS_ATTACH_RO;
            }

            UInt32 res = 0;
            res = VirtDisk.OpenVirtualDisk(ref vst, vhdx, mask, VirtDisk.OPEN_VIRTUAL_DISK_FLAG.OPEN_VIRTUAL_DISK_FLAG_NONE, IntPtr.Zero, ref virtualDiskHandle);
            if (res == VirtDisk.ERROR_SUCCESS)
            {
                ManualResetEvent stateChangeEvent = new ManualResetEvent(false);
                VirtDisk.OVERLAPPED Overlapped = new VirtDisk.OVERLAPPED();
                Overlapped.hEvent = stateChangeEvent.SafeWaitHandle.DangerousGetHandle();
                VirtDisk.VIRTUAL_DISK_PROGRESS Progress = new VirtDisk.VIRTUAL_DISK_PROGRESS();
           
                res = VirtDisk.CompactVirtualDisk(virtualDiskHandle, VirtDisk.COMPACT_VIRTUAL_DISK_FLAG.COMPACT_VIRTUAL_DISK_FLAG_NONE, IntPtr.Zero, ref Overlapped);
                if (res != VirtDisk.ERROR_SUCCESS && res != VirtDisk.ERROR_IO_PENDING)
                {
                    // throw some nice error message here perhaps as something went wrong
                    return false;
                }

                bool bPending = true;
                while (bPending)
                {
                    res = VirtDisk.GetVirtualDiskOperationProgress(virtualDiskHandle, ref Overlapped, ref Progress);
                    if (res == VirtDisk.ERROR_SUCCESS && Progress.OperationStatus == VirtDisk.ERROR_IO_PENDING)
                    {
                        var Percentage = (Progress.CurrentValue / Progress.CompletionValue) *100;
                        Console.WriteLine("Percentage: {0}", Percentage);
                        bPending = stateChangeEvent.WaitOne(100); // use no interval to wait until finished, but using some interval gives you time to show some busy indicator
                    }
                    // #todo error handling
                    else
                    {
                        break;
                    }
                }

                bResult = Progress.OperationStatus == VirtDisk.ERROR_SUCCESS;
            }

            return bResult;
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Size: {0}", Marshal.SizeOf(typeof(VirtDisk.VIRTUAL_STORAGE_TYPE)));
            string vhdx = @"c:\Temp\vhdx\test.vhdx";
            VirtDisk.VIRTUAL_STORAGE_TYPE vst = new VirtDisk.VIRTUAL_STORAGE_TYPE();
            vst.DeviceId = VirtDisk.VIRTUAL_STORAGE_TYPE_DEVICE_VHDX;
            vst.VendorId = VirtDisk.VIRTUAL_STORAGE_TYPE_VENDOR_MICROSOFT;
//            IntPtr hVirtDisk = IntPtr.Zero;
            UInt32 res;
            VirtualDiskSafeHandle virtualDiskHandle = new VirtualDiskSafeHandle();
            res = VirtDisk.OpenVirtualDisk(ref vst, vhdx, VirtDisk.VIRTUAL_DISK_ACCESS_MASK.VIRTUAL_DISK_ACCESS_GET_INFO, VirtDisk.OPEN_VIRTUAL_DISK_FLAG.OPEN_VIRTUAL_DISK_FLAG_NONE, IntPtr.Zero, ref virtualDiskHandle);
            if (res == VirtDisk.ERROR_SUCCESS)
            {
                VirtDisk.GET_VIRTUAL_DISK_INFO vdi = new VirtDisk.GET_VIRTUAL_DISK_INFO();
                vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_PROVIDER_SUBTYPE;
                ulong size = 32;

                res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref size, ref vdi, IntPtr.Zero);

                if (res == VirtDisk.ERROR_SUCCESS)
                {
                    switch (vdi.ProviderSubtype)
                    {
                        case 2:
                            Console.WriteLine("Fixed");
                            break;
                        case 3:
                            Console.WriteLine("Dynamically Expandible (sparse)");
                            break;
                        case 4:
                            Console.WriteLine("Differencing");
                            break;
                        default:
                            Console.WriteLine("Unknown");
                            break;
                    }
                }

                vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_SIZE;
                res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref size, ref vdi, IntPtr.Zero);

                if (res == VirtDisk.ERROR_SUCCESS)
                {
                    Console.WriteLine("BlockSize: {0}  SectorSize: {1}", vdi.Size.BlockSize, vdi.Size.SectorSize);
                    Console.WriteLine("PhysicalSize: {0} VirtualSize: {1}", vdi.Size.PhysicalSize, vdi.Size.VirtualSize);
                }

                vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_CHANGE_TRACKING_STATE;
                res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref size, ref vdi, IntPtr.Zero);
                if (res == VirtDisk.ERROR_SUCCESS)
                {
                    Console.WriteLine("Change Tracking Enabled: {0}", vdi.ChangeTracking.Enabled);
                }

                vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_IS_4K_ALIGNED;
                res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref size, ref vdi, IntPtr.Zero);
                if (res == VirtDisk.ERROR_SUCCESS)
                {
                    Console.WriteLine("Is aligned at 4KB: {0}", vdi.Is4kAligned);
                }

                vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_FRAGMENTATION;
                res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref size, ref vdi, IntPtr.Zero);
                if (res == VirtDisk.ERROR_SUCCESS)
                {
                    Console.WriteLine("Fragmention: {0}%", vdi.FragmentationPercentage);
                }

                vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_IS_LOADED;
                res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref size, ref vdi, IntPtr.Zero);
                if (res == VirtDisk.ERROR_SUCCESS)
                {
                    Console.WriteLine("Mounted: {0}", vdi.IsLoaded);
                }

                vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_VIRTUAL_DISK_ID;
                res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref size, ref vdi, IntPtr.Zero);
                if (res == VirtDisk.ERROR_SUCCESS)
                {
                    Console.WriteLine("Identifier: {0}", vdi.Identifier);
                }

                vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_SMALLEST_SAFE_VIRTUAL_SIZE;
                res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref size, ref vdi, IntPtr.Zero);
                if (res == VirtDisk.ERROR_SUCCESS)
                {
                    Console.WriteLine("Smallest Safe Virtual Size: {0}", vdi.SmallestSafeVirtualSize);
                }

                vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_PHYSICAL_DISK;
                res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref size, ref vdi, IntPtr.Zero);
                if (res == VirtDisk.ERROR_SUCCESS)
                {
                    Console.WriteLine("Physical Disk Info, Remote: {0} LogicalSectorSize: {1} PhysicalSectorSize: {2}", vdi.PhysicalDisk.IsRemote, vdi.PhysicalDisk.LogicalSectorSize, vdi.PhysicalDisk.PhysicalSectorSize);
                }

                vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_VHD_PHYSICAL_SECTOR_SIZE;
                res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref size, ref vdi, IntPtr.Zero);
                if (res == VirtDisk.ERROR_SUCCESS)
                {
                    Console.WriteLine("Physical Sector Size: {0}", vdi.VhdPhysicalSectorSize);
                }

                vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_PARENT_IDENTIFIER;
                res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref size, ref vdi, IntPtr.Zero);
                if (res == VirtDisk.ERROR_SUCCESS)
                {
                    Console.WriteLine("Parent Identifier (only for differencing disks): {0}", vdi.ParentIdentifier);

                    vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_PARENT_LOCATION;
                    res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref size, ref vdi, IntPtr.Zero);
                    if (res == VirtDisk.ERROR_SUCCESS)
                    {
                        Console.WriteLine("Parent Location (only for differencing disks): Resolved: {0} Location: {1}", vdi.ParentLocation.ParentResolved, vdi.ParentLocation.ParentLocationBuffer);
                    }

                    vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_PARENT_TIMESTAMP;
                    res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref size, ref vdi, IntPtr.Zero);
                    if (res == VirtDisk.ERROR_SUCCESS)
                    {
                        Console.WriteLine("Parent TimeStamp (only for differencing disks): Resolved: {0}", vdi.ParentTimestamp);
                    }
                }
                else if (res == VirtDisk.ERROR_VHD_INVALID_TYPE)
                {
                    Console.WriteLine("Not a Differencing disk");
                }


                bool bRes;
                bRes = CompactVHD(true);
                Console.WriteLine("Compact(True): {0}", bRes);
                bRes = CompactVHD(false);
                Console.WriteLine("Compact(False): {0}", bRes);
            }

        }

    }
}
