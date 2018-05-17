# TestRail-Scraper

This tool gathers information from all cases stored in TestRail and stores the data in MongoDB.

**How to set up:**
1. Create a file `bin\Debug\configuration\config.json` (you will need to create the 'configuration' folder and the config.json file)
2. In the config.json file which you created, paste the following json and fill it out with your TestRail login details:

  ```
    {
  "logFile": "log.txt",
  "adminPin": 0,
  "slack:apiToken": "",
  "testrail:user": "YOUR LOGIN EMAIL ADDRESS",
  "testrail:password": "YOUR PASSWORD"
}    
  ```
