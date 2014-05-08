using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using LibGit2Sharp;
using System.IO;
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
                var machinename = Environment.MachineName;
                if (rep.Branches[machinename] == null) //going to init commit and create branch for this machine, if not exists
                {
                    Console.WriteLine("New machine, creating branch for it");
                    rep.Checkout(rep.Branches["master"]);
                    rep.CreateBranch(machinename);
                }
                rep.Checkout(rep.Branches[machinename]);

                foreach (var Key in from str in File.ReadAllLines("./Parameters.txt") where !str.StartsWith("#") select str)
                {                
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from " + Key);
                    var data = new List<string>();
                    foreach(var vv in searcher.Get())
                        foreach (var vvv in vv.Properties)
                            data.Add(vvv.Name+" "+vvv.Value);
                    File.WriteAllLines(datadir + "/" + Key + ".txt", data.ToArray());                
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
