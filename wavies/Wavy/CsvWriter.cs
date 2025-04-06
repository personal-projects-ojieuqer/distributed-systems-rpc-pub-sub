namespace wavies.Wavy
{
    using System.Collections.Generic;
    using System.Linq;

    public static class CsvWriter
    {
        public static void Clear(string path)
        {
            File.WriteAllText(path, "Timestamp,SensorType,Value\n");
        }

        public static List<string> ReadDataLines(string path)
        {
            return File.Exists(path)
                ? File.ReadAllLines(path).Skip(1).ToList()
                : new List<string>();
        }
    }

}

