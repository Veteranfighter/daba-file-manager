﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace DabaFilemanager
{
    class Program
    {

        private static string key = "";

        static void Main(string[] args)
        {
            Console.WriteLine("Enter key to proceed. Wrong keys will cause corrupted files.");
            key = Console.ReadLine();
            Console.Clear();

            ConnectMySQL();

            while (true)
            {
                var curCommand = Console.ReadLine();
                if (curCommand.StartsWith("uploadfile"))
                {
                    uploadfileB64(curCommand.Replace("uploadfile", ""));
                }
                else if (curCommand.StartsWith("downloadfile"))
                {
                    downloadfile(int.Parse(curCommand.Split(' ')[1]));
                }
                else if (curCommand.StartsWith("printfiles"))
                {
                    printfiles();
                }
                else if (curCommand.StartsWith("help"))
                {
                    Console.WriteLine("[-- Help --]");
                    Console.WriteLine("     --> uploadfile \"<file path>\" [filename] [file description]    | Upload file from the specified path");
                    Console.WriteLine("     --> printfiles                                                  | Print all stored files");
                    Console.WriteLine("     --> downloadfile <id> [path]                                    | Downloads the file with the specified id to the path");
                    Console.WriteLine("     --> register <username> <email> <password>                      | Registers a new user");
                    Console.WriteLine("     --> login <username> <password>                                 | Login to account");
                    Console.WriteLine("     --> help                                                        | Print this message");
                    Console.WriteLine("[----------]");
                }
                else if (curCommand.StartsWith("register"))
                {
                    RegisterUser(curCommand.Replace("register", ""));
                }
                else if (curCommand.StartsWith("login"))
                {
                    LoginUser(curCommand.Replace("login", ""));
                }

            }

        }

        private static MySqlConnection connection;


        private static void RegisterUser(string input)
        {

            // 0: username, 1: email, 2: password
            string[] args = input.Split(' ');

            if (args.Length < 4)
            {
                Console.WriteLine("[!] Error: Invalid argument count. Use help for correct syntax");
                return;
            }

            string sql = "SELECT * FROM user WHERE username='" + args[1] + "' OR email='" + args[2] + "'";
            var cmd = new MySqlCommand(sql, connection);

            MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                Console.WriteLine("[!] Error: A user with this name or email is already registered");
                rdr.Close();
                return;
            }
            rdr.Close();

            var insertSQL = "INSERT INTO user(username, password, email) VALUES(@username, @password, @email)";
            var command = new MySqlCommand(insertSQL, connection);

            command.Parameters.AddWithValue("@username", args[1]);
            command.Parameters.AddWithValue("@email", args[2]);
            command.Parameters.AddWithValue("@password", args[3]);
            command.Prepare();

            command.ExecuteNonQuery();

            Console.WriteLine("\n[:)] Successfully registered. Username: " + args[1]);

        }

        private static void LoginUser(string input)
        {
            // 0: username 1: password
            string[] args = input.Split(' ');

            if (args.Length < 3)
            {
                Console.WriteLine("[!] Error: Invalid argument count. Use help for correct syntax");
                return;
            }

            string sql = "SELECT * FROM user WHERE username='" + args[1] + "'";
            var cmd = new MySqlCommand(sql, connection);

            MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                if (rdr.GetString(2) == args[2] & rdr.GetString(1) == args[1])
                {
                    loggedUserId = rdr.GetInt16(0);
                    loggedUserName = rdr.GetString(1);
                    loggedUserEmail = rdr.GetString(3);

                    Console.WriteLine("[:)] Success. Your logged in as " + loggedUserName + ": " + loggedUserEmail + " (" + loggedUserId + ") ");
                    break;
                }
                rdr.Close();
                Console.WriteLine("[!] Error: Login failed. Wrong credentials.");
                return;
            }
            rdr.Close();


        }

        private static int loggedUserId = -1;
        private static string loggedUserName = "";
        private static string loggedUserEmail = "";

        private static void ConnectMySQL()
        {
            string cs = @"server=0.tcp.eu.ngrok.io;userid=lockedstorage;password=,Seb8B?4=f3b-BM;database=lockedstorage;port=19273";

            connection = new MySqlConnection(cs);
            try
            {
                connection.Open();
                Console.WriteLine($"MySQL Connected. Version : {connection.ServerVersion}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Connection could not be established: " + ex.Message);
            }




        }

        private static bool uploadfileB64(string args)
        {

            if (!args.Contains("\""))
            {
                Console.WriteLine("!--! Error: Invalid argument count!");
                return false;
            }

            if (args.Length == 0)
            {
                Console.WriteLine("!--! Error: Invalid argument count!");
                return false;
            }

            string filePath = "null";

            int pFrom = args.IndexOf("\"") + "\"".Length;
            int pTo = args.LastIndexOf("\"");

            filePath = args.Substring(pFrom, pTo - pFrom);

            string[] finalArguments = args.Replace("\"" + filePath + "\"", "").Split(' ');

            if (!File.Exists(filePath))
            {
                Console.WriteLine("!--! Error: Invalid file path: <" + filePath + ">. Specify valid file path between \"\"");
                return false;
            }

            var watch = new System.Diagnostics.Stopwatch();

            watch.Start();

            Console.WriteLine("[DEBUG] -- Converting..");
            Byte[] bytes = File.ReadAllBytes(filePath);
            String file = Convert.ToBase64String(bytes);
            file = Encryption.Encrypt(file, key);

            Console.WriteLine("[DEBUG] -- Converting done");

            string description = "";
            string name = finalArguments.Length >= 3 ? finalArguments[2] : "NoName";

            for (int i = 3; i < finalArguments.Length; i++)
            {
                description += finalArguments[i] + " ";
            }

            Console.WriteLine("[DEBUG] -- Uploading..");

            var sql = "INSERT INTO storage(filePath, file, fileName, fileDescription) VALUES(@filePath, @file, @fileName, @fileDescription)";
            var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@filePath", filePath);
            command.Parameters.AddWithValue("@file", file);
            command.Parameters.AddWithValue("@fileName", name);
            command.Parameters.AddWithValue("@fileDescription", description);
            command.Prepare();

            command.ExecuteNonQuery();

            Console.WriteLine("[DEBUG] -- Uploading done..");

            watch.Stop();
            Console.WriteLine("DONE! Took " + watch.ElapsedMilliseconds + "ms!");
            Console.WriteLine("[-----------]");
            Console.WriteLine("[ Path: " + filePath);
            Console.WriteLine("[ Size: " + (double)new FileInfo(filePath).Length / 1000000 + "mb");
            Console.WriteLine("[ File Name: " + name);
            Console.WriteLine("[ File Description: " + description);
            Console.WriteLine("[-----------]");

            return true;
        }

        private static void printfiles()
        {
            string sql = "SELECT * FROM storage";
            var cmd = new MySqlCommand(sql, connection);

            MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                Console.WriteLine("\n[-- ID: " + rdr.GetInt32(0) + " --]");
                Console.WriteLine("     >--  Path: " + rdr.GetString(1) + " ");
                Console.WriteLine("     >--  Name: " + rdr.GetString(3) + " ");
                Console.WriteLine("     >--  Description: " + rdr.GetString(4) + " ");
            }
            rdr.Close();
            Console.WriteLine("\n");
        }

        private static bool downloadfile(int id)
        {
            Console.WriteLine("Searching for files with ID: " + id);

            string sql = "SELECT * FROM storage WHERE fileID = " + id;
            var cmd = new MySqlCommand(sql, connection);

            MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                Console.WriteLine("ID: {0} Path: {1} Name: {2} Description: {3}", rdr.GetInt32(0), rdr.GetString(1), rdr.GetString(3), rdr.GetString(4));
                FileStream fileStream = File.Create(new FileInfo(rdr.GetString(1)).Name);
                Console.WriteLine("Saving to: " + fileStream.Name);
                fileStream.Close();
                try
                {
                    Byte[] bytes = Convert.FromBase64String(Encryption.Decrypt(rdr.GetString(2), key));
                    File.WriteAllBytes(rdr.GetString(1), bytes);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Invalid key. Downloaded file is corrupted");
                }

            }
            rdr.Close();

            return true;
        }
    }
}
