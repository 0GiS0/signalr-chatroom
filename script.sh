#Create credentials (AZURE_CREDENTIALS) for GitHub Actions
az ad sp create-for-rbac --name "signalr-chatroom" --role contributor --sdk-auth