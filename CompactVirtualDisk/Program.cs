using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;
using System.ComponentModel;
using System.IO;

namespace CompactVirtualDisk
{
    class Program
    {
        static bool CompactVHD(string Filename, bool FileSystemAware = false)
        {
            bool bResult = false;
            UInt32 res;

            // handle to the vhd(x) file
            VirtualDiskSafeHandle virtualDiskHandle = new VirtualDiskSafeHandle();

            // create and initialize VIRTUAL_STORAGE_TYPE struct
            VirtDisk.VIRTUAL_STORAGE_TYPE vst = new VirtDisk.VIRTUAL_STORAGE_TYPE
            {
                // default to vhdx
                DeviceId = VirtDisk.VIRTUAL_STORAGE_TYPE_DEVICE_VHDX,
                VendorId = VirtDisk.VIRTUAL_STORAGE_TYPE_VENDOR_MICROSOFT
            };

            // if extension = vhx then change deviceid
            if (Path.GetExtension(Filename).Equals(".vhd", StringComparison.CurrentCultureIgnoreCase))
            {
                vst.DeviceId = VirtDisk.VIRTUAL_STORAGE_TYPE_DEVICE_VHD;
            }

            VirtDisk.VIRTUAL_DISK_ACCESS_MASK mask = VirtDisk.VIRTUAL_DISK_ACCESS_MASK.VIRTUAL_DISK_ACCESS_METAOPS;

            if (FileSystemAware)
            {
                mask |= VirtDisk.VIRTUAL_DISK_ACCESS_MASK.VIRTUAL_DISK_ACCESS_ATTACH_RO;
            }

            res = VirtDisk.OpenVirtualDisk(ref vst, Filename, mask, VirtDisk.OPEN_VIRTUAL_DISK_FLAG.OPEN_VIRTUAL_DISK_FLAG_NONE, IntPtr.Zero, ref virtualDiskHandle);
            if (res != VirtDisk.ERROR_SUCCESS)
            {
                throw new Win32Exception((int)res, "Failed to open Virtual Disk");
            }

            using (virtualDiskHandle)
            {
                ManualResetEvent stateChangeEvent = new ManualResetEvent(false);
                VirtDisk.OVERLAPPED Overlapped = new VirtDisk.OVERLAPPED();
                Overlapped.hEvent = stateChangeEvent.SafeWaitHandle.DangerousGetHandle();
                VirtDisk.VIRTUAL_DISK_PROGRESS Progress = new VirtDisk.VIRTUAL_DISK_PROGRESS();

                res = VirtDisk.CompactVirtualDisk(virtualDiskHandle, VirtDisk.COMPACT_VIRTUAL_DISK_FLAG.COMPACT_VIRTUAL_DISK_FLAG_NONE, IntPtr.Zero, ref Overlapped);
                if (res != VirtDisk.ERROR_SUCCESS && res != VirtDisk.ERROR_IO_PENDING)
                {
                    throw new Win32Exception((int)res, "Failed to compact Virtual Disk");
                }

                bool bPending = true;
                while (bPending)
                {
                    res = VirtDisk.GetVirtualDiskOperationProgress(virtualDiskHandle, ref Overlapped, ref Progress);
                    if (res != VirtDisk.ERROR_SUCCESS)
                    {
                        throw new Win32Exception((int)res, "Failed to compact Virtual Disk (GetVirtualDiskOperationProgress)");
                    }

                    else if (Progress.OperationStatus == VirtDisk.ERROR_IO_PENDING)
                    {
                        var Percentage = (Progress.CurrentValue / Progress.CompletionValue) * 100;
                        Console.WriteLine("Compact Percentage: {0}%", Percentage);
                        bPending = stateChangeEvent.WaitOne(100); // use no interval to wait until finished, but using some interval gives you time to show some busy indicator
                    }
                    // # failure or completed
                    else
                    {
                        break;
                    }
                }

                // did operation complete successfully?
                bResult = Progress.OperationStatus == VirtDisk.ERROR_SUCCESS;
            }

            return bResult;
        }
        static void PrintVirtualDiskInfo(string Filename)
        {
            // handle to the vhd(x) file
            VirtualDiskSafeHandle virtualDiskHandle = new VirtualDiskSafeHandle();

            // create and initialize VIRTUAL_STORAGE_TYPE struct
            VirtDisk.VIRTUAL_STORAGE_TYPE vst = new VirtDisk.VIRTUAL_STORAGE_TYPE
            {
                // default to vhdx
                DeviceId = VirtDisk.VIRTUAL_STORAGE_TYPE_DEVICE_VHDX,
                VendorId = VirtDisk.VIRTUAL_STORAGE_TYPE_VENDOR_MICROSOFT
            };
            // if extension = vhx then change deviceid
            if (Path.GetExtension(Filename).Equals(".vhd", StringComparison.CurrentCultureIgnoreCase))
            {
                vst.DeviceId = VirtDisk.VIRTUAL_STORAGE_TYPE_DEVICE_VHD;
            }

            // create GET_VIRTUAL_DISK_INFO struct
            VirtDisk.GET_VIRTUAL_DISK_INFO vdi = new VirtDisk.GET_VIRTUAL_DISK_INFO();

            // determine size of GET_VIRTUAL_DISK_INFO struct
            UInt32 dwSize = (UInt32)Marshal.SizeOf(typeof(VirtDisk.GET_VIRTUAL_DISK_INFO));

            UInt32 res;
            res = VirtDisk.OpenVirtualDisk(ref vst, Filename, VirtDisk.VIRTUAL_DISK_ACCESS_MASK.VIRTUAL_DISK_ACCESS_GET_INFO, VirtDisk.OPEN_VIRTUAL_DISK_FLAG.OPEN_VIRTUAL_DISK_FLAG_NONE, IntPtr.Zero, ref virtualDiskHandle);
            if (res != VirtDisk.ERROR_SUCCESS)
            {
                throw new Win32Exception((int)res, "Failed to open Virtual Disk");
            }

            using (virtualDiskHandle) // this will dispose the handle when it runs out of scope
            {
                vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_PROVIDER_SUBTYPE;
                res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref dwSize, ref vdi, IntPtr.Zero);
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
                res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref dwSize, ref vdi, IntPtr.Zero);

