# CloudSync-
CloudSync Windows Service

Note:- how to run this project in your machine.

Step 1: Create a Google Cloud Service Account
Go to the Google Cloud Console.
Create a new Project (or select an existing one) and search for "Google Drive API". Click Enable.
Go to APIs & Services -> Credentials.
Click Create Credentials -> Service Account.
Give it a name (e.g., cloudsync-worker).
Once created, click on your new Service Account, go to the Keys tab, click Add Key -> Create New Key, and choose JSON.
A .json file will securely download to your computer.

Step 2: Grant the Service Account Access
Your Service Account has its own email address (e.g., cloudsync-worker@yourproject.iam.gserviceaccount.com).

Go to your regular Google Drive in the browser.
Right-click the folder you want the service to sync files to and click Share.
Paste the Service Account's email address and give it Editor access.
Step 3: Add the Credentials to your Vault
Open the .json file you downloaded in Step 1 using Notepad or Visual Studio.
Select everything and copy it.
Open the SetupVault/Program.cs file you have open right now.
Replace the entire dummyJson string block with the actual JSON content you copied. (Make sure you keep the @ symbol before the string to allow multiple lines).
Run the SetupVault project again.
