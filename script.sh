#Get Azure Credentials
az ad sp create-for-rbac --name "gis-github-actions" \
    --sdk-auth --role contributor \
    --scopes /subscriptions/ca0cd4ab-5601-489a-9e4b-53db45be5503


#LUIS app

#1. Install Bot Framework CLI
npm i -g @microsoft/botframework-cli

#2. Create LUIS app
bf luis:application:create --name distinct-cow --subscriptionKey 06951862f73640408fb795626b4650fa --endpoint https://westeurope.api.cognitive.microsoft.com/ --versionId=0.1 --culture es-es

#3. Assign the prediction key
bf luis:application:assignazureaccount --help
token=$(az account get-access-token --query accessToken -o tsv)

bf luis:application:assignazureaccount --accountName=distinct-cow-luis --appId=e7251917-b57c-4475-9072-1ae953a892e8 \
--subscriptionKey=06951862f73640408fb795626b4650fa \
--azureSubscriptionId=ca0cd4ab-5601-489a-9e4b-53db45be5503  --endpoint=https://westeurope.api.cognitive.microsoft.com/ --resourceGroup=distinct-cow \
--armToken=$token