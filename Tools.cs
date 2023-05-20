using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;                                    // Path
using System.Runtime.InteropServices;               // DLLImport
using System.Globalization;                         // CultureInfo.InvariantCulture
using Microsoft.Win32;                              // Registry 
using System.Security;                              // UNUSED: fast file enumerator
using System.Security.Permissions;                  // UNUSED: fast file enumerator
using System.Diagnostics;                           // process, stopwatch
using System.Threading;                             // process
using System.Linq;                                  // .Sum
using System.Threading.Tasks;                       // auto close messagebox
using Microsoft.Win32.SafeHandles;
//using System.Management;                            // WMI stuff
using System.Collections;
using System.Net.NetworkInformation;                // enum PCs in local network via ping
using System.Text.RegularExpressions;               // regex 
using System.Drawing.Drawing2D;                     // xCursor 
using System.Runtime.ExceptionServices;             // [HandleProcessCorruptedStateExceptions] 
using System.Security.AccessControl;

namespace GrzTools
{
    //
    // 20120221: this class allows us to serialize/deserialize the set of image icons
    //
    [Serializable()]
    public class FlatImage
    {
        public Image _image { get; set; }
        public string _key { get; set; }

        public static void Serialize( ImageList il )
        {
            string path = Path.Combine(Application.StartupPath, "cfwimagelist.bin");
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

            List<FlatImage> fis = new List<FlatImage>();
            for ( int index = 0; index < il.Images.Count; index++ ) {
                FlatImage fi = new FlatImage();
                fi._key = il.Images.Keys[index];
                fi._image = il.Images[index];
                fis.Add(fi);
            }

            using ( FileStream stream = System.IO.File.OpenWrite(path) ) {
                formatter.Serialize(stream, fis);
            }
        }

        public static Image[] Deserialize()
        {
            List<Image> il = new List<Image>();

            string path = Path.Combine(Application.StartupPath, "cfwimagelist.bin");
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            try {
                using ( FileStream stream = System.IO.File.OpenRead(path) ) {
                    List<FlatImage> ilc = formatter.Deserialize(stream) as List<FlatImage>;
                    for ( int index = 0; index < ilc.Count; index++ ) {
                        Image img = ilc[index]._image;
//                        string key = ilc[index]._key;
                        il.Add(img);
                    }
                }
            } catch { }

            return il.ToArray();
        }
    }

    //
    // 20160221: individual icons to file extensions
    //
    public class RegisteredFileType
    {
        /// <summary>
        /// Structure that encapsulates basic information of icon embedded in a file.
        /// </summary>
        public struct EmbeddedIconInfo
        {
            public string FileName;
            public int IconIndex;
        }

        #region APIs

        [DllImport("shell32.dll", EntryPoint = "ExtractIconA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr ExtractIcon( int hInst, string lpszExeFileName, int nIconIndex );

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern uint ExtractIconEx( string szFileName, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, uint nIcons );

        [DllImport("user32.dll", EntryPoint = "DestroyIcon", SetLastError = true)]
        private static unsafe extern int DestroyIcon( IntPtr hIcon );

        [DllImport("shell32.dll", CharSet=CharSet.Auto)]
        private static extern int  SHGetFileInfo( string pszPath, int dwFileAttributes, out SHFILEINFO psfi, uint cbfileInfo, SHGFI uFlags );
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public SHFILEINFO( bool b )
            {
                hIcon=IntPtr.Zero;
                iIcon=0;
                dwAttributes=0;
                szDisplayName="";
                szTypeName="";
            }
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=80)]
            public string szTypeName;
        };
        [Flags]
        enum SHGFI : int
        {
            /// <summary>get icon</summary>
            Icon = 0x000000100,
            /// <summary>get display name</summary>
            DisplayName = 0x000000200,
            /// <summary>get type name</summary>
            TypeName = 0x000000400,
            /// <summary>get attributes</summary>
            Attributes = 0x000000800,
            /// <summary>get icon location</summary>
            IconLocation = 0x000001000,
            /// <summary>return exe type</summary>
            ExeType = 0x000002000,
            /// <summary>get system icon index</summary>
            SysIconIndex = 0x000004000,
            /// <summary>put a link overlay on icon</summary>
            LinkOverlay = 0x000008000,
            /// <summary>show icon in selected state</summary>
            Selected = 0x000010000,
            /// <summary>get only specified attributes</summary>
            Attr_Specified = 0x000020000,
            /// <summary>get large icon</summary>
            LargeIcon = 0x000000000,
            /// <summary>get small icon</summary>
            SmallIcon = 0x000000001,
            /// <summary>get open icon</summary>
            OpenIcon = 0x000000002,
            /// <summary>get shell size icon</summary>
            ShellIconSize = 0x000000004,
            /// <summary>pszPath is a pidl</summary>
            PIDL = 0x000000008,
            /// <summary>use passed dwFileAttribute</summary>
            UseFileAttributes = 0x000000010,
            /// <summary>apply the appropriate overlays</summary>
            AddOverlays = 0x000000020,
            /// <summary>Get the index of the overlay in the upper 8 bits of the iIcon</summary>
            OverlayIndex = 0x000000040,
        }   



        #endregion

        #region CORE METHODS

        // 20160626: alternative method to extract an icon 
        public static Icon GetIcon( string strPath, bool bSmall )
        {
            SHFILEINFO info = new SHFILEINFO(true);
            int cbFileInfo = Marshal.SizeOf(info);
            SHGFI flags;
            if ( bSmall )
                flags = SHGFI.Icon|SHGFI.SmallIcon|SHGFI.UseFileAttributes;
            else
                flags = SHGFI.Icon|SHGFI.LargeIcon|SHGFI.UseFileAttributes;

            SHGetFileInfo(strPath, 256, out info, (uint)cbFileInfo, flags);
            Icon icon = (Icon)Icon.FromHandle(info.hIcon).Clone();
            DestroyIcon(info.hIcon);

            return icon;
        }

        /// <summary>
        /// Gets registered file types and their associated icon in the system.
        /// </summary>
        /// <returns>Returns a hash table which contains the file extension as keys, the icon file and param as values.</returns>
        public static Hashtable GetFileTypeAndIcon()
        {
            try {
                // Create a registry key object to represent the HKEY_CLASSES_ROOT registry section
                RegistryKey rkRoot = Registry.ClassesRoot;

                //Gets all sub keys' names.
                string[] keyNames = rkRoot.GetSubKeyNames();
                Hashtable iconsInfo = new Hashtable();

                //Find the file icon.
                foreach ( string keyName in keyNames ) {

                    if ( String.IsNullOrEmpty(keyName) )
                        continue;

                    //If this key is not a file exttension(eg, .zip), skip it.
                    if ( keyName[0] != '.' )
                        continue;

                    RegistryKey rkFileType = rkRoot.OpenSubKey(keyName);
                    if ( rkFileType == null )
                        continue;

                    //Gets the default value of this key that contains the information of file type.
                    object defaultValue = rkFileType.GetValue("");
                    if ( defaultValue == null )
                        continue;

                    //Go to the key that specifies the default icon associates with this file type.
                    string defaultIcon = defaultValue.ToString() + "\\DefaultIcon";
                    RegistryKey rkFileIcon = rkRoot.OpenSubKey(defaultIcon);
                    if ( rkFileIcon != null ) {
                        //Get the file contains the icon and the index of the icon in that file.
                        object value = rkFileIcon.GetValue("");
                        if ( value != null ) {
                            //Clear all unecessary " sign in the string to avoid error.
                            string fileParam = value.ToString().Replace("\"", "");
                            if ( fileParam[1] == ':' ) {
                                iconsInfo.Add(keyName, fileParam);
                            }
                        }
                        rkFileIcon.Close();
                    }
                    rkFileType.Close();
                }
                rkRoot.Close();
                return iconsInfo;
            } catch ( Exception exc ) {
                throw exc;
            }
        }

        /// <summary>
        /// Extract the icon from file.
        /// </summary>
        /// <param name="fileAndParam">The params string, 
        /// such as ex: "C:\\Program Files\\NetMeeting\\conf.exe,1".</param>
        /// <returns>This method always returns the large size of the icon (may be 32x32 px).</returns>
        public static Icon ExtractIconFromFile( string fileAndParam )
        {
            try {
                EmbeddedIconInfo embeddedIcon = getEmbeddedIconInfo(fileAndParam);

                //Gets the handle of the icon.
                IntPtr lIcon = ExtractIcon(0, embeddedIcon.FileName, embeddedIcon.IconIndex);

                //Gets the real icon.
                return Icon.FromHandle(lIcon);
            } catch ( Exception exc ) {
                throw exc;
            }
        }

        /// <summary>
        /// Extract the icon from file.
        /// </summary>
        /// <param name="fileAndParam">The params string, 
        /// such as ex: "C:\\Program Files\\NetMeeting\\conf.exe,1".</param>
        /// <param name="isLarge">
        /// Determines the returned icon is a large (may be 32x32 px) 
        /// or small icon (16x16 px).</param>
        public static Icon ExtractIconFromFile( string fileAndParam, bool isLarge )
        {
//            if ( !System.IO.File.Exists(fileAndParam) ) {
//                return null;
//            }
            if ( fileAndParam.Length == 0 ) {
                return null;
            }

            unsafe {
                uint readIconCount = 0;
                IntPtr[] hDummy = new IntPtr[1] { IntPtr.Zero };
                IntPtr[] hIconEx = new IntPtr[1] { IntPtr.Zero };

                try {
                    EmbeddedIconInfo embeddedIcon = getEmbeddedIconInfo(fileAndParam);

                    if ( isLarge )
                        readIconCount = ExtractIconEx(embeddedIcon.FileName, 0, hIconEx, hDummy, 1);
                    else
                        readIconCount = ExtractIconEx(embeddedIcon.FileName, 0, hDummy, hIconEx, 1);

                    if ( readIconCount > 0 && hIconEx[0] != IntPtr.Zero ) {
                        // Get first icon.
                        Icon extractedIcon = (Icon)Icon.FromHandle(hIconEx[0]).Clone();

                        return extractedIcon;
                    } else // No icon read
                        return null;
                } catch ( Exception exc ) {
                    // Extract icon error.
                    return null;
                } finally {
                    // Release resources.
                    foreach ( IntPtr ptr in hIconEx )
                        if ( ptr != IntPtr.Zero )
                            DestroyIcon(ptr);

                    foreach ( IntPtr ptr in hDummy )
                        if ( ptr != IntPtr.Zero )
                            DestroyIcon(ptr);
                }
            }
        }

        #endregion

        #region UTILITY METHODS

        /// <summary>
        /// Parses the parameters string to the structure of EmbeddedIconInfo.
        /// </summary>
        /// <param name="fileAndParam">The params string, 
        /// such as ex: "C:\\Program Files\\NetMeeting\\conf.exe,1".</param>
        /// <returns></returns>
        protected static EmbeddedIconInfo getEmbeddedIconInfo( string fileAndParam )
        {
            EmbeddedIconInfo embeddedIcon = new EmbeddedIconInfo();

            if ( String.IsNullOrEmpty(fileAndParam) )
                return embeddedIcon;

            //Use to store the file contains icon.
            string fileName = String.Empty;

            //The index of the icon in the file.
            int iconIndex = 0;
            string iconIndexString = String.Empty;

            int commaIndex = fileAndParam.IndexOf(",");
            //if fileAndParam is some thing likes that: "C:\\Program Files\\NetMeeting\\conf.exe,1".
            if ( commaIndex > 0 ) {
                fileName = fileAndParam.Substring(0, commaIndex);
                iconIndexString = fileAndParam.Substring(commaIndex + 1);
            } else
                fileName = fileAndParam;

            if ( !String.IsNullOrEmpty(iconIndexString) ) {
                //Get the index of icon.
                iconIndex = int.Parse(iconIndexString);
                if ( iconIndex < 0 )
                    iconIndex = 0;  //To avoid the invalid index.
            }

            embeddedIcon.FileName = fileName;
            embeddedIcon.IconIndex = iconIndex;

            return embeddedIcon;
        }

