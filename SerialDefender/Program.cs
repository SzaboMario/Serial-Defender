using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Threading;
namespace SerialDefender
{
    class Program
    {
        static List<string> list = new List<string>();
        static List<string> exceptList = new List<string>();
        static DateTime now, next;
        static string old_report = "old_report.txt";
        static StreamWriter sw;
        static StreamReader sr;
        static Thread time, p1;
        static void Main(string[] args)
        {
            CPUInfo();
            IPInfo();
            Drvinfo();
            VGAinfo();
            MakeResultFile();
            time = new Thread(new ThreadStart(T1));
            time.Start();
            p1 = new Thread(new ThreadStart(Prog));
            p1.Start();
        }
        static void T1()
        {
            while (true)
            {
                //Console.WriteLine($"Serial Defender.");
                Console.WriteLine($"Most: {DateTime.Now}\nKövetkezo vizsgalat: {next}");
                Thread.Sleep(1000);
                Console.Clear();
            }
        }
        static void Prog()
        {
            now = DateTime.Now;
            next = now.AddSeconds(1);
            while (true)
            {
                list.Clear();
                now = DateTime.Now;
                if (now >= next)
                {
                    next = now.AddHours(1);
                    //next = now.AddSeconds(10);
                    Console.WriteLine("OK");
                    CPUInfo();
                    IPInfo();
                    Drvinfo();
                    VGAinfo();
                    MakeResultFile();
                }
                Thread.Sleep(1000);
            }
        }
        static bool FindOldReport()//1.
        {
            if (File.Exists(old_report))
            {
                return true;
            }
            return false;
        }
        static void MakeResultFile()
        {
            if (FindOldReport())
            {
                sw = new StreamWriter($"Valtozasok_listaja.txt", true);
                sw.WriteLine($"{now} Vizsgálat:");
                if (!CompareData())
                {
                    sw.WriteLine($"Eltérések a régi loghoz képest: {now}");
                    foreach (string item in exceptList)
                    {
                        sw.WriteLine(item);
                    }
                    sw.WriteLine("old_report fájl frissítve az új konfigra.\n");
                    MakeReportFile();
                }
                else
                {
                    sw.WriteLine("Nincs eltérés a legutóbbi vizsgálat óta.\n");
                }
                sw.Flush();
                sw.Close();
            }
            else
            {
                MakeReportFile();
            }
        }
        static void MakeReportFile()
        {
            StreamWriter sw2 = new StreamWriter(old_report);
            foreach (string i in list)
            {
                sw2.WriteLine(i);
            }
            sw2.Flush();
            sw2.Close();
        }
        static bool CompareData()
        {
            List<string> oldList = new List<string>();
            sr = new StreamReader(old_report);
            exceptList.Clear();
            while (!sr.EndOfStream)
            {
                oldList.Add(sr.ReadLine());
            }
            for (int i = 0; i < list.Count; i++)
            {
                if (!oldList.Contains(list[i]))
                {
                    exceptList.Add($"A most vizsgált eszköz nem található a old_report.txt-ben:\n{list[i]}");
                }
            }
            for (int i = 0; i < oldList.Count; i++)
            {
                if (!list.Contains(oldList[i]))
                {
                    exceptList.Add($"Old_report.txt-ben található eszköz nem található a most vizsgált eszközök között:\n{oldList[i]}");
                }
            }
            sr.Close();
            if (exceptList.Count > 0)
            {
                return false;
            }
            return true;
        }
        private static void w(string data)
        {
            Console.WriteLine(data);
        }
        private static void CPUInfo()
        {
            ManagementClass mgr = new ManagementClass("Win32_Processor");
            ManagementObjectCollection myManagementCollection = mgr.GetInstances();
            PropertyDataCollection myProperties = mgr.Properties;
            list.Add("----CPU:----");
            string name = "", serial = "";
            foreach (var i in myManagementCollection)
            {
                foreach (var it in myProperties)
                {
                    if (it.Name == "Name")
                    {
                        name = i.Properties[it.Name].Value.ToString();
                    }
                    if (it.Name == "ProcessorId")
                    {
                        serial = i.Properties[it.Name].Value.ToString();
                    }
                }
                list.Add($"Name: {name}, Serial: {serial}");
            }
            list.Add("------------");
        }
        private static void IPInfo()
        {
            using (var searcher = new ManagementObjectSearcher("select * from Win32_NetworkAdapter"))
            {
                list.Add("----Network:----");
                string name, serial;
                foreach (ManagementObject obj in searcher.Get())
                {
                    name = obj["Name"].ToString();
                    serial = ("DeviceID: " + obj["PNPDeviceID"]).Split('\\').Last();
                    if (serial.Contains("&"))
                    {
                        list.Add($"Name: {name}, Serial: {serial}");
                    }
                }
                list.Add("------------");
            }
        }
        private static void Drvinfo()
        {
            DriveInfo[] driveArr = DriveInfo.GetDrives();
            list.Add("---Drives:--");
            var index = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDiskToPartition").Get().Cast<ManagementObject>();
            var disks = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive").Get().Cast<ManagementObject>();
            foreach (DriveInfo item in driveArr)
            {
                string serial = "";
                var drive = (from i in index where i["Dependent"].ToString().Contains(item.Name.Split('\\').First()) select i).FirstOrDefault();
                if (drive != null)
                {
                    var key = drive["Antecedent"].ToString().Split('#')[1].Split(',')[0];
                    if (key != null)
                    {
                        var disk = (from fd in disks
                                    where
                                        fd["Name"].ToString() == "\\\\.\\PHYSICALDRIVE" + key
                                    select fd).FirstOrDefault();
                        serial = disk["PNPDeviceID"].ToString().Split('\\').Last();
                    }
                }
                if (serial != "")
                {
                    list.Add($"Drive name: {item.Name.Split('\\').First()}, serial: {serial}");
                }
            }
            list.Add("------------");
        }
        private static void VGAinfo()
        {
            using (var searcher = new ManagementObjectSearcher("select * from Win32_VideoController"))
            {
                list.Add("----VGA:----");
                string name, serial;
                foreach (ManagementObject obj in searcher.Get())
                {
                    name = "Name  -  " + obj["Name"];
                    serial = ("DeviceID  -  " + obj["PNPDeviceID"]).Split('\\').Last();
                    list.Add($"Name: {name}, Serial: {serial}");
                }
                list.Add("------------");
            }
        }
    }
}
