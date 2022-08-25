module Absolutejam.Config.Tests

open Expecto
open Absolutejam
open System.Collections.Generic
open Microsoft.Extensions.Configuration

module Expect =
    let zippedMatch<'t when 't : equality> (xs: ('t * 't) seq) =
        xs |> Seq.iter (fun (actual, expected) -> "Should match" |> Expect.equal actual expected)

type MockConfigProvider () =
    inherit ConfigurationProvider ()

    member this.Data= this.Data

    interface IConfigurationSource with
        member this.Build builder = 
            this :> IConfigurationProvider

module TestData =
    let names = [
        "Scanlan"
        "Grog"
        "Vax"
        "Vex"
        "Percy"
        "Keyleth"
        "Pyke"
    ]

module ConfigBuilder =
    let build (provider: MockConfigProvider) = ConfigurationBuilder().Add(provider).Build()
    let add key value (provider: #IConfigurationProvider) =
        provider.Set (key, value)
        provider

    let addMany (kvSeq: (string * string) seq) (provider: #IConfigurationProvider) =
        for (key, value) in kvSeq do provider.Set (key, value)
        provider

    let tryGet key (provider: #IConfigurationProvider) =
        match provider.TryGet key with
        | true, value -> Some value
        | _ -> None

let config =
    let sections =
        TestData.names
        |> Seq.indexed
        |> Seq.map (fun (i, name) -> $"Users:Accounts:{i}:Name", name)

    let list =
        TestData.names
        |> Seq.indexed
        |> Seq.map (fun (i, name) -> $"Users:UserNames:{i}", name)

    MockConfigProvider ()
    |> ConfigBuilder.addMany sections
    |> ConfigBuilder.addMany list
    |> ConfigBuilder.build

[<Tests>]
let expectedDataTests = 
    testList "expected data" [
        testCase "getSections" (fun _ ->
            let sections: IConfigurationSection list =
                "Users:Accounts" |> Config.getSections config 

            "Should have same amount of items as input data"
            |> Expect.hasLength sections TestData.names.Length

            sections
            |> Seq.map (fun cfg -> "Name" |> Config.getValue<string> cfg)
            |> Seq.zip TestData.names
            |> Expect.zippedMatch
        )

        testCase "getListOf" (fun _ ->
            let sections: string list =
                "Users:UserNames" |> Config.getListOf config id

            "Should have same amount of items as input data"
            |> Expect.hasLength sections TestData.names.Length

            sections
            |> Seq.zip TestData.names
            |> Expect.zippedMatch
        )
    ]

[<Tests>]
let missingDataTests = 
    testList "missing data" [
        testCase "getValues with missing data" (fun _ ->
            "Should throw a ConfigException"
            |> Expect.throwsT<ConfigException>
                (fun ex -> "Users:DoesNotExist" |> Config.getSections config |> ignore)
        )

        testCase "getListOf with missing data" (fun _ ->
            "Should throw a ConfigException"
            |> Expect.throwsT<ConfigException>
                (fun ex -> "Users:DoesNotExist" |> Config.getListOf config id |> ignore)
        )

        testCase "getSections with incorrect type" (fun _ ->
            let sections = "Users:Usernames" |> Config.getSections config
            sections
            |> Seq.map (fun x -> x.Value)
            |> Seq.zip TestData.names
            |> Expect.zippedMatch

            "Should throw a ConfigException"
            |> Expect.throwsT<ConfigException>
                (fun ex -> "Users:Usernames" |> Config.getSections config |> ignore)
        )

        testCase "getListOf with incorrect type" (fun _ ->
            "Should throw a ConfigException"
            |> Expect.throwsT<ConfigException>
                (fun ex -> "Users:Accounts" |> Config.getListOf config id |> ignore)
        )
    ]

[<EntryPoint>]
let main argv =
    let cliArgs = [
        CLIArguments.JoinWith "/"
        CLIArguments.No_Spinner 
        CLIArguments.Summary
        CLIArguments.List_Tests [ FocusState.Normal ]
    ]

    runTestsInAssemblyWithCLIArgs cliArgs argv