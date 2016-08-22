using System;
using System.Threading;
using System.IO;

namespace ChatServer
{
    class ServerMain
    {
        static void Main(string[] args)
        {
            Console.Title = "Milk Chat Server";
            //Setup options
            string[] parameters = new string[5];    //listeningPort, DB-IP, DB-port, maxClientNumber
            Protocol protocol;
            Setup(args, out parameters);
            if (parameters[4] == "web")
            {
                protocol = Protocol.Web;
            }
            else
            {
                protocol = Protocol.Tcp;
            }
            Console.WriteLine("\nMilk-Server set-up with the following parameters:");
            Console.WriteLine("\tListening port: \t" + parameters[0]);
            Console.WriteLine("\tGin-Server: \t\t" + parameters[1] + ":" + parameters[2]);
            Console.WriteLine("\tMaximum Clients: \t" + parameters[3]);
            Console.WriteLine("\tService Type: \t\t" + parameters[4] + "\n\n");

            //Create a thread which waits for a manual server termination command (escape key)
            Thread exitInputThread = new Thread(new ThreadStart(ExitInput));
            exitInputThread.Start();                
            Console.WriteLine("To Exit, Press ESCAPE.");
            Console.WriteLine("Otherwise, there's no proof for complete exit and sending FIN!\n");

            //Start the server and shutdown when over
            Server server = new Server(Int32.Parse(parameters[0]), parameters[1], Int32.Parse(parameters[2]), Int32.Parse(parameters[3]), protocol);
            server.Start();
            server.ShutDown();
        }

        //Code for a thread to detect the escape key for exiting the program
        private static void ExitInput()
        {
            ConsoleKeyInfo input;
            do
            {
                input = Console.ReadKey(false);
                
            } while (input.Key != ConsoleKey.Escape);

            Environment.Exit(0);
        }

        private static void Setup(string[] args, out string[] parameters)
        {
            parameters = new string[5];    //listeningPort, LS-IP, LoginServer-port, DB-IP, DB-port, maxClientNumber
            
            //Server setup process:
            // - Either get setup-values from command line OR config file (possibly default config file)
            // - Confirm all values obtained, otherwise set to hard-coded defaults
            if (args.Length >= 1)
            {
                switch (args[0])
                {
                    case "-help":
                        Console.WriteLine("For manual set-up, provide the following values:");
                        Console.WriteLine("\tChatServer listeningPort backEndIp backEndPort maxClientNum");
                        Console.WriteLine("To load from a custom config file, use the following:");
                        Console.WriteLine("\tChatServer -load filename.txt");
                        Console.WriteLine("The server will default to retrieving values from \"setup.txt\" and otherwise will inquire with the Console window as a last resort.");
                        return;
                    case "-load":
                        //Check for second-arg file name for filename override, then load data
                        string setupFileName;
                        if (args.Length >= 2)
                        {
                            setupFileName = args[1];
                        } else
                        {
                            setupFileName = "setup.txt";                                            //Default setup filename
                        }

                        var path = Path.Combine(Directory.GetCurrentDirectory(), setupFileName);    //Get the location of the config file

                        //Be sure to catch any file reading errors
                        try
                        {
                            args = File.ReadAllLines(path);                                         //Read the config file
                        }
                        catch (FileNotFoundException e)
                        {
                            Console.WriteLine("\nFileNotFoundException during " + setupFileName + " reading attempt.\n");
                            Console.WriteLine(e.HResult + " : " + e.Message);
                            Console.WriteLine("\nPlease ensure that " + setupFileName + " is contained within the application's directory.");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("\nException during " + setupFileName + " reading attempt.\n");
                            Console.WriteLine(e.HResult + " : " + e.Message);
                            Console.WriteLine("\nPlease ensure that " + setupFileName + " is contained within the application's directory.");
                        }
                        break;
                }
            }
            else                                                                                    //Default to reading the data from the default setup file (setup.txt)
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "setup.txt");              //Set the location of setup.txt

                //Be sure to catch any file reading errors
                try
                {
                    args = File.ReadAllLines(path);                                                 //Read the setup.txt
                }
                catch (FileNotFoundException e)
                {
                    Console.WriteLine("\nFileNotFoundException during setup.text reading attempt.\n");
                    Console.WriteLine(e.HResult + " : " + e.Message);
                    Console.WriteLine("\nPlease ensure that setup.text is contained within the application's directory.");
                }
                catch (Exception e)
                {
                    Console.WriteLine("\nException during setup.text reading attempt.\n");
                    Console.WriteLine(e.HResult + " : " + e.Message);
                    Console.WriteLine("\nPlease ensure that setup.text is contained within the application's directory.");
                }
            }

            //Load parameters from the command line arguments or what was obtained by a config file
            for (int i = 0; i < ((args.Length >= 5) ? 5 : args.Length); i++)
            {
                parameters[i] = args[i];
            }
            
            //Parameter Validation
            //Loop until all setup parameters are obtained an validated
            for (int index = 0; index < 5;index++)
            {
                if (parameters[index] == null || parameters[index] == "")
                {
                    //Set corresponding parameter to hard-coded defaults
                    switch (index)
                    {
                        case 0:
                            parameters[index] = "9000";
                            Console.WriteLine("No listening port found. Set to 9000.");
                            break;
                        case 1:
                            parameters[index] = "127.0.0.1";
                            Console.WriteLine("No back-end IP found. Set to 127.0.0.1.");
                            break;
                        case 2:
                            parameters[index] = "11000";
                            Console.WriteLine("No back-end port found. Set to 11000.");
                            break;
                        case 3:
                            parameters[index] = "1000";
                            Console.WriteLine("No max client number value found. Set to 1000.");
                            break;
                        case 4:
                            parameters[index] = "tcp";
                            Console.WriteLine("No protocol type found. Set to TCP.");
                            break;
                    }
                }
            }
        }
    }
}
