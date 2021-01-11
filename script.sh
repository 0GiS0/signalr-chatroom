#Get Azure Credentials
az ad sp create-for-rbac --name "gis-github-actions" \
    --sdk-auth --role contributor \
    --scopes /subscriptions/ca0cd4ab-5601-489a-9e4b-53db45be5503


#LUIS app

#1. Install Bot Framework CLI
npm i -g @microsoft/botframework-cli

#. Check if the app exists
bf luis:application:list --subscriptionKey f9b3dbe874384d2985db433c32ae3a83 --endpoint https://westeurope.api.cognitive.microsoft.com/ | jq -c '.[] | select(.name | . and contains('\"fitting-skink\"')) | .id'


#2. Create LUIS app
bf luis:application:create --name distinct-cow --subscriptionKey 06951862f73640408fb795626b4650fa --endpoint https://westeurope.api.cognitive.microsoft.com/ --versionId=0.1 --culture es-es

#3. Assign the prediction key
bf luis:application:assignazureaccount --azureSubscriptionId ca0cd4ab-5601-489a-9e4b-53db45be5503 --appId "0a215ad1-3d7f-430e-ba7d-c2ce37a791ab" --accountName="fitting-skink-luis-prediction" --subscriptionKey f9b3dbe874384d2985db433c32ae3a83 --endpoint https://westeurope.api.cognitive.microsoft.com/ --resourceGroup fitting-skink --armToken $(az account get-access-token --query accessToken -o tsv)

#4. Convert the lu file
bf luis:convert -i Chatroom/Offensive-Intents.lu -o ./model.json --name fitting-skink --versionid 0.1 

#5. Import the model.json into the LUIS app
bf luis:version:import --appId 57ee4be6-c6d0-491a-8242-fc35657b58da --subscriptionKey f9b3dbe874384d2985db433c32ae3a83 --endpoint https://westeurope.api.cognitive.microsoft.com/ --in model.json

bf luis:application:import --subscriptionKey f9b3dbe874384d2985db433c32ae3a83 --endpoint https://westeurope.api.cognitive.microsoft.com/  --in model.json --json