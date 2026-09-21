#!/bin/bash
echo "Initializing LocalStack S3 resources..."
awslocal s3 mb s3://drive-files || true
echo "Bucket 'drive-files' created successfully."
