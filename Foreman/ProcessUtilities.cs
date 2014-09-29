using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

/// <summary>
/// A utility class to determine a process parent.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ProcessUtilities
{
    // These members must match PROCESS_BASIC_INFORMATION
    internal IntPtr Reserved1;
    internal IntPtr PebBaseAddress;
    internal IntPtr Reserved2_0;
    internal IntPtr Reserved2_1;
    internal IntPtr UniqueProcessId;
    internal IntPtr InheritedFromUniqueProcessId;

    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ProcessUtilities processInformation, int processInformationLength, out int returnLength);

    /// <summary>
    /// Gets the parent process of the current process.
    /// </summary>
    /// <returns>An instance of the Process class.</returns>
    public static Process GetParentProcess()
    {
        return GetParentProcess(Process.GetCurrentProcess().Handle);
    }

    /// <summary>
    /// Gets the parent process of specified process.
    /// </summary>
    /// <param name="id">The process id.</param>
    /// <returns>An instance of the Process class.</returns>
    public static Process GetParentProcess(int id)
    {
        Process process = Process.GetProcessById(id);
        return GetParentProcess(process.Handle);
    }

    /// <summary>
    /// Gets the parent process of a specified process.
    /// </summary>
    /// <param name="handle">The process handle.</param>
    /// <returns>An instance of the Process class.</returns>
    public static Process GetParentProcess(IntPtr handle)
    {
        ProcessUtilities pbi = new ProcessUtilities();
        int returnLength;
        int status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out returnLength);
        if (status != 0)
            throw new Win32Exception(status);

        try
        {
            return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
        }
        catch (ArgumentException)
        {
            // not found
            return null;
        }
    }

    /// <summary>
    /// Gets a Dictionary of process IDs by their parent processID
    /// </summary>
    public static Dictionary<int, List<int>> PidsByParent()
    {
        var pidsByParent = new Dictionary<int, List<int>>();

        foreach (var objProcess in Process.GetProcesses())
        {
            try
            {
                var intChildPid = objProcess.Id;
                var intParentPid = GetParentProcess(intChildPid).Id;

                if (!pidsByParent.ContainsKey(intParentPid))
                {
                    pidsByParent[intParentPid] = new List<int>();
                }

                pidsByParent[intParentPid].Add(intChildPid);
            }
            catch
            {
            }
        }

        
        return (pidsByParent);
    }

    /// <summary>
    /// Kills the process and any child processes.
    /// </summary>
    /// <param name="pid">The pid.</param>
    public static void KillProcessTree(int pid)
    {
        var mappings = PidsByParent();
        KillPid(mappings, pid);
    }


    private static void KillPid(IDictionary<int, List<int>> mappings, int pid)
    {
        if (mappings.ContainsKey(pid))
        {
            //Kill Child Processes first
            foreach (var childPid in mappings[pid])
            {
                Debug.WriteLine("killing {0}", childPid);
                KillPid(mappings, childPid);
            }
        }

        try
        {
            Process.GetProcessById(pid).Kill();
        }
        catch (Exception ex)
        {
            Debug.WriteLine("error killing");
            Debug.WriteLine(ex.ToString());
        }
    }
}