﻿using System.IO;
using Microsoft.Win32;
using System;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;

namespace YAMS
{
    public static class Util
    {

        private static string strJRERegKey = "SOFTWARE\\JavaSoft\\Java Runtime Environment";
        private static string strJDKRegKey = "SOFTWARE\\JavaSoft\\Java Development Kit";
        private static string strJRERegKey32on64 = "SOFTWARE\\Wow6432Node\\JavaSoft\\Java Runtime Environment";
        private static string strJDKRegKey32on64 = "SOFTWARE\\Wow6432Node\\JavaSoft\\Java Development Kit";

        /// <summary>
        /// Detects if the JRE is installed using the regkey
        /// </summary>
        /// <returns>boolean indicating if the JRE is installed</returns>
        public static bool HasJRE()
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey subKey = rk.OpenSubKey(strJRERegKey);
                if (subKey != null) return true;
                else
                {
                    //We need to check if they're running 32-bit Java on a 64-bit OS
                    subKey = rk.OpenSubKey(strJRERegKey32on64);
                    if (subKey != null) return true;
                    else return false;
                } 
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Detects if the JDK is installed using the regkey
        /// </summary>
        /// <returns>boolean indicating if the JDK is installed</returns>
        public static bool HasJDK()
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey subKey = rk.OpenSubKey(strJDKRegKey);
                if (subKey != null) return true;
                else
                {
                    //We need to check if they're running 32-bit Java on a 64-bit OS
                    subKey = rk.OpenSubKey(strJDKRegKey32on64);
                    if (subKey != null) return true;
                    else return false;
                }
            }
            catch
            {
                return false;
            }
        
        }

        /// <summary>
        /// Looks for the Minecraft client in the system user's profile, the service runs as LOCAL SYSTEM, so for
        /// some of the third party apps we need to see if it is in this profile too
        /// </summary>
        /// <returns>boolean indicating if the minecraft client is in the SYSTEM account's AppData</returns>
        public static bool HasMCClientSystem()
        {
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"config\systemprofile\AppData\Roaming\.minecraft\bin\minecraft.jar"))) return true;
            else return false;
        }
        /// <summary>
        /// Checks if the Minecraft client is installed locally
        /// </summary>
        /// <returns>boolean indicating if the Minecraft jar is in the local user's AppData</returns>
        public static bool HasMCClientLocal()
        {
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @".minecraft\bin\minecraft.jar"))) return true;
            else return false;
        }
        public static void CopyMCClient()
        {
            Copy(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @".minecraft\"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"config\systemprofile\AppData\Roaming\.minecraft\"));
        }

        //Get the Java version from the registry
        public static string JavaVersion(string strType = "jre")
        {
            string strKey = "";
            string strKey32on64 = "";
            switch (strType)
            {
                case "jre":
                    strKey = strJRERegKey;
                    strKey32on64 = strJRERegKey32on64;
                    break;
                case "jdk":
                    strKey = strJDKRegKey;
                    strKey32on64 = strJDKRegKey32on64;
                    break;
            }
            RegistryKey rk = Registry.LocalMachine;
            RegistryKey subKey = rk.OpenSubKey(strKey);
            if (subKey != null) return subKey.GetValue("CurrentVersion").ToString();
            else
            {
                subKey = rk.OpenSubKey(strKey32on64);
                if (subKey != null) return subKey.GetValue("CurrentVersion").ToString();
                else return "";
            };
        }

        //Calculate the path to the Java executable
        public static string JavaPath(string strType = "jre")
        {
            string strKey = "";
            string strKey32on64 = "";
            switch (strType)
            {
                case "jre":
                    strKey = strJRERegKey;
                    strKey32on64 = strJRERegKey32on64;
                    break;
                case "jdk":
                    strKey = strJDKRegKey;
                    strKey32on64 = strJDKRegKey32on64;
                    break;
            }
            strKey += "\\" + JavaVersion(strType);
            strKey32on64 += "\\" + JavaVersion(strType);
            RegistryKey rk = Registry.LocalMachine;
            RegistryKey subKey = rk.OpenSubKey(strKey);
            if (subKey != null) return subKey.GetValue("JavaHome").ToString() + "\\bin\\";
            else
            {
                subKey = rk.OpenSubKey(strKey32on64);
                if (subKey != null) return subKey.GetValue("JavaHome").ToString() + "\\bin\\";
                else return "";
            };
        }

        //Replaces file 1 with file 2
        public static bool ReplaceFile(string strFileOriginal, string strFileReplacement) {
            try
            {
                if (File.Exists(strFileReplacement))
                {
                    if (File.Exists(strFileOriginal)) File.Delete(strFileOriginal);
                    File.Move(strFileReplacement, strFileOriginal);
                }
                return true;
           }
            catch {
                YAMS.Database.AddLog("Unable to update " + strFileOriginal, "updater", "error");
                return false;
            }
        }

        //Initial Set-up for first run only
        public static void FirstRun()
        {
            //Grab latest server jar
            YAMS.AutoUpdate.UpdateIfNeeded(YAMS.AutoUpdate.strMCServerURL, YAMS.Core.RootFolder + @"\lib\minecraft_server.jar.UPDATE");

            //Set our MC Defaults in the DB
            var NewServer = new List<KeyValuePair<string, string>>();
            NewServer.Add(new KeyValuePair<string, string>("admin-slot", "true"));
            NewServer.Add(new KeyValuePair<string, string>("enable-health", "true"));
            NewServer.Add(new KeyValuePair<string, string>("hellworld", "false"));
            NewServer.Add(new KeyValuePair<string, string>("level-name", @"world"));
            NewServer.Add(new KeyValuePair<string, string>("max-players", "20"));
            NewServer.Add(new KeyValuePair<string, string>("motd", "Welcome to a YAMS server!"));
            NewServer.Add(new KeyValuePair<string, string>("online-mode", "true"));
            NewServer.Add(new KeyValuePair<string, string>("public", "false"));
            NewServer.Add(new KeyValuePair<string, string>("pvp", "true"));
            NewServer.Add(new KeyValuePair<string, string>("server-ip", ""));
            NewServer.Add(new KeyValuePair<string, string>("server-name", "My YAMS MC Server"));
            NewServer.Add(new KeyValuePair<string, string>("server-port", "25565"));
            NewServer.Add(new KeyValuePair<string, string>("spawn-animals", "true"));
            NewServer.Add(new KeyValuePair<string, string>("spawn-monsters", "true"));
            NewServer.Add(new KeyValuePair<string, string>("verify-names", "true"));
            NewServer.Add(new KeyValuePair<string, string>("white-list", "false"));
            Database.NewServer(NewServer, "My First YAMS Server");

            //Set our YAMS Defaults
            Database.SaveSetting("UpdateJAR", "true");
            Database.SaveSetting("UpdateSVC", "true");
            Database.SaveSetting("UpdateGUI", "true");
            Database.SaveSetting("UpdateWeb", "true");
            Database.SaveSetting("UpdateAddons", "true");
            Database.SaveSetting("RestartOnJarUpdate", "true");
            Database.SaveSetting("RestartOnSVCUpdate", "true");
            Database.SaveSetting("Memory", "1024");
            Database.SaveSetting("EnableJavaOptimisations", "true");
            Database.SaveSetting("AdminListenPort", "56552"); //Use an IANA legal internal port 49152 - 65535
            Database.SaveSetting("PublicListenPort", Convert.ToString(Networking.TcpPort.FindNextAvailablePort(80))); //Find nearest open port to 80 for public site
            Database.SaveSetting("ExternalIP", Networking.GetExternalIP().ToString());
            Database.SaveSetting("ListenIP", Networking.GetListenIP().ToString());
            Database.SaveSetting("UpdateBranch", "live");

            //Run an update now
            //AutoUpdate.CheckUpdates();

            //Tell the DB that we've run this
            Database.SaveSetting("FirstRun", "true");

        }

       //What is the bitness of the system
        public static string GetBitness()
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return "x86";
                case 8:
                    return "x64";
                default:
                    return "x86";
            }
        }

        //Convert Boolean to string
        public static string BooleanToString(bool bolInput)
        {
            if (bolInput) return "true";
            else return "false";
        }

        public static void Copy(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            // Check if the target directory exists, if not, create it.
            if (Directory.Exists(target.FullName) == false)
            {
                Directory.CreateDirectory(target.FullName);
            }

            // Copy each file into it's new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        public static void PhoneHome()
        {
            string datCheck = DateTime.Now.ToString("dd-MM-yyyy");
            if (Database.GetSetting("LastPhoneHome", "YAMS") != datCheck)
            {
                //Count online players
                int intPlayers = 0;
                foreach (KeyValuePair<int, MCServer> kvp in Core.Servers)
                {
                    intPlayers = intPlayers + Database.GetPlayerCount(kvp.Value.ServerID);
                }

                //Check .NET versions
                RegistryKey installed_versions = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP");
                string[] version_names = installed_versions.GetSubKeyNames();
                //version names start with 'v', eg, 'v3.5' which needs to be trimmed off before conversion
                double Framework = Convert.ToDouble(version_names[version_names.Length - 1].Remove(0, 1), CultureInfo.InvariantCulture);
                int SP = Convert.ToInt32(installed_versions.OpenSubKey(version_names[version_names.Length - 1]).GetValue("SP", 0));

                //Collect Data
                string strVars = "servers=" + Core.Servers.Count +
                                 "&players=" + intPlayers +
                                 "&overviewer=" + Database.GetSetting("OverviewerInstalled", "YAMS") +
                                 "&c10t=" + Database.GetSetting("C10tInstalled", "YAMS") +
                                 "&tectonicus=" + Database.GetSetting("TectonicusInstalled", "YAMS") +
                                 "&biomeextractor=" + Database.GetSetting("BiomeExtractorInstalled", "YAMS") +
                                 "&nbtoolkit=" + Database.GetSetting("NBToolkitInstalled", "YAMS") +
                                 "&bukkit=" + Database.GetSetting("BukkitInstalled", "YAMS") +
                                 "&updateapps=" + Database.GetSetting("UpdateAddons", "YAMS") +
                                 "&updatejar=" + Database.GetSetting("UpdateJAR", "YAMS") +
                                 "&updategui=" + Database.GetSetting("UpdateGUI", "YAMS") +
                                 "&updatesvc=" + Database.GetSetting("UpdateSVC", "YAMS") +
                                 "&updateweb=" + Database.GetSetting("UpdateWeb", "YAMS") +
                                 "&highestnet=" + Framework.ToString() +
                                 "&highestSP=" + SP.ToString();

                //Send info
                try
                {
                    HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("http://yams.in/phonehome.php?" + strVars);
                    request.Method = "GET";

                    //Grab the response
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    Database.AddLog("Phoned home with data: " + strVars);
                    Database.SaveSetting("LastPhoneHome", datCheck);
                }
                catch (System.Net.WebException ex)
                {
                    Database.AddLog("Couldn't phone home: " + ex.Message);
                }
            }
        
        }

        //Add a process id to our list
        public static void AddPID(int intID)
        {
            try
            {
                StreamWriter twPids = new StreamWriter(Core.RootFolder + "\\pids.txt", true);
                twPids.WriteLine(intID);
                twPids.Flush();
                twPids.Close();
            } catch (Exception e) {
                Database.AddLog("Adding PID: " + e.Message, "pids", "warn");
            }
        }

        //Remove a process ID if it exists
        public static void RemovePID(int intID)
        {
            try
            {
                if (!File.Exists(Core.RootFolder + "\\pids.txt")) return;
            
                //Read all pids into an array, except the one we want
                ArrayList lines = new ArrayList();
                StreamReader trPids = new StreamReader(Core.RootFolder + "\\pids.txt");
                string line;
                while ((line = trPids.ReadLine()) != null)
                {
                    if (line != intID.ToString()) lines.Add(line);
                }
                trPids.Close();

                //delete pids file and recreate
                File.Delete(Core.RootFolder + "\\pids.txt");
                StreamWriter twPids = new StreamWriter(Core.RootFolder + "\\pids.txt", true);
                foreach (string l in lines)
                {
                    twPids.WriteLine(l);
                }
                twPids.Flush();
                twPids.Close();
            }
            catch (Exception e)
            {
                Database.AddLog("Removing PID: " + e.Message, "pids", "warn");
            }
        }

        //Check if a port is available
        public static bool PortIsBusy(int port)
        {
            IPGlobalProperties ipGP = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] endpoints = ipGP.GetActiveTcpListeners();
            if (endpoints == null || endpoints.Length == 0) return false;
            for (int i = 0; i < endpoints.Length; i++)
                if (endpoints[i].Port == port)
                    return true;
            return false;
        }

        //Search a text file for a string
        public static bool SearchFile(string strFileName, string strSearchText)
        {
            StreamReader reader = new StreamReader(strFileName);
            String text = reader.ReadToEnd();
            reader.Close();

            if (Regex.IsMatch(text, strSearchText)) return true;
            else return false;
        }

        /// <summary>
        /// Updates the Dynamic yams.at DNS.
        /// </summary>
        public static void UpdateDNS()
        {
            string externalIP = Networking.GetExternalIP().ToString();
            if (externalIP != Database.GetSetting("LastExternalIP", "YAMS"))
            {
                //IP has changed since last time we checked so update the DNS
                string strVars = "action=update&domain=" + Database.GetSetting("DNSName", "YAMS") + "&secret=" + Database.GetSetting("DNSSecret", "YAMS") + "&ip=" + externalIP;
                try
                {
                    HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("http://yams.in/dns/?" + strVars);
                    request.Method = "GET";

                    //Grab the response
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    Database.AddLog("Updated Dynamic DNS");
                    Database.SaveSetting("LastExternalIP", externalIP);
                }
                catch (System.Net.WebException ex)
                {
                    Database.AddLog("Couldn't update DNS: " + ex.Message);
                }
            }
        }

        //Emulates VBScript's Left http://www.mgbrown.com/PermaLink68.aspx
        public static string Left(string text, int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException("length", length, "length must be > 0");
            else if (length == 0 || text.Length == 0)
                return "";
            else if (text.Length <= length)
                return text;
            else
                return text.Substring(0, length);
        }
    }
}
