using System;
using System.Collections.Generic;
using Gurock.TestRail;
using Newtonsoft.Json.Linq;

namespace TestRailScraper
{
    class MainClass
    {
		private static readonly IConfigReader _configReader = new ConfigReader();
		//public static List<Project> projects = new List<Project>();

        public struct Project
		{
			public string id;
			public string name;
		}

        public struct Suite
		{
			public string id;
			public string name;
			public string projectId;
		}

        public struct Case
		{
			public string id;
			public string title;
			public string suiteId;
			public string suiteName;
			public string sectionId;
			public string sectionName;
			public string parentSectionId;
			public string projectId;
			public string projectName;
		}

        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
			APIClient client = ConnectToTestrail();

        }

		private static APIClient ConnectToTestrail()
        {
            APIClient client = new APIClient("https://qatestrail.hq.unity3d.com");
            client.User = _configReader.TestRailUser;
            client.Password = _configReader.TestRailPass;
            return client;
        }

		public static JArray GetProjects(APIClient client)
        {
            return (JArray)client.SendGet("get_projects");
        }

		public static JArray GetSuitesInProject(APIClient client, string projectID)
        {
            return (JArray)client.SendGet("get_suites/" + projectID);
        }
        
		public static JArray GetCasesInProject(APIClient client, string projectID)
        {
            return (JArray)client.SendGet("get_cases/" + projectID);
        }

		public static JObject GetSection(APIClient client, string sectionID)
        {
			return (JObject)client.SendGet("get_section/" + sectionID);
        }

		public static List<Project> CreateListOfProjects(JArray projectsArray)
		{
			List<Project> projects = new List<Project>();

			for (int i = 0; i < projectsArray.Count; i++)
			{
				JObject projectObject = projectsArray[i].ToObject<JObject>();
				string projectId = projectObject.Property("id").Value.ToString();
				string projectName = projectObject.Property("name").Value.ToString();

				Project currentProject;
				currentProject.id = projectId;
				currentProject.name = projectName;

				projects.Add(currentProject);
			}

			return projects;
		}

		public static List<Suite> CreateListOfSuites(APIClient client, List<Project> projects)
		{
			List<Suite> suites = new List<Suite>();

			for (int i = 0; i < projects.Count; i++)
			{
				JArray suitesArray = GetSuitesInProject(client, projects[i].id);
				for (int j = 0; j < suitesArray.Count; j++)
				{
					JObject suiteObject = suitesArray[j].ToObject<JObject>();
					string suiteId = suiteObject.Property("id").Value.ToString();
					string suiteName = suiteObject.Property("name").Value.ToString();
					string projectId = suiteObject.Property("project_id").Value.ToString();
                    
					Suite currentSuite;
					currentSuite.id = suiteId;
					currentSuite.name = suiteName;
					currentSuite.projectId = projectId;

					suites.Add(currentSuite);
				}
			}

			return suites;
		}

		public static List<Case> CreateListOfCases(APIClient client, List<Project> projects, List<Suite> suites)
		{
			List<Case> cases = new List<Case>();

			for (int i = 0; i < projects.Count; i++)
			{
				JArray casesArray = GetCasesInProject(client, projects[i].id);
				for (int j = 0; j < casesArray.Count; j++)
				{
					JObject caseObject = casesArray[j].ToObject<JObject>();

					string caseId = caseObject.Property("id").Value.ToString();
					string caseTitle = caseObject.Property("title").Value.ToString();
					string suiteId = caseObject.Property("suite_id").Value.ToString();
					string sectionId = caseObject.Property("section_id").Value.ToString();

					Suite currentSuite = suites.Find(x => x.id == suiteId);
					string suiteName = currentSuite.name;

					string projectId = projects[i].id;
					string projectName = projects[i].name;

					JObject currentSection = GetSection(client, sectionId);
					string sectionName = currentSection.Property("name").Value.ToString();

					string parentSectionId = "0";

					if (currentSection.Property("parent_id") != null)
					{
						parentSectionId = currentSection.Property("parent_id").Value.ToString();
					}

					Case currentCase;
					currentCase.id = caseId;
					currentCase.title = caseTitle;
					currentCase.suiteId = suiteId;
					currentCase.sectionId = sectionId;
					currentCase.suiteName = suiteName;
					currentCase.projectId = projectId;
					currentCase.projectName = projectName;
					currentCase.sectionName = sectionName;
					currentCase.parentSectionId = parentSectionId;

					cases.Add(currentCase);
                  
				}
			}

			return cases;
		}
    }
}
