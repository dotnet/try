// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

 // adapted from https://github.com/Microsoft/msbuild/blob/685deba56e0c5d6a311678948f6ef1be5a6005d1/src/Shared/NativeMethodsShared.cs

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace External
{
    /// <summary>
    /// Interop methods.
    /// </summary>
    internal static class NativeMethodsShared
    {
        #region Constants

        private const uint ERROR_ACCESS_DENIED = 0x5;

        // As defined in winnt.h:
        private const ushort PROCESSOR_ARCHITECTURE_INTEL = 0;

        private const ushort PROCESSOR_ARCHITECTURE_ARM = 5;
        private const ushort PROCESSOR_ARCHITECTURE_IA64 = 6;
        private const ushort PROCESSOR_ARCHITECTURE_AMD64 = 9;

        #endregion

        #region Enums

        private enum PROCESSINFOCLASS : int
        {
            ProcessBasicInformation = 0,
        };

        private enum eDesiredAccess : int
        {
            PROCESS_QUERY_INFORMATION = 0x0400,
        }

        /// <summary>
        /// Processor architecture values
        /// </summary>
        private enum ProcessorArchitectures
        {
            // Intel 32 bit
            X86,

            // AMD64 64 bit
            X64,

            // Itanium 64
            IA64,

            // ARM
            ARM,

            // Who knows
            Unknown
        }

        #endregion

        #region Structs

        /// <summary>
        /// Structure that contain information about the system on which we are running
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_INFO
        {
            // This is a union of a DWORD and a struct containing 2 WORDs.
            internal ushort wProcessorArchitecture;

            private ushort wReserved;

            private uint dwPageSize;
            private IntPtr lpMinimumApplicationAddress;
            private IntPtr lpMaximumApplicationAddress;
            private IntPtr dwActiveProcessorMask;
            private uint dwNumberOfProcessors;
            private uint dwProcessorType;
            private uint dwAllocationGranularity;
            private ushort wProcessorLevel;
            private ushort wProcessorRevision;
        }

        /// <summary>
        /// Wrap the intptr returned by OpenProcess in a safe handle.
        /// </summary>
        private class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            // Create a SafeHandle, informing the base class
            // that this SafeHandle instance "owns" the handle,
            // and therefore SafeHandle should call
            // our ReleaseHandle method when the SafeHandle
            // is no longer in use
            private SafeProcessHandle() : base(true)
            {
            }

            protected override bool ReleaseHandle()
            {
                return CloseHandle(handle);
            }

            [SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "Class name is NativeMethodsShared for increased clarity")]
            [DllImport("KERNEL32.DLL")]
            private static extern bool CloseHandle(IntPtr hObject);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr ExitStatus;
            public IntPtr PebBaseAddress;
            public IntPtr AffinityMask;
            public IntPtr BasePriority;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;

            public int Size
            {
                get
                {
                    return (6 * IntPtr.Size);
                }
            }
        };

        /// <summary>
        /// Contains information about a file or directory; used by GetFileAttributesEx.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WIN32_FILE_ATTRIBUTE_DATA
        {
            private int fileAttributes;
            private uint ftCreationTimeLow;
            private uint ftCreationTimeHigh;
            private uint ftLastAccessTimeLow;
            private uint ftLastAccessTimeHigh;
            private uint ftLastWriteTimeLow;
            private uint ftLastWriteTimeHigh;
            private uint fileSizeHigh;
            private uint fileSizeLow;
        }

        private class SystemInformationData
        {
            /// <summary>
            /// Architecture as far as the current process is concerned.
            /// It's x86 in wow64 (native architecture is x64 in that case).
            /// Otherwise it's the same as the native architecture.
            /// </summary>
            public readonly ProcessorArchitectures ProcessorArchitectureType;

            /// <summary>
            /// Actual architecture of the system.
            /// </summary>
            public readonly ProcessorArchitectures ProcessorArchitectureTypeNative;

            /// <summary>
            /// Convert SYSTEM_INFO architecture values to the private enum
            /// </summary>
            /// <param name="arch"></param>
            /// <returns></returns>
            private static ProcessorArchitectures ConvertSystemArchitecture(ushort arch)
            {
                switch (arch)
                {
                    case PROCESSOR_ARCHITECTURE_INTEL:
                        return ProcessorArchitectures.X86;
                    case PROCESSOR_ARCHITECTURE_AMD64:
                        return ProcessorArchitectures.X64;
                    case PROCESSOR_ARCHITECTURE_ARM:
                        return ProcessorArchitectures.ARM;
                    case PROCESSOR_ARCHITECTURE_IA64:
                        return ProcessorArchitectures.IA64;
                    default:
                        return ProcessorArchitectures.Unknown;
                }
            }

            /// <summary>
            /// Read system info values
            /// </summary>
            internal SystemInformationData()
            {
                ProcessorArchitectureType = ProcessorArchitectures.Unknown;
                ProcessorArchitectureTypeNative = ProcessorArchitectures.Unknown;

                if (IsWindows)
                {
                    var systemInfo = new SYSTEM_INFO();

                    GetSystemInfo(ref systemInfo);
                    ProcessorArchitectureType = ConvertSystemArchitecture(systemInfo.wProcessorArchitecture);

                    GetNativeSystemInfo(ref systemInfo);
                    ProcessorArchitectureTypeNative = ConvertSystemArchitecture(systemInfo.wProcessorArchitecture);
                }
                else
                {
                    try
                    {
                        // On Unix run 'uname -m' to get the architecture. It's common for Linux and Mac
                        using (
                            var proc =
                                Process.Start(
                                    new ProcessStartInfo("uname")
                                    {
                                        Arguments = "-m",
                                        UseShellExecute = false,
                                        RedirectStandardOutput = true,
                                        CreateNoWindow = true
                                    }))
                        {
                            string arch = null;
                            if (proc != null)
                            {
                                // Since uname -m simply returns kernel property, it should be quick.
                                // 1 second is the best guess for a safe timeout.
                                proc.WaitForExit(1000);
                                arch = proc.StandardOutput.ReadLine();
                            }

                            if (!string.IsNullOrEmpty(arch))
                            {
                                if (arch.StartsWith("x86_64", StringComparison.OrdinalIgnoreCase))
                                {
                                    ProcessorArchitectureType = ProcessorArchitectures.X64;
                                }
                                else if (arch.StartsWith("ia64", StringComparison.OrdinalIgnoreCase))
                                {
                                    ProcessorArchitectureType = ProcessorArchitectures.IA64;
                                }
                                else if (arch.StartsWith("arm", StringComparison.OrdinalIgnoreCase))
                                {
                                    ProcessorArchitectureType = ProcessorArchitectures.ARM;
                                }
                                else if (arch.StartsWith("i", StringComparison.OrdinalIgnoreCase)
                                         && arch.EndsWith("86", StringComparison.OrdinalIgnoreCase))
                                {
                                    ProcessorArchitectureType = ProcessorArchitectures.X86;
                                }
                            }
                        }
                    }
                    catch
                    {
                        ProcessorArchitectureType = ProcessorArchitectures.Unknown;
                    }

                    ProcessorArchitectureTypeNative = ProcessorArchitectureType;
                }
            }
        }

        #endregion

        #region Member data

        /// <summary>
        /// Gets a flag indicating if we are running under a Unix-like system (Mac, Linux, etc.)
        /// </summary>
        private static bool IsUnixLike
        {
            get
            {
                return IsLinux || IsOSX;
            }
        }

        /// <summary>
        /// Gets a flag indicating if we are running under Linux
        /// </summary>
        private static bool IsLinux
        {
            get
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            }
        }

        /// <summary>
        /// Gets a flag indicating if we are running under some version of Windows
        /// </summary>
        internal static bool IsWindows
        {
            get
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            }
        }

        /// <summary>
        /// Gets a flag indicating if we are running under Mac OSX
        /// </summary>
        private static bool IsOSX
        {
            get
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            }
        }

        #endregion

        #region Wrapper methods

        [SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "Class name is NativeMethodsShared for increased clarity")]
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern void GetSystemInfo(ref SYSTEM_INFO lpSystemInfo);

        [SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "Class name is NativeMethodsShared for increased clarity")]
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern void GetNativeSystemInfo(ref SYSTEM_INFO lpSystemInfo);

        /// <summary>
        /// Kills the specified process by id and all of its children recursively.
        /// </summary>
        internal static void KillTree(int processIdToKill)
        {
            // Note that GetProcessById does *NOT* privately hold on to the process handle.
            // Only when you create the process using the Process object
            // does the Process object retain the original handle.

            Process thisProcess = null;
            try
            {
                thisProcess = Process.GetProcessById(processIdToKill);
            }
            catch (ArgumentException)
            {
                // The process has already died for some reason.  So shrug and assume that any child processes
                // have all also either died or are in the process of doing so.
                return;
            }

            try
            {
                DateTime myStartTime = thisProcess.StartTime;

                // Grab the process handle.  We want to keep this open for the duration of the function so that
                // it cannot be reused while we are running.
                SafeProcessHandle hProcess = OpenProcess(eDesiredAccess.PROCESS_QUERY_INFORMATION, false, processIdToKill);
                if (hProcess.IsInvalid)
                {
                    return;
                }

                try
                {
                    try
                    {
                        // Kill this process, so that no further children can be created.
                        thisProcess.Kill();
                    }
                    catch (Win32Exception e)
                    {
                        // Access denied is potentially expected -- it happens when the process that
                        // we're attempting to kill is already dead.  So just ignore in that case.
                        if (e.NativeErrorCode != ERROR_ACCESS_DENIED)
                        {
                            throw;
                        }
                    }

                    // Now enumerate our children.  Children of this process are any process which has this process id as its parent
                    // and which also started after this process did.
                    List<KeyValuePair<int, SafeProcessHandle>> children = GetChildProcessIds(processIdToKill, myStartTime);

                    try
                    {
                        foreach (KeyValuePair<int, SafeProcessHandle> childProcessInfo in children)
                        {
                            KillTree(childProcessInfo.Key);
                        }
                    }
                    finally
                    {
                        foreach (KeyValuePair<int, SafeProcessHandle> childProcessInfo in children)
                        {
                            childProcessInfo.Value.Dispose();
                        }
                    }
                }
                finally
                {
                    // Release the handle.  After this point no more children of this process exist and this process has also exited.
                    hProcess.Dispose();
                }
            }
            finally
            {
                thisProcess.Dispose();
            }
        }

        /// <summary>
        /// Returns the parent process id for the specified process.
        /// Returns zero if it cannot be gotten for some reason.
        /// </summary>
        private static int GetParentProcessId(int processId)
        {
            int ParentID = 0;
#if !CLR2COMPATIBILITY
            if (IsUnixLike)
            {
                string line = null;

                try
                {
                    // /proc/<processID>/stat returns a bunch of space separated fields. Get that string
                    using (var fileStream = File.OpenRead("/proc/" + processId + "/stat"))
                    using (var r = new StreamReader(fileStream))
                        //  using (var r = FileUtilities.OpenRead("/proc/" + processId + "/stat"))
                    {
                        line = r.ReadLine();
                    }
                }
                catch // Ignore errors since the process may have terminated
                {
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    // One of the fields is the process name. It may contain any characters, but since it's
                    // in parenthesis, we can finds its end by looking for the last parenthesis. After that,
                    // there comes a space, then the second fields separated by a space is the parent id.
                    string[] statFields = line.Substring(line.LastIndexOf(')')).Split(new[] { ' ' }, 4);
                    if (statFields.Length >= 3)
                    {
                        ParentID = Int32.Parse(statFields[2]);
                    }
                }
            }
            else
#endif
            {
                SafeProcessHandle hProcess = OpenProcess(eDesiredAccess.PROCESS_QUERY_INFORMATION, false, processId);

                if (!hProcess.IsInvalid)
                {
                    try
                    {
                        // UNDONE: NtQueryInformationProcess will fail if we are not elevated and other process is. Advice is to change to use ToolHelp32 API's
                        // For now just return zero and worst case we will not kill some children.
                        PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();
                        int pSize = 0;

                        if (0 == NtQueryInformationProcess(hProcess, PROCESSINFOCLASS.ProcessBasicInformation, ref pbi, pbi.Size, ref pSize))
                        {
                            ParentID = (int) pbi.InheritedFromUniqueProcessId;
                        }
                    }
                    finally
                    {
                        hProcess.Dispose();
                    }
                }
            }

            return (ParentID);
        }

        /// <summary>
        /// Returns an array of all the immediate child processes by id.
        /// NOTE: The IntPtr in the tuple is the handle of the child process.  CloseHandle MUST be called on this.
        /// </summary>
        private static List<KeyValuePair<int, SafeProcessHandle>> GetChildProcessIds(int parentProcessId, DateTime parentStartTime)
        {
            List<KeyValuePair<int, SafeProcessHandle>> myChildren = new List<KeyValuePair<int, SafeProcessHandle>>();

            foreach (Process possibleChildProcess in Process.GetProcesses())
            {
                using (possibleChildProcess)
                {
                    // Hold the child process handle open so that children cannot die and restart with a different parent after we've started looking at it.
                    // This way, any handle we pass back is guaranteed to be one of our actual children.
                    SafeProcessHandle childHandle = OpenProcess(eDesiredAccess.PROCESS_QUERY_INFORMATION, false, possibleChildProcess.Id);
                    if (childHandle.IsInvalid)
                    {
                        continue;
                    }

                    bool keepHandle = false;
                    try
                    {
                        if (possibleChildProcess.StartTime > parentStartTime)
                        {
                            int childParentProcessId = GetParentProcessId(possibleChildProcess.Id);
                            if (childParentProcessId != 0)
                            {
                                if (parentProcessId == childParentProcessId)
                                {
                                    // Add this one
                                    myChildren.Add(new KeyValuePair<int, SafeProcessHandle>(possibleChildProcess.Id, childHandle));
                                    keepHandle = true;
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (!keepHandle)
                        {
                            childHandle.Dispose();
                        }
                    }
                }
            }

            return myChildren;
        }

        #endregion

        #region PInvoke

        [SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "Class name is NativeMethodsShared for increased clarity")]
        [DllImport("KERNEL32.DLL")]
        private static extern SafeProcessHandle OpenProcess(eDesiredAccess dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        [SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "Class name is NativeMethodsShared for increased clarity")]
        [DllImport("NTDLL.DLL")]
        private static extern int NtQueryInformationProcess(SafeProcessHandle hProcess, PROCESSINFOCLASS pic, ref PROCESS_BASIC_INFORMATION pbi, int cb, ref int pSize);

        #endregion

        #region Extensions

        #endregion
    }
}
