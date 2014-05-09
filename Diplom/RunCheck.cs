using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.IO;
using LibGit2Sharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RunCheck
{
    class RuncCheck
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Processing started");
            var datadir = "./data";
            
            if (!Repository.IsValid(datadir)) //if repository not exists we create it
            {
                Console.WriteLine("Empty git database, creating one");
                Repository.Init(datadir);
                var repo = (new Repository(datadir)).Commit(DateTime.Now.ToString());                
            }

            using (var rep = new Repository(datadir)) {
                var machinename = Environment.MachineName; //TODO: maybe some more unique identifier?
                if (rep.Branches[machinename] == null) //going to init commit and create branch for this machine, if not exists
                {
                    Console.WriteLine("New machine, creating branch for it");
                    rep.Checkout(rep.Branches["master"]);
                    rep.CreateBranch(machinename);
                }                
                rep.Checkout(rep.Branches[machinename]);
                foreach (var RegistryKey in JObject.Parse(File.ReadAllText("Parameters.json")))
                {                    
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from " + RegistryKey.Key);
                    var data = new List<string>();
                    if( RegistryKey.Value["fields"]!=null ) //if fields specified
                    {
                        foreach (var v in searcher.Get())
                            foreach (var p in v.Properties)
                                foreach (var o in RegistryKey.Value["fields"])
                                    if ( ((JProperty)o).Name == p.Name )
                                        data.Add(((JProperty)o).Value + " : " + p.Value);                        
                    }
                    else
                    {
                        foreach (var v in searcher.Get())
                            foreach (var p in v.Properties)
                                data.Add(p.Name + " : " + p.Value);
                    }

                    File.WriteAllLines(datadir + "/" + RegistryKey.Value["name"] + ".txt", data.ToArray());                
                }
                rep.Index.Stage("*");
                
                if (rep.Index.RetrieveStatus().Count()>0)
                {
                    Console.WriteLine("There is some changed data, committing it");
                    rep.Commit(DateTime.Now.ToString());
                }
                Console.WriteLine("Processint finished, Press any key to exit");
                Console.ReadKey();
            }
        }
    }
}
