[<RequireQualifiedAccess>]
module Absolutejam.Config

open System
open Microsoft.Extensions.Configuration

type ConfigException (path: string, key: string) =
    inherit Exception ()
    override this.Message = $"No config value(s) found at {path}:{key}"

let getSection (config: IConfiguration) key = 
    config.GetRequiredSection key

let tryGetSection (config: IConfigurationSection) key =
    config.GetSection key
    |> Option.ofObj
    |> Option.bind (fun section ->
        if isNull section.Value
           && Seq.length (section.GetChildren ()) < 1 then
            None
        else
            Some section
    )

let hasSection (config: IConfigurationSection) key : bool = Option.isSome (tryGetSection config key)

let getValue<'t> (config: IConfigurationSection) key : 't =
    if String.IsNullOrEmpty config.Value then
        let children = config.GetChildren () |> List.ofSeq

        let found =
            children
            |> List.tryFind (fun x -> x.Key = key)
            |> Option.bind (fun x ->
                if isNull x.Value then
                    None
                else
                    Some (x.Get<'t> ())
            )

        match found with
        | Some x -> x
        | None -> raise (ConfigException (config.Path, key))
    else
        config.GetValue<'t> (key)

let tryGetValue<'t> (config: IConfigurationSection) key : 't option =
    let stringValue = config.GetValue<string> key

    if String.IsNullOrEmpty stringValue then
        None
    else
        Some (config.GetValue<'t> key)

let private getPath (config: IConfiguration) =
    match config with
    | :? IConfigurationSection as section -> section.Path
    | :? IConfigurationRoot -> ""
    | _ -> "Unknown"

let private failConfig (config: IConfiguration) key =
    let path = getPath config 
    raise (ConfigException (path, key))

let getSections (config: IConfiguration) key : IConfigurationSection list =
    config.GetSection(key).GetChildren ()
    |> fun children ->
        if Seq.isEmpty children then
            failConfig config key
        else
            children
    |> List.ofSeq

let tryGetListOf<'t> (config: IConfiguration) transformer key : Option<'t list> =
    let sections = getSections config key

    sections
    |> List.map (fun section ->
        if isNull section.Value then
            failConfig config key
        else
            transformer section.Value
    )
    |> function
        | items ->
            if List.isEmpty items then
                None
            else
                Some items

let getListOf<'t> (config: IConfiguration) transformer key : 't list =
    match tryGetListOf config transformer key with
    | Some x -> x
    | None -> failConfig config key

let tryGetSections (config: #IConfiguration) key =
    let section = config.GetSection (key)

    if isNull section then
        []
    else
        section.GetChildren () |> List.ofSeq