                if (res == VirtDisk.ERROR_SUCCESS)
                {
                    Console.WriteLine("BlockSize: {0}  SectorSize: {1}", vdi.Size.BlockSize, vdi.Size.SectorSize);
                    Console.WriteLine("PhysicalSize: {0} VirtualSize: {1}", vdi.Size.PhysicalSize, vdi.Size.VirtualSize);
                }

                vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_CHANGE_TRACKING_STATE;
                res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref dwSize, ref vdi, IntPtr.Zero);
                if (res == VirtDisk.ERROR_SUCCESS)
                {
                    Console.WriteLine("Change Tracking Enabled: {0}", vdi.ChangeTracking.Enabled);
                }

                vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_IS_4K_ALIGNED;
                res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref dwSize, ref vdi, IntPtr.Zero);
                if (res == VirtDisk.ERROR_SUCCESS)
                {
                    Console.WriteLine("Is aligned at 4KB: {0}", vdi.Is4kAligned);
                }

                vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_FRAGMENTATION;
                res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref dwSize, ref vdi, IntPtr.Zero);
                if (res == VirtDisk.ERROR_SUCCESS)
                {
                    Console.WriteLine("Fragmention: {0}%", vdi.FragmentationPercentage);
                }

                vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_IS_LOADED;
                res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref dwSize, ref vdi, IntPtr.Zero);
                if (res == VirtDisk.ERROR_SUCCESS)
                {
                    Console.WriteLine("Mounted: {0}", vdi.IsLoaded);
                }

                vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_VIRTUAL_DISK_ID;
                res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref dwSize, ref vdi, IntPtr.Zero);
                if (res == VirtDisk.ERROR_SUCCESS)
                {
                    Console.WriteLine("Identifier: {0}", vdi.Identifier);
                }

                vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_SMALLEST_SAFE_VIRTUAL_SIZE;
                res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref dwSize, ref vdi, IntPtr.Zero);
                if (res == VirtDisk.ERROR_SUCCESS)
                {
                    Console.WriteLine("Smallest Safe Virtual Size: {0}", vdi.SmallestSafeVirtualSize);
                }

                vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_PHYSICAL_DISK;
                res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref dwSize, ref vdi, IntPtr.Zero);
                if (res == VirtDisk.ERROR_SUCCESS)
                {
                    Console.WriteLine("Physical Disk Info, Remote: {0} LogicalSectorSize: {1} PhysicalSectorSize: {2}", vdi.PhysicalDisk.IsRemote, vdi.PhysicalDisk.LogicalSectorSize, vdi.PhysicalDisk.PhysicalSectorSize);
                }

                vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_VHD_PHYSICAL_SECTOR_SIZE;
                res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref dwSize, ref vdi, IntPtr.Zero);
                if (res == VirtDisk.ERROR_SUCCESS)
                {
                    Console.WriteLine("Physical Sector Size: {0}", vdi.VhdPhysicalSectorSize);
                }

                vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_PARENT_IDENTIFIER;
                res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref dwSize, ref vdi, IntPtr.Zero);
                if (res == VirtDisk.ERROR_SUCCESS)
                {
                    Console.WriteLine("Parent Identifier (only for differencing disks): {0}", vdi.ParentIdentifier);

                    vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_PARENT_LOCATION;
                    res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref dwSize, ref vdi, IntPtr.Zero);
                    if (res == VirtDisk.ERROR_SUCCESS)
                    {
                        Console.WriteLine("Parent Location (only for differencing disks): Resolved: {0} Location: {1}", vdi.ParentLocation.ParentResolved, vdi.ParentLocation.ParentLocationBuffer);
                    }

                    vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_PARENT_TIMESTAMP;
                    res = VirtDisk.GetVirtualDiskInformation(virtualDiskHandle, ref dwSize, ref vdi, IntPtr.Zero);
                    if (res == VirtDisk.ERROR_SUCCESS)
                    {
                        Console.WriteLine("Parent TimeStamp (only for differencing disks): Resolved: {0}", vdi.ParentTimestamp);
                    }
                }
                else if (res == VirtDisk.ERROR_VHD_INVALID_TYPE)
                {
                    Console.WriteLine("Not a Differencing disk");
                }

            }
        }
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please specify Filename as an argument (path to vhd/vhdx file)");
                return 1;
            }

            bool bRes;
            string Filename = args[0];

            // dump virtual disk info
            PrintVirtualDiskInfo(Filename);

            // compact virtual disk with FileSystemAware option
            bRes = CompactVHD(Filename, true);
            Console.WriteLine("Compact(True): {0}", bRes);

            // compact virtual disk without FileSystemAware option
            bRes = CompactVHD(Filename, false);
            Console.WriteLine("Compact(False): {0}", bRes);

            return 0;
        }
    }
}
