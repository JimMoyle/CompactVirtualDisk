using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace CompactVirtualDisk
{
    [SecurityPermission(SecurityAction.Demand)]
    public class VirtualDiskSafeHandle : SafeHandle
    {
        public VirtualDiskSafeHandle() : base(IntPtr.Zero, true) { }

        public override bool IsInvalid => IsClosed || (handle == IntPtr.Zero);

        public bool IsOpen => !IsInvalid;

        protected override bool ReleaseHandle()
        {
            return VirtDisk.CloseHandle(handle);
        }

        public override string ToString()
        {
            return handle.ToString();
        }
    }
    class VirtDisk
    {
        public const UInt32 ERROR_SUCCESS = 0;
        public const UInt32 ERROR_IO_PENDING = 997;

        public const UInt32 VIRTUAL_STORAGE_TYPE_DEVICE_UNKNOWN = 0;
        public const UInt32 VIRTUAL_STORAGE_TYPE_DEVICE_ISO     = 1;
        public const UInt32 VIRTUAL_STORAGE_TYPE_DEVICE_VHD     = 2;
        public const UInt32 VIRTUAL_STORAGE_TYPE_DEVICE_VHDX    = 3;
        public const UInt32 VIRTUAL_STORAGE_TYPE_DEVICE_VHDSET  = 4;

        public static readonly Guid VIRTUAL_STORAGE_TYPE_VENDOR_MICROSOFT = new Guid("EC984AEC-A0F9-47e9-901F-71415A66345B");

        [StructLayout(LayoutKind.Sequential)]
        public struct VIRTUAL_STORAGE_TYPE
        {
            public UInt32 DeviceId;
            public Guid VendorId; //is marshalled?
        }

        public enum VIRTUAL_DISK_ACCESS_MASK
        {
            VIRTUAL_DISK_ACCESS_NONE = 0x00000000,
            VIRTUAL_DISK_ACCESS_ATTACH_RO = 0x00010000,
            VIRTUAL_DISK_ACCESS_ATTACH_RW = 0x00020000,
            VIRTUAL_DISK_ACCESS_DETACH = 0x00040000,
            VIRTUAL_DISK_ACCESS_GET_INFO = 0x00080000,
            VIRTUAL_DISK_ACCESS_CREATE = 0x00100000,
            VIRTUAL_DISK_ACCESS_METAOPS = 0x00200000,
            VIRTUAL_DISK_ACCESS_READ = 0x000d0000,
            VIRTUAL_DISK_ACCESS_ALL = 0x003f0000,
            //
            // A special flag to be used to test if the virtual disk needs to be
            // opened for write.
            //

            VIRTUAL_DISK_ACCESS_WRITABLE = 0x00320000
        };

        // Flags for OpenVirtualDisk
        public enum OPEN_VIRTUAL_DISK_FLAG
        {
            OPEN_VIRTUAL_DISK_FLAG_NONE = 0x00000000,

            // Open the backing store without opening any differencing chain parents.
            // This allows one to fixup broken parent links.
            OPEN_VIRTUAL_DISK_FLAG_NO_PARENTS = 0x00000001,

            // The backing store being opened is an empty file. Do not perform virtual
            // disk verification.
            OPEN_VIRTUAL_DISK_FLAG_BLANK_FILE = 0x00000002,

            // This flag is only specified at boot time to load the system disk
            // during virtual disk boot.  Must be kernel mode to specify this flag.
            OPEN_VIRTUAL_DISK_FLAG_BOOT_DRIVE = 0x00000004,

            // This flag causes the backing file to be opened in cached mode.
            OPEN_VIRTUAL_DISK_FLAG_CACHED_IO = 0x00000008,

            // Open the backing store without opening any differencing chain parents.
            // This allows one to fixup broken parent links temporarily without updating
            // the parent locator.
            OPEN_VIRTUAL_DISK_FLAG_CUSTOM_DIFF_CHAIN = 0x00000010,

            // This flag causes all backing stores except the leaf backing store to
            // be opened in cached mode.
            OPEN_VIRTUAL_DISK_FLAG_PARENT_CACHED_IO = 0x00000020,

            // This flag causes a Vhd Set file to be opened without any virtual disk.
            OPEN_VIRTUAL_DISK_FLAG_VHDSET_FILE_ONLY = 0x00000040,

            // For differencing disks, relative parent locators are not used when
            // determining the path of a parent VHD.
            OPEN_VIRTUAL_DISK_FLAG_IGNORE_RELATIVE_PARENT_LOCATOR = 0x00000080,

            // Disable flushing and FUA (both for payload data and for metadata)
            // for backing files associated with this virtual disk.
            OPEN_VIRTUAL_DISK_FLAG_NO_WRITE_HARDENING = 0x00000100,
        };

        // Version definitions
        public enum OPEN_VIRTUAL_DISK_VERSION
        {
            OPEN_VIRTUAL_DISK_VERSION_UNSPECIFIED = 0,
            OPEN_VIRTUAL_DISK_VERSION_1 = 1,
            OPEN_VIRTUAL_DISK_VERSION_2 = 2,
            OPEN_VIRTUAL_DISK_VERSION_3 = 3,

        };

        public struct Version1
        {
            public UInt32 RWDepth;
        }

        public struct Version2
        {
            public bool GetInfoOnly;
            public bool ReadOnly;
            public Guid ResiliencyGuid;
        }
        public struct Version3
        {
            public bool GetInfoOnly;
            public bool ReadOnly;
            public Guid ResiliencyGuid;
            public Guid SnapshotId;
        }

        // Version definitions
        public enum GET_VIRTUAL_DISK_INFO_VERSION
        {
            GET_VIRTUAL_DISK_INFO_UNSPECIFIED = 0,
            GET_VIRTUAL_DISK_INFO_SIZE = 1,
            GET_VIRTUAL_DISK_INFO_IDENTIFIER = 2,
            GET_VIRTUAL_DISK_INFO_PARENT_LOCATION = 3,
            GET_VIRTUAL_DISK_INFO_PARENT_IDENTIFIER = 4,
            GET_VIRTUAL_DISK_INFO_PARENT_TIMESTAMP = 5,
            GET_VIRTUAL_DISK_INFO_VIRTUAL_STORAGE_TYPE = 6,
            GET_VIRTUAL_DISK_INFO_PROVIDER_SUBTYPE = 7,
            GET_VIRTUAL_DISK_INFO_IS_4K_ALIGNED = 8,
            GET_VIRTUAL_DISK_INFO_PHYSICAL_DISK = 9,
            GET_VIRTUAL_DISK_INFO_VHD_PHYSICAL_SECTOR_SIZE = 10,
            GET_VIRTUAL_DISK_INFO_SMALLEST_SAFE_VIRTUAL_SIZE = 11,
            GET_VIRTUAL_DISK_INFO_FRAGMENTATION = 12,
            GET_VIRTUAL_DISK_INFO_IS_LOADED = 13,
            GET_VIRTUAL_DISK_INFO_VIRTUAL_DISK_ID = 14,
            GET_VIRTUAL_DISK_INFO_CHANGE_TRACKING_STATE = 15,

        };

        // Versioned OpenVirtualDisk parameter structure
        [StructLayout(LayoutKind.Explicit, Size = 24)] // check size
        public struct OPEN_VIRTUAL_DISK_PARAMETERS
        {
            [FieldOffset(0)]
            public OPEN_VIRTUAL_DISK_VERSION Version;
            [FieldOffset(sizeof(OPEN_VIRTUAL_DISK_VERSION))]
            public Version1 version1;
            [FieldOffset(sizeof(OPEN_VIRTUAL_DISK_VERSION))]
            public Version2 version2;
            [FieldOffset(sizeof(OPEN_VIRTUAL_DISK_VERSION))]
            public Version3 version3;
        };

        public struct ParentLocation
        {
            public bool ParentResolved;
            public IntPtr ParentLocationBuffer;
        }

        public struct PhysicalDisk
        {
            public UInt32 LogicalSectorSize;
            public UInt32 PhysicalSectorSize;
            public bool IsRemote;
        };

        public struct Size
        {
            public UInt64 VirtualSize;
            public UInt64 PhysicalSize;
            public UInt32 BlockSize;
            public UInt32 SectorSize;
        };

//        [StructLayout(LayoutKind.Explicit, Size = 12, Pack =4)] // check size
        [StructLayout(LayoutKind.Sequential, Size = 12, Pack = 1)] // check size
        public struct ChangeTracking
        {
            public UInt32 Enabled;    // is really a bool put UInt32 to prevent alignment exception
            public UInt32 NewerChanges; // is really a bool put UInt32 to prevent alignment exception
            public char MostRecentId;
            private byte padding1;
            private byte padding2;
        }

        // Versioned parameter structure for GetVirtualDiskInformation
        [StructLayout(LayoutKind.Explicit, Size = 32)] // check size
        public struct GET_VIRTUAL_DISK_INFO
        {
            [FieldOffset(0)]
            public GET_VIRTUAL_DISK_INFO_VERSION Version;
            // union starts here 
            [FieldOffset(8)]
            public Size size;

            [FieldOffset(8)]
            public Guid Identifier;

            [FieldOffset(8)]
            public ParentLocation parentLocation;

            [FieldOffset(8)]
            public Guid ParentIdentifier;
            [FieldOffset(8)]
            public UInt32 ParentTimestamp;

            [FieldOffset(8)]
            public VIRTUAL_STORAGE_TYPE VirtualStorageType;

            [FieldOffset(8)]
            public UInt32 ProviderSubtype;

            [FieldOffset(8)]
            public bool Is4kAligned;

            [FieldOffset(8)]
            public bool IsLoaded;

            [FieldOffset(8)]
            public PhysicalDisk physicalDisk;

            [FieldOffset(8)]
            public UInt32 VhdPhysicalSectorSize;
            [FieldOffset(8)]
            public UInt64 SmallestSafeVirtualSize;

            // GET_VIRTUAL_DISK_INFO_FRAGMENTATION
            [FieldOffset(8)]
            public UInt32 FragmentationPercentage;

            // GET_VIRTUAL_DISK_INFO_VIRTUAL_DISK_ID
            [FieldOffset(8)]
            public Guid VirtualDiskId;

            [FieldOffset(8)]
            ChangeTracking changeTracking;

        }

        //
        // CompactVirtualDisk
        //

        // Version definitions
        public enum COMPACT_VIRTUAL_DISK_VERSION
        {
            COMPACT_VIRTUAL_DISK_VERSION_UNSPECIFIED = 0,
            COMPACT_VIRTUAL_DISK_VERSION_1 = 1,

        };


        // Versioned structure for CompactVirtualDisk
        public struct Version1Union
        {
            UInt32 Reserved;
        }
        public struct COMPACT_VIRTUAL_DISK_PARAMETERS
        {
            COMPACT_VIRTUAL_DISK_VERSION Version;
            Version1Union Version1;
        };


        // Flags for CompactVirtualDisk
        public enum COMPACT_VIRTUAL_DISK_FLAG
        {
            COMPACT_VIRTUAL_DISK_FLAG_NONE = 0x00000000,
            COMPACT_VIRTUAL_DISK_FLAG_NO_ZERO_SCAN = 0x00000001,
            COMPACT_VIRTUAL_DISK_FLAG_NO_BLOCK_MOVES = 0x00000002,   
        };

        [StructLayout(LayoutKind.Explicit, Size = 20)]
        public struct OVERLAPPED
        {
            [FieldOffset(0)]
            public uint Internal;

            [FieldOffset(4)]
            public uint InternalHigh;

            [FieldOffset(8)]
            public uint Offset;

            [FieldOffset(12)]
            public uint OffsetHigh;

            [FieldOffset(8)]
            public IntPtr Pointer;

            [FieldOffset(16)]
            public IntPtr hEvent;
        }

        public struct VIRTUAL_DISK_PROGRESS
        {
            public UInt32 OperationStatus;
            public UInt64 CurrentValue;
            public UInt64 CompletionValue;
        };

        [DllImport("VirtDisk.dll", CharSet = CharSet.Unicode, SetLastError =false)]
        public static extern UInt32 OpenVirtualDisk(
            ref VIRTUAL_STORAGE_TYPE VirtualStorageType,
               string Path,
               VIRTUAL_DISK_ACCESS_MASK VirtualDiskAccessMask,
               OPEN_VIRTUAL_DISK_FLAG Flags,
               ref OPEN_VIRTUAL_DISK_PARAMETERS Parameters,
               out IntPtr Handle);

        [DllImport("VirtDisk.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern UInt32 OpenVirtualDisk(
               ref VIRTUAL_STORAGE_TYPE VirtualStorageType,
               string Path,
               VIRTUAL_DISK_ACCESS_MASK VirtualDiskAccessMask,
               OPEN_VIRTUAL_DISK_FLAG Flags,
               IntPtr Parameters,// to make it optional (pass IntPtr.Zero)
               out IntPtr Handle);


        [DllImport("VirtDisk.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern UInt32 GetVirtualDiskInformation(
            IntPtr VirtualDiskHandle,
            ref ulong VirtualDiskInfoSize,
            ref GET_VIRTUAL_DISK_INFO VirtualDiskInfo,
            ref ulong SizeUsed);

        [DllImport("VirtDisk.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern UInt32 GetVirtualDiskInformation(
            IntPtr VirtualDiskHandle,
            ref ulong VirtualDiskInfoSize,
            ref GET_VIRTUAL_DISK_INFO VirtualDiskInfo,
            IntPtr SizeUsed);// to make it optional (pass IntPtr.Zero)

        [DllImport("VirtDisk.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern UInt32 CompactVirtualDisk(
            IntPtr VirtualDiskHandle,
            COMPACT_VIRTUAL_DISK_FLAG Flags,
            ref COMPACT_VIRTUAL_DISK_PARAMETERS Parameters,
            ref OVERLAPPED Overlapped);

        [DllImport("VirtDisk.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern UInt32 CompactVirtualDisk(
            IntPtr VirtualDiskHandle,
            COMPACT_VIRTUAL_DISK_FLAG Flags,
            IntPtr Parameters,// to make it optional (pass IntPtr.Zero)
            ref OVERLAPPED Overlapped);

        [DllImport("VirtDisk.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern UInt32 CompactVirtualDisk(
            IntPtr VirtualDiskHandle,
            COMPACT_VIRTUAL_DISK_FLAG Flags,
            IntPtr Parameters,// to make it optional (pass IntPtr.Zero)
            IntPtr Overlapped); // to make it optional (pass IntPtr.Zero)

        [DllImport("VirtDisk.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern UInt32 GetVirtualDiskOperationProgress(
          IntPtr VirtualDiskHandle,
          ref OVERLAPPED Overlapped,
          ref VIRTUAL_DISK_PROGRESS Progress);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hHandle);
    }
}