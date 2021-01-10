#Get Azure Credentials
az ad sp create-for-rbac --name "gis-github-actions" \
    --sdk-auth --role contributor \
    --scopes /subscriptions/ca0cd4ab-5601-489a-9e4b-53db45be5503