        #endregion
    }

    //
    // 20120221: get physical drive info via DeviceIoControl, which is way faster than WMI
    //
    public class clsDiskInfoEx
    {

        private const long GenericRead = 0x80000000;
        private const int FileShareRead = 1;
        private const int Filesharewrite = 2;
        private const int OpenExisting = 3;
        private const int IoctlVolumeGetVolumeDiskExtents = 0x560000;
        private const int IncorrectFunction = 1;
        private const int ErrorInsufficientBuffer = 122;

        private const int MoreDataIsAvailable = 234;
        private List<string> currentDriveMappings;

        private string errorMessage;
        public enum RESOURCE_SCOPE
        {
            RESOURCE_CONNECTED = 0x1,
            RESOURCE_GLOBALNET = 0x2,
            RESOURCE_REMEMBERED = 0x3,
            RESOURCE_RECENT = 0x4,
            RESOURCE_CONTEXT = 0x5
        }

        public enum RESOURCE_TYPE
        {
            RESOURCETYPE_ANY = 0x0,
            RESOURCETYPE_DISK = 0x1,
            RESOURCETYPE_PRINT = 0x2,
            RESOURCETYPE_RESERVED = 0x8
        }

        public enum RESOURCE_USAGE
        {
            RESOURCEUSAGE_CONNECTABLE = 0x1,
            RESOURCEUSAGE_CONTAINER = 0x2,
            RESOURCEUSAGE_NOLOCALDEVICE = 0x4,
            RESOURCEUSAGE_SIBLING = 0x8,
            RESOURCEUSAGE_ATTACHED = 0x10,
            RESOURCEUSAGE_ALL = (RESOURCEUSAGE_CONNECTABLE | RESOURCEUSAGE_CONTAINER | RESOURCEUSAGE_ATTACHED)
        }

        public enum RESOURCE_DISPLAYTYPE
        {
            RESOURCEDISPLAYTYPE_GENERIC = 0x0,
            RESOURCEDISPLAYTYPE_DOMAIN = 0x1,
            RESOURCEDISPLAYTYPE_SERVER = 0x2,
            RESOURCEDISPLAYTYPE_SHARE = 0x3,
            RESOURCEDISPLAYTYPE_FILE = 0x4,
            RESOURCEDISPLAYTYPE_GROUP = 0x5,
            RESOURCEDISPLAYTYPE_NETWORK = 0x6,
            RESOURCEDISPLAYTYPE_ROOT = 0x7,
            RESOURCEDISPLAYTYPE_SHAREADMIN = 0x8,
            RESOURCEDISPLAYTYPE_DIRECTORY = 0x9,
            RESOURCEDISPLAYTYPE_TREE = 0xa,
            RESOURCEDISPLAYTYPE_NDSCONTAINER = 0xb
        }

        public enum NERR
        {
            NERR_Success = 0,
            ERROR_MORE_DATA = 234,
            ERROR_NO_BROWSER_SERVERS_FOUND = 6118,
            ERROR_INVALID_LEVEL = 124,
            ERROR_ACCESS_DENIED = 5,
            ERROR_INVALID_PARAMETER = 87,
            ERROR_NOT_ENOUGH_MEMORY = 8,
            ERROR_NETWORK_BUSY = 54,
            ERROR_BAD_NETPATH = 53,
            ERROR_NO_NETWORK = 1222,
            ERROR_INVALID_HANDLE_STATE = 1609,
            ERROR_EXTENDED_ERROR = 1208
        }

        public struct NETRESOURCE
        {
            public RESOURCE_SCOPE dwScope;
            public RESOURCE_TYPE dwType;
            public RESOURCE_DISPLAYTYPE dwDisplayType;
            public RESOURCE_USAGE dwUsage;
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
            public string lpLocalName;
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
            public string lpRemoteName;
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
            public string lpComment;
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
            public string lpProvider;
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]

        private static extern uint QueryDosDevice( string lpDeviceName, IntPtr lpTargetPath, uint ucchMax );

        private class NativeMethods
        {
            [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern SafeFileHandle CreateFile( string fileName, int desiredAccess, int shareMode, IntPtr securityAttributes, int creationDisposition, int flagsAndAttributes, IntPtr hTemplateFile );

            [DllImport("kernel32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DeviceIoControl( SafeFileHandle hVol, int controlCode, IntPtr inBuffer, int inBufferSize, ref DiskExtents outBuffer, int outBufferSize, ref int bytesReturned, IntPtr overlapped );

            [DllImport("kernel32", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DeviceIoControl( SafeFileHandle hVol, int controlCode, IntPtr inBuffer, int inBufferSize, IntPtr outBuffer, int outBufferSize, ref int bytesReturned, IntPtr overlapped );

            [DllImport("mpr.dll", CharSet = CharSet.Auto)]
            public static extern int WNetEnumResource( IntPtr hEnum, ref int lpcCount, IntPtr lpBuffer, ref int lpBufferSize );

            [DllImport("mpr.dll", CharSet = CharSet.Auto)]
            public static extern int WNetOpenEnum( RESOURCE_SCOPE dwScope, RESOURCE_TYPE dwType, RESOURCE_USAGE dwUsage, ref NETRESOURCE lpNetResource, ref IntPtr lphEnum );

            [DllImport("mpr.dll", CharSet = CharSet.Auto)]
            public static extern int WNetCloseEnum( IntPtr hEnum );
        }

        // DISK_EXTENT in the msdn.
        [StructLayout(LayoutKind.Sequential)]
        private struct DiskExtent
        {
            public int DiskNumber;
            public long StartingOffset;
            public long ExtentLength;
        }

        // DISK_EXTENTS
        [StructLayout(LayoutKind.Sequential)]
        private struct DiskExtents
        {
            public int numberOfExtents;
            // We can't marshal an array if we don't know its size.
            public DiskExtent first;
        }

        public clsDiskInfoEx()
        {
            Refresh();
        }

        public void Refresh()
        {
            errorMessage = "";
            currentDriveMappings = null;
            currentDriveMappings = new List<string>();
            GetPhysicalDisks(ref currentDriveMappings);
        }

        // A Volume could be on many physical drives.
        // Returns a list of string containing each physical drive the volume uses.
        // For CD Drives with no disc in it will return an empty list.
        private List<string> GetPhysicalDriveStrings( DriveInfo driveInfo )
        {
            SafeFileHandle sfh = null;
            List<string> physicalDrives = new List<string>(1);
            string path = "\\\\.\\" + driveInfo.RootDirectory.ToString().TrimEnd('\\');
            try {
                sfh = NativeMethods.CreateFile(path, 0, FileShareRead | Filesharewrite, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
                int bytesReturned = 0;
                DiskExtents de1 = new DiskExtents();
//                int numDiskExtents = 0;
                bool result = NativeMethods.DeviceIoControl(sfh, IoctlVolumeGetVolumeDiskExtents, IntPtr.Zero, 0, ref de1, Marshal.SizeOf(de1), ref bytesReturned, IntPtr.Zero);
                if ( result == true ) {
                    // there was only one disk extent. So the volume lies on 1 physical drive.
                    physicalDrives.Add("\\\\.\\PhysicalDrive" + de1.first.DiskNumber.ToString());
                    return physicalDrives;
                }
                if ( Marshal.GetLastWin32Error() == IncorrectFunction ) {
                    // The drive is removable and removed, like a CDRom with nothing in it.
                    return physicalDrives;
                }
                if ( Marshal.GetLastWin32Error() == MoreDataIsAvailable ) {
                    // This drive is part of a mirror or volume - handle it below. 
                } else if ( Marshal.GetLastWin32Error() != ErrorInsufficientBuffer ) {
                    throw new Win32Exception();
                }
                // Houston, we have a spanner. The volume is on multiple disks.
                // Untested...
                // We need a blob of memory for the DISK_EXTENTS structure, and all the DISK_EXTENTS
                int blobSize = Marshal.SizeOf(typeof(DiskExtents)) + (de1.numberOfExtents - 1) * Marshal.SizeOf(typeof(DiskExtent));
                IntPtr pBlob = Marshal.AllocHGlobal(blobSize);
                result = NativeMethods.DeviceIoControl(sfh, IoctlVolumeGetVolumeDiskExtents, IntPtr.Zero, 0, pBlob, blobSize, ref bytesReturned, IntPtr.Zero);
                if ( result == false )
                    throw new Win32Exception();
                // Read them out one at a time.
                IntPtr pNext = new IntPtr(pBlob.ToInt64() + 8);
                // is this always ok on 64 bit OSes? ToInt64?
                for ( int i = 0; i <= de1.numberOfExtents - 1; i++ ) {
                    DiskExtent diskExtentN = (DiskExtent)Marshal.PtrToStructure(pNext, typeof(DiskExtent));
                    physicalDrives.Add("\\\\.\\PhysicalDrive" + diskExtentN.DiskNumber.ToString());
                    pNext = new IntPtr(pNext.ToInt32() + Marshal.SizeOf(typeof(DiskExtent)));
                }
                return physicalDrives;
            } finally {
                if ( sfh != null ) {
                    if ( sfh.IsInvalid == false ) {
                        sfh.Close();
                    }
                    sfh.Dispose();
                }
            }
        }

        // A Volume could be on many physical drives.
        // here we just grab the first one and return
        // For CD Drives with no disc in it will return an empty list.
        public static string GetFirstPhysicalDriveString( string drive )
        {
            SafeFileHandle sfh = null;
            string physicalDrive = "";
            string path = "\\\\.\\" + drive;
            try {
                sfh = NativeMethods.CreateFile(path, 0, FileShareRead | Filesharewrite, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
                int bytesReturned = 0;
                DiskExtents de = new DiskExtents();
                bool result = NativeMethods.DeviceIoControl(sfh, IoctlVolumeGetVolumeDiskExtents, IntPtr.Zero, 0, ref de, Marshal.SizeOf(de), ref bytesReturned, IntPtr.Zero);
                if ( result == true ) {
                    // there was only one disk extent. So the volume lies on 1 physical drive.
                    physicalDrive = "PhysicalDrive " + de.first.DiskNumber.ToString();
                    return physicalDrive;
                }
                if ( Marshal.GetLastWin32Error() == IncorrectFunction ) {
                    // The drive is removable and removed, like a CDRom with nothing in it.
                    return physicalDrive;
                }
                if ( Marshal.GetLastWin32Error() == MoreDataIsAvailable ) {
                    // This drive is part of a mirror or volume - handle it below. 
                } else {
                    if ( Marshal.GetLastWin32Error() != ErrorInsufficientBuffer ) {
                        return physicalDrive;
                    }
                }
                // Houston, we have a spanner. The volume is on multiple disks.
                // Untested...
                // We need a blob of memory for the DISK_EXTENTS structure, and all the DISK_EXTENTS
                int blobSize = Marshal.SizeOf(typeof(DiskExtents)) + (de.numberOfExtents - 1) * Marshal.SizeOf(typeof(DiskExtent));
                IntPtr pBlob = Marshal.AllocHGlobal(blobSize);
                result = NativeMethods.DeviceIoControl(sfh, IoctlVolumeGetVolumeDiskExtents, IntPtr.Zero, 0, pBlob, blobSize, ref bytesReturned, IntPtr.Zero);
                if ( result == false ) {
                    // throw new Win32Exception();
                }
                // Read them out one at a time.
                IntPtr pNext = new IntPtr(pBlob.ToInt64() + 8);
                // is this always ok on 64 bit OSes? ToInt64?
                for ( int i = 0; i <= de.numberOfExtents - 1; i++ ) {
                    DiskExtent diskExtentN = (DiskExtent)Marshal.PtrToStructure(pNext, typeof(DiskExtent));
                    physicalDrive = "PhysicalDrive " + diskExtentN.DiskNumber.ToString() + " - and more drives";
                    return physicalDrive;
                }
                return physicalDrive;
            } finally {
                if ( sfh != null ) {
                    if ( sfh.IsInvalid == false ) {
                        sfh.Close();
                    }
                    sfh.Dispose();
                }
            }
        }

        private List<string> QueryDosDevice( string device )
        {

            int returnSize = 0;
            uint maxSize = 65536;
            string allDevices = null;
            IntPtr mem = default(IntPtr);
            string[] retval = null;
            List<string> results = new List<string>();

            // Convert an empty string into Nothing, so 
            // QueryDosDevice will return everything available.
            if ( string.IsNullOrEmpty(device.Trim()) )
                device = null;

            while ( returnSize == 0 ) {
                mem = Marshal.AllocHGlobal(Convert.ToInt32(maxSize));
                if ( mem != IntPtr.Zero ) {
                    try {
                        returnSize = Convert.ToInt32(QueryDosDevice(device, mem, maxSize));
                        if ( returnSize != 0 ) {
                            allDevices = Marshal.PtrToStringAuto(mem, returnSize);
                            retval = allDevices.Split('\0');
                            break; // TODO: might not be correct. Was : Exit Try
                        } else {
                            // This query produced no results. Exit the loop.
                            returnSize = -1;
                        }
                    } finally {
                        Marshal.FreeHGlobal(mem);
                    }
                } else {
                    throw new OutOfMemoryException();
                }
            }

            if ( retval != null ) {
                foreach ( string result in retval ) {
                    if ( !string.IsNullOrEmpty(result.Trim()) )
                        results.Add(result);
                }
            }

            return results;
        }

        public string GetPhysicalDiskParentFor( string logicalDisk )
        {

            string[] parts = null;

            if ( logicalDisk.Length > 0 ) {
                foreach ( string driveMapping in currentDriveMappings ) {
                    if ( logicalDisk.Substring(0, 2).ToUpper() == driveMapping.Substring(0, 2).ToUpper() ) {
                        parts = driveMapping.Split('=');
                        return parts[parts.Length - 1];
                    }
                }
            }

            return "";
        }

        public bool GetPhysicalDisks( ref List<string> theList )
        {
            List<string> drivesList = default(List<string>);
            List<string> tmpList = default(List<string>);
            string[] parts = null;
            StringBuilder drives = new StringBuilder();

            foreach ( DriveInfo logicalDrive in DriveInfo.GetDrives() ) {
                try {
                    drives.Remove(0, drives.Length);
                    drives.Append(logicalDrive.RootDirectory.ToString());
                    drives.Append("=");

                    if ( logicalDrive.DriveType == DriveType.Network ) {
                        // handle not connected network drives here.
                        if ( !GrzTools.Network.PingNetDriveOk(logicalDrive.Name.Substring(0, logicalDrive.Name.Length - 1)) ) {
                            drives.Append("n/a");
                            continue;
                        }
                        // handle connected network drives here
                        drives.Append(GetUncPathOfMappedDrive(logicalDrive.RootDirectory.ToString()));
                    } else {
                        if ( logicalDrive.DriveType == DriveType.CDRom ) {
                            // Attempt to get the CDRom's dos name from QueryDosDevice
                            tmpList = QueryDosDevice(logicalDrive.RootDirectory.ToString().Replace("\\", ""));
                            if ( tmpList.Count > 0 ) {
                                parts = tmpList[0].Trim().Split('\\');
                                if ( parts[parts.Length - 1].Length > 5 ) {
                                    if ( parts[parts.Length - 1].Substring(0, 5) == "CdRom" )
                                        parts[parts.Length - 1] = parts[parts.Length - 1].Replace("CdRom", "CD/DVD Rom ");
                                }
                                drives.Append(parts[parts.Length - 1]);
                            } else {
                                drives.Append("n/a");
                            }
                        } else {
                            drivesList = GetPhysicalDriveStrings(logicalDrive);
                            if ( drivesList.Count > 0 ) {
                                foreach ( string drive in drivesList ) {
                                    // Handle the spanners
                                    string tmp = drive.Replace("\\\\.\\", "");
                                    tmp = tmp.Replace("PhysicalDrive", "Physical Drive ");
                                    drives.Append(tmp);
                                    drives.Append(", ");
                                }
                                drives.Remove(drives.Length - 2, 2);
                            } else {
                                drives.Append("n/a");
                            }
                        }
                    }
                    theList.Add(drives.ToString());
                } catch ( Exception ex ) {
                    errorMessage = ex.Message;
                }
            }

            if ( !string.IsNullOrEmpty(errorMessage) ) {
                return false;
            } else {
                return true;
            }
        }

        public string GetUncPathOfMappedDrive( string driveLetter )
        {
            string functionReturnValue = null;

            if ( driveLetter.Substring(driveLetter.Length - 1, 1) == "\\" ) {
                driveLetter = driveLetter.Replace("\\", "");
            }
            functionReturnValue = "";

            List<string> nwDrives = new List<string>();
            string[] parts = null;


            NETRESOURCE o = new NETRESOURCE();
            if ( GetNetworkDrives(ref o, ref nwDrives) ) {
                foreach ( string driveMapping in nwDrives ) {
                    parts = driveMapping.Split('=');
                    if ( parts[0].Trim().ToLower() == driveLetter.Trim().ToLower() ) {
                        return parts[1];
                    }
                }
            }
            return functionReturnValue;

        }

        // Usage:
        //Dim nwDrives As New List(Of String)
        //GetNetworkDrives(Nothing, nwDrives)

        //For Each item As String In nwDrives
        //    'ListBox1.Items.Add(item)
        //Next
        public bool GetNetworkDrives( ref NETRESOURCE o, ref List<string> networkDriveCollection )
        {
            bool functionReturnValue = false;

            int iRet = 0;
            IntPtr ptrHandle = new IntPtr();

            try {
                iRet = NativeMethods.WNetOpenEnum(RESOURCE_SCOPE.RESOURCE_REMEMBERED, RESOURCE_TYPE.RESOURCETYPE_ANY, RESOURCE_USAGE.RESOURCEUSAGE_ATTACHED, ref o, ref ptrHandle);
                if ( iRet != 0 )
                    return functionReturnValue;

                int entries = 0;
                int buffer = 16384;
                IntPtr ptrBuffer = Marshal.AllocHGlobal(buffer);
                NETRESOURCE nr = default(NETRESOURCE);

                do {
                    entries = -1;
                    buffer = 16384;
                    iRet = NativeMethods.WNetEnumResource(ptrHandle, ref entries, ptrBuffer, ref buffer);
                    if ( iRet != 0 | entries < 1 )
                        break; // TODO: might not be correct. Was : Exit Do

                    Int32 ptr = ptrBuffer.ToInt32();
                    for ( int count = 0; count <= entries - 1; count++ ) {
                        nr = (NETRESOURCE)Marshal.PtrToStructure(new IntPtr(ptr), typeof(NETRESOURCE));
                        if ( (RESOURCE_USAGE.RESOURCEUSAGE_CONTAINER == (nr.dwUsage & RESOURCE_USAGE.RESOURCEUSAGE_CONTAINER)) ) {
                            if ( !GetNetworkDrives(ref nr, ref networkDriveCollection) ) {
                                throw new Exception("");
                            }
                        }

                        ptr += Marshal.SizeOf(nr);
                        networkDriveCollection.Add(string.Format(nr.lpLocalName + "=" + nr.lpRemoteName));
                    }
                } while ( true );
                Marshal.FreeHGlobal(ptrBuffer);
                iRet = NativeMethods.WNetCloseEnum(ptrHandle);
            } catch ( Exception ex ) {
                if ( !string.IsNullOrEmpty(ex.Message) )
                    networkDriveCollection.Add(ex.Message);
                return false;
            }

            return true;
//            return functionReturnValue;
        }

    }

    // 20161016: string tools
    public static class StringTools
    {
        // input a long number and get a human readable string back
        static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        static CultureInfo ci = new CultureInfo("de-DE");
        public static string SizeSuffix(Int64 value)
        {
            if ( value < 0 ) { return "-1"; } // +SizeSuffix(-value); }
            if ( value == 0 ) { return "0 bytes"; }
            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));
            return string.Format(ci, "{0:N1} {1}", adjustedSize, SizeSuffixes[mag]);
        }
    }

    // 20120221: network tools
    public static class Network
    {
        // 20160312: check whether a netdrive is accessible
        //           * returns true for special local folders  
        //           * returns true for local drives 
        public static bool PingNetDriveOk( string drive )
        {
            bool bRetVal = false;

            // Computer, Desktop, Downloads, Shared Folders are local ressources
            if ( drive[1] != ':' ) {
                return true;
            }

            // local drives are supposed to be always ok
            DriveInfo di = new DriveInfo(drive);
            if ( di.DriveType != DriveType.Network ) {
                return true;
            }

            // check network function via a Win32 API call
            if ( !IsNetworkAlive() ) {
                return false;
            }

            // convert a local drive name to a mapped network drive's connect string
            string unc = LocalToUNC(drive);
            if ( !unc.Contains('\\') ) {
                if ( unc == drive ) {
                    return true;
                } else {
                    return false;
                }
            }

            // ping the share
            var host = unc.Substring(unc.IndexOf("\\")).Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            Ping pinger = new Ping();
            PingReply reply = null;
            try {
                reply = pinger.Send(host, 10);
            } catch ( Exception ) {
                reply = null;
            }
            if ( reply != null && reply.Status == IPStatus.Success ) {
                bRetVal = true;
            }

            return bRetVal;
        }

        // 20161016: fastest way to find out whether network is down
        [DllImport("sensapi.dll")]
        static extern bool IsNetworkAlive( out int flags );
        public static bool IsNetworkAlive(int flags = 0)
        {
            return IsNetworkAlive(out flags);
        }

        // 20160312: check whether an IP is accessible
        public static bool PingIpOk( string ip )
        {
            bool bRetVal = false;

            if ( (ip == null) || (ip.Length == 0) ) {
                return bRetVal;
            }

            Ping pinger = new Ping();
            PingReply reply = pinger.Send(ip, 10);
            if ( reply != null && reply.Status == IPStatus.Success ) {
                bRetVal = true;
            }

            return bRetVal;
        }

        // get network drive mapping
        [DllImport("mpr.dll")]
        static extern int WNetGetUniversalNameA( string lpLocalPath, int dwInfoLevel, IntPtr lpBuffer, ref int lpBufferSize );
        public static string LocalToUNC( string localPath, int maxLen = 512 )
        {
            IntPtr lpBuff;
            // Allocate the memory
            try {
                lpBuff = Marshal.AllocHGlobal(maxLen);
            } catch ( OutOfMemoryException ) {
                return "out of memory";
            }

            try {
                int res = WNetGetUniversalNameA(localPath, 1, lpBuff, ref maxLen);
                if ( res != 0 ) {
                    if ( res == 2250 ) {  // aka "not connected"
                        return localPath;
                    } else {
                        return "WNetGetUniversalNameA failure";
                    }
                }
                // lpbuff is a structure, whose first element is a pointer to the UNC name (just going to be lpBuff + sizeof(int))
                return Marshal.PtrToStringAnsi(Marshal.ReadIntPtr(lpBuff));
            } catch ( Exception ) {
                return "Exception";
            } finally {
                Marshal.FreeHGlobal(lpBuff);
            }
        }
    }

    // 
    public static class InstalledPrograms
    {
        public static string ProgramPath( string program )
        {
            string ret = "";
            try {
                // x86 programs
                string registry_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
                using ( Microsoft.Win32.RegistryKey key = Registry.LocalMachine.OpenSubKey(registry_key) ) {
                    foreach ( string subkey_name in key.GetSubKeyNames() ) {
                        using ( RegistryKey subkey = key.OpenSubKey(subkey_name) ) {
                            string app = (string)subkey.GetValue("DisplayName");
                            if ( app != null ) {
                                if ( app.IndexOf(program, StringComparison.OrdinalIgnoreCase) != -1 ) {
                                    ret = subkey.GetValue("DisplayIcon").ToString();
                                    break;
                                }
                            }
                        }
                    }
                }
                if ( ret == "" ) {
                    // x64 programs
                    registry_key = @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
                    using ( Microsoft.Win32.RegistryKey key = Registry.LocalMachine.OpenSubKey(registry_key) ) {
                        foreach ( string subkey_name in key.GetSubKeyNames() ) {
                            using ( RegistryKey subkey = key.OpenSubKey(subkey_name) ) {
                                string app = (string)subkey.GetValue("DisplayName");
                                if ( app != null ) {
                                    if ( app.IndexOf(program, StringComparison.OrdinalIgnoreCase) != -1 ) {
                                        ret = subkey.GetValue("DisplayIcon").ToString();
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            } catch ( Exception ) { ;}
            return ret;
        }
    }

    public class FileTools
    {
        // 20161016: fast alternative to Directory.Exists, which may hang for ca. 20s on a not connected network drive
        public static bool PathExists( string path, int timeout = 100 ) 
        {
            if ( path == null ) {
                return false;
            }

            if ( !Network.PingNetDriveOk(path.Substring(0, 2)) ) {
                return false;
            }
            var task = new Task<bool>(() => { return System.IO.Directory.Exists(path); });
            task.Start();
            return task.Wait(timeout) && task.Result;
        }

        // 20161016: retrieve write permission status of a folder, answer 3 http://stackoverflow.com/questions/1410127/c-sharp-test-if-user-has-write-access-to-a-folder
        public static bool IsDirectoryWritable( string dirPath )
        {
            try {
                using ( FileStream fs = System.IO.File.Create(Path.Combine(dirPath, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose) ) { }
                return true;
            } catch {
                return false;
            }
        }

        // helper labelPrompt: gets proper directory capitalization
        public static string GetProperDirectoryCapitalization( DirectoryInfo dirInfo )
        {
            DirectoryInfo parentDirInfo = dirInfo.Parent;
            if ( null == parentDirInfo ) {
                return dirInfo.Name;
            }
            DirectoryInfo[] da = parentDirInfo.GetDirectories(dirInfo.Name);
            if ( da.Length > 0 ) {
                return Path.Combine(GetProperDirectoryCapitalization(parentDirInfo), parentDirInfo.GetDirectories(dirInfo.Name)[0].Name);
            } else {
                return "";
            }
        }

        // translate literal folder (Desktop, Documents etc.) names into OS understandable folders
        public static readonly Guid guid_Downloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHGetKnownFolderPath( [MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out string pszPath );
        public static string TranslateSpecialFolderNames( string stringPath )
        {
            string stringParse = stringPath;

            // replace Desktop with its real path
            if ( stringParse.StartsWith("Desktop") ) {
                stringParse = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }

            // replace Documents with its real path
            if ( stringParse.StartsWith("Documents") ) {
                stringParse = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            // replace Downloads with its real path
            if ( stringParse.StartsWith("Downloads") ) {
                string downloads;
                try {
                    SHGetKnownFolderPath(guid_Downloads, 0, IntPtr.Zero, out downloads);
                    downloads = downloads.ReplaceAt(0, Char.ToUpper(downloads[0]));
                } catch ( Exception ) {
                    string pathUserDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    downloads = Path.Combine(pathUserDocs, "Downloads");
                }
                stringParse = downloads;
            }

            return stringParse;
        }

        // app x86 no need to set anything and it works - app x64 doesn't work with neither setting
        // [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        // [StructLayout(LayoutKind.Sequential, Pack=4)]
        // [StructLayout(LayoutKind.Sequential, Pack=1)]
        private struct SHQUERYRBINFO
        {
            /// DWORD->unsigned int
            public uint cbSize;
            /// __int64
            public long i64Size;
            /// __int64
            public long i64NumItems;
        }

        // works only in x86 mode on 64bit OS - no chance to get it work for x64        
        /// Return Type: HRESULT->LONG->int
        /// pszRootPath: LPCTSTR->LPCWSTR->WCHAR*
        /// pSHQueryRBInfo: LPSHQUERYRBINFO->_SHQUERYRBINFO*
        [System.Runtime.InteropServices.DllImportAttribute("shell32.dll", EntryPoint = "SHQueryRecycleBinW")]
        private static extern int SHQueryRecycleBinW( [System.Runtime.InteropServices.InAttribute()] [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPTStr)] string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo );
/*
        public static bool DriveHasRecycleBin_x86( string Drive )
        {
            SHQUERYRBINFO Info = new SHQUERYRBINFO();
            Info.cbSize = 20; //sizeof(SHQUERYRBINFO)
            return SHQueryRecycleBinW(Drive, ref Info) == 0;
        }

*/
        // drive has a recycle bin?
        public static bool DriveHasRecycleBin( string Drive )
        {
            bool bHasBin = false;
            string path = Path.Combine(Drive, "$RECYCLE.BIN");
            if ( FastFileFind.FindFile(path) ) {
                bHasBin = true;
            } else {
                if ( System.IO.File.Exists(path) ) {  // Win 7, 8, 10
                    bHasBin = true;
                } else {
                    path = Path.Combine(Drive, "RECYCLER");
                    if ( System.IO.File.Exists(path) ) {  // Win XP
                        bHasBin = true;
                    } else {
                        SHQUERYRBINFO Info = new SHQUERYRBINFO();                    // all x86
                        Info.cbSize = 20;
                        bHasBin = SHQueryRecycleBinW(Drive, ref Info) == 0;
                    }
                }
            }
            return bHasBin;
        }

        // delete empty directories .net >=4.0
        public static bool DeleteEmptyDirs( string dir, ref int deleted )
        {
            bool bRetVal = true;

            try {
                foreach ( var d in Directory.EnumerateDirectories(dir) ) {
                    bRetVal = DeleteEmptyDirs(d, ref deleted);
                }

                var entries = Directory.EnumerateFileSystemEntries(dir);

                if ( !entries.Any() ) {
                    try {
                        Directory.Delete(dir);
                        deleted++;
                    } catch ( UnauthorizedAccessException ) { return false; } catch ( DirectoryNotFoundException ) { return false; } catch ( Exception ) { return false; }
                }
            } catch ( UnauthorizedAccessException ) { return false; }

            return bRetVal;
        }

        // NOT GOOD: could be very slow
        //public static long GetDirectorySize( string folderPath )
        //{
        //    long size = 0;
        //    try {
        //        DirectoryInfo di = new DirectoryInfo(folderPath);
        //        size = di.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length);
        //    } catch ( Exception ) {
        //        size *= -1;
        //    }
        //    return size;
        //}

        // explorer file properties
        public static bool ShowFileProperties( string Filename )
        {
            SHELLEXECUTEINFO info = new SHELLEXECUTEINFO();
            info.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(info);
            info.lpVerb = "properties";
            info.lpFile = Filename;
            info.nShow = SW_SHOW;
            info.fMask = SEE_MASK_INVOKEIDLIST;
            bool retVal =  ShellExecuteEx(ref info);

            return retVal;
        }
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern bool ShellExecuteEx( ref SHELLEXECUTEINFO lpExecInfo );
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpVerb;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpParameters;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }
        private const int SW_SHOW = 5;
        private const uint SEE_MASK_INVOKEIDLIST = 12;

        // this file copy method using streams allows implementation of an owner drawn progress bar, but may fail sometimes due to missing privileges
        public static bool FileCopyEx( string src, string dst, ProgressBar pb )
        {
            try {
                using ( Stream fr = new FileStream(src, FileMode.Open) )            // stream to read
                using ( Stream to = new FileStream(dst, FileMode.OpenOrCreate) ) {  // stream to write
                    int readCount;
                    byte[] buffer = new byte[1024];
                    while ( (readCount = fr.Read(buffer, 0, 1024)) != 0 ) {         // read & write loop
                        to.Write(buffer, 0, readCount);                             // write operation
                        pb.PerformStep();                                           // progress  
                        Application.DoEvents();                                     // cooperation  
                    }
                }
            } catch ( Exception ) {
                try {
                    System.IO.File.Copy(src, dst, true);
                } catch ( Exception ) {
                    return false;
                }
            }
            return true;
        }

        // distinguishes between text and binary files
        unsafe public static bool IsBinaryFile( string filePath )
        {
            // read sample size bytes
            try {
                int sampleSize = 1000;
                var buffer = new char[sampleSize];
                int length = 0;
                using ( var sr = new StreamReader(filePath) ) {
                    length = sr.Read(buffer, 0, sampleSize);
                }

                // look for zero
                for ( int i = 0; i<length; i++ ) {
                    if ( buffer[i] == '\0' ) {
                        return true;
                    }
                }
            } catch ( UnauthorizedAccessException ) {
                return true;
            }

            return false;
        }

        public static bool IsTextFile( out Encoding encoding, string fileName, int windowSize )
        {
            encoding = Encoding.Default;
            FileStream fileStream = null;
            try {
//                fileStream = File.OpenRead(fileName);
                fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            } catch ( Exception ) {
                encoding = Encoding.Default;
                return false;
            }

            // length to read
            long sz = fileStream.Length;
            long readLength = Math.Min(sz, windowSize);

            var rawData = new byte[readLength];
            var text = new char[readLength];
            var isText = true;

            // Read raw bytes
            var rawLength = fileStream.Read(rawData, 0, (int)readLength);
            fileStream.Seek(0, SeekOrigin.Begin);

            // Detect encoding correctly (from Rick Strahl's blog)
            // http://www.west-wind.com/weblog/posts/2007/Nov/28/Detecting-Text-Encoding-for-StreamReader
            if ( rawData.Length > 3 ) {
                if ( rawData[0] == 0xef && rawData[1] == 0xbb && rawData[2] == 0xbf ) {
                    encoding = Encoding.UTF8;
                } else if ( rawData[0] == 0xff && rawData[1] == 0xfe ) {
                    encoding = Encoding.Unicode;
                } else if ( rawData[0] == 0 && rawData[1] == 0 && rawData[2] == 0xfe && rawData[3] == 0xff ) {
                    encoding = Encoding.UTF32;
                } else if ( rawData[0] == 0x2b && rawData[1] == 0x2f && rawData[2] == 0x76 ) {
                    encoding = Encoding.UTF7;
                } else if ( rawData[0] == 0xfe && rawData[1] == 0xff ) {
                    encoding = Encoding.BigEndianUnicode; // utf-16be
                } else {
                    encoding = Encoding.Default;
                }
            }

            // Read text and detect the encoding
            using ( var streamReader = new StreamReader(fileStream) ) {
                streamReader.Read(text, 0, (int)readLength);
            }

            // if a string contains more than 5x '\0', it's rather unlike being text
            //string s = (new string(text)).Trim('\0');
            string s = new string(text);
            if ( encoding == Encoding.Default ) {
                int count = s.Count(f => f == '\0');
                if ( count > 5 ) {
                    return false;
                }
            }

            string umlaute = "äöüÄÖÜß";

            using ( var memoryStream = new MemoryStream() ) {
                using ( var streamWriter = new StreamWriter(memoryStream, encoding) ) {
                    // Write the text to a buffer
                    streamWriter.Write(text);
                    streamWriter.Flush();

                    // Get the buffer from the memory stream for comparision
                    var memoryBuffer = memoryStream.GetBuffer();

                    // Compare only bytes read
                    for ( var i = 0; i < rawLength && isText; i++ ) {
                        if ( umlaute.Contains((char)memoryBuffer[i]) ) {
                            continue;
                        }
                        if ( umlaute.Contains((char)rawData[i]) ) {
                            continue;
                        }
                        isText = rawData[i] == memoryBuffer[i];
                    }
                }
            }
            return isText;
        }
        
        public static bool IsTextFile_before20161016( out Encoding encoding, string fileName, int windowSize )
        {
            FileStream fileStream = null;
            try {
                fileStream = System.IO.File.OpenRead(fileName);
            } catch ( Exception ) {
                encoding = Encoding.Default;
                return false;
            }

            var rawData = new byte[windowSize];
            var text = new char[windowSize];
            var isText = true;

            // Read raw bytes
            var rawLength = fileStream.Read(rawData, 0, rawData.Length);
            fileStream.Seek(0, SeekOrigin.Begin);

            // Detect encoding correctly (from Rick Strahl's blog)
            // http://www.west-wind.com/weblog/posts/2007/Nov/28/Detecting-Text-Encoding-for-StreamReader
            if ( rawData[0] == 0xef && rawData[1] == 0xbb && rawData[2] == 0xbf ) {
                encoding = Encoding.UTF8;
            } else if ( rawData[0] == 0xff && rawData[1] == 0xfe ) {
                encoding = Encoding.Unicode;
            } else if ( rawData[0] == 0 && rawData[1] == 0 && rawData[2] == 0xfe && rawData[3] == 0xff ) {
                encoding = Encoding.UTF32;
            } else if ( rawData[0] == 0x2b && rawData[1] == 0x2f && rawData[2] == 0x76 ) {
                encoding = Encoding.UTF7;
            } else if ( rawData[0] == 0xfe && rawData[1] == 0xff ) {
                encoding = Encoding.BigEndianUnicode; // utf-16be
            } else {
                encoding = Encoding.Default;
            }

            // Read text and detect the encoding
            using ( var streamReader = new StreamReader(fileStream) ) {
                streamReader.Read(text, 0, text.Length);
            }

            using ( var memoryStream = new MemoryStream() ) {
                using ( var streamWriter = new StreamWriter(memoryStream, encoding) ) {
                    // Write the text to a buffer
                    streamWriter.Write(text);
                    streamWriter.Flush();

                    // Get the buffer from the memory stream for comparision
                    var memoryBuffer = memoryStream.GetBuffer();

                    // Compare only bytes read
                    for ( var i = 0; i < rawLength && isText; i++ ) {
                        isText = rawData[i] == memoryBuffer[i];
                    }
                }
            }

            fileStream.Close();

            return isText;
        }

        // returns spare disk space even on UNC paths
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetDiskFreeSpaceEx( string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes );
        public static bool DriveFreeBytes( string folderName, out ulong freespace, out ulong totalspace )
        {
            freespace = 0;
            totalspace = 0;
            if ( string.IsNullOrEmpty(folderName) ) {
                return false;
            }

            if ( !folderName.EndsWith("\\") ) {
                folderName += '\\';
            }

            ulong free = 0, total = 0, totalfree = 0;

            if ( GetDiskFreeSpaceEx(folderName, out free, out total, out totalfree) ) {
                freespace = free;
                totalspace = total;
                return true;
            } else {
                return false;
            }
        }
    }

    // way to find out, whether a PC has a toucscreen or not
    public class TouchTools
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int GetSystemMetrics( int nIndex );
        const int SM_MAXIMUMTOUCHES = 95;
        public static bool IsTouchDevice()
        {
            var count = GetSystemMetrics(SM_MAXIMUMTOUCHES);
            return (count>0);
        }
    }

    // INI-Files : the easiest (though outdated) way to manage setup info
    public class IniFile
    {
        public string path;
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString( string section, string key, string val, string filePath );
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString( string section, string key, string def, StringBuilder retVal, int size, string filePath );
        public IniFile( string INIPath )
        {
            path = INIPath;
        }
        public void IniWriteValue( string Section, string Key, string Value )
        {
            WritePrivateProfileString(Section, Key, Value, this.path);
        }
        public string IniReadValue( string Section, string Key, string DefaultValue )
        {
            StringBuilder retVal = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, DefaultValue, retVal, 255, this.path);
            return retVal.ToString();
        }
    }

    // https://stackoverflow.com/questions/26321366/fastest-way-to-get-directory-data-in-net
    public class Folder
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr FindFirstFileW( string lpFileName, out WIN32_FIND_DATAW lpFindFileData );
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool FindNextFile( IntPtr hFindFile, out WIN32_FIND_DATAW lpFindFileData );
        [DllImport("kernel32.dll")]
        private static extern bool FindClose( IntPtr hFindFile );
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WIN32_FIND_DATAW
        {
            public FileAttributes dwFileAttributes;
            internal System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            internal System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            internal System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public int nFileSizeHigh;
            public int nFileSizeLow;
            public int dwReserved0;
            public int dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }
        private static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        // parallel & recursion: a parallel foreach of a list of folders starts a recursion 
        // https://stackoverflow.com/questions/2106877/is-there-a-faster-way-than-this-to-find-all-the-files-in-a-directory-and-all-sub
        private int _count;
        private object _lockThis = new object();
        private bool _run;
        public void Stop()
        {
            _run = false;
        }
        public int GetFolderCount( string rootFolderPath )
        {
            _run = true;
            lock ( _lockThis ) {
                _count = 1;
            }
            getSubFolderCount(rootFolderPath);
            return _count;
        }
        private void getSubFolderCount( string rootFolderPath )
        {
            if ( !_run ) {
                return;
            }
            var subDirectories = getFolders(rootFolderPath);
            lock ( _lockThis ) {
                _count += subDirectories.Length;
            }
            Parallel.ForEach(subDirectories, new Action<string, ParallelLoopState>(( string directory, ParallelLoopState state ) => {
                getSubFolderCount(directory);
                if ( !_run ) {
                    state.Break();
                }
            }));
        }
        private string[] getFolders( string path )      
        { 
            List<string> list = new List<string>();
            WIN32_FIND_DATAW findData;
            IntPtr findHandle = INVALID_HANDLE_VALUE;
            try {
                findHandle = FindFirstFileW(path + @"\*", out findData);
                if ( findHandle != INVALID_HANDLE_VALUE ) {
                    do {
                        if ( findData.cFileName != "." && findData.cFileName != ".." ) {
                            string fullPath = path + @"\" + findData.cFileName;
                            if ( findData.dwFileAttributes.HasFlag(FileAttributes.Directory) ) {
                                list.Add(fullPath);
                            }
                        }
                    }
                    while ( FindNextFile(findHandle, out findData) && _run );
                }
            } catch ( Exception exception ) {
                Console.WriteLine("Caught exception while trying to enumerate a directory. {0}", exception.ToString());
                if ( findHandle != INVALID_HANDLE_VALUE ) FindClose(findHandle);
                return list.ToArray();
            }
            if ( findHandle != INVALID_HANDLE_VALUE ) FindClose(findHandle);
            return list.ToArray();
        }
    }

    //
    // fast file search
    //
    // -------------------------------------------------------
    // heavily inspired by Kåre Smith
    public class FastFileFind
    {
        // internal win32 stuff
        const int MAX_PATH = 260;
        const int MAX_ALTERNATE = 14;
        [StructLayout(LayoutKind.Sequential)]
        public struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        };
        public static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        public struct WIN32_FIND_DATA
        {
            public FileAttributes dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=MAX_PATH)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=MAX_ALTERNATE)]
            public string cAlternate;
        }
        [DllImport("kernel32", CharSet=CharSet.Auto)]
        public static extern IntPtr FindFirstFile( string lpFileName, out WIN32_FIND_DATA lpFindFileData );
        [DllImport("kernel32", CharSet=CharSet.Auto)]
        public static extern bool FindNextFile( IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData );
        [DllImport("kernel32.dll")]
        public static extern bool FindClose( IntPtr hFindFile );
        // wow64 redirection causes Windows\System32\ issues for 32bit apps running in 64bit environments
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool Wow64DisableWow64FsRedirection( ref IntPtr ptr );
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool Wow64RevertWow64FsRedirection( IntPtr ptr );
        // **
        [Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode), BestFitMapping(false)]
        public struct Win32FindData
        {
            public FileAttributes dwFileAttributes;
            public uint ftCreationTime_dwLowDateTime;
            public uint ftCreationTime_dwHighDateTime;
            public uint ftLastAccessTime_dwLowDateTime;
            public uint ftLastAccessTime_dwHighDateTime;
            public uint ftLastWriteTime_dwLowDateTime;
            public uint ftLastWriteTime_dwHighDateTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public int dwReserved0;
            public int dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }
        private enum IndexInfoLevels
        {
            FindExInfoStandard,
            FindExInfoBasic,
            FindExInfoMaxInfoLevel
        };
        private enum IndexSearchOps
        {
            FindExSearchNameMatch,
            FindExSearchLimitToDirectories,
            FindExSearchLimitToDevices
        };
        private const int FIND_FIRST_EX_LARGE_FETCH = 0x02;
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true), SuppressUnmanagedCodeSecurity]
        private static extern IntPtr FindFirstFileExW( string lpFileName, IndexInfoLevels infoLevels, out Win32FindData lpFindFileData, IndexSearchOps fSearchOp, IntPtr lpSearchFilter, int dwAdditionalFlag );
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true), SuppressUnmanagedCodeSecurity]
        private static extern bool FindNextFileW( IntPtr hndFindFile, out Win32FindData lpFindFileData );
        // **

        // public definition of the count&size event arguments class
        public class CountSizeEventArgs: EventArgs
        {
            public CountSizeEventArgs( long foldercount, long filecount, long size, string path )
            {
                FolderCount = foldercount;
                FileCount = filecount;
                Size  = size;
                Path = path;
            }
            public long FolderCount { get; set; }
            public long FileCount { get; set; }
            public long Size { get; set; }
            public string Path { get; set; }
        }

        // public definition of the event arguments class
        public class ChangeEventArgs: EventArgs
        {
            public ChangeEventArgs( string folder, string filename, string linetext, StringComparison stringcompare, string fileline, bool hit = false, bool error = false )
            {
                ProgFold      = folder;
                FileName      = filename;
                LineText      = linetext;
                StringCompare = stringcompare;
                FileLine      = fileline;
                Hit           = hit;
                this.error    = error;
            }
            public string ProgFold { get; set; }
            public string FileName { get; set; }
            public string LineText { get; set; }
            public StringComparison StringCompare { get; set; }
            public string FileLine { get; set; }
            public bool Hit { get; set; }
            public bool error { get; set; }
        }

        // internal member variables 
        private static Form m_parent;
        private int m_hit;

        // accumulators for folder/file count & file size
        private long _foc = 0;
        private long _fic = 0;
        private long _siz = 0;

        // public return struct for folder/file count & file size
        public struct FocFicSiz
        {
            public FocFicSiz( long d, long f, long s )
            {
                foc = d;
                fic = f;
                siz = s;
            }
            public long foc;
            public long fic;
            public long siz;
        }

        // public constructor
        public FastFileFind( Form parent )
        {
            m_parent = parent;
            m_hit = 0;
        }

        // public event handlers
        public event EventHandler<ChangeEventArgs> ChangeEvent;
        public event EventHandler<CountSizeEventArgs> CountSizeEvent;

        // retrieve mime type according to file extension and registry 
        string GetMimeType( FileInfo fileInfo )
        {
            string mimeType = "application/unknown";
            RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(fileInfo.Extension.ToLower());
            if ( regKey != null ) {
                object contentType = regKey.GetValue("Content Type");
                if ( contentType != null )
                    mimeType = contentType.ToString();
            }
            return mimeType;
        }
        public static string GetMimeType( string extension )
        {
            string mimeType = "unknown";
            RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(extension);
            if ( regKey != null ) {
                object contentType = regKey.GetValue("Content Type");
                if ( contentType != null ) {
                    mimeType = contentType.ToString();
                }
            }
            return mimeType;
        }


        public static class FindFilesPatternToRegex
        {
            private static Regex HasQuestionMarkRegEx   = new Regex(@"\?", RegexOptions.Compiled);
            private static Regex IllegalCharactersRegex  = new Regex("[" + @"\/:<>|" + "\"]", RegexOptions.Compiled);
            private static Regex CatchExtentionRegex    = new Regex(@"^\s*.+\.([^\.]+)\s*$", RegexOptions.Compiled);
            private static string NonDotCharacters      = @"[^.]*";
            public static Regex Convert( string pattern )
            {
                if ( pattern == null ) {
                    throw new ArgumentNullException();
                }
                pattern = pattern.Trim();
                if ( pattern.Length == 0 ) {
                    throw new ArgumentException("Pattern is empty.");
                }
                if ( IllegalCharactersRegex.IsMatch(pattern) ) {
                    throw new ArgumentException("Pattern contains illegal characters.");
                }
                bool hasExtension = CatchExtentionRegex.IsMatch(pattern);
                bool matchExact = false;
                if ( HasQuestionMarkRegEx.IsMatch(pattern) ) {
                    matchExact = true;
                } else if ( hasExtension ) {
                    matchExact = CatchExtentionRegex.Match(pattern).Groups[1].Length != 3;
                }
                string regexString = Regex.Escape(pattern);
                regexString = "^" + Regex.Replace(regexString, @"\\\*", ".*");
                regexString = Regex.Replace(regexString, @"\\\?", ".");
                if ( !matchExact && hasExtension ) {
                    regexString += NonDotCharacters;
                }
                regexString += "$";
                Regex regex = new Regex(regexString, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                return regex;
            }
        }


        //
        // public find methods
        //
        // -------------------------------------------------------------------------------------------------------------------------

        // simple file find
        //   - filename must comply to a pattern
        //   - returns a list of found files
        //   - optionally add folders to the list
        public static void FindFiles(ref List<String> lst, string sStartDir, string sFileMask, bool inclFolders, ref bool bRun )
        {
            Application.DoEvents();
            if ( !bRun ) {
                return;
            }

            // start searching with * and only catch directories
            WIN32_FIND_DATA dData;
            IntPtr dHandle;
            dHandle = FindFirstFile(@Path.Combine(@sStartDir, @"*"), out dData);
            if ( dHandle != INVALID_HANDLE_VALUE ) {
                do {
                    Application.DoEvents();
                    if ( (dData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory ) {
                        if ( dData.cFileName != "." && dData.cFileName != ".." ) {
                            string subdirectory = @Path.Combine(@sStartDir, @dData.cFileName);
                            // recursion starts, when a valid directory was found
                            FindFiles(ref lst, subdirectory, sFileMask, inclFolders, ref bRun);
                        }
                    }
                } while ( bRun && FindNextFile(dHandle, out dData) );
                FindClose(dHandle);
            }
            // optional: add folder to the list
            if ( inclFolders ) {
                lst.Add(@sStartDir);
            }
            // if no directory was found in current recursion, then the search starts again but now with the real file 'mask' and let pass only files
            WIN32_FIND_DATA fData;
            IntPtr fHandle = FindFirstFile(@Path.Combine(@sStartDir, @sFileMask), out fData);
            if ( fHandle != INVALID_HANDLE_VALUE ) {
                do {
                    Application.DoEvents();
                    if ( (fData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory ) {
                        // filename matches with 'mask'
                        lst.Add(@Path.Combine(@sStartDir, @fData.cFileName));
                    }
                } while ( bRun && FindNextFile(fHandle, out fData) );
                FindClose(fHandle);
            }
        }

        // simple file find as replacement for System.IO.File.Exists(..), which doesn't work on x64 systems when checking for $RECYCLE.BIN
        public static bool FindFile( string sFile )
        {
            bool retval = false;
            WIN32_FIND_DATA fData;
            IntPtr fHandle = FindFirstFile(sFile, out fData);
            if ( fHandle != INVALID_HANDLE_VALUE ) {
                retval = true;
            }
            FindClose(fHandle);
            return retval;
        }

        // set datetime to all directories & files underneath path
        public void SetDateTimeFilesFoldersRecursive( string sStartDir, DateTime dtCreated, DateTime dtModified, DateTime dtAccessed, ref int fails )
        {
            // start searching with * and only catch directories
            WIN32_FIND_DATA dData;
            IntPtr dHandle;
            dHandle = FindFirstFile(@Path.Combine(@sStartDir, @"*"), out dData);
            if ( dHandle != INVALID_HANDLE_VALUE ) {
                do {
                    Application.DoEvents();
                    if ( (dData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory ) {
                        if ( dData.cFileName != "." && dData.cFileName != ".." ) {
                            string subdirectory = @Path.Combine(@sStartDir, @dData.cFileName);
                            // do something with directory
                            try {
                                Directory.SetCreationTime(subdirectory, dtCreated);
                                Directory.SetLastWriteTime(subdirectory, dtModified);
                                Directory.SetLastAccessTime(subdirectory, dtAccessed);
                            } catch ( Exception ) { fails++; }
                            // iam alive 1
                            CountSizeEvent(m_parent, new CountSizeEventArgs(1, 0, 0, ""));
                            // recursion starts, when a valid directory was found
                            SetDateTimeFilesFoldersRecursive(subdirectory, dtCreated, dtModified, dtAccessed, ref fails);
                        }
                    }
                } while ( FindNextFile(dHandle, out dData) );
                FindClose(dHandle);
            }
            // if no directory was found in current recursion, then the search starts again but now with the real file 'mask' and let pass only files
            WIN32_FIND_DATA fData;
            IntPtr fHandle = FindFirstFile(@Path.Combine(@sStartDir, @"*.*"), out fData);
            if ( fHandle != INVALID_HANDLE_VALUE ) {
                do {
                    Application.DoEvents();
                    if ( (fData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory ) {
                        string fn = @Path.Combine(@sStartDir, @fData.cFileName);
                        // do something with filename
                        try {
                            System.IO.File.SetCreationTime(fn, dtCreated);
                            System.IO.File.SetLastWriteTime(fn, dtModified);
                            System.IO.File.SetLastAccessTime(fn, dtAccessed);
                            // iam alive 1
                            CountSizeEvent(m_parent, new CountSizeEventArgs(1, 0, 0, ""));
                        } catch ( Exception ) { fails++; }
                    }
                } while ( FindNextFile(fHandle, out fData) );
                FindClose(fHandle);
            }
            // a folder is done 0
            CountSizeEvent(m_parent, new CountSizeEventArgs(0, 0, 0, ""));
        }

        // files sizes beginning with a start folder, it's not as fast as FSO but works on C: and Documents
        //   - stoppable per 'run' 
        //   - filename complies to a pattern
        //   - returns file size
        public static void FileSizes( ref bool run, string @sStartDir, ref long size )
        {
            WIN32_FIND_DATA data;
            IntPtr handle;

            // start searching with * and only catch directories ...
            handle = FindFirstFile(@sStartDir + "\\*", out data);
            if ( handle != INVALID_HANDLE_VALUE ) {
                do {
                    if ( (data.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory ) {
                        if ( (data.cFileName != ".") && (data.cFileName != "..") ) {
                            string @subdirectory = @sStartDir + "\\" + @data.cFileName;
                            // ... recursion starts at anytime, a valid directory was found ... 
                            FileSizes(ref run, @subdirectory, ref size);
                            // >> recursion roll back point <<
                        }
                    }
                } while ( FindNextFile(handle, out data) && run );
                FindClose(handle);
            }

            // ... recursion is 1st time over, when "lowest level" directory was found --> then the search starts again but now with a file mask and let pass only files ...
            handle = FindFirstFile(@sStartDir + @"\\*.*", out data);
            if ( handle != INVALID_HANDLE_VALUE ) {
                do {
                    if ( (data.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory ) {
                        long fs = data.nFileSizeHigh;
                        fs <<= 0x20;
                        fs |= data.nFileSizeLow;
                        size += fs;
                    }
                } while ( FindNextFile(handle, out data) && run );
                FindClose(handle);
            }
            // ... and recursion rolls back
        }

        // count folders beginning with a start folder
        //   - stoppable per 'run' 
        //   - returns folder count via ffs.foc
        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public void FolderCount(ref FocFicSiz ffs, ref bool run, string sStartDir)
        {
            WIN32_FIND_DATA data;
            IntPtr handle = IntPtr.Zero;

            // search for * and only catch directories
            try {
                handle = FindFirstFile(sStartDir + "\\*", out data);
                if ( handle != INVALID_HANDLE_VALUE ) {
                    do {
                        if ( (data.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory ) {
                            if ( (data.cFileName != ".") && (data.cFileName != "..") ) {
                                string subdirectory = sStartDir + "\\" + data.cFileName;
                                _foc++;
                                // recursion starts with any valid directory found
                                FolderCount(ref ffs, ref run, subdirectory);
                            }
                        }
                    } while ( FindNextFile(handle, out data) && run );
                    FindClose(handle);
                }
            } catch { ;}
            if ( handle != IntPtr.Zero ) {
                FindClose(handle);
            }

            // save data to ffs return value
            ffs.foc += _foc;
            _foc = 0;
        }

        // count folders & files & sizes beginning with a start folder
        //   - stoppable per 'run' 
        //   - filename complies to a pattern
        //   - reports folder & file count & size via event to parent to show progress: not to 100% accurate for unknown reason
        //   - returns folder & file count & size via ffs: 100% accurate
        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public void FileCountSize(ref FocFicSiz ffs, ref bool run, string sStartDir, string sFileMask)
        {
            WIN32_FIND_DATA data;
            IntPtr handle;

            // start searching with * and only catch directories
            handle = FindFirstFile(sStartDir+"\\*", out data);
            if ( handle != INVALID_HANDLE_VALUE ) {
                do {
                    if ( (data.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory ) {
                        if ( (data.cFileName != ".") && (data.cFileName != "..") ) {
                            string subdirectory = sStartDir+"\\"+data.cFileName;
                            _foc++;
                            // recursion starts at anytime, a valid directory was found
                            FileCountSize(ref ffs, ref run, subdirectory, sFileMask);
                        }
                    }
                } while ( FindNextFile(handle, out data) && run );
                FindClose(handle);
            }

            // if no "deeper level" directory was found in current recursion, then the search starts again but now with the real file mask and let pass only files
            handle = FindFirstFile(sStartDir+"\\"+sFileMask, out data);
            if ( handle != INVALID_HANDLE_VALUE ) {
                do {
                    if ( (data.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory ) {
                        _fic++;
                        long fs = data.nFileSizeHigh;
                        fs <<= 0x20; //sizeof(uint) * 8;
                        fs |= data.nFileSizeLow;
                        _siz = _siz + fs;
                    }
                } while ( FindNextFile(handle, out data) && run );
                FindClose(handle);
            }

            // grant process cooperation each time we return from a directory
            // commented out due to parallel call of this function       
            //            Application.DoEvents();

            // save data to ffs return value
            ffs.foc += _foc;
            ffs.fic += _fic;
            ffs.siz += _siz;

            // raise event at parent each time we finished file find, aka we are done with a folder
            if ( (m_parent != null) && (CountSizeEvent != null) ) {
                CountSizeEvent(m_parent, new CountSizeEventArgs(_foc, _fic, _siz, sStartDir));
            }
            _foc = 0;
            _fic = 0;
            _siz = 0;
        }

        // count folders & files beginning with a start folder
        //   - filenames must comply to a pattern
        //   - returns folder & file count via ffs
        public static int FileFolderCount(string sStartDir, string sFileMask )
        {
            WIN32_FIND_DATA data;
            IntPtr handle;

            // return value
            int count = 0;

            // start searching with * and only catch directories
            handle = FindFirstFile(sStartDir+"\\*", out data);
            if ( handle != INVALID_HANDLE_VALUE ) {
                do {
                    if ( (data.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory ) {
                        if ( (data.cFileName != ".") && (data.cFileName != "..") ) {
                            string subdirectory = sStartDir+"\\"+data.cFileName;
                            count++;
                            // recursion starts at anytime, a valid directory was found
                            count += FileFolderCount(subdirectory, sFileMask);
                        }
                    }
                } while ( FindNextFile(handle, out data) );
                FindClose(handle);
            }

            // if no "deeper level" directory was found in current recursion, then the search starts again but now with the real file mask and let pass only files
            handle = FindFirstFile(sStartDir+"\\"+sFileMask, out data);
            if ( handle != INVALID_HANDLE_VALUE ) {
                do {
                    if ( (data.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory ) {
                        count++;
                    }
                } while ( FindNextFile(handle, out data) );
                FindClose(handle);
            }

            // save data to ffs return value
            return count;
        }

        // find top level files and folders: MainForm.LoadListView
        public List<ListViewItem> FindFilesFolders(string sStartDir, out int maxLen2, out int maxLen3, string[] iconsInfo, ref ImageList imgLst, int iLimit = int.MaxValue, string filter = "*.*", bool bSlowDrive = false, bool bHighlightEmptyFolder = false)
        {
//            Stopwatch sw = Stopwatch.StartNew();

            maxLen2 = 8;
            maxLen3 = 7;

            // disable redirection
            IntPtr wow64Value = IntPtr.Zero;
            try {
                Wow64DisableWow64FsRedirection(ref wow64Value);
            } catch ( Exception ) { ;}

            // we fill a temporary List of ListViewItem and drop it at once to the ListView lst
            List<ListViewItem> retList = new List<ListViewItem>();
            string[] strarr = new string[8] { "[..]", " ", "<PARENT>", " ", " ", " ", " ", "0" };

            // we always have a "level up", at least to "Computer"
//            strarr[2] = sw.ElapsedMilliseconds.ToString();
            retList.Add(new ListViewItem(strarr, 2));

            // vars
            WIN32_FIND_DATA dData;
            IntPtr dHandle;
            string path = @Path.Combine(@sStartDir, @"*.*");
            Win32FindData findData = new Win32FindData();             // winsxs: providing the 2 vars gains a bit IsDirEmpty(..)
            IntPtr findHandle = IntPtr.Zero;

            bool bFilter = (filter == "*.*") ? false : true;          // winsxs: RegEx and one loop for folders&files gains ca. 50ms compared to a second file loop and no RegEx
            Regex regex = FindFilesPatternToRegex.Convert(filter);

            dHandle = FindFirstFile(path, out dData);
            if ( dHandle != INVALID_HANDLE_VALUE ) {
                do {
                    if ( (dData.dwFileAttributes & FileAttributes.Directory) != 0 ) {
                        //
                        // ALL DIRECTORIES
                        //
                        if ( (dData.cFileName[0] == '.') && (dData.cFileName.Length < 3) ) {
                            continue;
                        }
                        // folder name
                        strarr[0] = @dData.cFileName;
                        // datetime
                        long ft = (((long)dData.ftCreationTime.dwHighDateTime) << 32) + dData.ftCreationTime.dwLowDateTime;
                        DateTime dtft = DateTime.FromFileTime(ft);
                        strarr[1] = dtft.ToString("dd.MM.yyyy HH:mm:ss");
                        strarr[2] = "<SUBDIR>"; //bShowFolderSize ? folderSize(@Path.Combine(@sStartDir, strarr[0])).ToString("0,0", CultureInfo.InvariantCulture) : "<SUBDIR>";
                        maxLen2 = Math.Max(maxLen2, strarr[2].Length);
                        strarr[3] = "";
                        strarr[7] = dtft.ToString("yyyyMMddHHmmssfffff"); // allows faster sort 
                        // 20150124: really nice to know, whether a folder is empty or not, will end up with a different image index --> DOWNSIDE: slows down winsxs dramatically
                        int imageNndx = 3;
                        if ( bHighlightEmptyFolder ) {
                            if ( IsDirEmptyEx(@sStartDir + "\\" + strarr[0] + "\\*", findData, findHandle) ) {           // costs @winsxs 800ms
                                imageNndx = 0;
                            }
                        }
                        // finally add folder to return list
//strarr[3] = sw.ElapsedMilliseconds.ToString();
                        retList.Add(new ListViewItem(strarr, imageNndx));
                    } else {
                        //
                        // ALL FILES
                        //
                        // RegEx is slightly faster than a 2nd loop of FindFirstFile / FindNextFile
                        if ( bFilter && !regex.IsMatch(@dData.cFileName) ) {
                            continue;
                        }
                        long ft = (((long)dData.ftLastWriteTime.dwHighDateTime) << 32) + dData.ftLastWriteTime.dwLowDateTime;
                        DateTime dtft = DateTime.FromFileTime(ft);
                        string ext = Path.GetExtension(@dData.cFileName).ToLower();
                        strarr[0] = @dData.cFileName;
                        strarr[1] = dtft.ToString("dd.MM.yyyy HH:mm:ss"); // https://msdn.microsoft.com/en-us/library/8kb3ddd4.aspx
                        strarr[7] = dtft.ToString("yyyyMMddHHmmssfffff"); // allows faster sort for filetime 
                        ulong fs = dData.nFileSizeHigh;
                        fs <<= 0x20; // aka sizeof(uint) * 8;
                        fs |= dData.nFileSizeLow;
                        // 20160206: we memorize the string length - makes later setting of column width much easier
                        strarr[2] = fs.ToString("0,0", CultureInfo.InvariantCulture);
                        maxLen2 = Math.Max(maxLen2, strarr[2].Length);
                        strarr[3] = GetMimeType(ext);
                        maxLen3 = Math.Max(maxLen3, strarr[3].Length);
                        // 20160221: get matching file icon
                        int imageindex = 1;                                   // default icon index is simple file  
                        if ( ext == ".dll" ) {                                // .dll has a homebrewn icon
                            imageindex = 10;
                        } else {
                            if ( ext == ".exe" ) {                            // .exe has its own icon stored within executable
                                if ( bSlowDrive ) {
                                    imageindex = 11;
                                } else {
//                                  supposed to give identical results as explorer: has hickups too 
                                    Icon icon = Icon.ExtractAssociatedIcon(Path.Combine(@sStartDir, @dData.cFileName));
//                                  sometimes hickups up to 13s ?   
//                                    Icon icon = GrzTools.RegisteredFileType.ExtractIconFromFile(Path.Combine(@sStartDir, @dData.cFileName), false);
//                                  20160626: test --> hickups up to 10s 
//                                    Icon icon = GrzTools.RegisteredFileType.GetIcon(Path.Combine(@sStartDir, @dData.cFileName), false);
                                    if ( icon != null ) {
                                        imgLst.Images.Add(icon);
                                        imageindex = imgLst.Images.Count - 1;
                                    } else {
                                        imageindex = 11;
                                    }
                                }
                            } else {
                                int iconPos = Array.IndexOf(iconsInfo, ext);  // all other files' extensions are stored in iconsInfo, we simply look up the matching index 
                                if ( iconPos != -1 ) {
                                    imageindex = iconPos + 12;
                                }
                            }
                        }
                        // finally add file to return list
//strarr[3] = sw.ElapsedMilliseconds.ToString();
                        retList.Add(new ListViewItem(strarr, imageindex));
                    }
                    // 20160417: stop iterating, if number is >iLimit - nobody knows how many files we could find
                    if ( retList.Count > iLimit ) {
                        break;
                    }
                } while ( FindNextFile(dHandle, out dData) );
                FindClose(dHandle);
            }

            // enable redirection
            try {
                Wow64RevertWow64FsRedirection(wow64Value);
            } catch ( Exception ) { ;}

            // return a list of ListViewItem
            return retList;
        }


        // find all (*.*) top level folders and files: FileSystemTreeView 
        public List<ListViewItem> FindFoldersFiles( string sStartDir )
        {
            // disable redirection
            IntPtr wow64Value = IntPtr.Zero;
            try {
                Wow64DisableWow64FsRedirection(ref wow64Value);
            } catch ( Exception ) { ;}

            if ( sStartDir.Length == 2 ) { // "only drive letter and :" makes sometimes trouble, sample d: on GERW206 
                sStartDir += "\\";
            }

            // start searching with * and only catch directories
            List<ListViewItem> retList = new List<ListViewItem>();    // we fill a temporary List of ListViewItem and drop it at once to the ListView lst 
            string[] strarr = new string[7] { "", "", "", "", "", "", "" };
            WIN32_FIND_DATA dData;
            IntPtr dHandle;
//            string path = @Path.Combine(@sStartDir, @"*.*");
            string path = @Path.Combine(@sStartDir, @"*");
            dHandle = FindFirstFile(path, out dData);
            if ( dHandle != INVALID_HANDLE_VALUE ) {
                do {
                    if ( (dData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory ) {
                        if ( (dData.cFileName[0] == '.') && (dData.cFileName.Length < 3) ) {
                            continue;
                        }
                        strarr[0] = @dData.cFileName;
                        strarr[1] = "";
                        strarr[2] = "";
                        strarr[3] = "";
                        strarr[4] = "";
                        strarr[5] = "";
                        strarr[6] = "";
                        ListViewItem lv = new ListViewItem(strarr, 0);
                        lv.Tag = "!" + strarr[0];       // in case of folders we add a preceding "!" to .Tag, ensures folders being on top after sorting
                        retList.Add(lv);
                    } else {
                        strarr[0] = @dData.cFileName;
                        long ft = (((long)dData.ftLastWriteTime.dwHighDateTime) << 32) + dData.ftLastWriteTime.dwLowDateTime;
                        DateTime dtft = DateTime.FromFileTime(ft);
                        strarr[1] = dtft.ToString("dd.MM.yyyy HH:mm:ss");
                        ulong fs = dData.nFileSizeHigh;
                        fs <<= 0x20;
                        fs |= dData.nFileSizeLow;
                        strarr[2] = fs.ToString("0,0", CultureInfo.InvariantCulture);
                        ListViewItem lv = new ListViewItem(strarr, 1);
                        lv.Tag = strarr[0];          // in case of files we simply add .Text to .Tag, ensures folders being on top after sorting
                        retList.Add(lv);
                    }
                } while ( FindNextFile(dHandle, out dData) );
                FindClose(dHandle);
            }

            // enable redirection
            try {
                Wow64RevertWow64FsRedirection(wow64Value);
            } catch ( Exception ) { ;}

            return retList;
        }


        // find top level folders and files
        public static List<string> FindTopFilesFolders( string sStartDir )
        {
            // disable redirection
            IntPtr wow64Value = IntPtr.Zero;
            try {
                Wow64DisableWow64FsRedirection(ref wow64Value);
            } catch ( Exception ) { ;}

            if ( sStartDir.Length == 2 ) { // "only drive letter and :" makes sometimes trouble, sample d: on GERW206 
                sStartDir += "\\";
            }

            // start searching with * and catch all names
            List<string> retList = new List<string>();    
            WIN32_FIND_DATA fData;
            IntPtr fHandle;
            string path = @Path.Combine(@sStartDir, @"*.*");
            fHandle = FindFirstFile(path, out fData);
            if ( fHandle != INVALID_HANDLE_VALUE ) {
                do {
                    string text = @fData.cFileName + (((fData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory) ? "!" : "");
                    retList.Add(text);
                } while ( FindNextFile(fHandle, out fData) );
                FindClose(fHandle);
            }

            // enable redirection
            try {
                Wow64RevertWow64FsRedirection(wow64Value);
            } catch ( Exception ) { ;}

            return retList;
        }

        // find top level folders
        public List<string> FindFolders( ref bool run, string sStartDir )
        {
            // disable redirection
            IntPtr wow64Value = IntPtr.Zero;
            try {
                Wow64DisableWow64FsRedirection(ref wow64Value);
            } catch ( Exception ) { ;}

            if ( sStartDir.Length == 2 ) { // "only drive letter and :" makes sometimes trouble, sample d: on GERW206 
                sStartDir += "\\";
            }
            List<string> retList = new List<string>();
            WIN32_FIND_DATA dData;
            IntPtr dHandle;
            string path = @Path.Combine(@sStartDir, @"*.*");
            dHandle = FindFirstFile(path, out dData);
            if ( dHandle != INVALID_HANDLE_VALUE ) {
                do {
                    if ( (dData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory ) {
                        if ( (dData.cFileName[0] == '.') && (dData.cFileName.Length < 3) ) {
                            continue;
                        }
                        // FORBIDDEN in parallel running tasks 
                        // Application.DoEvents();
                        retList.Add(@dData.cFileName);
                    }
                } while ( (FindNextFile(dHandle, out dData)) && run );
                FindClose(dHandle);
            }

            // enable redirection
            try {
                Wow64RevertWow64FsRedirection(wow64Value);
            } catch ( Exception ) { ;}

            return retList;
        }

/*      // Is a directory empty?
        // The following 4 approaches are pretty slow. 
        // The best choice 3) uses FindFirstFile/FindNextFile and could be improved in a Parallel.For
        //
        // 1) winsxs 1823ms
        [DllImport("Shlwapi.dll", EntryPoint = "PathIsDirectoryEmpty")]
        [return : MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsDirectoryEmplty( [MarshalAs(UnmanagedType.LPStr)]string directory );
        // 2) winsxs 23000ms
        public static bool IsDirectoryEmpty( string path )
        {
            try {
                return !Directory.EnumerateFileSystemEntries(path).Any();
            } catch ( Exception ) {
                return false;
            }
        }
        // 3) winsxs 1743ms
        public bool IsDirEmpty( string path )
        {
            bool empty = true;
            path += @"\*";
            WIN32_FIND_DATA findData;
            var findHandle = FindFirstFile(path, out findData);
            if ( findHandle != INVALID_HANDLE_VALUE ) {
                do {
                    if ( findData.cFileName != "." && findData.cFileName != ".." ) {
                        empty = false;
                        break;
                    }
                } while ( FindNextFile(findHandle, out findData) );
                FindClose(findHandle);
            }
            return empty;
        }
        // 4) winsxs 3800ms
        static bool IsEmpty( string path )
        {
            bool bRet = false;

            string[] dirs = null;
            string[] files = null;

            try {

                dirs = System.IO.Directory.GetDirectories(path);
                files = System.IO.Directory.GetFiles(path);

            } catch (Exception) {;}

            if ( (dirs!=null) && (files!=null) ) {
                if ( dirs.Length == 0 && files.Length == 0 )
                    bRet = true;
                else
                    bRet = false;
            }

            return bRet;
        }
*/

        // winsxs - method to find out whether a directory is empty or not: FindFirstFileExW is supposed to be faster than FindFirstFile, still no prove for this  
        public static bool IsDirEmptyEx( string path, Win32FindData winFindData, IntPtr findHandle )
        {
            findHandle = FindFirstFileExW(path, IndexInfoLevels.FindExInfoBasic, out winFindData, IndexSearchOps.FindExSearchNameMatch, IntPtr.Zero, 0);
            if ( findHandle != INVALID_HANDLE_VALUE ) {
                do {
                    if ( (winFindData.cFileName[0] != '.') || (winFindData.cFileName.Length > 2) ) {
                        FindClose(findHandle);
                        return false;
                    }
                } while ( FindNextFileW(findHandle, out winFindData) );
            }
            FindClose(findHandle);
            return true;
        }

        // winsxs - method to find out whether a directory is empty or not 
        public static bool IsDirEmpty( string path, WIN32_FIND_DATA winFindData, IntPtr findHandle )
        {
            findHandle = FindFirstFile(path, out winFindData);
            if ( findHandle != INVALID_HANDLE_VALUE ) {
                do {
                    if ( (winFindData.cFileName[0] != '.') || (winFindData.cFileName.Length > 2) ) {
                        FindClose(findHandle);
                        return false;
                    }
                } while ( FindNextFile(findHandle, out winFindData) );
            }
            FindClose(findHandle);
            return true;
        }



        //
        // extended file find: 
        //   - stoppable per 'run' 
        //   - folder must contain 'includes'
        //   - folder must not contain 'excludes'
        //   - filename must comply to a pattern
        //   - fires change event for each evaluated folder to mimik some progress
        //   - returns a list of found files
        public void FindFilesEx( List<String> lst, ref bool run, string sStartDir, string sFileMask, string[] includes = null, string[] excludes = null )
        {
            includes= includes ?? new string[] { "" };
            excludes= excludes ?? new string[] { "" };
            // start searching with * and only catch directories
            WIN32_FIND_DATA dData;
            IntPtr dHandle;
            dHandle = FindFirstFile(@Path.Combine(@sStartDir, @"*"), out dData);
            if ( dHandle != INVALID_HANDLE_VALUE ) {
                do {
                    Application.DoEvents();
                    if ( (dData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory ) {
                        if ( dData.cFileName != "." && dData.cFileName != ".." ) {
                            string subdirectory = @Path.Combine(@sStartDir, @dData.cFileName);
                            // skip all folders, which contain 'excludes'
                            bool bSkip = false;
                            if ( excludes[0].Length > 0 ) {
                                foreach ( string s in excludes ) {
                                    Application.DoEvents();
                                    if ( subdirectory.IndexOf(s) != -1 ) {
                                        bSkip = true;
                                        break;
                                    }
                                }
                                if ( bSkip ) {
                                    continue;
                                }
                            }
                            // recursion starts, when a valid directory was found
                            FindFilesEx(lst, ref run, subdirectory, sFileMask, includes, excludes);
                        }
                    }
                    // break?
                    if ( !run ) {
                        return;
                    }
                } while ( FindNextFile(dHandle, out dData) );
                FindClose(dHandle);
            }
            // foldername name must contain ...
            bool bProcessFolder = false;
            if ( includes[0].Length > 0 ) {
                foreach ( string s in includes ) {
                    Application.DoEvents();
                    if ( sStartDir.IndexOf(s) != -1 ) {
                        bProcessFolder = true;
                        break;
                    }
                }
            } else {
                bProcessFolder = true;
            }
            if ( bProcessFolder ) {
                // if no directory was found, then search again but now with the real file 'mask' and let pass only files
                WIN32_FIND_DATA fData;
                IntPtr fHandle = FindFirstFile(@Path.Combine(@sStartDir, @sFileMask), out fData);
                if ( fHandle != INVALID_HANDLE_VALUE ) {
                    do {
                        Application.DoEvents();
                        if ( (fData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory ) {
                            // filename of interest: until here it already matches with 'mask' and 'excludes'
                            string filename = @Path.Combine(@sStartDir, @fData.cFileName);
                            // filename now matches with 'mask', 'excludes' and 'includes'
                            lst.Add(filename);
                        }
                        // break?
                        if ( !run ) {
                            return;
                        }
                    } while ( FindNextFile(fHandle, out fData) ); 
                    FindClose(fHandle);
                }
            }
            // fire change event toward m_parent to allow progress indication: here we show the increasing number of visited folders 
            ChangeEvent(m_parent, new ChangeEventArgs((m_hit++).ToString(), null, null, StringComparison.Ordinal, null));
        }

        // vehicle for wow64-handling outside of the recursive method below
        public void FindFilesTextHelper( ref int run, ref bool bSkipBin, string sStartDir, string sFileMask, StringComparison sc, string[] buzzword, string[] includes = null, string[] excludes = null )
        {
            // paranoia
            includes = includes ?? new string[] { "" };
            excludes = excludes ?? new string[] { "" };  
            buzzword = buzzword ?? new string[] { "" };

            // disable wow64 redirection
            IntPtr wow64Value = IntPtr.Zero;
            try {
                Wow64DisableWow64FsRedirection(ref wow64Value);
            } catch ( Exception ) { ;}
            FindFilesInFoldersWithText(ref run, ref bSkipBin, sStartDir, sFileMask, sc, buzzword, includes, excludes);
            // enable redirection
            try {
                Wow64RevertWow64FsRedirection(wow64Value);
            } catch ( Exception ) { ;}
        }
        //
        // extended file find & text find: 
        //   - stoppable per 'run' 
        //   - file is searched for 'buzzword' array 
        //   - folder must contain 'includes'
        //   - folder must not contain 'excludes'
        //   - filename must comply to a pattern 'sFileMask'
        //   - skip binary files dependend on flag 'bSkipBin'
        //   - fires change events
        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        private void FindFilesInFoldersWithText( ref int run, ref bool bSkipBin, string sStartDir, string sFileMask, StringComparison sc, string[] buzzword, string[] includes = null, string[] excludes = null )
        {
            //
            // initially process the currently given folder
            //
            ProcessOneFolder(ref run, ref bSkipBin, sStartDir, sFileMask, sc, buzzword, includes, excludes);

            //
            // start searching with * and catch more/further directories
            //
            IntPtr dHandle;
            WIN32_FIND_DATA dData;
            dHandle = FindFirstFile(@Path.Combine( @sStartDir, @"*" ), out dData);
            if ( dHandle != INVALID_HANDLE_VALUE ) {
                do {
                    // break signalled?
                    if ( run == 0 ) {
                        return;
                    }
                    Application.DoEvents();
                    if ( (dData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory ) {

                        // skip nonsense folders
                        if ( dData.cFileName == "." || dData.cFileName == ".." ) {
                            continue;
                        } 

                        // update progress bar for each folder via event handler 
                        ChangeEvent(m_parent, new ChangeEventArgs(null, null, null, sc, null) );

                        // skip all folders, which contain 'excludes'
                        string subdirectory = @Path.Combine(@sStartDir, @dData.cFileName);
                        bool bSkip = false;
                        if ( excludes[0].Length > 0 ) {
                            foreach (string s in excludes) {
                                Application.DoEvents();
                                if ( subdirectory.IndexOf( s, sc ) != -1 ) {
                                    bSkip = true;
                                    break;
                                }
                            }  
                            if ( bSkip ) {
                                continue;
                            }
                        }

                        //
                        // start recursion with the next directory found
                        //
                        FindFilesInFoldersWithText( ref run, ref bSkipBin, subdirectory, sFileMask, sc, buzzword, includes, excludes);

                    }
                } while (FindNextFile(dHandle, out dData));
                FindClose(dHandle);
            }
        }

        // process one folder
        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        void ProcessOneFolder(ref int run, ref bool bSkipBin, string sStartDir, string sFileMask, StringComparison sc, string[] buzzword, string[] includes = null, string[] excludes = null)
        {
            // foldername name must contain ...
            bool bProcessFolder = false;
            if ( (includes.Length > 0) && (includes[0].Length > 0) ) {
                foreach (string s in includes) {
                    Application.DoEvents();
                    if ( sStartDir.IndexOf( s, sc ) != -1 ) {
                        bProcessFolder = true;
                        // folder is good to process
                        ChangeEvent(m_parent, new ChangeEventArgs(@sStartDir, null, null, sc, null) );
                        break;
                    }
                }
            } else {
                bProcessFolder = true;
            }
            if ( bProcessFolder ) {
                WIN32_FIND_DATA fData;
                string fp = @Path.Combine(@sStartDir, @sFileMask);
                IntPtr fHandle = FindFirstFile(fp, out fData);
                if ( fHandle != INVALID_HANDLE_VALUE ) {
                    do {
                        Application.DoEvents();
                        if ( (fData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory ) {

                            // filename of interest: until here it already matches with 'mask', 'excludes' and 'includes'
                            string filename = @Path.Combine(@sStartDir, @fData.cFileName);
                            string orifilename = filename;
                            string ext = "";
                            int dotPos = (@fData.cFileName).LastIndexOf('.');
                            if (dotPos != -1) {
                                ext = (@fData.cFileName).Substring(dotPos).ToLower() + ".";
                            }

                            // process buzzwords, if any
                            if ( buzzword[0].Length > 0 ) {

                                // skip "real binaries" but not our self declared non office binaries
                                bool bIsTextContent = false;
                                try {
                                    bIsTextContent = !GrzTools.FileTools.IsBinaryFile(filename);
                                } catch ( Exception ) {
                                    ChangeEvent(m_parent, new ChangeEventArgs(null, orifilename, "ERROR: accessing file - ", sc, null, false, true)); 
                                }
                                if ( bSkipBin && !bIsTextContent && (".pdf.xlsx.docx.msg.doc.xls.ppt.xlt.".IndexOf(ext) == -1) ) {
                                    continue;
                                }

                                // set text file encoding
                                Encoding encoding = Encoding.Default;

                                // search file content "by hand"
                                try {
                                    // indicator whether buzzword was found at all, needed after file processing to add a \r\n
                                    bool bSomethingFound = false;
                                    // preset "store filename once per evaluated file " handling
                                    bool bFirst = true;
                                    // have a line counter
                                    int linecnt = 1;
                                    int linecntEvt = 0;

                                    // data from file to array
                                    string[] lines;
                                    if ( !bIsTextContent ) {
                                        // special treatment for all not text files, they may contain . and ctrl-chars like hell, which are removed - after processing we have a 'text like' string array
                                        lines = ReadBinaryLines(filename);
                                    } else {
                                        // all text style data go directly from file to a string array
                                        lines = System.IO.File.ReadAllLines(filename, encoding);
                                    }

                                    // send linecount of current file
                                    string linecount = lines.Length.ToString() + "l";
                                    ChangeEvent(m_parent, new ChangeEventArgs(null, orifilename, null, sc, linecount));
                                    foreach (string line in lines) {
                                        Application.DoEvents();
                                        // while normal file processing, raise a change event by sending filename and current line number 
                                        if ( linecntEvt++ > 500 ) {
                                            ChangeEvent(m_parent, new ChangeEventArgs(null, orifilename, null, sc, linecnt.ToString()));
                                            linecntEvt = 0;
                                        }
                                        // loop current line with buzzword array
                                        foreach (string s in buzzword) {
                                            Application.DoEvents();
                                            // search line containing buzzword
                                            int findpos = line.IndexOf(s, sc);
                                            if ( findpos != -1 ) {
                                                // buzzword was found
                                                bSomethingFound = true;
                                                // add filename only once
                                                if ( bFirst ) {
                                                    bFirst = false;
                                                    // while file processing AND first positive result, raise change event and send 'filename' 
                                                    ChangeEvent(m_parent, new ChangeEventArgs(null, orifilename, null, sc, null));
                                                }
                                                // limit the length of the returned line to something useful
                                                string outputline = line;
                                                if ( line.Length > 150 ) {
                                                    outputline = line.Substring(findpos - 20, 20 + s.Length + 20);
                                                }
                                                // while file processing AND any positive result, raise event and send 'filename', 'line' and "linecnt"
                                                ChangeEvent(m_parent, new ChangeEventArgs(null, orifilename, outputline, sc, linecnt.ToString(), true ));
                                                break;
                                            }
                                        }
                                        // line counter
                                        linecnt++;
                                        // full break signaled?
                                        if ( run == 0 ) {
                                            break;
                                        }
                                        // skip this file signalled
                                        if ( run == -1 ) {
                                            run = 1;
                                            break;
                                        }
                                    }
                                    // get progressbar fully to the right when file is processed - HAS NO VISIBLE EFFECT
                                    ChangeEvent(m_parent, new ChangeEventArgs(null, orifilename, null, sc, lines.Length.ToString()));
                                    // after file processing add an empty row, if something was found
                                    if ( bSomethingFound ) {
                                        ChangeEvent(m_parent, new ChangeEventArgs((m_hit).ToString(), null, "\r\n", sc, null));
                                    }
                                } catch (Exception exc) {
                                    // file access error
                                    try {
                                        ChangeEvent(m_parent, new ChangeEventArgs(orifilename, null, "ERROR: search in file - ", sc, null, false, true));
                                    } catch {;}
                                }
                            } else {
                                // raise event in case of no buzwords: only found filenames matching to pattern will raise a signal
                                ChangeEvent(m_parent, new ChangeEventArgs(null, filename, null, sc, null));
                            }
                        }
                        // break?
                        if ( run == 0 ) {
                            return;
                        }
                    } while (FindNextFile(fHandle, out fData));
                    FindClose(fHandle);
                }
            }
            // fire change event toward m_parent to allow progress indication
            ChangeEvent(m_parent, new ChangeEventArgs(sStartDir, null, null, sc, null));
        }

        // reading from a binary: treat file like text file via replacing all impeding chars
        string[] ReadBinaryLines( string filename )
        {
            // list of strings
            List<string> retlist = new List<string>();
            // filestream
            var filestream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            // file
            var file = new StreamReader(filestream, Encoding.Default);
            // read buffer
            char[] lineOfText = new char[4096];
            // number of read chars
            int ret = 0;
            do {
                // read sort of a line, though it's rather a number of chars
                ret = file.Read(lineOfText, 0, lineOfText.Length);
                // remove all ctrl-chars
                string line = new string(lineOfText);
                for ( int i = 0; i<line.Length; i++ ) {
                    if ( char.IsControl(line[i]) ) {
                        line = line.Remove(i, 1);
                        i--;
                    }
                }
                // append modified read string
                retlist.Add(line);
                // stop reading at eof
            } while (ret > 0); // eof = -1
            // return read lines 
            return retlist.ToArray();
        }
    }

    // a self closing message box
    public class AutoMessageBox
    {
        AutoMessageBox( string text, string caption, int timeout )
        {
            var w = new Form() { Size = new Size(0, 0) };
            TaskEx.Delay(timeout)
                  .ContinueWith(( t ) => w.Close(), TaskScheduler.FromCurrentSynchronizationContext());
            MessageBox.Show(w, text, caption);
        }
        public static void Show( string text, string caption, int timeout )
        {
            new AutoMessageBox(text, caption, timeout);
        }
        public static class TaskEx
        {
            public static Task Delay( int dueTimeMs )
            {
                var tcs = new TaskCompletionSource<object>();
                var ctr = new CancellationTokenRegistration();
                var timer = new System.Threading.Timer(delegate( object self ) {
                    ctr.Dispose();
                    ((System.Threading.Timer)self).Dispose();
                    tcs.TrySetResult(null);
                });
                timer.Change(dueTimeMs, -1);
                return tcs.Task;
            }
        }
    }

    // low level mouse coordinates need 32/64 bit depending treatment
    public static class WinAPIHelper
    {
        public static Point GetPoint( IntPtr lParam )
        {
            return new Point(GetInt(lParam));
        }
        public static MouseButtons GetButtons( IntPtr wParam )
        {
            MouseButtons buttons = MouseButtons.None;
            int btns = GetInt(wParam);
            if ( (btns & MK_LBUTTON) != 0 ) buttons |= MouseButtons.Left;
            if ( (btns & MK_RBUTTON) != 0 ) buttons |= MouseButtons.Right;
            return buttons;
        }
        static int GetInt( IntPtr ptr )
        {
            return IntPtr.Size == 8 ? unchecked((int)ptr.ToInt64()) : ptr.ToInt32();
        }
        const int MK_LBUTTON = 1;
        const int MK_RBUTTON = 2;
    }

    // obtain app for extension via API
    public class FileAssociation
    {
        [DllImport("Shlwapi.dll", SetLastError = true)]
        static extern uint AssocQueryString( AssocF flags, AssocStr str, string pszAssoc, string pszExtra, [Out] StringBuilder pszOut, [In][Out] ref uint pcchOut );

        public static string Get( string doctype )
        {
            uint pcchOut = 0;
            AssocQueryString(AssocF.Verify, AssocStr.Executable, doctype, null, null, ref pcchOut);
            StringBuilder pszOut = new StringBuilder((int)pcchOut);
            AssocQueryString(AssocF.Verify, AssocStr.Executable, doctype, null, pszOut, ref pcchOut);
            string doc = pszOut.ToString();
            return doc;
        }

        public enum AssocF
        {
            Init_NoRemapCLSID = 0x1,
            Init_ByExeName = 0x2,
            Open_ByExeName = 0x2,
            Init_DefaultToStar = 0x4,
            Init_DefaultToFolder = 0x8,
            NoUserSettings = 0x10,
            NoTruncate = 0x20,
            Verify = 0x40,
            RemapRunDll = 0x80,
            NoFixUps = 0x100,
            IgnoreBaseClass = 0x200
        }
        public enum AssocStr
        {
            Command = 1,
            Executable,
            FriendlyDocName,
            FriendlyAppName,
            NoOpen,
            ShellNewValue,
            DDECommand,
            DDEIfExec,
            DDEApplication,
            DDETopic
        }
    }

    // DOES NOT ALWAYS WORK WELL !!! --> http://stackoverflow.com/questions/162331/finding-the-default-application-for-opening-a-particular-file-type-on-windows
    // obtain from Registry registered Application for a given file extension
    public class RegisteredApplication
    {
        public static bool TryGetRegisteredApplication( string extension, out string registeredApp )
        {
            string extensionId = GetClassesRootKeyDefaultValue(extension);
            if ( extensionId == null ) {
                registeredApp = null;
                return false;
            }
            string openCommand = GetClassesRootKeyDefaultValue(Path.Combine(new[] { extensionId, "shell", "open", "command" }));
            if ( openCommand == null ) {
                registeredApp = null;
                return false;
            }
            registeredApp = openCommand.Replace("%1", string.Empty).Replace("\"", string.Empty).Trim();
            // sometimes /dde is added to the app
            if ( registeredApp.IndexOf("/") != -1 ) {
                registeredApp = registeredApp.Substring(0, registeredApp.IndexOf("/")).Trim();
            }
            return true;
        }
        private static string GetClassesRootKeyDefaultValue( string keyPath )
        {
            using ( var key = Registry.ClassesRoot.OpenSubKey(keyPath) ) {
                if ( key == null ) {
                    return null;
                }
                var defaultValue = key.GetValue(null);
                if ( defaultValue == null ) {
                    return null;
                }
                return defaultValue.ToString();
            }
        }
    }

    // UNUSED: alternative class for fast file find via win32
    public class FastDirectoryEnumerator
    {
        [Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode), BestFitMapping(false)]
        internal struct Win32FindData
        {
            public FileAttributes dwFileAttributes;
            public uint ftCreationTime_dwLowDateTime;
            public uint ftCreationTime_dwHighDateTime;
            public uint ftLastAccessTime_dwLowDateTime;
            public uint ftLastAccessTime_dwHighDateTime;
            public uint ftLastWriteTime_dwLowDateTime;
            public uint ftLastWriteTime_dwHighDateTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public int dwReserved0;
            public int dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [Serializable]
        public struct FileData
        {
            public readonly FileAttributes Attributes;
            public readonly DateTime CreationTimeUtc;
            public readonly DateTime LastAccessTimeUtc;
            public readonly DateTime LastWriteTimeUtc;
            public readonly string Name;
            public readonly string Path;
            public readonly long Size;
            internal FileData( string dir, Win32FindData findData )
            {
                Attributes = findData.dwFileAttributes;
                CreationTimeUtc = ConvertDateTime(findData.ftCreationTime_dwHighDateTime, findData.ftCreationTime_dwLowDateTime);
                LastAccessTimeUtc = ConvertDateTime(findData.ftLastAccessTime_dwHighDateTime, findData.ftLastAccessTime_dwLowDateTime);
                LastWriteTimeUtc = ConvertDateTime(findData.ftLastWriteTime_dwHighDateTime, findData.ftLastWriteTime_dwLowDateTime);
                Size = CombineHighLowInts(findData.nFileSizeHigh, findData.nFileSizeLow);
                Name = findData.cFileName;
                Path = System.IO.Path.Combine(dir, findData.cFileName);
            }
            public DateTime CreationTime
            {
                get { return CreationTimeUtc.ToLocalTime(); }
            }
            public DateTime LastAccessTime
            {
                get { return LastAccessTimeUtc.ToLocalTime(); }
            }
            public DateTime LastWriteTime
            {
                get { return LastWriteTimeUtc.ToLocalTime(); }
            }
            private static long CombineHighLowInts( uint high, uint low )
            {
                long fs = high;
                fs <<= 0x20; //sizeof(uint) * 8;
                fs |= low;

                return fs; // (((long)high) << 0x20) | low;
            }
            private static DateTime ConvertDateTime( uint high, uint low )
            {
                long fileTime = CombineHighLowInts(high, low);
                return DateTime.FromFileTimeUtc(fileTime);
            }
        }

        public FastDirectoryEnumerator()
        {
            // Enders - war mal im Original: PermissionState.None
            new FileIOPermission(PermissionState.Unrestricted) { AllFiles = FileIOPermissionAccess.PathDiscovery }.Demand();
        }

        // counts/returns all directories underneath path
        public IEnumerable<string> GetAllDirectories( string path )
        {
            foreach ( var dir in GetDirectories(path, "*") ) {
                if ( dir == ".." || dir == "." )
                    continue;
                yield return dir;
                foreach ( var subDir in GetAllDirectories(Path.Combine(path, dir)) )
                    yield return subDir;
            }
        }

        public IEnumerable<string> GetDirectories( string path, string searchPattern )
        {
            Win32FindData winFindData;
            var findHandle = FindFirstFileExW(Path.Combine(path, searchPattern), IndexInfoLevels.FindExInfoBasic, out winFindData, IndexSearchOps.FindExSearchLimitToDirectories, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);
            if ( findHandle == InvalidHandleValue )
                yield break;
            try {
                do {
                    if ( !m_bRun ) {
                        FindClose(findHandle);
                        yield break;
                    }
                    if ( winFindData.cFileName == "." || winFindData.cFileName == ".." )
                        continue;
                    // FindExSearchLimitToDirectories is advisory only. If the file system does not support directory filtering, this flag is silently ignored.
                    if ( (winFindData.dwFileAttributes & FileAttributes.Directory) != 0 ) {
                        yield return Path.Combine(path, winFindData.cFileName);
                    }
                } while ( FindNextFileW(findHandle, out winFindData) );
            } finally {
                FindClose(findHandle);
            }
        }

        //
        // counts/returns all directories underneath path = split[0], which do not match to strings in split[1] ... split[N]
        //
        // tricky: I didn't find another way to start this in a Task with multiple parameters AND a return value
        // therefore I put everything into one single string sarr ==> "startpath;exclude1;exclude2;...;excludeN"
        public bool m_bRun = true;
        public IEnumerable<string> GetAllDirectoriesEx( string sarr )
        {
            string[] split = sarr.Split(';');
            foreach ( var dir in GetDirectories(split[0], "*") ) {
                if ( !m_bRun ) {
                    yield break;
                }
                if ( dir == ".." || dir == "." ) {
                    continue;
                }
                if ( split.Length > 1 ) {
                    bool skip = false;
                    for ( int i=1; i<split.Length; i++ ) {
                        if ( !m_bRun ) {
                            yield break;
                        }
                        if ( dir.Contains(split[i]) ) {
                            skip = true;
                            break;
                        }
                    }
                    if ( skip ) {
                        continue;
                    }
                }
                yield return dir;
                string newsarr = Path.Combine(split[0], dir);
                if ( split.Length > 1 ) {
                    for ( int i=1; i<split.Length; i++ ) {
                        newsarr += ";" + split[i];
                    }
                }
                foreach ( var subDir in GetAllDirectoriesEx(newsarr) ) {
                    if ( !m_bRun ) {
                        yield break;
                    }
                    yield return subDir;
                }
            }
        }
        // public static IEnumerable<FileData> GetFiles( string path, string searchPattern )
        public static IEnumerable<FileInfo> GetFiles( string path, string searchPattern )
        {
            Win32FindData winFindData;
            string searchPath = Path.Combine(path, searchPattern);
            if (path.Length > 247) {
                yield break;
            }
            var findHandle = FindFirstFileExW(searchPath, IndexInfoLevels.FindExInfoBasic, out winFindData, IndexSearchOps.FindExSearchNameMatch, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);
            if ( findHandle == InvalidHandleValue )
                yield break;
            try {
                do {
                    if ( winFindData.cFileName == "." || winFindData.cFileName == ".." )
                        continue;
                    if ( (winFindData.dwFileAttributes & FileAttributes.Directory) == 0 ) {
                        string fullPath = Path.Combine(path, winFindData.cFileName);
                        if (path.Length > 247) {
                            continue;
                        } else {
                            if (fullPath.Length > 259) {
                                continue;
                            } else {
                                yield return new FileInfo(fullPath);
                            }
                        }
                    }
                } while ( FindNextFileW(findHandle, out winFindData) );
            } finally {
                FindClose(findHandle);
            }
        }
        private enum IndexInfoLevels
        {
            FindExInfoStandard,
            FindExInfoBasic,
            FindExInfoMaxInfoLevel
        };
        private enum IndexSearchOps
        {
            FindExSearchNameMatch,
            FindExSearchLimitToDirectories,
            FindExSearchLimitToDevices
        };
        private const int FIND_FIRST_EX_LARGE_FETCH = 0x02;
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true), SuppressUnmanagedCodeSecurity]
        private static extern IntPtr FindFirstFileExW( string lpFileName, IndexInfoLevels infoLevels, out Win32FindData lpFindFileData, IndexSearchOps fSearchOp, IntPtr lpSearchFilter, int dwAdditionalFlag );
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true), SuppressUnmanagedCodeSecurity]
        private static extern bool FindNextFileW( IntPtr hndFindFile, out Win32FindData lpFindFileData );
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FindClose( IntPtr hFindFile );
        private static readonly IntPtr InvalidHandleValue = new IntPtr(-1);
    }

    // useful to tell a control to either paint it or not
    public class DrawingControl
    {
        [DllImport("user32.dll")]
        private static extern int SendMessage( IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam );
        private const int WM_SETREDRAW = 11;
        public static void SuspendDrawing( Control parent )
        {
            SendMessage(parent.Handle, WM_SETREDRAW, false, 0);
        }
        public static void ResumeDrawing( Control parent )
        {
            SendMessage(parent.Handle, WM_SETREDRAW, true, 0);
            parent.Refresh();
        }
    }

    // UNUSED: enumwindows, enumchildwindows, getwindowtext, setwindowtext from win32
    public class FindWindow
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText( IntPtr hWnd, StringBuilder strText, int maxCount );
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength( IntPtr hWnd );

        private delegate bool EnumWindowsProc( IntPtr hWnd, IntPtr lParam );
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows( EnumWindowsProc enumProc, IntPtr lParam );

        public delegate bool EnumChildProc( IntPtr hwnd, IntPtr lParam );
        [DllImport("user32.Dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows( IntPtr parentHandle, EnumChildProc callback, IntPtr lParam );

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage( IntPtr hWnd, uint Msg, IntPtr wParam, string lParam );
        const uint WM_SETTEXT = 0x000C;

        // set window text of a given window
        public static void SetWindowText( IntPtr hWnd, string text )
        {
            SendMessage(hWnd, WM_SETTEXT, IntPtr.Zero, text);
        }
        // get window text of a given handle
        public static string GetWindowText( IntPtr hWnd )
        {
            int size = GetWindowTextLength(hWnd);
            if ( size++ > 0 ) {
                var builder = new StringBuilder(size);
                GetWindowText(hWnd, builder, builder.Capacity);
                return builder.ToString();
            }
            return String.Empty;
        }
        // find first top level window containing titleText an return its window handle 
        public static IntPtr FindWindowWithText( string titleText )
        {
            IntPtr window = IntPtr.Zero;
            List<IntPtr> windows = new List<IntPtr>();
            EnumWindows(delegate( IntPtr wnd, IntPtr param ) {

                if ( GetWindowText(wnd).Contains(titleText) ) {
                    window = wnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            return window;
        }
        // find first window of a given parentHandle containing titleText an return its window handle 
        public static IntPtr FindChildWindowWithText( IntPtr parentHandle, string titleText )
        {
            var result = IntPtr.Zero;
            if ( parentHandle == IntPtr.Zero ) {
                parentHandle = Process.GetCurrentProcess().MainWindowHandle;
            }
            EnumChildWindows(parentHandle, ( hwnd, param ) => {
                if ( GetWindowText(hwnd).Contains(titleText) ) {
                    result = hwnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            return result;
        }
    }

    public class ShellApi
    {
        public delegate Int32 BrowseCallbackProc( IntPtr hwnd, UInt32 uMsg, Int32 lParam, Int32 lpData );

        // Contains parameters for the SHBrowseForFolder function and receives information about the folder selected 
        // by the user.
        [StructLayout(LayoutKind.Sequential)]
        public struct BROWSEINFO
        {
            public IntPtr hwndOwner;				// Handle to the owner window for the dialog box.

            public IntPtr pidlRoot;					// Pointer to an item identifier list (PIDL) specifying the 
            // location of the root folder from which to start browsing.

            [MarshalAs(UnmanagedType.LPStr)]		// Address of a buffer to receive the display name of the 
            public String pszDisplayName;			// folder selected by the user.

            [MarshalAs(UnmanagedType.LPStr)]		// Address of a null-terminated string that is displayed 
            public String lpszTitle;				// above the tree view control in the dialog box.

            public UInt32 ulFlags;					// Flags specifying the options for the dialog box. 

            [MarshalAs(UnmanagedType.FunctionPtr)]	// Address of an application-defined function that the 
            public BrowseCallbackProc lpfn;			// dialog box calls when an event occurs.

            public Int32 lParam;					// Application-defined value that the dialog box passes to 
            // the callback function

            public Int32 iImage;					// Variable to receive the image associated with the selected folder.
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct STRRET
        {
            [FieldOffset(0)]
            public UInt32 uType;						// One of the STRRET_* values

            [FieldOffset(4)]
            public IntPtr pOleStr;						// must be freed by caller of GetDisplayNameOf

            [FieldOffset(4)]
            public IntPtr pStr;							// NOT USED

            [FieldOffset(4)]
            public UInt32 uOffset;						// Offset into SHITEMID

            [FieldOffset(4)]
            public IntPtr cStr;							// Buffer to fill in (ANSI)
        }

        // Contains information used by ShellExecuteEx
        [StructLayout(LayoutKind.Sequential)]
        public struct SHELLEXECUTEINFO
        {
            public UInt32 cbSize;					// Size of the structure, in bytes. 
            public UInt32 fMask;					// Array of flags that indicate the content and validity of the 
            // other structure members.
            public IntPtr hwnd;						// Window handle to any message boxes that the system might produce
            // while executing this function. 
            [MarshalAs(UnmanagedType.LPWStr)]
            public String lpVerb;					// String, referred to as a verb, that specifies the action to 
            // be performed. 
            [MarshalAs(UnmanagedType.LPWStr)]
            public String lpFile;					// Address of a null-terminated string that specifies the name of 
            // the file or object on which ShellExecuteEx will perform the 
            // action specified by the lpVerb parameter.
            [MarshalAs(UnmanagedType.LPWStr)]
            public String lpParameters;				// Address of a null-terminated string that contains the 
            // application parameters.
            [MarshalAs(UnmanagedType.LPWStr)]
            public String lpDirectory;				// Address of a null-terminated string that specifies the name of 
            // the working directory. 
            public Int32 nShow;						// Flags that specify how an application is to be shown when it 
            // is opened.
            public IntPtr hInstApp;					// If the function succeeds, it sets this member to a value 
            // greater than 32.
            public IntPtr lpIDList;					// Address of an ITEMIDLIST structure to contain an item identifier
            // list uniquely identifying the file to execute.
            [MarshalAs(UnmanagedType.LPWStr)]
            public String lpClass;					// Address of a null-terminated string that specifies the name of 
            // a file class or a globally unique identifier (GUID). 
            public IntPtr hkeyClass;				// Handle to the registry key for the file class.
            public UInt32 dwHotKey;					// Hot key to associate with the application.
            public IntPtr hIconMonitor;				// Handle to the icon for the file class. OR Handle to the monitor 
            // upon which the document is to be displayed. 
            public IntPtr hProcess;					// Handle to the newly started application.
        }

        #region SHFILEOPSTRUCT

        private enum MachineType
        {
            unknown,
            win32,
            win64
        }

        // Contains information that the SHFileOperation function uses to perform file operations.

        // 2007/08/29 Wolfram Bernhadt
        // The "Pack=2" is crucial, since the default Pack is 4, which leads to a wrongly marshalled
        // structure.

        // 2010/01/05 Wolfram Bernhardt
        // "Pack=2" is crucial... that still hold for 32bit-versions of Windows.
        // For 64bit-Versions "Pack=8" ist the correct value. I am not sure why this is, but it works that way.
        // To make this library work, I don't see a way around using two prepared structures with
        // different Pack-Values
        public struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;                     // Window handle to the dialog box to display information about the 
            // status of the file operation. 
            public UInt32 wFunc;                    // Value that indicates which operation to perform.
            public IntPtr pFrom;                    // Address of a buffer to specify one or more source file names. 
            // These names must be fully qualified paths. Standard Microsoft® 
            // MS-DOS® wild cards, such as "*", are permitted in the file-name 
            // position. Although this member is declared as a null-terminated 
            // string, it is used as a buffer to hold multiple file names. Each 
            // file name must be terminated by a single NULL character. An	
            // additional NULL character must be appended to the end of the 
            // final name to indicate the end of pFrom. 
            public IntPtr pTo;                      // Address of a buffer to contain the name of the destination file or 
            // directory. This parameter must be set to NULL if it is not used.
            // Like pFrom, the pTo member is also a double-null terminated 
            // string and is handled in much the same way. 
            public UInt16 fFlags;                   // Flags that control the file operation. 
            public Int32 fAnyOperationsAborted;     // Value that receives TRUE if the user aborted any file operations
            // before they were completed, or FALSE otherwise. 
            public IntPtr hNameMappings;            // A handle to a name mapping object containing the old and new 
            // names of the renamed files. This member is used only if the 
            // fFlags member includes the FOF_WANTMAPPINGHANDLE flag.
            [MarshalAs(UnmanagedType.LPWStr)]
            public String lpszProgressTitle;        // Address of a string to use as the title of a progress dialog box.
            // This member is used only if fFlags includes the 
            // FOF_SIMPLEPROGRESS flag.


            public void CopyFrom( SHFILEOPSTRUCT32 initializer )
            {
                this.hwnd = initializer.hwnd;
                this.wFunc = initializer.wFunc;
                this.pFrom = initializer.pFrom;
                this.pTo = initializer.pTo;
                this.fFlags = initializer.fFlags;
                this.fAnyOperationsAborted = initializer.fAnyOperationsAborted;
                this.hNameMappings = initializer.hNameMappings;
                this.lpszProgressTitle = initializer.lpszProgressTitle;
            }

            public void CopyFrom( SHFILEOPSTRUCT64 initializer )
            {
                this.hwnd = initializer.hwnd;
                this.wFunc = initializer.wFunc;
                this.pFrom = initializer.pFrom;
                this.pTo = initializer.pTo;
                this.fFlags = initializer.fFlags;
                this.fAnyOperationsAborted = initializer.fAnyOperationsAborted;
                this.hNameMappings = initializer.hNameMappings;
                this.lpszProgressTitle = initializer.lpszProgressTitle;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 2)]
        public struct SHFILEOPSTRUCT32
        {
            public IntPtr hwnd;                     // Window handle to the dialog box to display information about the 
            // status of the file operation. 
            public UInt32 wFunc;                    // Value that indicates which operation to perform.
            public IntPtr pFrom;                    // Address of a buffer to specify one or more source file names. 
            // These names must be fully qualified paths. Standard Microsoft® 
            // MS-DOS® wild cards, such as "*", are permitted in the file-name 
            // position. Although this member is declared as a null-terminated 
            // string, it is used as a buffer to hold multiple file names. Each 
            // file name must be terminated by a single NULL character. An	
            // additional NULL character must be appended to the end of the 
            // final name to indicate the end of pFrom. 
            public IntPtr pTo;                      // Address of a buffer to contain the name of the destination file or 
            // directory. This parameter must be set to NULL if it is not used.
            // Like pFrom, the pTo member is also a double-null terminated 
            // string and is handled in much the same way. 
            public UInt16 fFlags;                   // Flags that control the file operation. 
            public Int32 fAnyOperationsAborted;     // Value that receives TRUE if the user aborted any file operations
            // before they were completed, or FALSE otherwise. 
            public IntPtr hNameMappings;            // A handle to a name mapping object containing the old and new 
            // names of the renamed files. This member is used only if the 
            // fFlags member includes the FOF_WANTMAPPINGHANDLE flag.
            [MarshalAs(UnmanagedType.LPWStr)]
            public String lpszProgressTitle;        // Address of a string to use as the title of a progress dialog box.
            // This member is used only if fFlags includes the 
            // FOF_SIMPLEPROGRESS flag.

            public SHFILEOPSTRUCT32( SHFILEOPSTRUCT initializer )
            {
                this.hwnd = initializer.hwnd;
                this.wFunc = initializer.wFunc;
                this.pFrom = initializer.pFrom;
                this.pTo = initializer.pTo;
                this.fFlags = initializer.fFlags;
                this.fAnyOperationsAborted = initializer.fAnyOperationsAborted;
                this.hNameMappings = initializer.hNameMappings;
                this.lpszProgressTitle = initializer.lpszProgressTitle;
            }
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
        public struct SHFILEOPSTRUCT64
        {
            public IntPtr hwnd;                     // Window handle to the dialog box to display information about the 
            // status of the file operation. 
            public UInt32 wFunc;                    // Value that indicates which operation to perform.
            public IntPtr pFrom;                    // Address of a buffer to specify one or more source file names. 
            // These names must be fully qualified paths. Standard Microsoft® 
            // MS-DOS® wild cards, such as "*", are permitted in the file-name 
            // position. Although this member is declared as a null-terminated 
            // string, it is used as a buffer to hold multiple file names. Each 
            // file name must be terminated by a single NULL character. An	
            // additional NULL character must be appended to the end of the 
            // final name to indicate the end of pFrom. 
            public IntPtr pTo;                      // Address of a buffer to contain the name of the destination file or 
            // directory. This parameter must be set to NULL if it is not used.
            // Like pFrom, the pTo member is also a double-null terminated 
            // string and is handled in much the same way. 
            public UInt16 fFlags;                   // Flags that control the file operation. 
            public Int32 fAnyOperationsAborted;     // Value that receives TRUE if the user aborted any file operations
            // before they were completed, or FALSE otherwise. 
            public IntPtr hNameMappings;            // A handle to a name mapping object containing the old and new 
            // names of the renamed files. This member is used only if the 
            // fFlags member includes the FOF_WANTMAPPINGHANDLE flag.
            [MarshalAs(UnmanagedType.LPWStr)]
            public String lpszProgressTitle;        // Address of a string to use as the title of a progress dialog box.
            // This member is used only if fFlags includes the 
            // FOF_SIMPLEPROGRESS flag.

            public SHFILEOPSTRUCT64( SHFILEOPSTRUCT initializer )
            {
                this.hwnd = initializer.hwnd;
                this.wFunc = initializer.wFunc;
                this.pFrom = initializer.pFrom;
                this.pTo = initializer.pTo;
                this.fFlags = initializer.fFlags;
                this.fAnyOperationsAborted = initializer.fAnyOperationsAborted;
                this.hNameMappings = initializer.hNameMappings;
                this.lpszProgressTitle = initializer.lpszProgressTitle;
            }
        }


        // Copies, moves, renames, or deletes a file system object.

        // To deal with the described 32 vs 64 bit - issues we need two methods here.
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHFileOperation")]
        public static extern Int32 SHFileOperation32(
            ref SHFILEOPSTRUCT32 lpFileOp );

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHFileOperation")]
        public static extern Int32 SHFileOperation64(
            ref SHFILEOPSTRUCT64 lpFileOp );         // Address of an SHFILEOPSTRUCT structure that contains information 
        // this function needs to carry out the specified operation. This 
        // parameter must contain a valid value that is not NULL. You are 
        // responsibile for validating the value. If you do not validate it, 
        // you will experience unexpected results. 


        public static Int32 SHFileOperation( ref SHFILEOPSTRUCT lpFileOp )
        {
            MachineType mt = GetMachineType();
            Int32 result;

            switch ( mt ) {
                case MachineType.win32:
                    SHFILEOPSTRUCT32 fos32 = new SHFILEOPSTRUCT32(lpFileOp);
                    result = SHFileOperation32(ref fos32);
                    lpFileOp.CopyFrom(fos32);
                    break;

                case MachineType.win64:
                    SHFILEOPSTRUCT64 fos64 = new SHFILEOPSTRUCT64(lpFileOp);
                    result = SHFileOperation64(ref fos64);
                    lpFileOp.CopyFrom(fos64);
                    break;

                default:
                    throw new ArgumentException("Hell, what kind of computer are you using? It's not 32 and not 64 bit");
            }

            return result;
        }

        private static MachineType GetMachineType()
        {
            // To determine which kind of machine we are running on (32 vs. 64 bit) I just check the size of an IntPtr
            // http://blogs.msdn.com/kstanton/archive/2004/04/20/116923.aspx

            switch ( IntPtr.Size ) {
                case 4:
                    return MachineType.win32;
                case 8:
                    return MachineType.win64;
                default:
                    return MachineType.unknown;
            }
        }

        #endregion SHFILEOPSTRUCT


        // Newly added on 2007/98/29 by Wolfram Bernhardt to make hNameMappings work
        // Contains information about hNameMappings
        [StructLayout(LayoutKind.Sequential)]
        public struct SHNAMEMAPPINGINDEXSTRUCT
        {
            public UInt32 counter;                    // The number of NameMapping that have been made during a 
            // copy-operation.
            public IntPtr firstMappingStruct;                    // Pointer to the first NameMappingStruct
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SHNAMEMAPPINGSTRUCT
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public String pszOldPath;

            [MarshalAs(UnmanagedType.LPWStr)]
            public String pszNewPath;

            public Int32 cchOldPath;
            public Int32 cchNewPath;
        }

        // Retrieves a pointer to the Shell's IMalloc interface.
        [DllImport("shell32.dll")]
        public static extern Int32 SHGetMalloc(
            out IntPtr hObject );	// Address of a pointer that receives the Shell's IMalloc interface pointer. 

        // Retrieves the path of a folder as an PIDL.
        [DllImport("shell32.dll")]
        public static extern Int32 SHGetFolderLocation(
            IntPtr hwndOwner,		// Handle to the owner window.
            Int32 nFolder,			// A CSIDL value that identifies the folder to be located.
            IntPtr hToken,			// Token that can be used to represent a particular user.
            UInt32 dwReserved,		// Reserved.
            out IntPtr ppidl );		// Address of a pointer to an item identifier list structure 
        // specifying the folder's location relative to the root of the namespace 
        // (the desktop). 

        // Converts an item identifier list to a file system path. 
        [DllImport("shell32.dll")]
        public static extern Int32 SHGetPathFromIDList(
            IntPtr pidl,			// Address of an item identifier list that specifies a file or directory location 
            // relative to the root of the namespace (the desktop). 
            StringBuilder pszPath );	// Address of a buffer to receive the file system path.


        // Takes the CSIDL of a folder and returns the pathname.
        [DllImport("shell32.dll")]
        public static extern Int32 SHGetFolderPath(
            IntPtr hwndOwner,			// Handle to an owner window.
            Int32 nFolder,				// A CSIDL value that identifies the folder whose path is to be retrieved.
            IntPtr hToken,				// An access token that can be used to represent a particular user.
            UInt32 dwFlags,				// Flags to specify which path is to be returned. It is used for cases where 
            // the folder associated with a CSIDL may be moved or renamed by the user. 
            StringBuilder pszPath );		// Pointer to a null-terminated string which will receive the path.

        // Translates a Shell namespace object's display name into an item identifier list and returns the attributes 
        // of the object. This function is the preferred method to convert a string to a pointer to an item 
        // identifier list (PIDL). 
        [DllImport("shell32.dll")]
        public static extern Int32 SHParseDisplayName(
            [MarshalAs(UnmanagedType.LPWStr)]
			String pszName,				// Pointer to a zero-terminated wide string that contains the display name 
            // to parse. 
            IntPtr pbc,					// Optional bind context that controls the parsing operation. This parameter 
            // is normally set to NULL.
            out IntPtr ppidl,			// Address of a pointer to a variable of type ITEMIDLIST that receives the item
            // identifier list for the object.
            UInt32 sfgaoIn,				// ULONG value that specifies the attributes to query.
            out UInt32 psfgaoOut );		// Pointer to a ULONG. On return, those attributes that are true for the 
        // object and were requested in sfgaoIn will be set. 


        // Retrieves the IShellFolder interface for the desktop folder, which is the root of the Shell's namespace. 
        [DllImport("shell32.dll")]
        public static extern Int32 SHGetDesktopFolder(
            out IntPtr ppshf );			// Address that receives an IShellFolder interface pointer for the 
        // desktop folder.

        // This function takes the fully-qualified pointer to an item identifier list (PIDL) of a namespace object, 
        // and returns a specified interface pointer on the parent object.
        [DllImport("shell32.dll")]
        public static extern Int32 SHBindToParent(
            IntPtr pidl,			// The item's PIDL. 
            [MarshalAs(UnmanagedType.LPStruct)]
			Guid riid,				// The REFIID of one of the interfaces exposed by the item's parent object. 
            out IntPtr ppv,			// A pointer to the interface specified by riid. You must release the object when 
            // you are finished. 
            ref IntPtr ppidlLast );	// The item's PIDL relative to the parent folder. This PIDL can be used with many
        // of the methods supported by the parent folder's interfaces. If you set ppidlLast 
        // to NULL, the PIDL will not be returned. 

        // Accepts a STRRET structure returned by IShellFolder::GetDisplayNameOf that contains or points to a 
        // string, and then returns that string as a BSTR.
        [DllImport("shlwapi.dll")]
        public static extern Int32 StrRetToBSTR(
            ref STRRET pstr,		// Pointer to a STRRET structure.
            IntPtr pidl,			// Pointer to an ITEMIDLIST uniquely identifying a file object or subfolder relative
            // to the parent folder.
            [MarshalAs(UnmanagedType.BStr)]
			out String pbstr );		// Pointer to a variable of type BSTR that contains the converted string.

        // Takes a STRRET structure returned by IShellFolder::GetDisplayNameOf, converts it to a string, and 
        // places the result in a buffer. 
        [DllImport("shlwapi.dll")]
        public static extern Int32 StrRetToBuf(
            ref STRRET pstr,		// Pointer to the STRRET structure. When the function returns, this pointer will no
            // longer be valid.
            IntPtr pidl,			// Pointer to the item's ITEMIDLIST structure.
            StringBuilder pszBuf,	// Buffer to hold the display name. It will be returned as a null-terminated
            // string. If cchBuf is too small, the name will be truncated to fit. 
            UInt32 cchBuf );			// Size of pszBuf, in characters. If cchBuf is too small, the string will be 
        // truncated to fit. 



        // Displays a dialog box that enables the user to select a Shell folder. 
        [DllImport("shell32.dll")]
        public static extern IntPtr SHBrowseForFolder(
            ref BROWSEINFO lbpi );	// Pointer to a BROWSEINFO structure that contains information used to display 
        // the dialog box. 

        // Performs an operation on a specified file.
        [DllImport("shell32.dll")]
        public static extern IntPtr ShellExecute(
            IntPtr hwnd,			// Handle to a parent window.
            [MarshalAs(UnmanagedType.LPStr)]
			String lpOperation,		// Pointer to a null-terminated string, referred to in this case as a verb, 
            // that specifies the action to be performed.
            [MarshalAs(UnmanagedType.LPStr)]
			String lpFile,			// Pointer to a null-terminated string that specifies the file or object on which 
            // to execute the specified verb.
            [MarshalAs(UnmanagedType.LPStr)]
			String lpParameters,	// If the lpFile parameter specifies an executable file, lpParameters is a pointer 
            // to a null-terminated string that specifies the parameters to be passed 
            // to the application.
            [MarshalAs(UnmanagedType.LPStr)]
			String lpDirectory,		// Pointer to a null-terminated string that specifies the default directory. 
            Int32 nShowCmd );		// Flags that specify how an application is to be displayed when it is opened.

        // Performs an action on a file. 
        [DllImport("shell32.dll")]
        public static extern Int32 ShellExecuteEx(
            ref SHELLEXECUTEINFO lpExecInfo );	// Address of a SHELLEXECUTEINFO structure that contains and receives 
        // information about the application being executed. 


        //wob
        [DllImport("shell32.dll")]
        public static extern Int32 SHFreeNameMappings(
            IntPtr hNameMappings );              // Release hNameMappings. This should always be called when
        // NameMapping were requested


        // Notifies the system of an event that an application has performed. An application should use this function
        // if it performs an action that may affect the Shell. 
        [DllImport("shell32.dll")]
        public static extern void SHChangeNotify(
            UInt32 wEventId,				// Describes the event that has occurred. the 
            // ShellChangeNotificationEvents enum contains a list of options.
            UInt32 uFlags,					// Flags that indicate the meaning of the dwItem1 and dwItem2 parameters.
            IntPtr dwItem1,					// First event-dependent value. 
            IntPtr dwItem2 );				// Second event-dependent value. 

        // Adds a document to the Shell's list of recently used documents or clears all documents from the list. 
        [DllImport("shell32.dll")]
        public static extern void SHAddToRecentDocs(
            UInt32 uFlags,					// Flag that indicates the meaning of the pv parameter.
            IntPtr pv );						// A pointer to either a null-terminated string with the path and file name 
        // of the document, or a PIDL that identifies the document's file object. 
        // Set this parameter to NULL to clear all documents from the list. 
        [DllImport("shell32.dll")]
        public static extern void SHAddToRecentDocs(
            UInt32 uFlags,
            [MarshalAs(UnmanagedType.LPWStr)]
			String pv );

        // Executes a command on a printer object. 
        [DllImport("shell32.dll")]
        public static extern Int32 SHInvokePrinterCommand(
            IntPtr hwnd,						// Handle of the window that will be used as the parent of any windows 
            // or dialog boxes that are created during the operation.
            UInt32 uAction,						// A value that determines the type of printer operation that will be 
            // performed.
            [MarshalAs(UnmanagedType.LPWStr)]
			String lpBuf1,						// Address of a null_terminated string that contains additional 
            // information for the printer command. 
            [MarshalAs(UnmanagedType.LPWStr)]	
			String lpBuf2,						// Address of a null-terminated string that contains additional
            // information for the printer command. 
            Int32 fModal );						//  value that determines whether SHInvokePrinterCommand should return
        // after initializing the command or wait until the command is completed.


        public static Int16 GetHResultCode( Int32 hr )
        {
            hr = hr & 0x0000ffff;
            return (Int16)hr;
        }


        public enum CSIDL
        {
            CSIDL_FLAG_CREATE				= (0x8000),	// Version 5.0. Combine this CSIDL with any of the following 
            //CSIDLs to force the creation of the associated folder. 
            CSIDL_ADMINTOOLS				= (0x0030),	// Version 5.0. The file system directory that is used to store 
            // administrative tools for an individual user. The Microsoft 
            // Management Console (MMC) will save customized consoles to 
            // this directory, and it will roam with the user.
            CSIDL_ALTSTARTUP				= (0x001d),	// The file system directory that corresponds to the user's 
            // nonlocalized Startup program group.
            CSIDL_APPDATA					= (0x001a),	// Version 4.71. The file system directory that serves as a 
            // common repository for application-specific data. A typical
            // path is C:\Documents and Settings\username\Application Data. 
            // This CSIDL is supported by the redistributable Shfolder.dll 
            // for systems that do not have the Microsoft® Internet 
            // Explorer 4.0 integrated Shell installed.
            CSIDL_BITBUCKET					= (0x000a),	// The virtual folder containing the objects in the user's 
            // Recycle Bin.
            CSIDL_CDBURN_AREA				= (0x003b),	// Version 6.0. The file system directory acting as a staging
            // area for files waiting to be written to CD. A typical path 
            // is C:\Documents and Settings\username\Local Settings\
            // Application Data\Microsoft\CD Burning.
            CSIDL_COMMON_ADMINTOOLS			= (0x002f),	// Version 5.0. The file system directory containing 
            // administrative tools for all users of the computer.
            CSIDL_COMMON_ALTSTARTUP			= (0x001e), // The file system directory that corresponds to the 
            // nonlocalized Startup program group for all users. Valid only 
            // for Microsoft Windows NT® systems.
            CSIDL_COMMON_APPDATA			= (0x0023), // Version 5.0. The file system directory containing application 
            // data for all users. A typical path is C:\Documents and 
            // Settings\All Users\Application Data.
            CSIDL_COMMON_DESKTOPDIRECTORY	= (0x0019), // The file system directory that contains files and folders 
            // that appear on the desktop for all users. A typical path is 
            // C:\Documents and Settings\All Users\Desktop. Valid only for 
            // Windows NT systems.
            CSIDL_COMMON_DOCUMENTS			= (0x002e), // The file system directory that contains documents that are 
            // common to all users. A typical paths is C:\Documents and 
            // Settings\All Users\Documents. Valid for Windows NT systems 
            // and Microsoft Windows® 95 and Windows 98 systems with 
            // Shfolder.dll installed.
            CSIDL_COMMON_FAVORITES			= (0x001f), // The file system directory that serves as a common repository
            // for favorite items common to all users. Valid only for 
            // Windows NT systems.
            CSIDL_COMMON_MUSIC				= (0x0035), // Version 6.0. The file system directory that serves as a 
            // repository for music files common to all users. A typical 
            // path is C:\Documents and Settings\All Users\Documents\
            // My Music.
            CSIDL_COMMON_PICTURES			= (0x0036), // Version 6.0. The file system directory that serves as a 
            // repository for image files common to all users. A typical 
            // path is C:\Documents and Settings\All Users\Documents\
            // My Pictures.
            CSIDL_COMMON_PROGRAMS			= (0x0017), // The file system directory that contains the directories for 
            // the common program groups that appear on the Start menu for
            // all users. A typical path is C:\Documents and Settings\
            // All Users\Start Menu\Programs. Valid only for Windows NT 
            // systems.
            CSIDL_COMMON_STARTMENU			= (0x0016), // The file system directory that contains the programs and 
            // folders that appear on the Start menu for all users. A 
            // typical path is C:\Documents and Settings\All Users\
            // Start Menu. Valid only for Windows NT systems.
            CSIDL_COMMON_STARTUP			= (0x0018), // The file system directory that contains the programs that 
            // appear in the Startup folder for all users. A typical path 
            // is C:\Documents and Settings\All Users\Start Menu\Programs\
            // Startup. Valid only for Windows NT systems.
            CSIDL_COMMON_TEMPLATES			= (0x002d), // The file system directory that contains the templates that 
            // are available to all users. A typical path is C:\Documents 
            // and Settings\All Users\Templates. Valid only for Windows 
            // NT systems.
            CSIDL_COMMON_VIDEO				= (0x0037), // Version 6.0. The file system directory that serves as a 
            // repository for video files common to all users. A typical 
            // path is C:\Documents and Settings\All Users\Documents\
            // My Videos.
            CSIDL_CONTROLS					= (0x0003), // The virtual folder containing icons for the Control Panel 
            // applications.
            CSIDL_COOKIES					= (0x0021), // The file system directory that serves as a common repository 
            // for Internet cookies. A typical path is C:\Documents and 
            // Settings\username\Cookies.
            CSIDL_DESKTOP					= (0x0000), // The virtual folder representing the Windows desktop, the root 
            // of the namespace.
            CSIDL_DESKTOPDIRECTORY			= (0x0010), // The file system directory used to physically store file 
            // objects on the desktop (not to be confused with the desktop 
            // folder itself). A typical path is C:\Documents and 
            // Settings\username\Desktop.
            CSIDL_DRIVES					= (0x0011), // The virtual folder representing My Computer, containing 
            // everything on the local computer: storage devices, printers,
            // and Control Panel. The folder may also contain mapped 
            // network drives.
            CSIDL_FAVORITES					= (0x0006), // The file system directory that serves as a common repository 
            // for the user's favorite items. A typical path is C:\Documents
            // and Settings\username\Favorites.
            CSIDL_FONTS						= (0x0014), // A virtual folder containing fonts. A typical path is 
            // C:\Windows\Fonts.
            CSIDL_HISTORY					= (0x0022), // The file system directory that serves as a common repository
            // for Internet history items.
            CSIDL_INTERNET					= (0x0001), // A virtual folder representing the Internet.
            CSIDL_INTERNET_CACHE			= (0x0020), // Version 4.72. The file system directory that serves as a 
            // common repository for temporary Internet files. A typical 
            // path is C:\Documents and Settings\username\Local Settings\
            // Temporary Internet Files.
            CSIDL_LOCAL_APPDATA				= (0x001c), // Version 5.0. The file system directory that serves as a data
            // repository for local (nonroaming) applications. A typical 
            // path is C:\Documents and Settings\username\Local Settings\
            // Application Data.
            CSIDL_MYDOCUMENTS				= (0x000c), // Version 6.0. The virtual folder representing the My Documents
            // desktop item. This should not be confused with 
            // CSIDL_PERSONAL, which represents the file system folder that 
            // physically stores the documents.
            CSIDL_MYMUSIC					= (0x000d), // The file system directory that serves as a common repository 
            // for music files. A typical path is C:\Documents and Settings
            // \User\My Documents\My Music.
            CSIDL_MYPICTURES				= (0x0027), // Version 5.0. The file system directory that serves as a 
            // common repository for image files. A typical path is 
            // C:\Documents and Settings\username\My Documents\My Pictures.
            CSIDL_MYVIDEO					= (0x000e), // Version 6.0. The file system directory that serves as a 
            // common repository for video files. A typical path is 
            // C:\Documents and Settings\username\My Documents\My Videos.
            CSIDL_NETHOOD					= (0x0013), // A file system directory containing the link objects that may 
            // exist in the My Network Places virtual folder. It is not the
            // same as CSIDL_NETWORK, which represents the network namespace
            // root. A typical path is C:\Documents and Settings\username\
            // NetHood.
            CSIDL_NETWORK					= (0x0012), // A virtual folder representing Network Neighborhood, the root
            // of the network namespace hierarchy.
            CSIDL_PERSONAL					= (0x0005), // The file system directory used to physically store a user's
            // common repository of documents. A typical path is 
            // C:\Documents and Settings\username\My Documents. This should
            // be distinguished from the virtual My Documents folder in 
            // the namespace, identified by CSIDL_MYDOCUMENTS. 
            CSIDL_PRINTERS					= (0x0004), // The virtual folder containing installed printers.
            CSIDL_PRINTHOOD					= (0x001b), // The file system directory that contains the link objects that
            // can exist in the Printers virtual folder. A typical path is 
            // C:\Documents and Settings\username\PrintHood.
            CSIDL_PROFILE					= (0x0028), // Version 5.0. The user's profile folder. A typical path is 
            // C:\Documents and Settings\username. Applications should not 
            // create files or folders at this level; they should put their
            // data under the locations referred to by CSIDL_APPDATA or
            // CSIDL_LOCAL_APPDATA.
            CSIDL_PROFILES					= (0x003e), // Version 6.0. The file system directory containing user 
            // profile folders. A typical path is C:\Documents and Settings.
            CSIDL_PROGRAM_FILES				= (0x0026), // Version 5.0. The Program Files folder. A typical path is 
            // C:\Program Files.
            CSIDL_PROGRAM_FILES_COMMON		= (0x002b), // Version 5.0. A folder for components that are shared across 
            // applications. A typical path is C:\Program Files\Common. 
            // Valid only for Windows NT, Windows 2000, and Windows XP 
            // systems. Not valid for Windows Millennium Edition 
            // (Windows Me).
            CSIDL_PROGRAMS					= (0x0002), // The file system directory that contains the user's program 
            // groups (which are themselves file system directories).
            // A typical path is C:\Documents and Settings\username\
            // Start Menu\Programs. 
            CSIDL_RECENT					= (0x0008), // The file system directory that contains shortcuts to the 
            // user's most recently used documents. A typical path is 
            // C:\Documents and Settings\username\My Recent Documents. 
            // To create a shortcut in this folder, use SHAddToRecentDocs.
            // In addition to creating the shortcut, this function updates
            // the Shell's list of recent documents and adds the shortcut 
            // to the My Recent Documents submenu of the Start menu.
            CSIDL_SENDTO					= (0x0009), // The file system directory that contains Send To menu items.
            // A typical path is C:\Documents and Settings\username\SendTo.
            CSIDL_STARTMENU					= (0x000b), // The file system directory containing Start menu items. A 
            // typical path is C:\Documents and Settings\username\Start Menu.
            CSIDL_STARTUP					= (0x0007), // The file system directory that corresponds to the user's 
            // Startup program group. The system starts these programs 
            // whenever any user logs onto Windows NT or starts Windows 95.
            // A typical path is C:\Documents and Settings\username\
            // Start Menu\Programs\Startup.
            CSIDL_SYSTEM					= (0x0025), // Version 5.0. The Windows System folder. A typical path is 
            // C:\Windows\System32.
            CSIDL_TEMPLATES					= (0x0015), // The file system directory that serves as a common repository
            // for document templates. A typical path is C:\Documents 
            // and Settings\username\Templates.
            CSIDL_WINDOWS					= (0x0024), // Version 5.0. The Windows directory or SYSROOT. This 
            // corresponds to the %windir% or %SYSTEMROOT% environment 
            // variables. A typical path is C:\Windows.
        }

        public enum SHGFP_TYPE
        {
            SHGFP_TYPE_CURRENT = 0,		// current value for user, verify it exists
            SHGFP_TYPE_DEFAULT = 1		// default value, may not exist
        }

        public enum SFGAO: uint
        {
            SFGAO_CANCOPY           = 0x00000001,	// Objects can be copied    
            SFGAO_CANMOVE           = 0x00000002,	// Objects can be moved     
            SFGAO_CANLINK           = 0x00000004,	// Objects can be linked    
            SFGAO_STORAGE           = 0x00000008,   // supports BindToObject(IID_IStorage)
            SFGAO_CANRENAME         = 0x00000010,   // Objects can be renamed
            SFGAO_CANDELETE         = 0x00000020,   // Objects can be deleted
            SFGAO_HASPROPSHEET      = 0x00000040,   // Objects have property sheets
            SFGAO_DROPTARGET        = 0x00000100,   // Objects are drop target
            SFGAO_CAPABILITYMASK    = 0x00000177,	// This flag is a mask for the capability flags.
            SFGAO_ENCRYPTED         = 0x00002000,   // object is encrypted (use alt color)
            SFGAO_ISSLOW            = 0x00004000,   // 'slow' object
            SFGAO_GHOSTED           = 0x00008000,   // ghosted icon
            SFGAO_LINK              = 0x00010000,   // Shortcut (link)
            SFGAO_SHARE             = 0x00020000,   // shared
            SFGAO_READONLY          = 0x00040000,   // read-only
            SFGAO_HIDDEN            = 0x00080000,   // hidden object
            SFGAO_DISPLAYATTRMASK   = 0x000FC000,	// This flag is a mask for the display attributes.
            SFGAO_FILESYSANCESTOR   = 0x10000000,   // may contain children with SFGAO_FILESYSTEM
            SFGAO_FOLDER            = 0x20000000,   // support BindToObject(IID_IShellFolder)
            SFGAO_FILESYSTEM        = 0x40000000,   // is a win32 file system object (file/folder/root)
            SFGAO_HASSUBFOLDER      = 0x80000000,   // may contain children with SFGAO_FOLDER
            SFGAO_CONTENTSMASK      = 0x80000000,	// This flag is a mask for the contents attributes.
            SFGAO_VALIDATE          = 0x01000000,   // invalidate cached information
            SFGAO_REMOVABLE         = 0x02000000,   // is this removeable media?
            SFGAO_COMPRESSED        = 0x04000000,   // Object is compressed (use alt color)
            SFGAO_BROWSABLE         = 0x08000000,   // supports IShellFolder, but only implements CreateViewObject() (non-folder view)
            SFGAO_NONENUMERATED     = 0x00100000,   // is a non-enumerated object
            SFGAO_NEWCONTENT        = 0x00200000,   // should show bold in explorer tree
            SFGAO_CANMONIKER        = 0x00400000,   // defunct
            SFGAO_HASSTORAGE        = 0x00400000,   // defunct
            SFGAO_STREAM            = 0x00400000,   // supports BindToObject(IID_IStream)
            SFGAO_STORAGEANCESTOR   = 0x00800000,   // may contain children with SFGAO_STORAGE or SFGAO_STREAM
            SFGAO_STORAGECAPMASK    = 0x70C50008    // for determining storage capabilities, ie for open/save semantics

        }

        public enum SHCONTF
        {
            SHCONTF_FOLDERS             = 0x0020,   // only want folders enumerated (SFGAO_FOLDER)
            SHCONTF_NONFOLDERS          = 0x0040,   // include non folders
            SHCONTF_INCLUDEHIDDEN       = 0x0080,   // show items normally hidden
            SHCONTF_INIT_ON_FIRST_NEXT  = 0x0100,   // allow EnumObject() to return before validating enum
            SHCONTF_NETPRINTERSRCH      = 0x0200,   // hint that client is looking for printers
            SHCONTF_SHAREABLE           = 0x0400,   // hint that client is looking sharable resources (remote shares)
            SHCONTF_STORAGE             = 0x0800,   // include all items with accessible storage and their ancestors
        }

        public enum SHCIDS: uint
        {
            SHCIDS_ALLFIELDS        = 0x80000000,	// Compare all the information contained in the ITEMIDLIST 
            // structure, not just the display names
            SHCIDS_CANONICALONLY    = 0x10000000,	// When comparing by name, compare the system names but not the 
            // display names. 
            SHCIDS_BITMASK          = 0xFFFF0000,
            SHCIDS_COLUMNMASK       = 0x0000FFFF
        }

        public enum SHGNO
        {
            SHGDN_NORMAL             = 0x0000,		// default (display purpose)
            SHGDN_INFOLDER           = 0x0001,		// displayed under a folder (relative)
            SHGDN_FOREDITING         = 0x1000,		// for in-place editing
            SHGDN_FORADDRESSBAR      = 0x4000,		// UI friendly parsing name (remove ugly stuff)
            SHGDN_FORPARSING         = 0x8000		// parsing name for ParseDisplayName()
        }

        public enum STRRET_TYPE
        {
            STRRET_WSTR      = 0x0000,			// Use STRRET.pOleStr
            STRRET_OFFSET    = 0x0001,			// Use STRRET.uOffset to Ansi
            STRRET_CSTR      = 0x0002			// Use STRRET.cStr
        }


        public enum PrinterActions
        {
            PRINTACTION_OPEN				= 0,	// The printer specified by the name in lpBuf1 will be opened. 
            // lpBuf2 is ignored. 
            PRINTACTION_PROPERTIES			= 1,	// The properties for the printer specified by the name in lpBuf1
            // will be displayed. lpBuf2 can either be NULL or specify.
            PRINTACTION_NETINSTALL			= 2,	// The network printer specified by the name in lpBuf1 will be 
            // installed. lpBuf2 is ignored. 
            PRINTACTION_NETINSTALLLINK		= 3,	// A shortcut to the network printer specified by the name in lpBuf1
            // will be created. lpBuf2 specifies the drive and path of the folder 
            // in which the shortcut will be created. The network printer must 
            // have already been installed on the local computer. 
            PRINTACTION_TESTPAGE			= 4,	// A test page will be printed on the printer specified by the name
            // in lpBuf1. lpBuf2 is ignored. 
            PRINTACTION_OPENNETPRN			= 5,	// The network printer specified by the name in lpBuf1 will be
            // opened. lpBuf2 is ignored. 
            PRINTACTION_DOCUMENTDEFAULTS	= 6,	// Microsoft® Windows NT® only. The default document properties for
            // the printer specified by the name in lpBuf1 will be displayed. 
            // lpBuf2 is ignored. 
            PRINTACTION_SERVERPROPERTIES	= 7		// Windows NT only. The properties for the server of the printer 
            // specified by the name in lpBuf1 will be displayed. lpBuf2 
            // is ignored.
        }
    }
    public class ShellFileOperation
    {
        public class ShellNameMapping
        {
            private string destinationPath;
            private string renamedDestinationPath;

            public ShellNameMapping( string OldPath, string NewPath )
            {
                destinationPath = OldPath;
                renamedDestinationPath = NewPath;
            }

            public string DestinationPath
            {
                get { return destinationPath; }
            }

            public string RenamedDestinationPath
            {
                get { return renamedDestinationPath; }
            }
        }

        public enum FileOperations
        {
            FO_MOVE = 0x0001,		// Move the files specified in pFrom to the location specified in pTo. 
            FO_COPY = 0x0002,		// Copy the files specified in the pFrom member to the location specified 
            // in the pTo member. 
            FO_DELETE = 0x0003,		// Delete the files specified in pFrom. 
            FO_RENAME = 0x0004		// Rename the file specified in pFrom. You cannot use this flag to rename 
            // multiple files with a single function call. Use FO_MOVE instead. 
        }

        [Flags]
        public enum ShellFileOperationFlags
        {
            FOF_MULTIDESTFILES = 0x0001,	// The pTo member specifies multiple destination files (one for 
            // each source file) rather than one directory where all source 
            // files are to be deposited. 
            FOF_CONFIRMMOUSE = 0x0002,	// Not currently used. 
            FOF_SILENT = 0x0004,	// Do not display a progress dialog box. 
            FOF_RENAMEONCOLLISION = 0x0008,	// Give the file being operated on a new name in a move, copy, or 
            // rename operation if a file with the target name already exists. 
            FOF_NOCONFIRMATION = 0x0010,	// Respond with "Yes to All" for any dialog box that is displayed. 
            FOF_WANTMAPPINGHANDLE = 0x0020,	// If FOF_RENAMEONCOLLISION is specified and any files were renamed,
            // assign a name mapping object containing their old and new names 
            // to the hNameMappings member.
            FOF_ALLOWUNDO = 0x0040,	// Preserve Undo information, if possible. If pFrom does not 
            // contain fully qualified path and file names, this flag is ignored. 
            FOF_FILESONLY = 0x0080,	// Perform the operation on files only if a wildcard file 
            // name (*.*) is specified. 
            FOF_SIMPLEPROGRESS = 0x0100,	// Display a progress dialog box but do not show the file names. 
            FOF_NOCONFIRMMKDIR = 0x0200,	// Do not confirm the creation of a new directory if the operation
            // requires one to be created. 
            FOF_NOERRORUI = 0x0400,	// Do not display a user interface if an error occurs. 
            FOF_NOCOPYSECURITYATTRIBS = 0x0800,	// Do not copy the security attributes of the file.
            FOF_NORECURSION = 0x1000,	// Only operate in the local directory. Don't operate recursively
            // into subdirectories.
            FOF_NO_CONNECTED_ELEMENTS = 0x2000,	// Do not move connected files as a group. Only move the 
            // specified files. 
            FOF_WANTNUKEWARNING = 0x4000,	// Send a warning if a file is being destroyed during a delete 
            // operation rather than recycled. This flag partially 
            // overrides FOF_NOCONFIRMATION.
            FOF_NORECURSEREPARSE = 0x8000		// Treat reparse points as objects, not containers.

        }

        [Flags]
        public enum ShellChangeNotificationEvents: uint
        {
            SHCNE_RENAMEITEM = 0x00000001,	// The name of a nonfolder item has changed. SHCNF_IDLIST or 
            // SHCNF_PATH must be specified in uFlags. dwItem1 contains the 
            // previous PIDL or name of the item. dwItem2 contains the new PIDL
            // or name of the item. 
            SHCNE_CREATE = 0x00000002,	// A nonfolder item has been created. SHCNF_IDLIST or SHCNF_PATH 
            // must be specified in uFlags. dwItem1 contains the item that was 
            // created. dwItem2 is not used and should be NULL. 
            SHCNE_DELETE = 0x00000004,	// A nonfolder item has been deleted. SHCNF_IDLIST or SHCNF_PATH
            // must be specified in uFlags. dwItem1 contains the item that was 
            // deleted. dwItem2 is not used and should be NULL. 
            SHCNE_MKDIR = 0x00000008,	// A folder has been created. SHCNF_IDLIST or SHCNF_PATH must be 
            // specified in uFlags. dwItem1 contains the folder that was 
            // created. dwItem2 is not used and should be NULL. 
            SHCNE_RMDIR = 0x00000010,	// A folder has been removed. SHCNF_IDLIST or SHCNF_PATH must be 
            // specified in uFlags. dwItem1 contains the folder that was 
            // removed. dwItem2 is not used and should be NULL. 
            SHCNE_MEDIAINSERTED = 0x00000020,	// Storage media has been inserted into a drive. SHCNF_IDLIST or
            // SHCNF_PATH must be specified in uFlags. dwItem1 contains the root
            // of the drive that contains the new media. dwItem2 is not used 
            // and should be NULL. 
            SHCNE_MEDIAREMOVED = 0x00000040,	// Storage media has been removed from a drive. SHCNF_IDLIST or 
            // SHCNF_PATH must be specified in uFlags. dwItem1 contains the root
            // of the drive from which the media was removed. dwItem2 is not 
            // used and should be NULL. 
            SHCNE_DRIVEREMOVED = 0x00000080,	// A drive has been removed. SHCNF_IDLIST or SHCNF_PATH must be 
            // specified in uFlags. dwItem1 contains the root of the drive that
            // was removed. dwItem2 is not used and should be NULL. 
            SHCNE_DRIVEADD = 0x00000100,	// A drive has been added. SHCNF_IDLIST or SHCNF_PATH must be 
            // specified in uFlags. dwItem1 contains the root of the drive that
            // was added. dwItem2 is not used and should be NULL. 
            SHCNE_NETSHARE = 0x00000200,	// A folder on the local computer is being shared via the network.
            // SHCNF_IDLIST or SHCNF_PATH must be specified in uFlags. dwItem1
            // contains the folder that is being shared. dwItem2 is not used and
            // should be NULL. 
            SHCNE_NETUNSHARE = 0x00000400,	// A folder on the local computer is no longer being shared via the
            // network. SHCNF_IDLIST or SHCNF_PATH must be specified in uFlags. 
            // dwItem1 contains the folder that is no longer being shared. 
            // dwItem2 is not used and should be NULL. 
            SHCNE_ATTRIBUTES = 0x00000800,	// The attributes of an item or folder have changed. SHCNF_IDLIST
            // or SHCNF_PATH must be specified in uFlags. dwItem1 contains the
            // item or folder that has changed. dwItem2 is not used and should
            // be NULL. 
            SHCNE_UPDATEDIR = 0x00001000,	// The contents of an existing folder have changed, but the folder
            // still exists and has not been renamed. SHCNF_IDLIST or SHCNF_PATH
            // must be specified in uFlags. dwItem1 contains the folder that 
            // has changed. dwItem2 is not used and should be NULL. If a folder
            // has been created, deleted, or renamed, use SHCNE_MKDIR, 
            // SHCNE_RMDIR, or SHCNE_RENAMEFOLDER, respectively, instead. 
            SHCNE_UPDATEITEM = 0x00002000,	// An existing nonfolder item has changed, but the item still exists
            // and has not been renamed. SHCNF_IDLIST or SHCNF_PATH must be 
            // specified in uFlags. dwItem1 contains the item that has changed.
            // dwItem2 is not used and should be NULL. If a nonfolder item has 
            // been created, deleted, or renamed, use SHCNE_CREATE, 
            // SHCNE_DELETE, or SHCNE_RENAMEITEM, respectively, instead. 
            SHCNE_SERVERDISCONNECT = 0x00004000,	// The computer has disconnected from a server. SHCNF_IDLIST or 
            // SHCNF_PATH must be specified in uFlags. dwItem1 contains the 
            // server from which the computer was disconnected. dwItem2 is not
            // used and should be NULL.
            SHCNE_UPDATEIMAGE = 0x00008000,	// An image in the system image list has changed. SHCNF_DWORD must be 
            // specified in uFlags. dwItem1 contains the index in the system image 
            // list that has changed. dwItem2 is not used and should be NULL. 
            SHCNE_DRIVEADDGUI = 0x00010000,	// A drive has been added and the Shell should create a new window
            // for the drive. SHCNF_IDLIST or SHCNF_PATH must be specified in 
            // uFlags. dwItem1 contains the root of the drive that was added. 
            // dwItem2 is not used and should be NULL. 
            SHCNE_RENAMEFOLDER = 0x00020000,	// The name of a folder has changed. SHCNF_IDLIST or SHCNF_PATH must
            // be specified in uFlags. dwItem1 contains the previous pointer to
            // an item identifier list (PIDL) or name of the folder. dwItem2 
            // contains the new PIDL or name of the folder. 
            SHCNE_FREESPACE = 0x00040000,	// The amount of free space on a drive has changed. SHCNF_IDLIST or
            // SHCNF_PATH must be specified in uFlags. dwItem1 contains the root
            // of the drive on which the free space changed. dwItem2 is not used
            // and should be NULL. 
            SHCNE_EXTENDED_EVENT = 0x04000000,	// Not currently used. 
            SHCNE_ASSOCCHANGED = 0x08000000,	// A file type association has changed. SHCNF_IDLIST must be 
            // specified in the uFlags parameter. dwItem1 and dwItem2 are not
            // used and must be NULL. 
            SHCNE_DISKEVENTS = 0x0002381F,	// Specifies a combination of all of the disk event identifiers. 
            SHCNE_GLOBALEVENTS = 0x0C0581E0,	// Specifies a combination of all of the global event identifiers. 
            SHCNE_ALLEVENTS = 0x7FFFFFFF,	// All events have occurred. 
            SHCNE_INTERRUPT = 0x80000000	// The specified event occurred as a result of a system interrupt.
            // As this value modifies other event values, it cannot be used alone.
        }


        public enum ShellChangeNotificationFlags
        {
            SHCNF_IDLIST = 0x0000,	// dwItem1 and dwItem2 are the addresses of ITEMIDLIST structures that
            // represent the item(s) affected by the change. Each ITEMIDLIST must be 
            // relative to the desktop folder. 
            SHCNF_PATHA = 0x0001,	// dwItem1 and dwItem2 are the addresses of null-terminated strings of 
            // maximum length MAX_PATH that contain the full path names of the items 
            // affected by the change. 
            SHCNF_PRINTERA = 0x0002,	// dwItem1 and dwItem2 are the addresses of null-terminated strings that 
            // represent the friendly names of the printer(s) affected by the change. 
            SHCNF_DWORD = 0x0003,	// The dwItem1 and dwItem2 parameters are DWORD values. 
            SHCNF_PATHW = 0x0005,	// like SHCNF_PATHA but unicode string
            SHCNF_PRINTERW = 0x0006,	// like SHCNF_PRINTERA but unicode string
            SHCNF_TYPE = 0x00FF,
            SHCNF_FLUSH = 0x1000,	// The function should not return until the notification has been delivered 
            // to all affected components. As this flag modifies other data-type flags,
            // it cannot by used by itself.
            SHCNF_FLUSHNOWAIT = 0x2000	// The function should begin delivering notifications to all affected 
            // components but should return as soon as the notification process has
            // begun. As this flag modifies other data-type flags, it cannot by used 
            // by itself.
        }

        // properties
        public FileOperations Operation;
        public IntPtr OwnerWindow;
        public ShellFileOperationFlags OperationFlags;
        public String ProgressTitle;
        public String[] SourceFiles;
        public String[] DestFiles;
        public ShellNameMapping[] NameMappings;

        public ShellFileOperation()
        {
            // set default properties
            Operation = FileOperations.FO_COPY;
            OwnerWindow = IntPtr.Zero;
            OperationFlags = ShellFileOperationFlags.FOF_ALLOWUNDO | ShellFileOperationFlags.FOF_MULTIDESTFILES | ShellFileOperationFlags.FOF_NO_CONNECTED_ELEMENTS | ShellFileOperationFlags.FOF_WANTNUKEWARNING;
            ProgressTitle = "";
            NameMappings = null;
        }

        public bool DoOperation()
        {
            ShellApi.SHFILEOPSTRUCT FileOpStruct = new ShellApi.SHFILEOPSTRUCT();

            FileOpStruct.hwnd = OwnerWindow;
            FileOpStruct.wFunc = (uint)Operation;

            String multiSource = StringArrayToMultiString(SourceFiles);
            String multiDest = StringArrayToMultiString(DestFiles);
            FileOpStruct.pFrom = Marshal.StringToHGlobalUni(multiSource);
            FileOpStruct.pTo = Marshal.StringToHGlobalUni(multiDest);

            FileOpStruct.fFlags = (ushort)OperationFlags;
            FileOpStruct.lpszProgressTitle = ProgressTitle;
            FileOpStruct.fAnyOperationsAborted = 0;
            FileOpStruct.hNameMappings = IntPtr.Zero;
            this.NameMappings = new ShellNameMapping[0];

            int RetVal;
            RetVal = ShellApi.SHFileOperation(ref FileOpStruct);

            ShellApi.SHChangeNotify(
                (uint)ShellChangeNotificationEvents.SHCNE_ALLEVENTS,
                (uint)ShellChangeNotificationFlags.SHCNF_DWORD,
                IntPtr.Zero,
                IntPtr.Zero);

            if ( RetVal != 0 )
                return false;

            if ( FileOpStruct.fAnyOperationsAborted != 0 )
                return false;

            // Newly added on 2007/08/29 to make hNameMappings work
            if ( FileOpStruct.hNameMappings != IntPtr.Zero ) {
                // Get MappingTable
                ShellApi.SHNAMEMAPPINGINDEXSTRUCT mappingIndex = (ShellApi.SHNAMEMAPPINGINDEXSTRUCT)Marshal.PtrToStructure(
                    FileOpStruct.hNameMappings,
                    typeof(ShellApi.SHNAMEMAPPINGINDEXSTRUCT));

                // Prepare array
                this.NameMappings = new ShellNameMapping[mappingIndex.counter];

                // Set pointer to first mapping struct
                IntPtr mover = mappingIndex.firstMappingStruct;
                for ( int i = 0; i < mappingIndex.counter; i++ ) {
                    ShellApi.SHNAMEMAPPINGSTRUCT oneNameMappingStruct =
                        (ShellApi.SHNAMEMAPPINGSTRUCT)Marshal.PtrToStructure(mover, typeof(ShellApi.SHNAMEMAPPINGSTRUCT));

                    this.NameMappings[i] = new ShellNameMapping(oneNameMappingStruct.pszOldPath, oneNameMappingStruct.pszNewPath);

                    // move pointer to the next mapping struct 
                    mover = (IntPtr)((int)mover + Marshal.SizeOf(typeof(ShellApi.SHNAMEMAPPINGSTRUCT)));
                }

                // Free NameMappings in memory
                ShellApi.SHFreeNameMappings(FileOpStruct.hNameMappings);
            }

            return true;
        }

        private static String StringArrayToMultiString( String[] stringArray )
        {
            String multiString = "";

            if ( stringArray == null )
                return "";

            for ( int i = 0; i < stringArray.Length; i++ )
                multiString += stringArray[i] + '\0';

            multiString += '\0';

            return multiString;
        }
    }

    public class DriveSettings
    {
        private enum ResourceScope
        {
            RESOURCE_CONNECTED = 1,
            RESOURCE_GLOBALNET,
            RESOURCE_REMEMBERED,
            RESOURCE_RECENT,
            RESOURCE_CONTEXT
        }
        private enum ResourceType
        {
            RESOURCETYPE_ANY,
            RESOURCETYPE_DISK,
            RESOURCETYPE_PRINT,
            RESOURCETYPE_RESERVED
        }
        private enum ResourceUsage
        {
            RESOURCEUSAGE_CONNECTABLE = 0x00000001,
            RESOURCEUSAGE_CONTAINER = 0x00000002,
            RESOURCEUSAGE_NOLOCALDEVICE = 0x00000004,
            RESOURCEUSAGE_SIBLING = 0x00000008,
            RESOURCEUSAGE_ATTACHED = 0x00000010
        }
        private enum ResourceDisplayType
        {
            RESOURCEDISPLAYTYPE_GENERIC,
            RESOURCEDISPLAYTYPE_DOMAIN,
            RESOURCEDISPLAYTYPE_SERVER,
            RESOURCEDISPLAYTYPE_SHARE,
            RESOURCEDISPLAYTYPE_FILE,
            RESOURCEDISPLAYTYPE_GROUP,
            RESOURCEDISPLAYTYPE_NETWORK,
            RESOURCEDISPLAYTYPE_ROOT,
            RESOURCEDISPLAYTYPE_SHAREADMIN,
            RESOURCEDISPLAYTYPE_DIRECTORY,
            RESOURCEDISPLAYTYPE_TREE,
            RESOURCEDISPLAYTYPE_NDSCONTAINER
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct NETRESOURCE
        {
            public ResourceScope oResourceScope;
            public ResourceType oResourceType;
            public ResourceDisplayType oDisplayType;
            public ResourceUsage oResourceUsage;
            public string sLocalName;
            public string sRemoteName;
            public string sComments;
            public string sProvider;
        }
        [DllImport("mpr.dll")]
        private static extern int WNetAddConnection2( ref NETRESOURCE oNetworkResource, string sPassword, string sUserName, int iFlags );
        [DllImport("mpr.dll")]
        private static extern int WNetCancelConnection2( string sLocalName, uint iFlags, int iForce );
        public static int MapNetworkDrive( string sDriveLetter, string sNetworkPath, string sUser, string sPwd, bool bReconnectAtLogon )
        {
            //Checks if the last character is \ as this causes error on mapping a drive.
            if ( sNetworkPath.Substring(sNetworkPath.Length - 1, 1) == @"\" ) {
                sNetworkPath = sNetworkPath.Substring(0, sNetworkPath.Length - 1);
            }

            NETRESOURCE oNetworkResource = new NETRESOURCE();
            oNetworkResource.oResourceType = ResourceType.RESOURCETYPE_DISK;
            oNetworkResource.sLocalName = sDriveLetter + ":";
            oNetworkResource.sRemoteName = sNetworkPath;

            // If Drive is already mapped disconnect the current mapping before adding the new mapping
            if ( IsDriveMapped(sDriveLetter) ) {
                DisconnectNetworkDrive(sDriveLetter, true, false);
            }

            // reconnect at logon
            int flag = bReconnectAtLogon ? 1 : 0;

            return WNetAddConnection2(ref oNetworkResource, sPwd, sUser, flag);
        }
        public static int DisconnectNetworkDrive( string sDriveLetter, bool bForceDisconnect, bool bUpdateProfile )
        {
            uint flags = (uint)(bUpdateProfile ? 1 : 0);
            if ( bForceDisconnect ) {
                return WNetCancelConnection2(sDriveLetter + ":", flags, 1);
            } else {
                return WNetCancelConnection2(sDriveLetter + ":", flags, 0);
            }
        }
        public static bool IsDriveMapped( string sDriveLetter )
        {
            string[] DriveList = Environment.GetLogicalDrives();
            for ( int i = 0; i < DriveList.Length; i++ ) {
                if ( sDriveLetter + ":\\" == DriveList[i].ToString() ) {
                    return true;
                }
            }
            return false;
        }
    }

    //
    // http://www.codeproject.com/Articles/18062/Detecting-USB-Drive-Removal-in-a-C-Program
    //
    // Delegate for event handler to handle the device events 
    public delegate void DriveDetectorEventHandler( Object sender, DriveDetectorEventArgs e );
    /// <summary>
    /// Our class for passing in custom arguments to our event handlers 
    /// 
    /// </summary>
    public class DriveDetectorEventArgs: EventArgs
    {
        public DriveDetectorEventArgs()
        {
            Cancel = false;
            Drive = "";
            HookQueryRemove = false;
        }

        /// <summary>
        /// Get/Set the value indicating that the event should be cancelled 
        /// Only in QueryRemove handler.
        /// </summary>
        public bool Cancel;

        /// <summary>
        /// Drive letter for the device which caused this event 
        /// </summary>
        public string Drive;

        /// <summary>
        /// Set to true in your DeviceArrived event handler if you wish to receive the 
        /// QueryRemove event for this drive. 
        /// </summary>
        public bool HookQueryRemove;

    }


    /// <summary>
    /// Detects insertion or removal of removable drives.
    /// Use it in 1 or 2 steps:
    /// 1) Create instance of this class in your project and add handlers for the
    /// DeviceArrived, DeviceRemoved and QueryRemove events.
    /// AND (if you do not want drive detector to creaate a hidden form))
    /// 2) Override WndProc in your form and call DriveDetector's WndProc from there. 
    /// If you do not want to do step 2, just use the DriveDetector constructor without arguments and
    /// it will create its own invisible form to receive messages from Windows.
    /// </summary>
    class DriveDetector: IDisposable
    {
        /// <summary>
        /// Events signalized to the client app.
        /// Add handlers for these events in your form to be notified of removable device events 
        /// </summary>
        public event DriveDetectorEventHandler DeviceArrived;
        public event DriveDetectorEventHandler DeviceRemoved;
        public event DriveDetectorEventHandler QueryRemove;

        /// <summary>
        /// The easiest way to use DriveDetector. 
        /// It will create hidden form for processing Windows messages about USB drives
        /// You do not need to override WndProc in your form.
        /// </summary>
        public DriveDetector()
        {
        }

        /// <summary>
        /// Constructs DriveDetector object setting also path to file which should be opened
        /// when registering for query remove.  
        /// </summary>
        ///<param name="control">object which will receive Windows messages. 
        /// Pass "this" as this argument from your form class.</param>
        /// <param name="FileToOpen">Optional. Name of a file on the removable drive which should be opened. 
        /// If null, root directory of the drive will be opened. Opening a file is needed for us 
        /// to be able to register for the query remove message. TIP: For files use relative path without drive letter.
        /// e.g. "SomeFolder\file_on_flash.txt"</param>
        public DriveDetector( Control control, string FileToOpen )
        {
            Init(control, FileToOpen);
            EnableQueryRemove(FileToOpen);
        }

        /// <summary>
        /// init the DriveDetector object
        /// </summary>
        /// <param name="intPtr"></param>
        private void Init( Control control, string fileToOpen )
        {
            mFileToOpen = fileToOpen;
            mFileOnFlash = null;
            mDeviceNotifyHandle = IntPtr.Zero;
            mRecipientHandle = control.Handle;
            mDirHandle = IntPtr.Zero;   // handle to the root directory of the flash drive which we open 
            mCurrentDrive = "";
        }

        /// <summary>
        /// Gets the value indicating whether the query remove event will be fired.
        /// </summary>
        public bool IsQueryHooked
        {
            get
            {
                if ( mDeviceNotifyHandle == IntPtr.Zero )
                    return false;
                else
                    return true;
            }
        }

        /// <summary>
        /// Gets letter of drive which is currently hooked. Empty string if none.
        /// See also IsQueryHooked.
        /// </summary>
        public string HookedDrive
        {
            get
            {
                return mCurrentDrive;
            }
        }

        /// <summary>
        /// Gets the file stream for file which this class opened on a drive to be notified
        /// about it's removal. 
        /// This will be null unless you specified a file to open (DriveDetector opens root directory of the flash drive) 
        /// </summary>
        public FileStream OpenedFile
        {
            get
            {
                return mFileOnFlash;
            }
        }

        /// <summary>
        /// Hooks specified drive to receive a message when it is being removed.  
        /// This can be achieved also by setting e.HookQueryRemove to true in your 
        /// DeviceArrived event handler. 
        /// By default DriveDetector will open the root directory of the flash drive to obtain notification handle
        /// from Windows (to learn when the drive is about to be removed). 
        /// </summary>
        /// <param name="fileOnDrive">Drive letter or relative path to a file on the drive which should be 
        /// used to get a handle - required for registering to receive query remove messages.
        /// If only drive letter is specified (e.g. "D:\\", root directory of the drive will be opened.</param>
        /// <returns>true if hooked ok, false otherwise</returns>
        public bool EnableQueryRemove( string fileOnDrive )
        {
            if ( fileOnDrive == null || fileOnDrive.Length == 0 )
                throw new ArgumentException("Drive path must be supplied to register for Query remove.");

            if ( fileOnDrive.Length == 2 && fileOnDrive[1] == ':' )
                fileOnDrive += '\\';        // append "\\" if only drive letter with ":" was passed in.

            if ( mDeviceNotifyHandle != IntPtr.Zero ) {
                // Unregister first...
                RegisterForDeviceChange(false, null);
            }

            if ( Path.GetFileName(fileOnDrive).Length == 0 ||!System.IO.File.Exists(fileOnDrive) )
                mFileToOpen = null;     // use root directory...
            else
                mFileToOpen = fileOnDrive;

            RegisterQuery(Path.GetPathRoot(fileOnDrive));
            if ( mDeviceNotifyHandle == IntPtr.Zero )
                return false;   // failed to register

            return true;
        }

        /// <summary>
        /// Unhooks any currently hooked drive so that the query remove 
        /// message is not generated for it.
        /// </summary>
        public void DisableQueryRemove()
        {
            if ( mDeviceNotifyHandle != IntPtr.Zero ) {
                RegisterForDeviceChange(false, null);
            }
        }


        /// <summary>
        /// Unregister and close the file we may have opened on the removable drive. 
        /// Garbage collector will call this method.
        /// </summary>
        public void Dispose()
        {
            RegisterForDeviceChange(false, null);
        }


        #region WindowProc
        /// <summary>
        /// Message handler which must be called from client form.
        /// Processes Windows messages and calls event handlers. 
        /// </summary>
        /// <param name="m"></param>
        public void WndProc( ref Message m )
        {
            int devType;
            char c;

            if ( m.Msg == WM_DEVICECHANGE ) {
                // WM_DEVICECHANGE can have several meanings depending on the WParam value...
                switch ( m.WParam.ToInt32() ) {

                    //
                    // New device has just arrived
                    //
                    case DBT_DEVICEARRIVAL:

                        devType = Marshal.ReadInt32(m.LParam, 4);
                        if ( devType == DBT_DEVTYP_VOLUME ) {
                            DEV_BROADCAST_VOLUME vol;
                            vol = (DEV_BROADCAST_VOLUME)
                                Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_VOLUME));

                            // Get the drive letter 
                            c = DriveMaskToLetter(vol.dbcv_unitmask);


                            //
                            // Call the client event handler
                            //
                            // We should create copy of the event before testing it and
                            // calling the delegate - if any
                            DriveDetectorEventHandler tempDeviceArrived = DeviceArrived;
                            if ( tempDeviceArrived != null ) {
                                DriveDetectorEventArgs e = new DriveDetectorEventArgs();
                                e.Drive = c + ":\\";
                                tempDeviceArrived(this, e);

                                // Register for query remove if requested
                                if ( e.HookQueryRemove ) {
                                    // If something is already hooked, unhook it now
                                    if ( mDeviceNotifyHandle != IntPtr.Zero ) {
                                        RegisterForDeviceChange(false, null);
                                    }

                                    RegisterQuery(c + ":\\");
                                }
                            }     // if  has event handler


                        }
                        break;



                    //
                    // Device is about to be removed
                    // Any application can cancel the removal
                    //
                    case DBT_DEVICEQUERYREMOVE:

                        devType = Marshal.ReadInt32(m.LParam, 4);
                        if ( devType == DBT_DEVTYP_HANDLE ) {
                            // TODO: we could get the handle for which this message is sent 
                            // from vol.dbch_handle and compare it against a list of handles for 
                            // which we have registered the query remove message (?)                                                 
                            //DEV_BROADCAST_HANDLE vol;
                            //vol = (DEV_BROADCAST_HANDLE)
                            //   Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_HANDLE));
                            // if ( vol.dbch_handle ....


                            //
                            // Call the event handler in client
                            //
                            DriveDetectorEventHandler tempQuery = QueryRemove;
                            if ( tempQuery != null ) {
                                DriveDetectorEventArgs e = new DriveDetectorEventArgs();
                                e.Drive = mCurrentDrive;        // drive which is hooked
                                tempQuery(this, e);

                                // If the client wants to cancel, let Windows know
                                if ( e.Cancel ) {
                                    m.Result = (IntPtr)BROADCAST_QUERY_DENY;
                                } else {
                                    // Change 28.10.2007: Unregister the notification, this will
                                    // close the handle to file or root directory also. 
                                    // We have to close it anyway to allow the removal so
                                    // even if some other app cancels the removal we would not know about it...                                    
                                    RegisterForDeviceChange(false, null);   // will also close the mFileOnFlash
                                }

                            }
                        }
                        break;


                    //
                    // Device has been removed
                    //
                    case DBT_DEVICEREMOVECOMPLETE:

                        devType = Marshal.ReadInt32(m.LParam, 4);
                        if ( devType == DBT_DEVTYP_VOLUME ) {
                            devType = Marshal.ReadInt32(m.LParam, 4);
                            if ( devType == DBT_DEVTYP_VOLUME ) {
                                DEV_BROADCAST_VOLUME vol;
                                vol = (DEV_BROADCAST_VOLUME)
                                    Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_VOLUME));
                                c = DriveMaskToLetter(vol.dbcv_unitmask);

                                //
                                // Call the client event handler
                                //
                                DriveDetectorEventHandler tempDeviceRemoved = DeviceRemoved;
                                if ( tempDeviceRemoved != null ) {
                                    DriveDetectorEventArgs e = new DriveDetectorEventArgs();
                                    e.Drive = c + ":\\";
                                    tempDeviceRemoved(this, e);
                                }

                                // TODO: we could unregister the notify handle here if we knew it is the
                                // right drive which has been just removed
                                //RegisterForDeviceChange(false, null);
                            }
                        }
                        break;
                }

            }

        }

        #endregion



        #region  Private Area

        /// <summary>
        /// New: 28.10.2007 - handle to root directory of flash drive which is opened
        /// for device notification
        /// </summary>
        private IntPtr mDirHandle = IntPtr.Zero;

        /// <summary>
        /// Class which contains also handle to the file opened on the flash drive
        /// </summary>
        private FileStream mFileOnFlash = null;

        /// <summary>
        /// Name of the file to try to open on the removable drive for query remove registration
        /// </summary>
        private string mFileToOpen;

        /// <summary>
        /// Handle to file which we keep opened on the drive if query remove message is required by the client
        /// </summary>       
        private IntPtr mDeviceNotifyHandle;

        /// <summary>
        /// Handle of the window which receives messages from Windows. This will be a form.
        /// </summary>
        private IntPtr mRecipientHandle;

        /// <summary>
        /// Drive which is currently hooked for query remove
        /// </summary>
        private string mCurrentDrive;


        // Win32 constants
        private const int DBT_DEVTYP_DEVICEINTERFACE = 5;
        private const int DBT_DEVTYP_HANDLE = 6;
        private const int BROADCAST_QUERY_DENY = 0x424D5144;
        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000; // system detected a new device
        private const int DBT_DEVICEQUERYREMOVE = 0x8001;   // Preparing to remove (any program can disable the removal)
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004; // removed 
        private const int DBT_DEVTYP_VOLUME = 0x00000002; // drive type is logical volume

        /// <summary>
        /// Registers for receiving the query remove message for a given drive.
        /// We need to open a handle on that drive and register with this handle. 
        /// Client can specify this file in mFileToOpen or we will open root directory of the drive
        /// </summary>
        /// <param name="drive">drive for which to register. </param>
        private void RegisterQuery( string drive )
        {
            bool register = true;

            if ( mFileToOpen == null ) {
                // Change 28.10.2007 - Open the root directory if no file specified - leave mFileToOpen null 
                // If client gave us no file, let's pick one on the drive... 
                //mFileToOpen = GetAnyFile(drive);
                //if (mFileToOpen.Length == 0)
                //    return;     // no file found on the flash drive                
            } else {
                // Make sure the path in mFileToOpen contains valid drive
                // If there is a drive letter in the path, it may be different from the  actual
                // letter assigned to the drive now. We will cut it off and merge the actual drive 
                // with the rest of the path.
                if ( mFileToOpen.Contains(":") ) {
                    string tmp = mFileToOpen.Substring(3);
                    string root = Path.GetPathRoot(drive);
                    mFileToOpen = Path.Combine(root, tmp);
                } else
                    mFileToOpen = Path.Combine(drive, mFileToOpen);
            }


            try {
                //mFileOnFlash = new FileStream(mFileToOpen, FileMode.Open);
                // Change 28.10.2007 - Open the root directory 
                if ( mFileToOpen == null )  // open root directory
                    mFileOnFlash = null;
                else
                    mFileOnFlash = new FileStream(mFileToOpen, FileMode.Open);
            } catch ( Exception ) {
                // just do not register if the file could not be opened
                register = false;
            }


            if ( register ) {
                //RegisterForDeviceChange(true, mFileOnFlash.SafeFileHandle);
                //mCurrentDrive = drive;
                // Change 28.10.2007 - Open the root directory 
                if ( mFileOnFlash == null )
                    RegisterForDeviceChange(drive);
                else
                    // old version
                    RegisterForDeviceChange(true, mFileOnFlash.SafeFileHandle);

                mCurrentDrive = drive;
            }


        }


        /// <summary>
        /// New version which gets the handle automatically for specified directory
        /// Only for registering! Unregister with the old version of this function...
        /// </summary>
        /// <param name="register"></param>
        /// <param name="dirPath">e.g. C:\\dir</param>
        private void RegisterForDeviceChange( string dirPath )
        {
            IntPtr handle = Native.OpenDirectory(dirPath);
            if ( handle == IntPtr.Zero ) {
                mDeviceNotifyHandle = IntPtr.Zero;
                return;
            } else
                mDirHandle = handle;    // save handle for closing it when unregistering

            // Register for handle
            DEV_BROADCAST_HANDLE data = new DEV_BROADCAST_HANDLE();
            data.dbch_devicetype = DBT_DEVTYP_HANDLE;
            data.dbch_reserved = 0;
            data.dbch_nameoffset = 0;
            //data.dbch_data = null;
            //data.dbch_eventguid = 0;
            data.dbch_handle = handle;
            data.dbch_hdevnotify = (IntPtr)0;
            int size = Marshal.SizeOf(data);
            data.dbch_size = size;
            IntPtr buffer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, buffer, true);

            mDeviceNotifyHandle = Native.RegisterDeviceNotification(mRecipientHandle, buffer, 0);

        }

        /// <summary>
        /// Registers to be notified when the volume is about to be removed
        /// This is requierd if you want to get the QUERY REMOVE messages
        /// </summary>
        /// <param name="register">true to register, false to unregister</param>
        /// <param name="fileHandle">handle of a file opened on the removable drive</param>
        private void RegisterForDeviceChange( bool register, SafeFileHandle fileHandle )
        {
            if ( register ) {
                // Register for handle
                DEV_BROADCAST_HANDLE data = new DEV_BROADCAST_HANDLE();
                data.dbch_devicetype = DBT_DEVTYP_HANDLE;
                data.dbch_reserved = 0;
                data.dbch_nameoffset = 0;
                //data.dbch_data = null;
                //data.dbch_eventguid = 0;
                data.dbch_handle = fileHandle.DangerousGetHandle(); //Marshal. fileHandle; 
                data.dbch_hdevnotify = (IntPtr)0;
                int size = Marshal.SizeOf(data);
                data.dbch_size = size;
                IntPtr buffer = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(data, buffer, true);

                mDeviceNotifyHandle = Native.RegisterDeviceNotification(mRecipientHandle, buffer, 0);
            } else {
                // close the directory handle
                if ( mDirHandle != IntPtr.Zero ) {
                    Native.CloseDirectoryHandle(mDirHandle);
                    //    string er = Marshal.GetLastWin32Error().ToString();
                }

                // unregister
                if ( mDeviceNotifyHandle != IntPtr.Zero ) {
                    Native.UnregisterDeviceNotification(mDeviceNotifyHandle);
                }


                mDeviceNotifyHandle = IntPtr.Zero;
                mDirHandle = IntPtr.Zero;

                mCurrentDrive = "";
                if ( mFileOnFlash != null ) {
                    mFileOnFlash.Close();
                    mFileOnFlash = null;
                }
            }

        }

        /// <summary>
        /// Gets drive letter from a bit mask where bit 0 = A, bit 1 = B etc.
        /// There can actually be more than one drive in the mask but we 
        /// just use the last one in this case.
        /// </summary>
        /// <param name="mask"></param>
        /// <returns></returns>
        private static char DriveMaskToLetter( int mask )
        {
            char letter;
            string drives = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            // 1 = A
            // 2 = B
            // 4 = C...
            int cnt = 0;
            int pom = mask / 2;
            while ( pom != 0 ) {
                // while there is any bit set in the mask
                // shift it to the righ...                
                pom = pom / 2;
                cnt++;
            }

            if ( cnt < drives.Length )
                letter = drives[cnt];
            else
                letter = '?';

            return letter;
        }

        /* 28.10.2007 - no longer needed
        /// <summary>
        /// Searches for any file in a given path and returns its full path
        /// </summary>
        /// <param name="drive">drive to search</param>
        /// <returns>path of the file or empty string</returns>
        private string GetAnyFile(string drive)
        {
            string file = "";
            // First try files in the root
            string[] files = Directory.GetFiles(drive);
            if (files.Length == 0)
            {
                // if no file in the root, search whole drive
                files = Directory.GetFiles(drive, "*.*", SearchOption.AllDirectories);
            }
                
            if (files.Length > 0)
                file = files[0];        // get the first file

            // return empty string if no file found
            return file;
        }*/
        #endregion


        #region Native Win32 API
        /// <summary>
        /// WinAPI functions
        /// </summary>        
        private class Native
        {
            //   HDEVNOTIFY RegisterDeviceNotification(HANDLE hRecipient,LPVOID NotificationFilter,DWORD Flags);
            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr RegisterDeviceNotification( IntPtr hRecipient, IntPtr NotificationFilter, uint Flags );

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern uint UnregisterDeviceNotification( IntPtr hHandle );

            //
            // CreateFile  - MSDN
            const uint GENERIC_READ = 0x80000000;
            const uint OPEN_EXISTING = 3;
            const uint FILE_SHARE_READ = 0x00000001;
            const uint FILE_SHARE_WRITE = 0x00000002;
            const uint FILE_ATTRIBUTE_NORMAL = 128;
            const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
            static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);


            // should be "static extern unsafe"
            [DllImport("kernel32", SetLastError = true)]
            static extern IntPtr CreateFile(
                  string FileName,                    // file name
                  uint DesiredAccess,                 // access mode
                  uint ShareMode,                     // share mode
                  uint SecurityAttributes,            // Security Attributes
                  uint CreationDisposition,           // how to create
                  uint FlagsAndAttributes,            // file attributes
                  int hTemplateFile                   // handle to template file
                  );


            [DllImport("kernel32", SetLastError = true)]
            static extern bool CloseHandle(
                  IntPtr hObject   // handle to object
                  );

            /// <summary>
            /// Opens a directory, returns it's handle or zero.
            /// </summary>
            /// <param name="dirPath">path to the directory, e.g. "C:\\dir"</param>
            /// <returns>handle to the directory. Close it with CloseHandle().</returns>
            static public IntPtr OpenDirectory( string dirPath )
            {
                // open the existing file for reading          
                IntPtr handle = CreateFile(
                      dirPath,
                      GENERIC_READ,
                      FILE_SHARE_READ | FILE_SHARE_WRITE,
                      0,
                      OPEN_EXISTING,
                      FILE_FLAG_BACKUP_SEMANTICS | FILE_ATTRIBUTE_NORMAL,
                      0);

                if ( handle == INVALID_HANDLE_VALUE )
                    return IntPtr.Zero;
                else
                    return handle;
            }


            public static bool CloseDirectoryHandle( IntPtr handle )
            {
                return CloseHandle(handle);
            }
        }


        // Structure with information for RegisterDeviceNotification.
        [StructLayout(LayoutKind.Sequential)]
        public struct DEV_BROADCAST_HANDLE
        {
            public int dbch_size;
            public int dbch_devicetype;
            public int dbch_reserved;
            public IntPtr dbch_handle;
            public IntPtr dbch_hdevnotify;
            public Guid dbch_eventguid;
            public long dbch_nameoffset;
            //public byte[] dbch_data[1]; // = new byte[1];
            public byte dbch_data;
            public byte dbch_data1;
        }

        // Struct for parameters of the WM_DEVICECHANGE message
        [StructLayout(LayoutKind.Sequential)]
        public struct DEV_BROADCAST_VOLUME
        {
            public int dbcv_size;
            public int dbcv_devicetype;
            public int dbcv_reserved;
            public int dbcv_unitmask;
        }
        #endregion

    }

    public class UsbEject
    {
        /// <summary>
        /// The device class for volume devices.
        /// </summary>
        public class VolumeDeviceClass: DeviceClass
        {
            internal SortedDictionary<string, string> _logicalDrives = new SortedDictionary<string, string>();

            /// <summary>
            /// Initializes a new instance of the VolumeDeviceClass class.
            /// </summary>
            public VolumeDeviceClass() : base(new Guid(Native.GUID_DEVINTERFACE_VOLUME))
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach ( DriveInfo drive in drives ) {
                    // 20161016: check whether network drive is accessible --> a not connected network drive may hang for 20s when asking for "Native.GetVolumeNameForVolumeMountPoint"
                    if ( drive.DriveType == DriveType.Network ) {
                        if ( !GrzTools.Network.PingNetDriveOk(drive.Name.Substring(0, drive.Name.Length-1)) ) {
                            continue;
                        }
                    }
                    StringBuilder sb = new StringBuilder(1024);
                    if ( Native.GetVolumeNameForVolumeMountPoint(drive.Name, sb, sb.Capacity) ) {
                        _logicalDrives[sb.ToString()] = drive.Name.Replace("\\", "");
                        Console.WriteLine(drive + " ==> " + sb.ToString());
                    }
                }
            }

            internal override Device CreateDevice( DeviceClass deviceClass, Native.SP_DEVINFO_DATA deviceInfoData, string path, int index, int disknum = -1 )
            {
                return new Volume(deviceClass, deviceInfoData, path, index);
            }
        }

        /// <summary>
        /// A volume device.
        /// </summary>
        public class Volume: Device, IComparable
        {
            private string _volumeName;
            private string _logicalDrive;
            private int[] _diskNumbers;
            private List<Device> _disks;
            private List<Device> _removableDevices;

            internal Volume( DeviceClass deviceClass, Native.SP_DEVINFO_DATA deviceInfoData, string path, int index )
                : base(deviceClass, deviceInfoData, path, index)
            {
            }

            /// <summary>
            /// Gets the volume's name.
            /// </summary>
            public string VolumeName
            {
                get
                {
                    if ( _volumeName == null ) {
                        StringBuilder sb = new StringBuilder(1024);
                        if ( !Native.GetVolumeNameForVolumeMountPoint(Path + "\\", sb, sb.Capacity) ) {
                            // throw new Win32Exception(Marshal.GetLastWin32Error());

                        }

                        if ( sb.Length > 0 ) {
                            _volumeName = sb.ToString();
                        }
                    }
                    return _volumeName;
                }
            }

            /// <summary>
            /// Gets the volume's logical drive in the form [letter]:\
            /// </summary>
            public string LogicalDrive
            {
                get
                {
                    if ( (_logicalDrive == null) && (VolumeName != null) ) {
                        ((VolumeDeviceClass)DeviceClass)._logicalDrives.TryGetValue(VolumeName, out _logicalDrive);
                    }
                    return _logicalDrive;
                }
            }

            /// <summary>
            /// Gets a value indicating whether this volume is a based on USB devices.
            /// </summary>
            public override bool IsUsb
            {
                get
                {
                    if ( Disks != null ) {
                        foreach ( Device disk in Disks ) {
                            if ( disk.IsUsb )
                                return true;
                        }
                    }
                    return false;
                }
            }

            /// <summary>
            /// Gets a list of underlying disks for this volume.
            /// </summary>
            public List<Device> Disks
            {
                get
                {
                    if ( _disks == null ) {
                        _disks = new List<Device>();

                        if ( DiskNumbers != null ) {
                            DiskDeviceClass disks = new DiskDeviceClass();
                            foreach ( int index in DiskNumbers ) {
                                foreach ( Device disk in disks.Devices ) {
                                    if ( disk.DiskNumber == index ) {
                                        _disks.Add(disk);
                                    }
                                }
                            }
                        }
                    }
                    return _disks;
                }
            }

            public int[] DiskNumbers
            {
                get
                {
                    if ( _diskNumbers == null ) {
                        List<int> numbers = new List<int>();
                        if ( LogicalDrive != null ) {
                            Console.WriteLine("Finding disk extents for volume: " + LogicalDrive);
                            IntPtr hFile = Native.CreateFile(@"\\.\" + LogicalDrive, 0, Native.FILE_SHARE_READ | Native.FILE_SHARE_WRITE, IntPtr.Zero, Native.OPEN_EXISTING, 0, IntPtr.Zero);
                            if ( hFile.ToInt32() == Native.INVALID_HANDLE_VALUE )
                                throw new Win32Exception(Marshal.GetLastWin32Error());

                            int size = 0x400; // some big size
                            IntPtr buffer = Marshal.AllocHGlobal(size);
                            int bytesReturned = 0;
                            try {
                                if ( !Native.DeviceIoControl(hFile, Native.IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS, IntPtr.Zero, 0, buffer, size, out bytesReturned, IntPtr.Zero) ) {
                                    // do nothing here on purpose
                                }
                            } finally {
                                Native.CloseHandle(hFile);
                            }

                            if ( bytesReturned > 0 ) {
                                int numberOfDiskExtents = (int)Marshal.PtrToStructure(buffer, typeof(int));
                                for ( int i = 0; i < numberOfDiskExtents; i++ ) {
                                    IntPtr extentPtr = new IntPtr(buffer.ToInt32() + Marshal.SizeOf(typeof(long)) + i * Marshal.SizeOf(typeof(Native.DISK_EXTENT)));
                                    Native.DISK_EXTENT extent = (Native.DISK_EXTENT)Marshal.PtrToStructure(extentPtr, typeof(Native.DISK_EXTENT));
                                    numbers.Add(extent.DiskNumber);
                                }
                            }
                            Marshal.FreeHGlobal(buffer);
                        }

                        _diskNumbers = new int[numbers.Count];
                        numbers.CopyTo(_diskNumbers);
                    }
                    return _diskNumbers;
                }
            }

            /// <summary>
            /// Gets a list of removable devices for this volume.
            /// </summary>
            public override List<Device> RemovableDevices
            {
                get
                {
                    if ( _removableDevices == null ) {
                        _removableDevices = new List<Device>();
                        if ( Disks == null ) {
                            _removableDevices = base.RemovableDevices;
                        } else {
                            foreach ( Device disk in Disks ) {
                                foreach ( Device device in disk.RemovableDevices ) {
                                    _removableDevices.Add(device);
                                }
                            }
                        }
                    }
                    return _removableDevices;
                }
            }

            /// <summary>
            /// Compares the current instance with another object of the same type.
            /// </summary>
            /// <param name="obj">An object to compare with this instance.</param>
            /// <returns>A 32-bit signed integer that indicates the relative order of the comparands.</returns>
            public override int CompareTo( object obj )
            {
                Volume device = obj as Volume;
                if ( device == null )
                    throw new ArgumentException();

                if ( LogicalDrive == null )
                    return 1;

                if ( device.LogicalDrive == null )
                    return -1;

                return LogicalDrive.CompareTo(device.LogicalDrive);
            }
        }

        internal sealed class Native
        {
            // from winuser.h
            internal const int WM_DEVICECHANGE = 0x0219;

            // from winbase.h
            internal const int INVALID_HANDLE_VALUE = -1;
            internal const int GENERIC_READ = unchecked((int)0x80000000);
            internal const int FILE_SHARE_READ = 0x00000001;
            internal const int FILE_SHARE_WRITE = 0x00000002;
            internal const int OPEN_EXISTING = 3;

            [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern bool GetVolumeNameForVolumeMountPoint(
                string volumeName,
                StringBuilder uniqueVolumeName,
                int uniqueNameBufferCapacity );

            [DllImport("Kernel32.dll", SetLastError = true)]
            internal static extern IntPtr CreateFile( string lpFileName, int dwDesiredAccess, int dwShareMode, IntPtr lpSecurityAttributes, int dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile );

            [DllImport("Kernel32.dll", SetLastError = true)]
            internal static extern bool DeviceIoControl( IntPtr hDevice, int dwIoControlCode, IntPtr lpInBuffer, int nInBufferSize, IntPtr lpOutBuffer, int nOutBufferSize, out int lpBytesReturned, IntPtr lpOverlapped );

            [DllImport("Kernel32.dll", SetLastError = true)]
            internal static extern bool CloseHandle( IntPtr hObject );

            // from winerror.h
            internal const int ERROR_NO_MORE_ITEMS = 259;
            internal const int ERROR_INSUFFICIENT_BUFFER = 122;
            internal const int ERROR_INVALID_DATA = 13;

            // from winioctl.h
            internal const string GUID_DEVINTERFACE_VOLUME = "53f5630d-b6bf-11d0-94f2-00a0c91efb8b";
            internal const string GUID_DEVINTERFACE_DISK = "53f56307-b6bf-11d0-94f2-00a0c91efb8b";
            internal const int IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS = 0x00560000;
            internal const int IOCTL_STORAGE_GET_DEVICE_NUMBER      = 0x002d1080;

            [StructLayout(LayoutKind.Sequential)]
            internal struct DISK_EXTENT
            {
                internal int DiskNumber;
                internal long StartingOffset;
                internal long ExtentLength;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct STORAGE_DEVICE_NUMBER
            {
                public int DeviceType;
                public int DeviceNumber;
                public int PartitionNumber;
            }

            // from cfg.h
            internal enum PNP_VETO_TYPE
            {
                Ok,

                TypeUnknown,
                LegacyDevice,
                PendingClose,
                WindowsApp,
                WindowsService,
                OutstandingOpen,
                Device,
                Driver,
                IllegalDeviceRequest,
                InsufficientPower,
                NonDisableable,
                LegacyDriver,
            }

            // from cfgmgr32.h
            [DllImport("setupapi.dll")]
            internal static extern int CM_Get_Parent(
                ref int pdnDevInst,
                int dnDevInst,
                int ulFlags );

            [DllImport("setupapi.dll")]
            internal static extern int CM_Get_Device_ID(
                int dnDevInst,
                StringBuilder buffer,
                int bufferLen,
                int ulFlags );

            [DllImport("setupapi.dll")]
            internal static extern int CM_Request_Device_Eject(
                int dnDevInst,
                out PNP_VETO_TYPE pVetoType,
                StringBuilder pszVetoName,
                int ulNameLength,
                int ulFlags
                );

            [DllImport("setupapi.dll", EntryPoint = "CM_Request_Device_Eject")]
            internal static extern int CM_Request_Device_Eject_NoUi(
                int dnDevInst,
                IntPtr pVetoType,
                StringBuilder pszVetoName,
                int ulNameLength,
                int ulFlags
                );

            // from setupapi.h
            internal const int DIGCF_PRESENT = (0x00000002);
            internal const int DIGCF_DEVICEINTERFACE = (0x00000010);

            internal const int SPDRP_DEVICEDESC = 0x00000000;
            internal const int SPDRP_CAPABILITIES = 0x0000000F;
            internal const int SPDRP_CLASS = 0x00000007;
            internal const int SPDRP_CLASSGUID = 0x00000008;
            internal const int SPDRP_FRIENDLYNAME = 0x0000000C;

            [StructLayout(LayoutKind.Sequential)]
            internal class SP_DEVINFO_DATA
            {
                internal uint cbSize = (uint)Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
                internal Guid classGuid = Guid.Empty; // temp
                internal uint devInst = 0; // dumy
                internal IntPtr reserved = IntPtr.Zero;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]  // x64  !! this setting is not clear: 2 works too on x64 !!!
            internal struct SP_DEVICE_INTERFACE_DETAIL_DATA
            {
                internal uint cbSize;
                internal char devicePath;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal class SP_DEVICE_INTERFACE_DATA
            {
                internal uint cbSize = (uint)Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));
                internal Guid interfaceClassGuid = Guid.Empty; // temp
                internal uint flags = 0;
                internal IntPtr reserved = IntPtr.Zero;
            }

            [DllImport("setupapi.dll")]
            internal static extern IntPtr SetupDiGetClassDevs(
                ref Guid classGuid,
                int enumerator,
                IntPtr hwndParent,
                int flags );

            [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
            internal static extern bool SetupDiEnumDeviceInterfaces(
                IntPtr deviceInfoSet,
                SP_DEVINFO_DATA deviceInfoData,
                ref Guid interfaceClassGuid,
                int memberIndex,
                SP_DEVICE_INTERFACE_DATA deviceInterfaceData );

            [DllImport("setupapi.dll")]
            internal static extern bool SetupDiOpenDeviceInfo(
                IntPtr deviceInfoSet,
                string deviceInstanceId,
                IntPtr hwndParent,
                int openFlags,
                SP_DEVINFO_DATA deviceInfoData
                );

            [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
            internal static extern bool SetupDiGetDeviceInterfaceDetail(
                IntPtr deviceInfoSet,
                SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
                IntPtr deviceInterfaceDetailData,
                int deviceInterfaceDetailDataSize,
                ref int requiredSize,
                SP_DEVINFO_DATA deviceInfoData );

            [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern bool SetupDiGetDeviceRegistryProperty(
                IntPtr deviceInfoSet,
                SP_DEVINFO_DATA deviceInfoData,
                int property,
                out int propertyRegDataType,
                IntPtr propertyBuffer,
                int propertyBufferSize,
                out int requiredSize
                );

            [DllImport("setupapi.dll")]
            internal static extern uint SetupDiDestroyDeviceInfoList(
                IntPtr deviceInfoSet );


            private Native()
            {
            }
        }

        /// <summary>
        /// The device class for disk devices.
        /// </summary>
        public class DiskDeviceClass: DeviceClass
        {
            /// <summary>
            /// Initializes a new instance of the DiskDeviceClass class.
            /// </summary>
            public DiskDeviceClass()
                : base(new Guid(Native.GUID_DEVINTERFACE_DISK))
            {
            }
        }
        /// <summary>
        /// A generic base class for physical device classes.
        /// </summary>
        public abstract class DeviceClass: IDisposable
        {
            private IntPtr _deviceInfoSet;
            private Guid _classGuid;
            private List<Device> _devices;

            protected DeviceClass( Guid classGuid )
                : this(classGuid, IntPtr.Zero)
            {
            }

            internal virtual Device CreateDevice( DeviceClass deviceClass, Native.SP_DEVINFO_DATA deviceInfoData, string path, int index, int disknum = -1 )
            {
                return new Device(deviceClass, deviceInfoData, path, index, disknum);
            }

            /// <summary>
            /// Initializes a new instance of the DeviceClass class.
            /// </summary>
            /// <param name="classGuid">A device class Guid.</param>
            /// <param name="hwndParent">The handle of the top-level window to be used for any user interface or IntPtr.Zero for no handle.</param>
            protected DeviceClass( Guid classGuid, IntPtr hwndParent )
            {
                _classGuid = classGuid;

                _deviceInfoSet = Native.SetupDiGetClassDevs(ref _classGuid, 0, hwndParent, Native.DIGCF_DEVICEINTERFACE | Native.DIGCF_PRESENT);
                if ( _deviceInfoSet.ToInt32() == Native.INVALID_HANDLE_VALUE )
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                if ( _deviceInfoSet != IntPtr.Zero ) {
                    Native.SetupDiDestroyDeviceInfoList(_deviceInfoSet);
                    _deviceInfoSet = IntPtr.Zero;
                }
            }

            /// <summary>
            /// Gets the device class's guid.
            /// </summary>
            public Guid ClassGuid
            {
                get
                {
                    return _classGuid;
                }
            }

            /// <summary>
            /// Gets the list of devices of this device class.
            /// </summary>
            public List<Device> Devices
            {
                get
                {
                    if ( _devices == null ) {
                        _devices = new List<Device>();
                        int index = 0;
                        while ( true ) {
                            Native.SP_DEVICE_INTERFACE_DATA interfaceData = new Native.SP_DEVICE_INTERFACE_DATA();
                            interfaceData.cbSize = (uint)Marshal.SizeOf(interfaceData);

                            if ( !Native.SetupDiEnumDeviceInterfaces(_deviceInfoSet, null, ref _classGuid, index, interfaceData) ) {
                                int error = Marshal.GetLastWin32Error();
                                if ( error != Native.ERROR_NO_MORE_ITEMS )
                                    throw new Win32Exception(error);
                                break;
                            }

                            Native.SP_DEVINFO_DATA devData = new Native.SP_DEVINFO_DATA();
                            int size = 0;
                            if ( !Native.SetupDiGetDeviceInterfaceDetail(_deviceInfoSet, interfaceData, IntPtr.Zero, 0, ref size, devData) ) {
                                int error = Marshal.GetLastWin32Error();
                                if ( error != Native.ERROR_INSUFFICIENT_BUFFER )
                                    throw new Win32Exception(error);
                            }

                            IntPtr buffer = Marshal.AllocHGlobal(size);
                            Native.SP_DEVICE_INTERFACE_DETAIL_DATA detailData = new Native.SP_DEVICE_INTERFACE_DETAIL_DATA();
                            if ( IntPtr.Size == 8 ) {   // x64
                                detailData.cbSize = 8;
                            } else {                    // x32  
//                                detailData.cbSize = (uint)Marshal.SizeOf(typeof(Native.SP_DEVICE_INTERFACE_DETAIL_DATA));
                                detailData.cbSize = (uint)(4 + Marshal.SystemDefaultCharSize);
                            }
                            Marshal.StructureToPtr(detailData, buffer, false);

                            if ( !Native.SetupDiGetDeviceInterfaceDetail(_deviceInfoSet, interfaceData, buffer, size, ref size, devData) ) {
                                Marshal.FreeHGlobal(buffer);
                                throw new Win32Exception(Marshal.GetLastWin32Error());
                            }

                            IntPtr pDevicePath = (IntPtr)((int)buffer + Marshal.SizeOf(typeof(int)));
                            string devicePath = Marshal.PtrToStringAuto(pDevicePath);
                            Marshal.FreeHGlobal(buffer);

                            if ( _classGuid.Equals(new Guid(Native.GUID_DEVINTERFACE_DISK)) ) {
                                // Find disks
                                IntPtr hFile = Native.CreateFile(devicePath, 0, Native.FILE_SHARE_READ | Native.FILE_SHARE_WRITE, IntPtr.Zero, Native.OPEN_EXISTING, 0, IntPtr.Zero);
                                if ( hFile.ToInt32() == Native.INVALID_HANDLE_VALUE )
                                    throw new Win32Exception(Marshal.GetLastWin32Error());

                                int bytesReturned = 0;
                                int numBufSize = 0x400; // some big size
                                IntPtr numBuffer = Marshal.AllocHGlobal(numBufSize);
                                Native.STORAGE_DEVICE_NUMBER disknum;

                                try {
                                    if ( !Native.DeviceIoControl(hFile, Native.IOCTL_STORAGE_GET_DEVICE_NUMBER, IntPtr.Zero, 0, numBuffer, numBufSize, out bytesReturned, IntPtr.Zero) ) {
                                        Console.WriteLine("IOCTL failed.");
                                    }
                                } catch ( Exception ex ) {
                                    Console.WriteLine("Exception calling ioctl: " + ex);
                                } finally {
                                    Native.CloseHandle(hFile);
                                }

                                if ( bytesReturned > 0 )
                                    disknum = (Native.STORAGE_DEVICE_NUMBER)Marshal.PtrToStructure(numBuffer, typeof(Native.STORAGE_DEVICE_NUMBER));
                                else
                                    disknum = new Native.STORAGE_DEVICE_NUMBER() { DeviceNumber = -1, DeviceType = -1, PartitionNumber = -1 };

                                Device device = CreateDevice(this, devData, devicePath, index, disknum.DeviceNumber);
                                _devices.Add(device);

                                try {
                                    Marshal.FreeHGlobal(hFile);
                                } catch ( Exception ) { ;}
                            } else {
                                Device device = CreateDevice(this, devData, devicePath, index);
                                _devices.Add(device);
                            }

                            index++;
                        }
                        _devices.Sort();
                    }
                    return _devices;
                }
            }

            internal Native.SP_DEVINFO_DATA GetInfo( int dnDevInst )
            {
                StringBuilder sb = new StringBuilder(1024);
                int hr = Native.CM_Get_Device_ID(dnDevInst, sb, sb.Capacity, 0);
                if ( hr != 0 )
                    throw new Win32Exception(hr);

                Native.SP_DEVINFO_DATA devData = new Native.SP_DEVINFO_DATA();
                devData.cbSize = (uint)Marshal.SizeOf(typeof(Native.SP_DEVINFO_DATA));
                if ( !Native.SetupDiOpenDeviceInfo(_deviceInfoSet, sb.ToString(), IntPtr.Zero, 0, devData) )
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                return devData;
            }

            internal string GetProperty( Native.SP_DEVINFO_DATA devData, int property, string defaultValue )
            {
                if ( devData == null )
                    throw new ArgumentNullException("devData");

                int propertyRegDataType = 0;
                int requiredSize;
                int propertyBufferSize = 1024;

                IntPtr propertyBuffer = Marshal.AllocHGlobal(propertyBufferSize);
                if ( !Native.SetupDiGetDeviceRegistryProperty(_deviceInfoSet,
                    devData,
                    property,
                    out propertyRegDataType,
                    propertyBuffer,
                    propertyBufferSize,
                    out requiredSize) ) {
                    Marshal.FreeHGlobal(propertyBuffer);
                    int error = Marshal.GetLastWin32Error();
                    if ( error != Native.ERROR_INVALID_DATA )
                        throw new Win32Exception(error);
                    return defaultValue;
                }

                string value = Marshal.PtrToStringAuto(propertyBuffer);
                Marshal.FreeHGlobal(propertyBuffer);
                return value;
            }

            internal int GetProperty( Native.SP_DEVINFO_DATA devData, int property, int defaultValue )
            {
                if ( devData == null )
                    throw new ArgumentNullException("devData");

                int propertyRegDataType = 0;
                int requiredSize;
                int propertyBufferSize = Marshal.SizeOf(typeof(int));

                IntPtr propertyBuffer = Marshal.AllocHGlobal(propertyBufferSize);
                if ( !Native.SetupDiGetDeviceRegistryProperty(_deviceInfoSet,
                    devData,
                    property,
                    out propertyRegDataType,
                    propertyBuffer,
                    propertyBufferSize,
                    out requiredSize) ) {
                    Marshal.FreeHGlobal(propertyBuffer);
                    int error = Marshal.GetLastWin32Error();
                    if ( error != Native.ERROR_INVALID_DATA )
                        throw new Win32Exception(error);
                    return defaultValue;
                }

                int value = (int)Marshal.PtrToStructure(propertyBuffer, typeof(int));
                Marshal.FreeHGlobal(propertyBuffer);
                return value;
            }

            internal Guid GetProperty( Native.SP_DEVINFO_DATA devData, int property, Guid defaultValue )
            {
                if ( devData == null )
                    throw new ArgumentNullException("devData");

                int propertyRegDataType = 0;
                int requiredSize;
                int propertyBufferSize = Marshal.SizeOf(typeof(Guid));

                IntPtr propertyBuffer = Marshal.AllocHGlobal(propertyBufferSize);
                if ( !Native.SetupDiGetDeviceRegistryProperty(_deviceInfoSet,
                    devData,
                    property,
                    out propertyRegDataType,
                    propertyBuffer,
                    propertyBufferSize,
                    out requiredSize) ) {
                    Marshal.FreeHGlobal(propertyBuffer);
                    int error = Marshal.GetLastWin32Error();
                    if ( error != Native.ERROR_INVALID_DATA )
                        throw new Win32Exception(error);
                    return defaultValue;
                }

                Guid value = (Guid)Marshal.PtrToStructure(propertyBuffer, typeof(Guid));
                Marshal.FreeHGlobal(propertyBuffer);
                return value;
            }
        }

        /// <summary>
        /// Contains constants for determining devices capabilities.
        /// This enumeration has a FlagsAttribute attribute that allows a bitwise combination of its member values.
        /// </summary>
        [Flags]
        public enum DeviceCapabilities
        {
            Unknown = 0x00000000,
            // matches cfmgr32.h CM_DEVCAP_* definitions

            LockSupported = 0x00000001,
            EjectSupported = 0x00000002,
            Removable = 0x00000004,
            DockDevice = 0x00000008,
            UniqueId = 0x00000010,
            SilentInstall =0x00000020,
            RawDeviceOk = 0x00000040,
            SurpriseRemovalOk = 0x00000080,
            HardwareDisabled = 0x00000100,
            NonDynamic = 0x00000200,
        }

        /// <summary>
        /// A generic base class for physical devices.
        /// </summary>
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public class Device: IComparable
        {
            private string _path;
            private DeviceClass _deviceClass;
            private string _description;
            private string _class;
            private string _classGuid;
            private int _disknum;
            private Device _parent;
            private int _index;
            private DeviceCapabilities _capabilities = DeviceCapabilities.Unknown;
            private List<Device> _removableDevices;
            private string _friendlyName;
            private Native.SP_DEVINFO_DATA _deviceInfoData;

            internal Device( DeviceClass deviceClass, Native.SP_DEVINFO_DATA deviceInfoData, string path, int index, int disknum = -1 )
            {
                if ( deviceClass == null )
                    throw new ArgumentNullException("deviceClass");

                if ( deviceInfoData == null )
                    throw new ArgumentNullException("deviceInfoData");

                _deviceClass = deviceClass;
                _path = path; // may be null
                _deviceInfoData = deviceInfoData;
                _index = index;
                _disknum = disknum;
            }

            /// <summary>
            /// Gets the device's index.
            /// </summary>
            public int Index
            {
                get
                {
                    return _index;
                }
            }

            /// <summary>
            /// Gets the device's class instance.
            /// </summary>
            [Browsable(false)]
            public DeviceClass DeviceClass
            {
                get
                {
                    return _deviceClass;
                }
            }

            /// <summary>
            /// Gets the device's path.
            /// </summary>
            public string Path
            {
                get
                {
                    if ( _path == null ) {
                    }
                    return _path;
                }
            }

            public int DiskNumber
            {
                get
                {
                    return _disknum;
                }
            }

            /// <summary>
            /// Gets the device's instance handle.
            /// </summary>
            public int InstanceHandle
            {
                get
                {
                    return (int)_deviceInfoData.devInst;
                }
            }

            /// <summary>
            /// Gets the device's class name.
            /// </summary>
            public string Class
            {
                get
                {
                    if ( _class == null ) {
                        _class = _deviceClass.GetProperty(_deviceInfoData, Native.SPDRP_CLASS, null);
                    }
                    return _class;
                }
            }

            /// <summary>
            /// Gets the device's class Guid as a string.
            /// </summary>
            public string ClassGuid
            {
                get
                {
                    if ( _classGuid == null ) {
                        _classGuid = _deviceClass.GetProperty(_deviceInfoData, Native.SPDRP_CLASSGUID, null);
                    }
                    return _classGuid;
                }
            }

            /// <summary>
            /// Gets the device's description.
            /// </summary>
            public string Description
            {
                get
                {
                    if ( _description == null ) {
                        _description = _deviceClass.GetProperty(_deviceInfoData, Native.SPDRP_DEVICEDESC, null);
                    }
                    return _description;
                }
            }

            /// <summary>
            /// Gets the device's friendly name.
            /// </summary>
            public string FriendlyName
            {
                get
                {
                    if ( _friendlyName == null ) {
                        _friendlyName = _deviceClass.GetProperty(_deviceInfoData, Native.SPDRP_FRIENDLYNAME, null);
                    }
                    return _friendlyName;
                }
            }

            /// <summary>
            /// Gets the device's capabilities.
            /// </summary>
            public DeviceCapabilities Capabilities
            {
                get
                {
                    if ( _capabilities == DeviceCapabilities.Unknown ) {
                        _capabilities = (DeviceCapabilities)_deviceClass.GetProperty(_deviceInfoData, Native.SPDRP_CAPABILITIES, 0);
                    }
                    return _capabilities;
                }
            }

            /// <summary>
            /// Gets a value indicating whether this device is a USB device.
            /// </summary>
            public virtual bool IsUsb
            {
                get
                {
                    if ( Class == "USB" )
                        return true;

                    if ( Parent == null )
                        return false;

                    return Parent.IsUsb;
                }
            }

            /// <summary>
            /// Gets the device's parent device or null if this device has not parent.
            /// </summary>
            public Device Parent
            {
                get
                {
                    if ( _parent == null ) {
                        int parentDevInst = 0;
                        int hr = Native.CM_Get_Parent(ref parentDevInst, (int)_deviceInfoData.devInst, 0);
                        if ( hr == 0 ) {
                            _parent = new Device(_deviceClass, _deviceClass.GetInfo(parentDevInst), null, -1);
                        }
                    }
                    return _parent;
                }
            }

            /// <summary>
            /// Gets this device's list of removable devices.
            /// Removable devices are parent devices that can be removed.
            /// </summary>
            public virtual List<Device> RemovableDevices
            {
                get
                {
                    if ( _removableDevices == null ) {
                        _removableDevices = new List<Device>();

                        if ( (Capabilities & DeviceCapabilities.Removable) != 0 ) {
                            _removableDevices.Add(this);
                        } else {
                            if ( Parent != null ) {
                                foreach ( Device device in Parent.RemovableDevices ) {
                                    _removableDevices.Add(device);
                                }
                            }
                        }
                    }
                    return _removableDevices;
                }
            }

            /// <summary>
            /// Ejects the device.
            /// </summary>
            /// <param name="allowUI">Pass true to allow the Windows shell to display any related UI element, false otherwise.</param>
            /// <returns>null if no error occured, otherwise a contextual text.</returns>
            public string Eject( bool allowUI )
            {
                foreach ( Device device in RemovableDevices ) {
                    if ( allowUI ) {
                        Native.CM_Request_Device_Eject_NoUi(device.InstanceHandle, IntPtr.Zero, null, 0, 0);
                        // don't handle errors, there should be a UI for this
                    } else {
                        StringBuilder sb = new StringBuilder(1024);

                        Native.PNP_VETO_TYPE veto;
                        int hr = Native.CM_Request_Device_Eject(device.InstanceHandle, out veto, sb, sb.Capacity, 0);
                        if ( hr != 0 ) {
                            try {
                                throw new Win32Exception(hr);
                            } catch ( Exception ex ) {
                                string msg = ex.Message + "\r\n\r\nExecute 'Drive - Properties - Tools - Error-checking'.";
                                MessageBox.Show(msg, "Error");
                                return msg;
                            }
                        }
                        if ( veto != Native.PNP_VETO_TYPE.Ok ) {
                            string msg = veto.ToString();
                            MessageBox.Show(msg, "Error");
                            return msg;
                        }
                    }

                }
                return null;
            }

            /// <summary>
            /// Compares the current instance with another object of the same type.
            /// </summary>
            /// <param name="obj">An object to compare with this instance.</param>
            /// <returns>A 32-bit signed integer that indicates the relative order of the comparands.</returns>
            public virtual int CompareTo( object obj )
            {
                Device device = obj as Device;
                if ( device == null )
                    throw new ArgumentException();

                return Index.CompareTo(device.Index);
            }
        }
    }

    // string extension method
    public static class Extensions
    {
        public static string ReplaceAt( this string input, int index, char newChar )
        {
            if ( input == null ) {
                throw new ArgumentNullException("input");
            }
            char[] chars = input.ToCharArray();
            chars[index] = newChar;
            return new string(chars);
        }
    }

}