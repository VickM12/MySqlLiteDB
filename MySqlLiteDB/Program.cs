using MySqlLiteDB.Engine;

class Program
{
    static void Main()
    {
        Console.WriteLine("MiniDB ready. Type your SQL:");

        var engine = new MiniDbEngine();

        while (true)
        {
            Console.Write("mdb> ");
            string input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input.ToLower() == "exit") break;

            engine.Execute(input);
        }
    }
}