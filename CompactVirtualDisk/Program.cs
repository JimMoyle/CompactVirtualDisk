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
            string vhdx = @"c:\Temp\vhdx\test.vhdx";
            bool bResult = false;
            VirtDisk.VIRTUAL_STORAGE_TYPE vst = new VirtDisk.VIRTUAL_STORAGE_TYPE();
            vst.DeviceId = VirtDisk.VIRTUAL_STORAGE_TYPE_DEVICE_VHDX;
            vst.VendorId = VirtDisk.VIRTUAL_STORAGE_TYPE_VENDOR_MICROSOFT;
            VirtDisk.VIRTUAL_DISK_ACCESS_MASK mask = VirtDisk.VIRTUAL_DISK_ACCESS_MASK.VIRTUAL_DISK_ACCESS_METAOPS;
            if (FileSystemAware)
            {
                mask |= VirtDisk.VIRTUAL_DISK_ACCESS_MASK.VIRTUAL_DISK_ACCESS_ATTACH_RO;
            }

            IntPtr hVirtDisk;// = IntPtr.Zero;
            UInt32 res;
            res = VirtDisk.OpenVirtualDisk(ref vst, vhdx, mask, VirtDisk.OPEN_VIRTUAL_DISK_FLAG.OPEN_VIRTUAL_DISK_FLAG_NONE, IntPtr.Zero, out hVirtDisk);
            if (res == VirtDisk.ERROR_SUCCESS)
            {
                ManualResetEvent stateChangeEvent = new ManualResetEvent(false);
                VirtDisk.OVERLAPPED Overlapped = new VirtDisk.OVERLAPPED();
                Overlapped.hEvent = stateChangeEvent.SafeWaitHandle.DangerousGetHandle();
                VirtDisk.VIRTUAL_DISK_PROGRESS Progress = new VirtDisk.VIRTUAL_DISK_PROGRESS();
           
                res = VirtDisk.CompactVirtualDisk(hVirtDisk, VirtDisk.COMPACT_VIRTUAL_DISK_FLAG.COMPACT_VIRTUAL_DISK_FLAG_NONE, IntPtr.Zero, ref Overlapped);
                if (res != VirtDisk.ERROR_SUCCESS && res != VirtDisk.ERROR_IO_PENDING)
                {
                    // throw some nice error message here perhaps as something went wrong
                    return false;
                }

                bool bPending = true;
                while (bPending)
                {
                    res = VirtDisk.GetVirtualDiskOperationProgress(hVirtDisk, ref Overlapped, ref Progress);
                    if (res == VirtDisk.ERROR_SUCCESS && Progress.OperationStatus == VirtDisk.ERROR_IO_PENDING)
                    {
                        var Percentage = (Progress.CurrentValue / Progress.CompletionValue) *100;
                        Console.WriteLine("Percentage: {0}", Percentage);
                        bPending = stateChangeEvent.WaitOne(100); // use no interval to wait until finished, but using some interval gives you time to show some busy indicator
                    }
                    else
                    {
                        break;
                    }
                }

                bResult = Progress.OperationStatus == VirtDisk.ERROR_SUCCESS;
                VirtDisk.CloseHandle(hVirtDisk);
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
            IntPtr hVirtDisk;// = IntPtr.Zero;
            UInt32 res;
            res = VirtDisk.OpenVirtualDisk(ref vst, vhdx, VirtDisk.VIRTUAL_DISK_ACCESS_MASK.VIRTUAL_DISK_ACCESS_GET_INFO, VirtDisk.OPEN_VIRTUAL_DISK_FLAG.OPEN_VIRTUAL_DISK_FLAG_NONE, IntPtr.Zero, out hVirtDisk);
            if (res == VirtDisk.ERROR_SUCCESS)
            {
                VirtDisk.GET_VIRTUAL_DISK_INFO vdi = new VirtDisk.GET_VIRTUAL_DISK_INFO();
                vdi.Version = VirtDisk.GET_VIRTUAL_DISK_INFO_VERSION.GET_VIRTUAL_DISK_INFO_PROVIDER_SUBTYPE;
                ulong size = 32;

                res = VirtDisk.GetVirtualDiskInformation(hVirtDisk, ref size, ref vdi, IntPtr.Zero);

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
                res = VirtDisk.GetVirtualDiskInformation(hVirtDisk, ref size, ref vdi, IntPtr.Zero);

                if (res == VirtDisk.ERROR_SUCCESS)
                {
                    Console.WriteLine("BlockSize: {0}  SectorSize: {1}", vdi.size.BlockSize, vdi.size.SectorSize);
                    Console.WriteLine("PhysicalSize: {0} VirtualSize: {1}", vdi.size.PhysicalSize, vdi.size.VirtualSize);
                }
                VirtDisk.CloseHandle(hVirtDisk);

                bool bRes;
                bRes = CompactVHD(true);
                Console.WriteLine("Compact(True): {0}", bRes);
                bRes = CompactVHD(false);
                Console.WriteLine("Compact(False): {0}", bRes);
            }

        }

    }
}
