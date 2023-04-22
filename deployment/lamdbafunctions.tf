data "aws_iam_policy_document" "assume_role" {
  statement {
    effect = "Allow"

    principals {
      type        = "Service"
      identifiers = ["lambda.amazonaws.com"]
    }

    actions = [
        "sts:AssumeRole"
    ]
  }
}

resource "aws_iam_policy" "lambda_logs" {
  name        = "lambdalogs"
  path        = "/"
  description = "My lambda policy"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = [
            "logs:CreateLogGroup",
            "logs:CreateLogStream",
            "logs:PutLogEvents"
        ]
        Effect   = "Allow"
        Resource = "arn:aws:logs:*:*:*"
      },
    ]
  })
}

resource "aws_iam_policy" "lambda_s3" {
  name        = "lambdas3access"
  path        = "/"
  description = "My lambda policy"

  policy = jsonencode({
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "s3:Get*",
            ],
            "Resource": "*"
        }
    ]
})
}

resource "aws_iam_policy" "lambda_sqs" {
  name        = "lambdasqsaccess"
  path        = "/"
  description = "My lambda policy"

  policy = jsonencode({
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "sqs:ReceiveMessage",
                "sqs:DeleteMessage",
                "sqs:SendMessage",
                "sqs:GetQueueAttributes",
            ],
            "Resource": "*"
        }
    ]
})
}

resource "aws_iam_role" "iam_for_lambda" {
  name               = "iam_for_lambda"
  assume_role_policy = data.aws_iam_policy_document.assume_role.json
  managed_policy_arns = [ aws_iam_policy.lambda_logs.arn, aws_iam_policy.lambda_s3.arn, aws_iam_policy.lambda_sqs.arn ]
}

data "archive_file" "filecheck" {
  type        = "zip"
  source_dir = "./FileCheckFunction"
  output_path = "./FileCheckFunction.zip"
}

resource "aws_lambda_function" "filecheck" {
  filename      = data.archive_file.filecheck.output_path 
  function_name = "filecheckfunction"
  role          = aws_iam_role.iam_for_lambda.arn
  handler       = "FileCheckFunction::FileCheckFunction.Function::FunctionHandler"

  runtime = "dotnet6"

  source_code_hash = data.archive_file.filecheck.output_base64sha256

  memory_size                    = "256"
  timeout                        = 120
  publish = true
  package_type = "Zip"
  #reserved_concurrent_executions = 1
  
}

resource "aws_s3_bucket_notification" "filecheck" {
  bucket = aws_s3_bucket.filestorage.id

  lambda_function {
    lambda_function_arn = aws_lambda_function.filecheck.arn
    events              = ["s3:ObjectCreated:Put"]
    filter_suffix       = ".csv"
  }

  depends_on = [aws_lambda_permission.allow_bucket]
}

resource "aws_lambda_permission" "allow_bucket" {
  statement_id  = "AllowExecutionFromS3Bucket"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.filecheck.arn
  principal     = "s3.amazonaws.com"
  source_arn    = aws_s3_bucket.filestorage.arn
}


