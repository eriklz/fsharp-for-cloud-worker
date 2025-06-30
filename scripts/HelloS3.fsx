#!/usr/bin/env dotnet fsi

// Get the AWS SDK packages needed
#r "nuget: AWSSDK.Core"
#r "nuget: AWSSDK.S3"

open Amazon.S3
open Amazon.S3.Model

let getBucketInfo (bucket: S3Bucket) =
    $"Name: {bucket.BucketName} created at {bucket.CreationDate}"

let listBuckets (s3Client: AmazonS3Client) =
    task {
        let! response = s3Client.ListBucketsAsync()
        return response
    }

let helloS3 () =
    task {
        let client = new AmazonS3Client()
        let! response = listBuckets client
        let bucketsInfo = List.ofSeq response.Buckets |> List.map getBucketInfo
        for bucketInfo in bucketsInfo do
                printfn "%s" bucketInfo
    } |> Async.AwaitTask |> Async.RunSynchronously
helloS3()
