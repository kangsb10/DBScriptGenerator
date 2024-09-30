namespace DBScripter
{
    class Program
    {
        //https://dba.stackexchange.com/questions/123225/sql-server-programatically-trigger-export-of-db-schema

        static void Main(string[] args)
        {
            Init init = new Init();
            init.Run();
        }
    }
}
