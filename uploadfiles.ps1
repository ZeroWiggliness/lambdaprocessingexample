
$date = Get-Date -Format "yyyyMMdd"
aws s3api put-object --bucket filestorage-test-dv123 --key "clients_$date.csv" --body .\examplefiles\clients.csv
aws s3api put-object --bucket filestorage-test-dv123 --key "accounts_$date.csv" --body .\examplefiles\accounts.csv
aws s3api put-object --bucket filestorage-test-dv123 --key "portfolios_$date.csv" --body .\examplefiles\portfolios.csv
aws s3api put-object --bucket filestorage-test-dv123 --key "transactions_$date.csv" --body .\examplefiles\transactions.csv