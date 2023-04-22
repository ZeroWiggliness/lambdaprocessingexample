resource "aws_s3_bucket" "filestorage" {
  bucket = local.bucket_name

  tags = {
    Name        = "File storage"
  }

  force_destroy = true
}

resource "aws_iam_user_policy" "filestorage" {
  name = "filestorage"
  user = aws_iam_user.filestorage.name

  # Terraform's "jsonencode" function converts a
  # Terraform expression result to valid JSON syntax.
  policy = jsonencode({
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": "s3:ListBucket",
            "Resource": "arn:aws:s3:::${aws_s3_bucket.filestorage.bucket}"
        },
        {
            "Effect": "Allow",
            "Action": [
                "s3:GetObject",
                "s3:PutObject",
                "s3:DeleteObject"
            ],
            "Resource": "arn:aws:s3:::${aws_s3_bucket.filestorage.bucket}/*"
        }
    ]
})
}

resource "aws_iam_user" "filestorage" {
  name = "filestorage"
  path = "/system/"
}

resource "aws_iam_access_key" "filestorage" {
  user = aws_iam_user.filestorage.name
}