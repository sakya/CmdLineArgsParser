using CmdLineArgsParser;
using Newtonsoft.Json;

namespace Demo
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string[] testArgs =
            {
                "backup",
                "--input", "/home/user/file",
                "-yw",
                "-r", "5"
            };

            var parser = new Parser(new ParserSettings());
            parser.ShowUsage<Options>();

            var options = parser.Parse<Options>(testArgs, out var errors);
            if (errors.Count > 0) {
                Console.WriteLine();
                Console.WriteLine("Errors:");
                foreach (var error in errors) {
                    Console.WriteLine(error.Message);
                }
            }

            Console.WriteLine();
            Console.WriteLine(JsonConvert.SerializeObject(options, Formatting.Indented));
        }
    }
}
