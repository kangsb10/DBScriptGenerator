namespace DBScripter
{
    class Init : ezBase
    {

        public void Run()
        {
            //1
            DBscripter scripter = new DBscripter();
            scripter.ScriptingDB();

            //2
            //GitAPI gitAPI = new GitAPI();

        }

    }
}
