using GitLabApiClient;

namespace DBScripter
{
    class GitAPI : ezBase
    {

        public GitAPI()
        {

            var client = new GitLabClient(GetSystemConfigValue("gitlabProjectURL"), GetSystemConfigValue("gitlabToken"));

            


        }

    }
}
