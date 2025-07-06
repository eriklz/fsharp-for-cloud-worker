#!/usr/bin/env dotnet fsi

// Get the AWS SDK packages needed
#r "nuget: AWSSDK.Core"
#r "nuget: AWSSDK.EC2"

open System
open Amazon.EC2
open Amazon.EC2.Model

type IpAddressOrDns = 
    | IpAddress of string
    | DnsName of string

let toString v  = 
    match v with
    | IpAddress s -> s
    | DnsName s -> s

type InstanceInfo =
    { InstanceId:         string
      Name:               string
      InstanceType:       string
      PrivateHostAddress: IpAddressOrDns
      LaunchTime:         DateTime option
      State:              string }

let rec getTagValue key (tags: Tag list) =
    match tags with
    | [] -> ""
    | head :: tail -> if head.Key = key then head.Value  else getTagValue key tail

let getInstanceInfo (instance: Instance) =
    let tags = List.ofSeq instance.Tags
    {
        InstanceId = instance.InstanceId
        Name = getTagValue "Name" tags
        InstanceType = instance.InstanceType.Value
        PrivateHostAddress = IpAddress instance.PrivateIpAddress
        LaunchTime = if instance.LaunchTime.HasValue then Some instance.LaunchTime.Value else None
        State = instance.State.Name.Value
    }

let yyyymmddhhmmss (dt: DateTime option) =
    match dt with
      | Some x -> x.ToString "yyyy-MM-dd HH:mm:ss"
      | None -> "not launched"

let printInstanceInfo (instanceInfos: InstanceInfo list) : unit =
    if instanceInfos.IsEmpty then
        printfn "No instance info available!"
    else
        printfn "%-20s %-20s %-16s %-16s %-20s %-10s" 
            "InstanceId" "Name" "InstanceType" "Private IP" "LaunchTime" "State"
        for ii in instanceInfos do
            printfn "%-20s %-20s %-16s %-16s %-20s %-10s"
                ii.InstanceId ii.Name ii.InstanceType (toString ii.PrivateHostAddress) (yyyymmddhhmmss ii.LaunchTime) ii.State

let getInstances (reservations: Reservation list) =
    reservations |> List.collect (fun x -> List.ofSeq x.Instances)


let describeInstances (client: AmazonEC2Client) =
    task {
        return! client.DescribeInstancesAsync()
    }


let showServers (args: string[]) =
    if args.Length > 0 then
        do Environment.SetEnvironmentVariable ("AWS_PROFILE", args.[0])
    let client = new AmazonEC2Client()
    task {
        let! response = describeInstances client
        let infoList = 
            if response.Reservations.Count = 0 then
                List.empty<Instance>
            else
                List.ofSeq response.Reservations |> getInstances
        infoList
            |> List.map getInstanceInfo
            |> printInstanceInfo
    } |> Async.AwaitTask |> Async.RunSynchronously


showServers (fsi.CommandLineArgs |> Array.skip 1)

