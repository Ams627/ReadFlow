using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace ReadFlow
{
    using System.Runtime;

    internal class FRecord
    {
        public uint Origin { get; set; }
        public uint Destination { get; set; }
        public ushort Route { get; set; }
        public ushort EndDate { get; set; }
        public ushort StartDate { get; set; }
        public ushort Toc { get; set; }

        /// <summary>
        ///  top bit (bit 15) is usage code - one = G, zero = A, bit 14 is direction (1 = S, 0 = R),
        /// bits 13 and 12 are CLI. Bits 11 and 10 are ns_disc_ind 
        /// </summary>
        public uint FlowId { get; set; }

        public FRecord(string line)
        {
            if (line.Substring(0, 2) != "RF")
            {
                throw new Exception($"Line must start with RF");
            }
            Origin = RjisParser.GetNlc(line, 2);
            Destination = RjisParser.GetNlc(line, 6);
            Route = (ushort)RjisParser.GetInt(line, 10, 5);
            EndDate = RjisDate.Parse(line, 20).Serial;
            StartDate = RjisDate.Parse(line, 28).Serial;
            Toc = RjisParser.GetBase36(line, 36, 3);
            FlowId = (uint)RjisParser.GetInt(line, 42, 7);
        }
    }

    internal class Program
    {
        private static void CollectPrint()
        {
            //            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, blocking: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, blocking: true);
            var totalmemory = GC.GetTotalMemory(true);
            Console.WriteLine($"total memory used is {totalmemory:n0}");
        }

        private static void Main(string[] args)
        {
            try
            {
                var frecordList = new List<FRecord>(700_000);
                var flowIdToFare = new Dictionary<uint, List<ulong>>(6_000_000);
                var keyToFare = new Dictionary<ulong, ulong>();

                var filename = @"s:\rjfaf782.ffl";
                foreach (var line in File.ReadLines(filename).Where(x=>x.Length > 2 && x[0] != '/'))
                {
                    if (line[1] == 'F')
                    {
                        frecordList.Add(new FRecord(line));
                    }
                    else if (line[1] == 'T')
                    {
                        var (rjisKey, flowid, value) = RjisParser.GetKeysValue(line);
                        flowIdToFare.AddEntryToList(flowid, value);
                        keyToFare.Add(rjisKey, value);
                    }
                }
                Console.WriteLine($"keyToFare count: {keyToFare.Count()}");
                Console.WriteLine($"flowIdToFare count: {flowIdToFare.Count()}");
                flowIdToFare.Clear();
                flowIdToFare = null;
                CollectPrint();
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                var codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                var progname = Path.GetFileNameWithoutExtension(codeBase);
                Console.Error.WriteLine(progname + ": Error: " + ex.Message);
            }

        }
    }
}
