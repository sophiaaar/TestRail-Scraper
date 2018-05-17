using System;
using System.Collections.Generic;
using Gurock.TestRail;
using Newtonsoft.Json.Linq;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB;
using System.Threading.Tasks;

namespace TestRailScraper
{
	class MainClass
	{
		private static readonly IConfigReader _configReader = new ConfigReader();

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
			public string projectName;
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

			JArray projectsArray = GetProjects(client);
			List<Project> projects = CreateListOfProjects(projectsArray);
			List<Suite> suites = CreateListOfSuites(client, projects);
			//List<Case> cases = CreateListOfCases(client, projects, suites);

			MongoClient mongoClient = ConnectToMongo();

			List<Case> cases = CreateListOfCases(client, projects, suites, mongoClient);

			//AddToDatabase(mongoClient, cases);

		}

		private static APIClient ConnectToTestrail()
		{
			APIClient client = new APIClient("TESTRAIL SERVER");
			client.User = _configReader.TestRailUser;
			client.Password = _configReader.TestRailPass;
			return client;
		}
        
		private static MongoClient ConnectToMongo()
		{
			string mongoConnectionString = "mongodb://{username}:{password}@{server}:{port}/{database}"; //redacted - insert database url here
			MongoClient mongoClient = new MongoClient(mongoConnectionString);
			return mongoClient;
		}

		private static void AddToDatabase(MongoClient mongoClient, List<Case> cases)
		{
			var mongoData = mongoClient.GetDatabase("DB");
            var mongoCollection = mongoData.GetCollection<BsonDocument>("COLLECTION");

			for (int i = 0; i < cases.Count; i++)
			{
				var document = new BsonDocument()
				{
					{"case_id", cases[i].id},
					{"case_title", cases[i].title},
					{"suite_id", cases[i].suiteId},
					{"suite_name", cases[i].suiteName},
					{"section_id", cases[i].sectionName},
					{"section_name", cases[i].sectionName},
					{"parent_section_id", cases[i].parentSectionId},
					{"project_id", cases[i].projectId},
					{"project_name", cases[i].projectName}
				};

				mongoCollection.InsertOneAsync(document);
			}
		}

		public static async Task AddToDatabase(MongoClient mongoClient, Case currentCase)
        {
            var mongoData = mongoClient.GetDatabase("DB");
            var mongoCollection = mongoData.GetCollection<BsonDocument>("COLLECTION");

			var document = new BsonDocument()
                {
                    {"case_id", currentCase.id},
                    {"case_title", currentCase.title},
                    {"suite_id", currentCase.suiteId},
                    {"suite_name", currentCase.suiteName},
                    {"section_id", currentCase.sectionId},
                    {"section_name", currentCase.sectionName},
                    {"parent_section_id", currentCase.parentSectionId},
                    {"project_id", currentCase.projectId},
                    {"project_name", currentCase.projectName}
                };

            await mongoCollection.InsertOneAsync(document);
        }

		public static JArray GetProjects(APIClient client)
        {
			return (JArray)client.SendGet("get_projects" + "&is_completed=0"); //returns only active projects
        }

		public static JArray GetSuitesInProject(APIClient client, string projectID)
        {
			return (JArray)client.SendGet("get_suites/" + projectID);
        }
        
		public static JArray GetCasesInProject(APIClient client, string projectID, string suiteID)
        {
			return (JArray)client.SendGet("get_cases/" + projectID + "&suite_id=" + suiteID);
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

				if (projectId != "1") //skip the TestProject
				{
					projects.Add(currentProject);
				}
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
					currentSuite.projectName = projects[i].name;

					suites.Add(currentSuite);
				}
			}

			return suites;
		}

		public static List<Case> CreateListOfCases(APIClient client, List<Project> projects, List<Suite> suites, MongoClient mongoClient)
		{
			List<Case> cases = new List<Case>();

			for (int k = 0; k < suites.Count; k++)
            {
				Suite currentSuite = suites[k];

				JArray casesArray = GetCasesInProject(client, currentSuite.projectId, currentSuite.id);
                for (int j = 0; j < casesArray.Count; j++)
                {
                    JObject caseObject = casesArray[j].ToObject<JObject>();

                    string caseId = caseObject.Property("id").Value.ToString();
                    string caseTitle = caseObject.Property("title").Value.ToString();
                    string suiteId = caseObject.Property("suite_id").Value.ToString();
                    string sectionId = caseObject.Property("section_id").Value.ToString();

                    //Suite currentSuite = suites.Find(x => x.id == suiteId);
                    string suiteName = currentSuite.name;

					string projectId = currentSuite.projectId;
                    
					string projectName = currentSuite.projectName;

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


                    Task add = AddToDatabase(mongoClient, currentCase);
                    try
                    {
                        add.Wait();
                    }
                    catch (AggregateException aggEx)
                    {
                        aggEx.Handle(x =>
                        {
                            var mwx = x as MongoWriteException;
                            if (mwx != null && mwx.WriteError.Category == ServerErrorCategory.DuplicateKey)
                            {
                                // mwx.WriteError.Message contains the duplicate key error message
                                return true;
                            }
                            return false;
                        });
                    }

                    cases.Add(currentCase);

                }
            }

			return cases;
		}
    }
}
