using Npgsql;
using System.Diagnostics;
using System.Reflection.PortableExecutable;

class Program
{
    static void Main(String[] args)
    {
        var cs = "Host=localhost;Port=5432;Database=kursachdb;Username=postgres;Password=alex83953458130";
        using NpgsqlConnection connection = new NpgsqlConnection(cs);

        connection.Open();

        Console.WriteLine("Connected successful...");
        Console.WriteLine("Type 'exit' to close the app");

        string command = "";

        while (command != "exit")
        {
            command = Console.ReadLine();

            switch (command[0].ToString())
            {
                case "0":
                    DeleteTable(connection);
                    break;
                case "1":
                    CreateTable(connection);
                    break;
                case "2":
                    string words = string.Join(" ", command.Split(' ').Skip(1));
                    string[] elements = words.Split(" ");
                    AddToTable(connection, elements[0], elements[1], elements[2]);
                    break;
                case "3":
                    SelectFromTable(connection);
                    break;
                case "4":
                    AddMultipleToTable(connection);
                    break;
                case "5":
                    SelectChoosenFromTable(connection);
                    break;
                case "6":
                    SelectChoosenFromTableFaster(connection);
                    break;
                default:
                    break;
            }

        }

        connection.Close();
        Console.WriteLine("Connected has been closed.");
    }

    static void DeleteTable(NpgsqlConnection connection)
    {
        using var cmd = new NpgsqlCommand();
        cmd.Connection = connection;
        cmd.CommandText = "DROP TABLE IF EXISTS person;";
        cmd.ExecuteNonQuery();
    }

    static void CreateTable(NpgsqlConnection connection)
    {
        using var cmd = new NpgsqlCommand();
        cmd.Connection = connection;

        cmd.CommandText = "DROP TABLE IF EXISTS person;";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE person(FIO VARCHAR(255), birthday DATE, gender CHAR(1));";
        cmd.ExecuteNonQuery();

    }
    static void AddToTable(NpgsqlConnection connection, string fio, string birthday, string gender)
    {
        using var cmd = new NpgsqlCommand();
        cmd.Connection = connection;
        cmd.CommandText = @"INSERT INTO person(FIO, birthday, gender) VALUES('" + fio 
            + @"', '" + birthday + @"', '" + gender + @"');";
        cmd.ExecuteNonQuery();
    }

    static void SelectFromTable(NpgsqlConnection connection)
    {
        string request = @"SELECT DISTINCT ON (fio, birthday) * FROM person ORDER BY fio;";
        using var cmd = new NpgsqlCommand(request, connection);
        
        using NpgsqlDataReader rdr = cmd.ExecuteReader();
        var currentDate = DateTime.Now.ToString("yyyy");

        while (rdr.Read())
        {
            var personaDate = rdr.GetDateTime(1).Year;
            int years = Convert.ToInt32(currentDate) - Convert.ToInt32(personaDate);
            Console.WriteLine($"{rdr.GetString(0)}, {rdr.GetDateTime(1).Date}, {rdr.GetChar(2)}, {years}");
        }
    }

    static void AddMultipleToTable(NpgsqlConnection connection)
    {
        using var cmd = new NpgsqlCommand();
        cmd.Connection = connection;
        for (int i = 0; i < 1000000; i++)
        {
            var rndFio = GenerateFio();
            var rndDate = GenerateDate();
            var rndGender = GenerateGender();
            cmd.CommandText = @"INSERT INTO person(FIO, birthday, gender) VALUES('" + rndFio
                + @"', '" + rndDate + @"', '" + rndGender + @"');";
            cmd.ExecuteNonQuery();
        }

        for (int i = 0; i < 100000; i++)
        {
            var rndFio = GenerateFio();
            var rndDate = GenerateDate();
            cmd.CommandText = @"INSERT INTO person(FIO, birthday, gender) VALUES('F" + rndFio.Substring(1)
                + @"', '" + rndDate + @"', 'M');";
            cmd.ExecuteNonQuery();
        }
    }

    static void SelectChoosenFromTable(NpgsqlConnection connection)
    {
        string request = @"SELECT * FROM person WHERE (lower(fio) SIMILAR TO '(f)%') AND gender SIMILAR TO '(M)%';";
        using var cmd = new NpgsqlCommand(request, connection);
        
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        using NpgsqlDataReader rdr = cmd.ExecuteReader();
        stopwatch.Stop();
        while (rdr.Read())
        {
            Console.WriteLine($"{rdr.GetString(0)}, {rdr.GetDateTime(1).Date}, {rdr.GetChar(2)}");
        }
        Console.WriteLine($"Request execution time: {stopwatch.ElapsedMilliseconds} milliseconds");
    }

    static void SelectChoosenFromTableFaster(NpgsqlConnection connection)
    {
        string request = @"SELECT t.relname AS person, i.relname AS person_index, a.attname AS fio
                        FROM pg_class t, pg_class i, pg_index ix, pg_attribute a
                        WHERE t.oid = ix.indrelid AND i.oid = ix.indexrelid AND a.attrelid = t.oid
                            AND a.attnum = ANY(ix.indkey) AND t.relkind = 'r' AND t.relname LIKE 'person'
                        ORDER BY t.relname, i.relname;";
        using var cmd = new NpgsqlCommand(request, connection);

        using NpgsqlDataReader rdr = cmd.ExecuteReader();

        if (rdr.GetValues != null)
        {
            using var new_cmd = new NpgsqlCommand();
            new_cmd.Connection = connection;
            new_cmd.CommandText = @"CREATE INDEX person_index ON person(fio, gender);";
            new_cmd.ExecuteNonQuery();

        }
        SelectChoosenFromTable(connection);
    }

    public static string GenerateFio()
    {
        Random r = new Random();
        int len = r.Next(5,11);
        string[] consonants = { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "l", "n", "p", "q", "r", "s", "sh", "zh", "t", "v", "w", "x" };
        string[] vowels = { "a", "e", "i", "o", "u", "ae", "y" };
        string Name = "";
        Name += consonants[r.Next(consonants.Length)].ToUpper();
        Name += vowels[r.Next(vowels.Length)];
        int b = 2;
        while (b < len)
        {
            Name += consonants[r.Next(consonants.Length)];
            b++;
            Name += vowels[r.Next(vowels.Length)];
            b++;
        }

        return Name;
    }

    public static DateTime GenerateDate()
    {
        Random r = new Random();
        DateTime start = new DateTime(1995, 1, 1);
        int range = (DateTime.Today - start).Days;
        return start.AddDays(r.Next(range));
    }

    public static string GenerateGender()
    {
        Random r = new Random();
        var gen = r.Next(0, 2);
        if (gen == 0)
            return "F";
        else
            return "M";
    }
}   
