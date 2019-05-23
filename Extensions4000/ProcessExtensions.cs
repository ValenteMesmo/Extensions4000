using System.ComponentModel;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
    public static class ProcessExtensions
    {
        /// <summary>
        /// Add the process to be tracked. If our current process is killed, the child processes
        /// that we are tracking will be automatically killed, too. If the child process terminates
        /// first, that's fine, too.</summary>
        /// <param name="process"></param>
        public static void AttachToCurrentProcess(this Process process)
        {
            if (s_jobHandle != IntPtr.Zero)
            {
                bool success = AssignProcessToJobObject(s_jobHandle, process.Handle);
                if (!success && !process.HasExited)
                    throw new Win32Exception();
            }
        }

        static ProcessExtensions()
        {
            // This feature requires Windows 8 or later. To support Windows 7 requires
            //  registry settings to be added if you are using Visual Studio plus an
            //  app.manifest change.
            //  https://stackoverflow.com/a/4232259/386091
            //  https://stackoverflow.com/a/9507862/386091
            if (Environment.OSVersion.Version < new Version(6, 2))
                return;

            // The job name is optional (and can be null) but it helps with diagnostics.
            //  If it's not null, it has to be unique. Use SysInternals' Handle command-line
            //  utility: handle -a ChildProcessTracker
            string jobName = "ChildProcessTracker" + Process.GetCurrentProcess().Id;
            s_jobHandle = CreateJobObject(IntPtr.Zero, jobName);

            var info = new JOBOBJECT_BASIC_LIMIT_INFORMATION();

            // This is the key flag. When our process is killed, Windows will automatically
            //  close the job handle, and when that happens, we want the child processes to
            //  be killed, too.
            info.LimitFlags = JOBOBJECTLIMIT.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

            var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
            extendedInfo.BasicLimitInformation = info;

            int length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
            IntPtr extendedInfoPtr = Marshal.AllocHGlobal(length);
            try
            {
                Marshal.StructureToPtr(extendedInfo, extendedInfoPtr, false);

                if (!SetInformationJobObject(s_jobHandle, JobObjectInfoType.ExtendedLimitInformation,
                    extendedInfoPtr, (uint)length))
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                Marshal.FreeHGlobal(extendedInfoPtr);
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string name);

        [DllImport("kernel32.dll")]
        static extern bool SetInformationJobObject(IntPtr job, JobObjectInfoType infoType,
            IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

        // Windows will automatically close any open job handles when our process terminates.
        //  This can be verified by using SysInternals' Handle utility. When the job handle
        //  is closed, the child processes will be killed.
        private static readonly IntPtr s_jobHandle;
    }

    internal enum JobObjectInfoType
    {
        AssociateCompletionPortInformation = 7,
        BasicLimitInformation = 2,
        BasicUIRestrictions = 4,
        EndOfJobTimeInformation = 6,
        ExtendedLimitInformation = 9,
        SecurityLimitInformation = 5,
        GroupInformation = 11
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        internal Int64 PerProcessUserTimeLimit;
        internal Int64 PerJobUserTimeLimit;
        internal JOBOBJECTLIMIT LimitFlags;
        internal UIntPtr MinimumWorkingSetSize;
        internal UIntPtr MaximumWorkingSetSize;
        internal UInt32 ActiveProcessLimit;
        internal Int64 Affinity;
        internal UInt32 PriorityClass;
        internal UInt32 SchedulingClass;
    }

    [Flags]
    internal enum JOBOBJECTLIMIT : uint
    {
        JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IO_COUNTERS
    {
        internal UInt64 ReadOperationCount;
        internal UInt64 WriteOperationCount;
        internal UInt64 OtherOperationCount;
        internal UInt64 ReadTransferCount;
        internal UInt64 WriteTransferCount;
        internal UInt64 OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        internal JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        internal IO_COUNTERS IoInfo;
        internal UIntPtr ProcessMemoryLimit;
        internal UIntPtr JobMemoryLimit;
        internal UIntPtr PeakProcessMemoryUsed;
        internal UIntPtr PeakJobMemoryUsed;
    }
}
